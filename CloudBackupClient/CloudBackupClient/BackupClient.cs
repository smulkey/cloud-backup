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

namespace CloudBackupClient
{
    public class BackupClient
    {
                
        private string backupCacheDir;
        private int maxCacheMB;
        private int totalRunTimeSeconds;
        private DateTime stopRunTime;        
        private IServiceProvider serviceProvider;

        static void Main(string[] args)
        {
            try
            {                                                                                
                (new BackupClient()).Start();
                
            } catch(Exception ex)
            {                
                Console.WriteLine("Program fail with exception: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void Start()
        {
            this.Logger.LogInformation("Initializing BackupClient services...");

            var appConfig = new ConfigurationBuilder()
                                    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

            this.serviceProvider = new ServiceCollection()
                                            .AddSingleton<ICloudBackupArchiveProvider, FileSystemBackupArchiveProvider>()
                                            .AddSingleton<IClientDBHandler, InMemoryDBHandler>()
                                            .AddSingleton<IConfigurationRoot>(provider => appConfig)
                                            .AddSingleton<IClientFileCacheHandler, LocalClientFileCacheHandler>()
                                            .AddLogging(builder => builder.AddConsole())
                                            .BuildServiceProvider();

            this.Logger.LogInformation("Starting backup run at {0}", DateTime.Now.ToString());

            var appConfigRoot = this.serviceProvider.GetService<IConfigurationRoot>();
            var config = appConfigRoot.GetSection("BackupSettings");

            if (config == null)
            {
                throw new Exception("No config section found for backup settings");
            }

            this.stopRunTime = DateTime.Now.AddSeconds(this.TotalRunTimeSeconds);
            
            try
            {
                BackupRun br = null;

                try
                {                     
                    br = GetOpenBackupRun();

                    if (br == null)
                    {
                        this.Logger.LogInformation("Creating new backup run");
                        br = CreateBackupRun();
                    }

                    this.Logger.LogInformation(String.Format("Processing open backup run with ID: {0}", br.BackupRunID));

                    ArchiveCurrentBackupRun(br);
                }
                catch (Exception ex)
                {
                    if (br != null)
                    {
                        br.BackupRunCompleted = true;
                        br.BackupRunEnd = DateTime.Now;
                        br.FailedWithException = true;
                        br.ExceptionMessage = String.Format("{0}:{1}", ex.Message, ex.StackTrace);

                    try
                    {
                        this.ClientDBHandler.UpdateBackupRun(br);
                    }
                    catch (Exception dbEx)
                    {
                        this.Logger.LogError(dbEx, "Couldn't save backup run exception: {0}", dbEx.Message);
                    }

                }

                    this.Logger.LogError(ex, "Failure in processing backup run: {0}", ex.Message);
                }               
            }
            catch(Exception ex)
            {
                this.Logger.LogError(ex, "Failure in starting DB: {0}", ex.Message);
            }
            finally
            {
                this.ClientDBHandler.Dispose();
            }
            
        }

        private void ArchiveCurrentBackupRun(BackupRun br)
        {
            bool haltTimeNotExceeded;

            do
            {
                InitializeBackupRun(br);

                foreach (var item in br.BackupFileRefs)
                {
                    if (item.CopiedToCache == true && item.CopiedToArchive == false && br.FailedWithException == false)
                    {
                        try
                        {
                            var cacheFileStream = this.ClientFileCacheHandler.GetCacheStreamForItem(item, br);
                            
                            bool success = this.serviceProvider.GetService<ICloudBackupArchiveProvider>().ArchiveFile(br, item, cacheFileStream);

                            cacheFileStream.Close();

                            if (success == false)
                            {
                                throw new Exception(String.Format("Filed to archive file ref: {0}", item.FullFileName));
                            }
                            else
                            {
                                Logger.LogInformation("Arcive copy successful for source file: {0}", item.FullFileName);

                                item.CopiedToArchive = true;

                                this.ClientDBHandler.UpdateBackupFileRef(item);                                                               
                            }
                        }
                        catch (Exception ex)
                        {
                            br.FailedWithException = true;
                            br.BackupRunEnd = DateTime.Now;
                            br.ExceptionMessage = ex.Message;

                            //  dbContext.Update<BackupRun>(br);

                            this.ClientDBHandler.UpdateBackupRun(br);

                            throw new Exception(String.Format("Backup run with ID {0} failed with exception: {1}", br.BackupRunID, ex.Message));
                        }
                        finally
                        {

                            this.ClientDBHandler.UpdateBackupRun(br);                         
                        }
                    }
                }

                haltTimeNotExceeded = (DateTime.Now.CompareTo(this.stopRunTime) < 0);

                Logger.LogInformation("Completed cache copy run with BackupRun.Completed = {0} and halt time {1} exceeded", 
                                        br.BackupRunCompleted, 
                                        (haltTimeNotExceeded ? "was not" : "was"));
                
            } while (br.BackupRunCompleted == false && haltTimeNotExceeded == true);


            foreach (var backupRef in br.BackupFileRefs)
            {
                this.ClientFileCacheHandler.CompleteFileArchive(backupRef, br);
            }           
        }
                
        private BackupRun InitializeBackupRun(BackupRun br)
        {
            Logger.LogDebug("BackupClient.InitializeNewBackupRun called");

            this.ClientFileCacheHandler.PopulateFilesForBackupRun(br);
            
            long currentBytes;
            string cacheRef;
            int fileCount;

            br.BackupRunStart = DateTime.Now;

            currentBytes = 0;

            //File count tracked separately in case the last run is all directories
            fileCount = 0;

            this.Logger.LogInformation("Copying scanned files to backup cache");
            

            foreach (var fileRef in br.BackupFileRefs)
            {
                if (currentBytes > this.MaxCacheMB * 1000000)
                {
                    Logger.LogInformation("Halting cache file copy due to max copy bytes exceded");
                    break;
                }

                if (fileRef.CopiedToCache == true)
                {
                    Logger.LogDebug(String.Format("Skipping file already copied to backup cache: {0}", fileRef.FullFileName));
                    continue;
                }

                cacheRef = GetCacheEntryForFile(fileRef.FullFileName, br);

                //Verify file wasn't removed from source or already copied to the cache
                if ((Directory.Exists(fileRef.FullFileName) || File.Exists(fileRef.FullFileName)) && File.Exists(cacheRef) == false)
                {
                    if (Directory.Exists(fileRef.FullFileName))
                    {
                        Logger.LogInformation(String.Format("Creating backup cache directory: {0}", cacheRef));

                        Directory.CreateDirectory(cacheRef);
                    }
                    else
                    {
                        if(File.Exists(cacheRef))
                        {
                            Logger.LogInformation(String.Format("Deleting previous version of cache file: {0}", cacheRef));

                            File.Delete(cacheRef);
                        }

                        Logger.LogInformation(String.Format("Copying cache file from source: {0} to target: {1}", fileRef.FullFileName, cacheRef));

                        File.Copy(fileRef.FullFileName, cacheRef);
                        var cacheFile = new FileInfo(cacheRef);
                        currentBytes += cacheFile.Length;

                        Logger.LogDebug(String.Format("Cache copy current bytes count: {0}", currentBytes));
                    }

                    fileRef.CopiedToCache = true;

                    fileCount += 1;

                    Logger.LogDebug("Saving file ref changes");

                    this.ClientDBHandler.UpdateBackupFileRef(fileRef);
                }
            }

            if (fileCount == 0)
            {
                Logger.LogInformation("Backup file count == 0, setting complete flag");

                br.BackupRunCompleted = true;
                br.BackupRunEnd = DateTime.Now;

                this.ClientDBHandler.UpdateBackupRun(br);
            }
                            
            return br;
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

            var br = new BackupRun
            {
                BackupFileRefs = new List<BackupRunFileRef>(),
                BackupDirectories = new List<BackupDirectoryRef>()
            };


            string[] dirList = this.GetBackupSettingsConfigValue("BackupDirectories").Split(',');

            for (int i = 0; i < dirList.Length; i++)
            {
                br.BackupDirectories.Add(new BackupDirectoryRef { DirectoryFullFileName = dirList[i] });
            }

            this.ClientDBHandler.AddBackupRun(br);
          
            Logger.LogInformation(String.Format("Created new backup run with ID: {0}", br.BackupRunID));

            return br;
        }

        private BackupRun GetOpenBackupRun()
        {
            this.Logger.LogDebug("GetOpenBackupRun called");

            var openBackupRuns = this.ClientDBHandler.GetOpenBackupRuns();

            if(openBackupRuns.Count > 1)
            {
                throw new Exception("More than one open backup");
            }            
            else
            {                
                return openBackupRuns.Count == 0 ? null : openBackupRuns.First<BackupRun>();
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
        

        private int MaxCacheMB => (this.maxCacheMB == 0)
                                    ? this.maxCacheMB = Int32.Parse(this.GetBackupSettingsConfigValue("MaxCacheMB"))
                                    : this.maxCacheMB;                    

        private int TotalRunTimeSeconds => (this.totalRunTimeSeconds == 0)
                                            ? this.totalRunTimeSeconds = Int32.Parse(this.GetBackupSettingsConfigValue("TotalRunTimeSeconds"))
                                            : this.totalRunTimeSeconds;        
                
        private IClientDBHandler ClientDBHandler => this.serviceProvider.GetService<IClientDBHandler>();

        private IClientFileCacheHandler ClientFileCacheHandler => this.serviceProvider.GetService<IClientFileCacheHandler>();

        private ILogger Logger => this.serviceProvider.GetService<ILogger<BackupClient>>();          
        
    }
}
