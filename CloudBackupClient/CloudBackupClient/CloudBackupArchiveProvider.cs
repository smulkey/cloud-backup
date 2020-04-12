using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;

namespace CloudBackupClient
{
    public interface CloudBackupArchiveProvider
    {
        public bool Initialized { get; }

        public void Configure(IConfigurationSection configSection);

        public bool ArchiveFile(BackupRun backupRun, BackupRunFileRef fileRef, FileInfo cacheFile);
        
    }
}
