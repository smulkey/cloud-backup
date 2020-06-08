using CloudBackupClient.Models;
using System;
using System.Collections.Generic;

namespace CloudBackupClient.ClientDBHandlers
{
    public interface IClientDBHandler : IDisposable
    {
        
        BackupRun GetBackupRun(int backupRunID);

        IList<BackupRun> GetOpenBackupRuns();

        void AddBackupRun(BackupRun backupRun);

        void UpdateBackupFileRef(BackupRunFileRef backupRunFileRef);

        void UpdateBackupRun(BackupRun backupRun);
    }
}
