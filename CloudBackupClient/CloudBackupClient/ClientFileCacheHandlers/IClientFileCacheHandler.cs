using CloudBackupClient.Models;
using System.IO;

namespace CloudBackupClient.ClientFileCacheHandlers
{
    public interface IClientFileCacheHandler : ICloudBackupService
    {
        void InitializeBackupRun(BackupRun backupRun);

        Stream GetCacheStreamForItem(BackupRunFileRef backupRef, BackupRun backupRun);
        
        void CompleteFileArchive(BackupRunFileRef backupRef, BackupRun backupRun);        
    }
}
