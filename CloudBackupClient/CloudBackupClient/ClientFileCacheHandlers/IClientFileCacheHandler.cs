using CloudBackupClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CloudBackupClient.ClientFileCacheHandlers
{
    interface IClientFileCacheHandler : IDisposable
    {
        void PopulateFilesForBackupRun(BackupRun backupRun);

        Stream GetCacheStreamForItem(BackupRunFileRef backupRef, BackupRun backupRun);
        
        void CompleteFileArchive(BackupRunFileRef backupRef, BackupRun backupRun);
    }
}
