using System;
using System.Collections.Generic;
using CloudBackupClient.Models;
using CloudBackupClient.ClientFileCacheHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CloudBackupClient.ClientFileCacheHandlers
{
    class TestFileCacheHandler : IClientFileCacheHandler
    {
        public IDictionary<string, byte[]> FileSet { get; set; }

        private IServiceProvider serviceProvider;
                
        public TestFileCacheHandler(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
                
        public void PopulateFilesForBackupRun(BackupRun backupRun)
        {
            Logger.LogDebug("PopulateFilesForBackupRun called");

            foreach(var fileName in this.FileSet.Keys)
            {
                var brf = new BackupRunFileRef
                {
                    BackupRunID = backupRun.BackupRunID,
                    FullFileName = fileName
                };

                backupRun.BackupFileRefs.Add(brf);
            }

            Logger.LogDebug($"Added {backupRun.BackupFileRefs.Count} file refernces to backup run");
        }

        public void InitializeBackupRun(BackupRun backupRun)
        {
            Logger.LogDebug("InitializeBackupRun called - no activity");
        }

        public Stream GetCacheStreamForItem(BackupRunFileRef backupFileRef, BackupRun backupRun)
        {
            Logger.LogDebug($"GetCacheStreamForItem called - returning bytes for file name key: {backupFileRef.FullFileName}");

            return new MemoryStream(this.FileSet[backupFileRef.FullFileName]);            
        }

        public void CompleteFileArchive(BackupRunFileRef backupRef, BackupRun br)
        {
            Logger.LogDebug("BackupRunFileRef called - no activity");
        }


        public void Dispose()
        {
            //noop
        }
        
        private ILogger Logger => this.serviceProvider.GetService<ILogger<TestFileCacheHandler>>();
    }
}
