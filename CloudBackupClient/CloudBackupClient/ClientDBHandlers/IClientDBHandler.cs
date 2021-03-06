﻿using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudBackupClient.ClientDBHandlers
{
    public interface IClientDBHandler : IDisposable
    {
        IList<BackupRun> GetOpenBackupRuns();

        void AddBackupRun(BackupRun br);

        void UpdateBackupFileRef(BackupRunFileRef item);

        void UpdateBackupRun(BackupRun br);       
    }
}
