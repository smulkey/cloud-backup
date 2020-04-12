using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CloudBackupClient.Data;
using CloudBackupClient.Models;
using CloudBackupClient.Providers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace CloudBackupClient
{
    class BackupClient
    {        

        private IConfigurationRoot appConfig;
        private string backupCacheDir;
        private int maxCacheMB;
        private int totalRunTimeSeconds;
        private DateTime stopRunTime;
        private CloudBackupArchiveProvider archiveProvider;

        ILogger logger;


        static void Main(string[] args)
        {
            try
            {
                BackupClient client = new BackupClient();
                client.Start();
            } catch(Exception ex)
            {                
                Console.WriteLine("Program fail with exception: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void Start()
        {
            this.Logger.LogInformation("Starting backup run at {0}", DateTime.Now.ToString());

            var config = this.AppConfig.GetSection("BackupSettings");

            if (config == null)
            {
                throw new Exception("No config section found for backup settings");
            }

            this.stopRunTime = DateTime.Now.AddSeconds(this.TotalRunTimeSeconds);
                        
            var connection = new SqliteConnection(this.AppConfig.GetConnectionString("BackupRunConnStr"));

            try
            {
                connection.Open();

                var options = new DbContextOptionsBuilder<CloudBackupDbContext>()
                    .UseSqlite(connection)
                    .Options;

                using (var dbContext = new CloudBackupDbContext(options))
                {
                    BackupRun br = null;

                    try
                    {
                        dbContext.Database.EnsureCreated();

                        br = GetOpenBackupRun(dbContext);

                        if (br == null)
                        {
                            this.Logger.LogInformation("Creating new backup run");
                            br = CreateBackupRun(dbContext);
                        }

                        this.Logger.LogInformation(String.Format("Processing open backup run with ID: {0}", br.BackupRunID));

                        ArchiveCurrentBackupRun(dbContext, br);
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
                                dbContext.SaveChanges();
                            }
                            catch (Exception dbEx)
                            {
                                this.Logger.LogError(dbEx, "Couldn't save backup run exception: {0}", dbEx.Message);
                            }

                        }

                        this.Logger.LogError(ex, "Failure in processing backup run: {0}", ex.Message);
                    }
                }
            }
            catch(Exception ex)
            {
                this.Logger.LogError(ex, "Failure in starting DB: {0}", ex.Message);
            }
            finally
            {
                connection.Close();
            }
            
        }

        private void ArchiveCurrentBackupRun(CloudBackupDbContext dbContext, BackupRun br)
        {
            bool haltTimeNotExceeded;

            do
            {
                InitializeBackupRun(dbContext, br);

                foreach (var item in br.BackupFileRefs)
                {
                    if (item.CopiedToCache == true && item.CopiedToArchive == false && br.FailedWithException == false)
                    {
                        try
                        {
                            FileInfo cacheFile = new FileInfo(GetCacheEntryForFile(item.FullFileName, br));
                            bool success = this.ArchiveProvider.ArchiveFile(br, item, cacheFile);

                            if (success == false)
                            {
                                throw new Exception(String.Format("Filed to archive file ref: {0}", item.FullFileName));
                            }
                            else
                            {
                                Logger.LogInformation("Arcive copy successful for source file: {0}", item.FullFileName);

                                item.CopiedToArchive = true;

                                dbContext.Update<BackupRunFileRef>(item);                                
                            }
                        }
                        catch (Exception ex)
                        {
                            br.FailedWithException = true;
                            br.BackupRunEnd = DateTime.Now;
                            br.ExceptionMessage = ex.Message;

                            dbContext.Update<BackupRun>(br);

                            throw new Exception(String.Format("Backup run with ID {0} failed with exception: {1}", br.BackupRunID, ex.Message));
                        }
                        finally
                        {
                            dbContext.SaveChanges();
                        }
                    }
                }

                haltTimeNotExceeded = (DateTime.Now.CompareTo(this.StopRunTime) < 0);

                Logger.LogInformation("Completed cache copy run with BackupRun.Completed = {0} and halt time {1} exceeded", 
                                        br.BackupRunCompleted, 
                                        (haltTimeNotExceeded ? "was not" : "was"));
                
            } while (br.BackupRunCompleted == false && haltTimeNotExceeded == true);


            foreach (var backupRef in br.BackupFileRefs)
            {
                string cacheFileName = GetCacheEntryForFile(backupRef.FullFileName, br);

                if (backupRef.CopiedToArchive && File.Exists(cacheFileName))
                {
                    Logger.LogDebug("Deleting cache file after archive: {0}", cacheFileName);

                    File.Delete(cacheFileName);
                }
            }           
        }

        private string GetCacheEntryForFile(string fullFileName, BackupRun br)
        {
            return String.Format("{0}{1}{2}{3}", this.BackupCacheDirectory, br.BackupRunID, Path.DirectorySeparatorChar, fullFileName.Substring(fullFileName.IndexOf(":") + 1));
        }

        private BackupRun InitializeBackupRun(CloudBackupDbContext dbContext, BackupRun br)
        {
            Logger.LogDebug("BackupClient.InitializeNewBackupRun called");

            this.ScanBackupDirectoriesForRun(dbContext, br);            
            long currentBytes;
            string cacheRef;
            int fileCount;

            br.BackupRunStart = DateTime.Now;

            currentBytes = 0;

            //File count tracked separately in case the last run is all directories
            fileCount = 0;

            logger.LogInformation("Copying scanned files to backup cache");
            string backupCacheFullDir = String.Format("{0}{1}", this.BackupCacheDirectory, br.BackupRunID);

            if (Directory.Exists(backupCacheFullDir) == false)
            {
                Logger.LogInformation(String.Format("Creating new backup cache directory at {0}", backupCacheFullDir));

                Directory.CreateDirectory(backupCacheFullDir);
            }

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

                    Logger.LogDebug("Saving dbContext changes");
                    dbContext.SaveChanges();
                }
            }

            if (fileCount == 0)
            {
                Logger.LogInformation("Backup file count == 0, setting complete flag");

                br.BackupRunCompleted = true;
                br.BackupRunEnd = DateTime.Now;

                dbContext.SaveChanges();
            }
                            
            return br;
        }

        private BackupRun CreateBackupRun(CloudBackupDbContext dbContext)
        {
            Logger.LogDebug("CreateBackupRun called");

            var config = this.AppConfig.GetSection("BackupSettings");

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

            dbContext.Add<BackupRun>(br);
            dbContext.SaveChanges();

            Logger.LogInformation(String.Format("Created new backup run with ID: {0}", br.BackupRunID));

            return br;
        }

        private void ScanBackupDirectoriesForRun(CloudBackupDbContext dbContext, BackupRun br)
        {
            List<FileInfo> directoryFiles = new List<FileInfo>();

            Logger.LogInformation("Starting directory scan for backup run");

            foreach (var dirRef in br.BackupDirectories)
            {
                ScanDirectoryContents(dirRef.DirectoryFullFileName, directoryFiles);
            }

            foreach (FileInfo info in directoryFiles)
            {
                //Make sure file wasn't added in an ealier scan for this run
                if (br.BackupFileRefs.Where(b => b.FullFileName.ToLower().Equals(info.FullName.ToLower())).Count() == 0)
                {
                    Logger.LogInformation(String.Format("Adding file ref {0} to backup run ID {1}", info.FullName, br.BackupRunID));
                    br.BackupFileRefs.Add(new BackupRunFileRef { FullFileName = info.FullName });
                }
                
            }           

        }

        private BackupRun GetOpenBackupRun(CloudBackupDbContext dbContext)
        {
            this.Logger.LogDebug("GetOpenBackupRun called");

            var openBackupRuns = dbContext.BackupRuns.Where<BackupRun>(b => b.BackupRunCompleted == false).ToList<BackupRun>();
            
            if(openBackupRuns.Count > 1)
            {
                throw new Exception("More than one open backup");
            }            
            else
            {                
                return openBackupRuns.Count == 0 ? null : openBackupRuns.First<BackupRun>();
            }            
        }

        private List<FileInfo> ScanDirectoryContents(String rootDir, List<FileInfo> directoryFiles)
        {
            if( String.IsNullOrWhiteSpace(rootDir) )
            {
                throw new Exception("No root directory provided");
            } else if( !Directory.Exists(rootDir) )
            {
                throw new Exception(String.Format("Root directory {0} doesn't exist", rootDir));
            }
                        
            PopulateFileList(rootDir, directoryFiles);

            return directoryFiles;
        }

        private void PopulateFileList(string fileName, List<FileInfo> fileList)
        {
            if(File.Exists(fileName) || Directory.Exists(fileName))
            {
                fileList.Add(new FileInfo(fileName));

                if(Directory.Exists(fileName))
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
            var config = this.AppConfig.GetSection("BackupSettings");

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

        private void InitializeDB()
        {
            // In-memory database only exists while the connection is open
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<CloudBackupDbContext>()
                    .UseSqlite(connection)
                    .Options;

                // Create the schema in the database
                using (var context = new CloudBackupDbContext(options))
                {
                    context.Database.EnsureCreated();
                }

                
            }
            finally
            {
                connection.Close();
            }
        }
    
        private IConfigurationRoot AppConfig
        {
            get {

                if (this.appConfig == null)
                {
                    var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                    this.appConfig = builder.Build();
                }

                return this.appConfig;
            }
        }

        private string BackupCacheDirectory
        {
            get
            {
                if(backupCacheDir == null)
                {
                    backupCacheDir = this.GetBackupSettingsConfigValue("TempCopyDirectory");
                }

                return backupCacheDir;
            }
        }

        private int MaxCacheMB
        {
            get
            {
                if(this.maxCacheMB == 0)
                {
                    this.maxCacheMB = Int32.Parse(this.GetBackupSettingsConfigValue("MaxCacheMB"));
                }

                return this.maxCacheMB;
            }
        }

        private int TotalRunTimeSeconds
        {
            get
            {
                if (this.totalRunTimeSeconds == 0)
                {
                    this.totalRunTimeSeconds = Int32.Parse(this.GetBackupSettingsConfigValue("TotalRunTimeSeconds"));
                }

                return this.totalRunTimeSeconds;
            }
        }

        private DateTime StopRunTime
        {
            get
            {               
                return this.stopRunTime;
            }
        }

        private CloudBackupArchiveProvider ArchiveProvider
        {
            get
            {
                if (this.archiveProvider == null)
                {
                    var providerType = this.GetBackupSettingsConfigValue("ArchiveProviderType");

                    if (String.IsNullOrWhiteSpace(providerType))
                    {
                        throw new Exception("No archive provider type assigned in configuration");
                    }

                    var providerSettings = this.GetBackupSettingsConfigValue("ArchiveProviderConfig");

                    if (String.IsNullOrWhiteSpace(providerSettings))
                    {
                        throw new Exception("No archive provider settings assigned in configuration");
                    }

                    var providerConfig = this.AppConfig.GetSection("BackupSettings");

                    if (providerConfig == null)
                    {
                        throw new Exception("No provider configuration section found for backup settings");
                    }

                    if (providerType.ToLower().Equals("filesystem"))
                    {
                        this.archiveProvider = new FileSystemBackupArchiveProvider();
                    }
                    else
                    {
                        throw new Exception(String.Format("No provider found for type: {0}", providerType));
                    }
                }

                //TODO: Investigate where DI is happening for the provider and change intialize handling
                if( this.archiveProvider.Initialized == false )
                {
                    this.archiveProvider.Configure(this.AppConfig.GetSection("FileSystemArchiveTestConfig"));
                }

                return this.archiveProvider;
            }
        }

        private ILogger Logger
        {
            get
            {
                if (this.logger == null)
                {                  

                    var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder
                            .AddFilter("Microsoft", LogLevel.Warning)
                            .AddFilter("System", LogLevel.Warning)
                            .AddFilter("CloudBackupClient.BackupClient", LogLevel.Debug)
                            .AddConsole();
                    });

                    this.logger = loggerFactory.CreateLogger<BackupClient>();
                }
                
                return this.logger;
            }
        }
    }
}
