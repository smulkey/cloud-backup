using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudBackupClient
{
    public interface ICloudBackupArchiveProvider
    {
        public bool ArchiveFile(BackupRun backupRun, BackupRunFileRef fileRef, Stream cacheFileStream);
        
    }
}
