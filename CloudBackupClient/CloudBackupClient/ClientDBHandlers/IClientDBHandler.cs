using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudBackupClient.ClientDBHandlers
{
    public interface IClientDBHandler : ICloudBackupService
    {
        
        BackupRun GetBackupRun(int backupRunID);

        IList<BackupRun> GetOpenBackupRuns();

        void AddBackupRun(BackupRun backupRun);

        void UpdateBackupFileRef(BackupRunFileRef backupRunFileRef);

        void UpdateBackupRun(BackupRun backupRun);
    }
}
