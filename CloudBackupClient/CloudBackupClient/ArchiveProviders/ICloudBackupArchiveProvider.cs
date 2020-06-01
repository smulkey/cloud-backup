using CloudBackupClient.Models;
using System.IO;
using System.Threading.Tasks;

namespace CloudBackupClient.ArchiveProviders
{
    public interface ICloudBackupArchiveProvider : ICloudBackupService
    {
        public Task<bool> ArchiveFileAsync(BackupRun backupRun, BackupRunFileRef fileRef, Stream cacheFileStream);
        
    }
}
