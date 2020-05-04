using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Enumeration;
using System.Linq;
using System.Net;
using System.Text;
using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace CloudBackupClient.ClientFileCacheHandlers
{
    public class LocalClientFileCacheHandler : IClientFileCacheHandler
    {
        private IServiceProvider serviceProvider;

        
        public LocalClientFileCacheHandler()
        {
            
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            Logger.LogDebug("LocalClientFileCacheHandler.Initialize called");
        }

        public void InitializeBackupRun(BackupRun backupRun)
        {
            Logger.LogDebug("LocalClientFileCacheHandler.InitializeNewBackupRun called");

            long currentBytes;
            string cacheRef;
  
            backupRun.BackupRunStart = DateTime.Now;

            currentBytes = 0;

            string backupCacheFullDir = this.GetCacheDirectory(backupRun);

            if (this.FileSystem.CheckFileExists(backupCacheFullDir) == false)
            {
                Logger.LogInformation(String.Format("Creating new backup cache directory at {0}", backupCacheFullDir));

                this.FileSystem.CreateDirectory(backupCacheFullDir);
            }
            
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

                cacheRef = GetCacheEntryForFileRef(fileRef, backupRun);
                                
                //Verify file wasn't removed from source or already copied to the cache
                if ((this.FileSystem.CheckFileRefIsDirectory(fileRef) || this.FileSystem.CheckFileRefExists(fileRef)))
                {                   
                    if (this.FileSystem.CheckFileRefIsDirectory(fileRef))
                    {
                        Logger.LogInformation(String.Format("Creating backup cache directory: {0}", cacheRef));

                        this.FileSystem.CreateDirectory(cacheRef);
                    }
                    else
                    {
                        if (this.FileSystem.CheckFileExists(cacheRef))
                        {
                            Logger.LogInformation(String.Format("Deleting previous version of cache file: {0}", cacheRef));

                            this.FileSystem.DeleteFile(cacheRef);
                        }
                        else 
                        {
                            var dirPath = this.FileSystem.GetFileParentDirectoryName(cacheRef);

                            if (this.FileSystem.CheckFileExists(dirPath) == false)
                            {
                                this.FileSystem.CreateDirectory(dirPath);
                            }
                        }    

                        Logger.LogInformation(String.Format("Copying cache file from source: {0} to target: {1}", fileRef.FullFileName, cacheRef));

                        var retry = 3;
                        var success = false;

                        while (!success)
                        {
                            try
                            {
                                this.FileSystem.CopyFileRef(fileRef, cacheRef);                                
                                currentBytes += this.FileSystem.GetFileLength(cacheRef);

                                success = true;

                                Logger.LogDebug(String.Format("Cache copy current bytes count: {0}", currentBytes));
                            } 
                            catch(IOException ioEx)
                            {
                                if(ioEx.Message.IndexOf("Not Found") > 0 && --retry > 0)
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

                    Logger.LogDebug("Saving file ref changes");                    
                }
            }                
        }

        public Stream GetCacheStreamForItem(BackupRunFileRef backupRunFileRef, BackupRun backupRun)
        {
            var cacheFileName = GetCacheEntryForFileRef(backupRunFileRef, backupRun);

            Logger.LogDebug("Returning cache file after archive: {0}", cacheFileName);

            return this.FileSystem.FileInfo.FromFileName(cacheFileName).OpenRead();            
        }

        public void CompleteFileArchive(BackupRunFileRef backupRunFileRef, BackupRun backupRun)
        {
            var cacheFileName = GetCacheEntryForFileRef(backupRunFileRef, backupRun);

            Logger.LogDebug("Deleting cache file after archive: {0}", cacheFileName);

            this.FileSystem.DeleteFile(cacheFileName);
        }

        public void Dispose()
        {
            //noop
        }
       
        private string GetCacheDirectory(BackupRun backupRun) => String.Format("{0}{1}BackupRun-{2}",
                                                                                                this.LocalBackupCacheSettings.GetSection("TempCopyDirectory").Value,
                                                                                                Path.DirectorySeparatorChar,
                                                                                                backupRun.BackupRunID);

        //TODO Find better way to share this with tests
        public string GetCacheEntryForFileRef(BackupRunFileRef fileRef, BackupRun backupRun) => String.Format("{0}{1}",
                                                                                                this.GetCacheDirectory(backupRun),                                                                                                 
                                                                                                fileRef.FullFileName.Substring(fileRef.FullFileName.IndexOf(":") + 1));
        private IFileSystem FileSystem => this.serviceProvider.GetService<IFileSystem>();

        private IConfigurationSection LocalBackupCacheSettings => this.serviceProvider.GetService<IConfigurationRoot>().GetSection("LocalClientFileCacheConfig");
        
        private ILogger Logger => this.serviceProvider.GetService<ILogger<LocalClientFileCacheHandler>>();
    }
}
