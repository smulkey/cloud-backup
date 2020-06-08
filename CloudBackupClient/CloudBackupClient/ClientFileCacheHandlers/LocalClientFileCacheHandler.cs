using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Abstractions;

namespace CloudBackupClient.ClientFileCacheHandlers
{
    public class LocalClientFileCacheHandler : IClientFileCacheHandler
    {
        private readonly IConfiguration configuration;

        private readonly IFileSystem fileSystem;

        private readonly ILogger<LocalClientFileCacheHandler> logger;

        private const string LocalCacheConfigSettingsSectionName = "LocalClientFileCacheConfig";

        private const string MaxCacheMBSettingKey = "MaxCacheMBSetting";

        private const string TempCopyDirectoryKey = "TempCopyDirectory";

        public LocalClientFileCacheHandler(IConfiguration configuration,
                                           IFileSystem fileSystem,
                                           ILogger<LocalClientFileCacheHandler> logger)
        {
            this.configuration = configuration;
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public void InitializeBackupRun(BackupRun backupRun)
        {
            this.logger.LogDebug("LocalClientFileCacheHandler.InitializeNewBackupRun called");

            long currentBytes;
            string cacheRef;

            backupRun.BackupRunStart = DateTime.Now;

            currentBytes = 0;

            string backupCacheFullDir = this.GetCacheDirectory(backupRun);

            if (this.fileSystem.CheckFileExists(backupCacheFullDir) == false)
            {
                this.logger.LogInformation(String.Format("Creating new backup cache directory at {0}", backupCacheFullDir));

                this.fileSystem.CreateDirectory(backupCacheFullDir);
            }

            this.logger.LogInformation("Copying scanned files to backup cache");

            var backupCacheSettings = this.configuration.GetSection(LocalCacheConfigSettingsSectionName);
            int maxCacheMB = int.Parse(backupCacheSettings[MaxCacheMBSettingKey]);

            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                if (currentBytes > maxCacheMB * 1000000)
                {
                    this.logger.LogInformation("Halting cache file copy due to max copy bytes exceded");
                    break;
                }

                if (fileRef.CopiedToCache == true)
                {
                    this.logger.LogDebug(String.Format("Skipping file already copied to backup cache: {0}", fileRef.FullFileName));
                    continue;
                }

                cacheRef = GetCacheEntryForFileRef(fileRef, backupRun);

                //Verify file wasn't removed from source or already copied to the cache
                if ((this.fileSystem.CheckFileRefIsDirectory(fileRef) || this.fileSystem.CheckFileRefExists(fileRef)))
                {
                    if (this.fileSystem.CheckFileRefIsDirectory(fileRef))
                    {
                        this.logger.LogInformation(String.Format("Creating backup cache directory: {0}", cacheRef));

                        this.fileSystem.CreateDirectory(cacheRef);
                    }
                    else
                    {
                        if (this.fileSystem.CheckFileExists(cacheRef))
                        {
                            this.logger.LogInformation(String.Format("Deleting previous version of cache file: {0}", cacheRef));

                            this.fileSystem.DeleteFile(cacheRef);
                        }
                        else
                        {
                            var dirPath = this.fileSystem.GetFileParentDirectoryName(cacheRef);

                            if (this.fileSystem.CheckFileExists(dirPath) == false)
                            {
                                this.fileSystem.CreateDirectory(dirPath);
                            }
                        }

                        this.logger.LogInformation(String.Format("Copying cache file from source: {0} to target: {1}", fileRef.FullFileName, cacheRef));

                        var retry = 3;
                        var success = false;

                        while (!success)
                        {
                            try
                            {
                                this.fileSystem.CopyFileRef(fileRef, cacheRef);
                                currentBytes += this.fileSystem.GetFileLength(cacheRef);

                                success = true;

                                this.logger.LogDebug(String.Format("Cache copy current bytes count: {0}", currentBytes));
                            }
                            catch (IOException ioEx)
                            {
                                if (ioEx.Message.IndexOf("Not Found") > 0 && --retry > 0)
                                {
                                    System.Threading.Thread.Sleep(500);
                                    this.logger.LogWarning($"Failed copy attempt with exception {ioEx.Message}");
                                }
                                else
                                {
                                    throw ioEx;
                                }
                            }
                        }
                    }

                    fileRef.CopiedToCache = true;

                    this.logger.LogDebug("Saving file ref changes");
                }
            }
        }

        public Stream GetCacheStreamForItem(BackupRunFileRef backupRunFileRef, BackupRun backupRun)
        {
            var cacheFileName = GetCacheEntryForFileRef(backupRunFileRef, backupRun);

            this.logger.LogDebug("Returning cache file after archive: {0}", cacheFileName);

            var retryCount = 3;
            var success = false;
            Stream fileStream = null;

            do
            {
                try
                {
                    fileStream = this.fileSystem.OpenRead(cacheFileName);

                    success = true;
                }
                catch (IOException ioEx)
                {
                    if (--retryCount >= 0)
                    {
                        this.logger.LogWarning($"Unable to open file stream for {cacheFileName}, retying...");
                        System.Threading.Thread.Sleep(500);
                    }
                    else
                    {
                        this.logger.LogError($"Failed to open file stream for {cacheFileName}", ioEx);
                        throw;
                    }
                }
            }
            while (success == false);
            return fileStream;
        }

        public void CompleteFileArchive(BackupRunFileRef backupRunFileRef, BackupRun backupRun)
        {
            var cacheFileName = GetCacheEntryForFileRef(backupRunFileRef, backupRun);

            this.logger.LogDebug("Deleting cache file after archive: {0}", cacheFileName);

            var fileInfo = this.fileSystem.FileInfo.FromFileName(cacheFileName);
            if (fileInfo.IsReadOnly)
            {
                // Clear RO flag before delete
                this.fileSystem.File.SetAttributes(cacheFileName, new FileAttributes());
            }

            this.fileSystem.DeleteFile(cacheFileName);

            this.logger.LogInformation($"Deleted cache file {cacheFileName}");
        }

        public void Dispose()
        {
            //noop
        }

        private string GetCacheDirectory(BackupRun backupRun) => String.Format("{0}{1}BackupRun-{2}",
                                                                                                this.configuration.GetSection(LocalCacheConfigSettingsSectionName)[TempCopyDirectoryKey],
                                                                                                Path.DirectorySeparatorChar,
                                                                                                backupRun.BackupRunID);

        //TODO Find better way to share this with tests
        public string GetCacheEntryForFileRef(BackupRunFileRef fileRef, BackupRun backupRun) => String.Format("{0}{1}",
                                                                                                this.GetCacheDirectory(backupRun),
                                                                                                fileRef.FullFileName.Substring(fileRef.FullFileName.IndexOf(":") + 1));    

        //private IConfiguration GetConfiguration()
        //{
        //    Mock<IConfiguration> configuration = new Mock<IConfiguration>();

        //    return configuration.
                
        //        ;
        //}
    }
}
