
using System;
using System.Collections.Generic;
using CloudBackupClient.Data;
using CloudBackupClient.Models;
using CloudBackupClient.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using System.IO;

namespace CloudBackupClient
{
    public class BackupClient
    {   
     
        private int totalRunTimeSeconds;
        private DateTime stopRunTime;        
        private IServiceProvider serviceProvider;

        static void Main(string[] args)
        {
            try
            {
                var appConfig = new ConfigurationBuilder()
                                        .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

                var serviceProvider = new ServiceCollection()
                                                .AddSingleton<ICloudBackupArchiveProvider, FileSystemBackupArchiveProvider>()
                                                .AddSingleton<IClientDBHandler, SqliteDBHandler>()
                                                .AddSingleton<IConfigurationRoot>(provider => appConfig)
                                                .AddSingleton<IClientFileCacheHandler, LocalClientFileCacheHandler>()
                                                .AddLogging(builder => builder.AddConsole())
                                                .BuildServiceProvider();

                //TODO Get from properties file
                var dbProperties = new Dictionary<string, string>();
                dbProperties[nameof(SqliteDBHandler.ConnectionString)] = serviceProvider.GetService<IConfigurationRoot>().GetSection("LocalDBTestConfig").GetSection("BackupRunConnStr").Value;
                                
                ((SqliteDBHandler)serviceProvider.GetService<IClientDBHandler>()).Initialize(dbProperties);

                (new BackupClient()).Start(serviceProvider);
                
            } catch(Exception ex)
            {                
                Console.WriteLine("Program fail with exception: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void Start(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.Logger.LogInformation("Starting backup run at {0}", DateTime.Now.ToString());

            var appConfigRoot = this.serviceProvider.GetService<IConfigurationRoot>();
            var config = appConfigRoot.GetSection("BackupSettings");

            if (config == null)
            {
                throw new Exception("No config section found for backup settings");
            }

            this.stopRunTime = DateTime.Now.AddSeconds(this.TotalRunTimeSeconds);
            
            BackupRun backupRun = null;
                                
            try
            {
                var openBackupRuns = this.ClientDBHandler.GetOpenBackupRuns();

                if (openBackupRuns.Count > 1)
                {
                    throw new Exception("More than one open backup");
                }
                else if(openBackupRuns.Count == 1)
                {                    
                    backupRun = openBackupRuns.First<BackupRun>();
                    this.Logger.LogInformation($"Restarting backup run with ID {backupRun.BackupRunID}");
                }
                else
                {                    
                    this.Logger.LogInformation("Creating new backup run");
                    backupRun = CreateBackupRun();
                }
                
                this.Logger.LogInformation(String.Format("Processing open backup run with ID: {0}", backupRun.BackupRunID));

                ArchiveCurrentBackupRun(backupRun);
            }
            catch (Exception ex)
            {
                if (backupRun == null)
                {
                    this.Logger.LogError($"Couldn't complete processing due to error - {ex.Message} {ex.StackTrace}");
                }
                else
                { 
                    this.Logger.LogError($"Error in processing backup run with ID: {backupRun.BackupRunID} - error message: {ex.Message}  {ex.StackTrace}");

                    backupRun.BackupRunCompleted = true;
                    backupRun.BackupRunEnd = DateTime.Now;
                    backupRun.FailedWithException = true;
                    backupRun.ExceptionMessage = String.Format("{0}:{1}", ex.Message, ex.StackTrace);

                    try
                    {
                        this.ClientDBHandler.UpdateBackupRun(backupRun);
                    }
                    catch (Exception dbEx)
                    {
                        this.Logger.LogError(dbEx, "Couldn't save backup run exception: {0}", dbEx.Message);
                    }
                }
            }
            finally
            {
                this.ClientDBHandler.Dispose();
            }
        }

        //int haltCount = 0;
        private void ArchiveCurrentBackupRun(BackupRun backupRun)
        {
            bool haltTimeNotExceeded = false;
            int filesArchived;

            do
            {
                filesArchived = 0; 

                this.PopulateFilesForBackupRun(backupRun);

                this.ClientFileCacheHandler.InitializeBackupRun(backupRun);
                               
                this.ClientDBHandler.UpdateBackupRun(backupRun);


                foreach (var backupFileRef in backupRun.BackupFileRefs)
                {
                    //TODO Find way to store empty directories

                    if (Directory.Exists(backupFileRef.FullFileName) == false &&
                        backupFileRef.CopiedToCache == true && 
                        backupFileRef.CopiedToArchive == false && 
                        backupRun.FailedWithException == false && 
                        haltTimeNotExceeded == true)
                    {
                        bool success;

                        using (var cacheFileStream = this.ClientFileCacheHandler.GetCacheStreamForItem(backupFileRef, backupRun))
                        {
                            success = this.serviceProvider.GetService<ICloudBackupArchiveProvider>().ArchiveFile(backupRun, backupFileRef, cacheFileStream);                            
                        }

                        if (success == false)
                        {
                            throw new Exception(String.Format("Filed to archive file ref: {0}", backupFileRef.FullFileName));
                        }
                        else
                        {
                            Logger.LogInformation("Archive copy successful for source file: {0}", backupFileRef.FullFileName);

                            filesArchived += 1;

                            backupFileRef.CopiedToArchive = true;

                            this.ClientDBHandler.UpdateBackupFileRef(backupFileRef);

                            this.ClientFileCacheHandler.CompleteFileArchive(backupFileRef, backupRun);
                        }                       
                    }

                    haltTimeNotExceeded = (DateTime.Now.CompareTo(this.stopRunTime) < 0);

                    //if (haltCount++ > 2) haltTimeNotExceeded = false;

                    Logger.LogInformation("Completed cache copy run on backup ref ID = {0} and halt time {1} exceeded",
                                            backupFileRef.BackupRunFileRefID,
                                            (haltTimeNotExceeded ? "was not" : "was"));
                }

            } while (backupRun.BackupRunCompleted == false && haltTimeNotExceeded == true && filesArchived > 0);

            if( haltTimeNotExceeded == false )
            {
                Logger.LogInformation($"Exiting backup run ID {backupRun.BackupRunID} due to halt time exceeded");    
            } 
            else if(filesArchived == 0)
            {
                Logger.LogInformation($"Setting complete flag for backup run ID: {backupRun.BackupRunID}");

                backupRun.BackupRunCompleted = true;
                this.ClientDBHandler.UpdateBackupRun(backupRun);
            }
        }
        
        private BackupRun CreateBackupRun()
        {
            Logger.LogDebug("CreateBackupRun called");
            var appConfigRoot = this.serviceProvider.GetService<IConfigurationRoot>();
            var config = appConfigRoot.GetSection("BackupSettings");
            
            if (config == null)
            {
                throw new Exception("No config section found for backup settings");
            }

            config = config.GetSection("BackupDirectories");

            if (config == null)
            {
                throw new Exception("No config section found for backup directories");
            }

            var backupRun = new BackupRun
                                {
                                    BackupFileRefs = new List<BackupRunFileRef>(),
                                    BackupDirectories = new List<BackupDirectoryRef>()
                                };


            string[] dirList = this.GetBackupSettingsConfigValue("BackupDirectories").Split(',');

            for (int i = 0; i < dirList.Length; i++)
            {
                backupRun.BackupDirectories.Add(new BackupDirectoryRef { DirectoryFullFileName = dirList[i] });
            }

            this.ClientDBHandler.AddBackupRun(backupRun);
          
            foreach(var backupDirectoryRef in backupRun.BackupDirectories)
            {
                backupDirectoryRef.BackupRunID = backupRun.BackupRunID;

                this.ClientDBHandler.UpdateBackupDirectoryRef(backupDirectoryRef);
            }            

            Logger.LogInformation(String.Format("Created new backup run with ID: {0}", backupRun.BackupRunID));

            return backupRun;
        }

        private void PopulateFilesForBackupRun(BackupRun backupRun)
        {
            List<FileInfo> directoryFiles = new List<FileInfo>();

            Logger.LogInformation("Starting directory scan for backup run");

            foreach (var dirRef in backupRun.BackupDirectories)
            {
                var rootDir = dirRef.DirectoryFullFileName;

                if (String.IsNullOrWhiteSpace(rootDir))
                {
                    throw new Exception("No root directory provided");
                }
                else if (!Directory.Exists(rootDir))
                {
                    throw new Exception(String.Format("Root directory {0} doesn't exist", rootDir));
                }

                PopulateFileList(rootDir, directoryFiles);
            }

            foreach (FileInfo info in directoryFiles)
            {
                //Make sure file wasn't added in an ealier scan for this run
                if (backupRun.BackupFileRefs.Where<BackupRunFileRef>(r => r.FullFileName.ToLower().Equals(info.FullName.ToLower())).ToList().Count == 0)
                {
                    Logger.LogInformation(String.Format("Adding file ref {0} to backup run ID {1}", info.FullName, backupRun.BackupRunID));
                    backupRun.BackupFileRefs.Add(new BackupRunFileRef { FullFileName = info.FullName });
                }

            }
        }

        private void PopulateFileList(string fileName, List<FileInfo> fileList)
        {
            if (File.Exists(fileName) || Directory.Exists(fileName))
            {
                fileList.Add(new FileInfo(fileName));

                if (Directory.Exists(fileName))
                {
                    foreach (string dirFile in Directory.EnumerateFiles(fileName))
                    {
                        fileList.Add(new FileInfo(dirFile));
                    }

                    foreach (string dirFile in Directory.EnumerateDirectories(fileName))
                    {
                        PopulateFileList(dirFile, fileList);
                    }
                }
            }
        }
        
        private string GetBackupSettingsConfigValue(string key)
        {
            var appConfigRoot = this.serviceProvider.GetService<IConfigurationRoot>();
            var config = appConfigRoot.GetSection("BackupSettings");
            
            if (config == null)
            {
                throw new Exception("No config section found for backup settings");
            }

            config = config.GetSection(key);

            if (config == null)
            {
                throw new Exception(String.Format("No config section found for {0}", key));
            }

            return config.Value;
        }
   
        private int TotalRunTimeSeconds => (this.totalRunTimeSeconds == 0)
                                            ? this.totalRunTimeSeconds = Int32.Parse(this.GetBackupSettingsConfigValue("TotalRunTimeSeconds"))
                                            : this.totalRunTimeSeconds;        
                
        private IClientDBHandler ClientDBHandler => this.serviceProvider.GetService<IClientDBHandler>();

        private IClientFileCacheHandler ClientFileCacheHandler => this.serviceProvider.GetService<IClientFileCacheHandler>();

        private ILogger Logger => this.serviceProvider.GetService<ILogger<BackupClient>>();
        
    }
}
