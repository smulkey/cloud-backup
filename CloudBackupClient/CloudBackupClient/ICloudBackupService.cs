using System;
using System.Collections.Generic;
using System.Text;

namespace CloudBackupClient
{
    public interface ICloudBackupService : IDisposable
    {
        public void Initialize(IServiceProvider serviceProvider);
    }
}
