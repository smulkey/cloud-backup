using System;

namespace CloudBackupClient
{
    public interface ICloudBackupService : IDisposable
    {
        public void Initialize(IServiceProvider serviceProvider);
    }
}
