using CloudBackupClient.ArchiveProviders;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Text;

namespace CloudBackupClient.Tests.UnitTests
{
    public class FileSystemBackupArchiveProvider_Tests : CloudBackupTestBase
    {
        protected override string ConfigurationJson => throw new NotImplementedException();


        //override protected string ConfigurationJson => $"{{\"LocalClientFileCacheConfig\": {{ \"TempCopyDirectory\": \"{this.TempDirectory}\", \"MaxCacheMB\": {this.MaxMBTestSize}}} }}";

        public FileSystemBackupArchiveProvider_Tests()
        {

        }

        override public void Dispose()
        {
            this.CloudBackupArchiveProvider.Dispose();
        }

        override protected ICloudBackupArchiveProvider CloudBackupArchiveProviderTemplate => new FileSystemBackupArchiveProvider();

        private ICloudBackupArchiveProvider CloudBackupArchiveProvider => this.ServiceProvider.GetService<ICloudBackupArchiveProvider>();
    }
}
