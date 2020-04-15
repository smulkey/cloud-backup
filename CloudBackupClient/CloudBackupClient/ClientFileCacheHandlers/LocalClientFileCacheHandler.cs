using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloudBackupClient.ClientFileCacheHandlers
{
    class LocalClientFileCacheHandler : IClientFileCacheHandler
    {
        private IServiceProvider serviceProvider;
        
        public LocalClientFileCacheHandler(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void InitializeBackupRun(BackupRun backupRun)
        {
            Logger.LogDebug("BackupClient.InitializeNewBackupRun called");

            long currentBytes;
            string cacheRef;
            int fileCount;

            backupRun.BackupRunStart = DateTime.Now;

            currentBytes = 0;

            string backupCacheFullDir = this.GetCacheDirectory(backupRun);

            if (Directory.Exists(backupCacheFullDir) == false)
            {
                Logger.LogInformation(String.Format("Creating new backup cache directory at {0}", backupCacheFullDir));

                Directory.CreateDirectory(backupCacheFullDir);
            }

            //File count tracked separately in case the last run is all directories
            fileCount = 0;

            this.Logger.LogInformation("Copying scanned files to backup cache");

            int maxCacheMB = int.Parse(this.LocalBackupCacheSettings.GetSection("MaxCacheMB").Value);

            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                if (currentBytes > maxCacheMB * 1000000)
                {
                    Logger.LogInformation("Halting cache file copy due to max copy bytes exceded");
                    break;
                }

                if (fileRef.CopiedToCache == true)
                {
                    Logger.LogDebug(String.Format("Skipping file already copied to backup cache: {0}", fileRef.FullFileName));
                    continue;
                }

                cacheRef = GetCacheEntryForFile(fileRef.FullFileName, backupRun);

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
                        if (File.Exists(cacheRef))
                        {
                            Logger.LogInformation(String.Format("Deleting previous version of cache file: {0}", cacheRef));

                            File.Delete(cacheRef);
                        }

                        Logger.LogInformation(String.Format("Copying cache file from source: {0} to target: {1}", fileRef.FullFileName, cacheRef));

                        var retry = 3;
                        var success = false;

                        while (!success)
                        {
                            try
                            {
                                File.Copy(fileRef.FullFileName, cacheRef);
                                var cacheFile = new FileInfo(cacheRef);
                                currentBytes += cacheFile.Length;

                                success = true;

                                Logger.LogDebug(String.Format("Cache copy current bytes count: {0}", currentBytes));
                            } 
                            catch(IOException ioEx)
                            {
                                if( --retry > 0)
                                {
                                    System.Threading.Thread.Sleep(500);
                                    Logger.LogWarning($"Failed copy attempt with exception {ioEx.Message}");
                                }
                                else
                                {
                                    throw ioEx;
                                }
                            }
                        }
                    }

                    fileRef.CopiedToCache = true;

                    fileCount += 1;

                    Logger.LogDebug("Saving file ref changes");                    
                }
            }

            if (fileCount == 0)
            {
                Logger.LogInformation("Backup file count == 0, setting complete flag");

                backupRun.BackupRunCompleted = true;
                backupRun.BackupRunEnd = DateTime.Now;                
            }          
        }

        public Stream GetCacheStreamForItem(BackupRunFileRef item, BackupRun br)
        {
            var cacheFile = new FileInfo(GetCacheEntryForFile(item.FullFileName, br));

            if( File.Exists(cacheFile.FullName) == false)
            {
                cacheFile.Create();
            }

            return cacheFile.OpenRead();
        }

        public void CompleteFileArchive(BackupRunFileRef backupRef, BackupRun br)
        {
            var cacheFileName = GetCacheEntryForFile(backupRef.FullFileName, br);

            if (File.Exists(cacheFileName))
            {
                Logger.LogDebug("Deleting cache file after archive: {0}", cacheFileName);
                                
                File.Delete(cacheFileName);
            }
        }


        public void Dispose()
        {
            //noop
        }

        private string GetCacheDirectory(BackupRun backupRun) => String.Format("{0}{1}BackupRun-{2}",
                                                                                                this.TempCopyDirectory,
                                                                                                Path.DirectorySeparatorChar,
                                                                                                backupRun.BackupRunID);

        private string GetCacheEntryForFile(string fullFileName, BackupRun backupRun) => String.Format("{0}{1}", 
                                                                                                this.GetCacheDirectory(backupRun),                                                                                                 
                                                                                                fullFileName.Substring(fullFileName.IndexOf(":") + 1));

        private string TempCopyDirectory => this.LocalBackupCacheSettings.GetSection("TempCopyDirectory").Value;

        private IConfigurationSection LocalBackupCacheSettings => this.serviceProvider.GetService<IConfigurationRoot>().GetSection("LocalClientFileCacheConfig");
        
        private ILogger Logger => this.serviceProvider.GetService<ILogger<LocalClientFileCacheHandler>>();
    }
}
