using CloudBackupClient.ClientFileCacheHandlers;
using System.IO;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class LocalClientFileCacheHandler_Test : CloudBackupTestBase
    {
        private string TempDirectory { get; set; }

        override protected string ConfigurationJson => $"{{\"LocalClientFileCacheConfig\": {{ \"TempCopyDirectory\": \"{this.TempDirectory}\", \"MaxCacheMB\": 1}} }}";

        public LocalClientFileCacheHandler_Test()
        {
            this.TempDirectory = @"G:\CloudBackupTestTemp";

            if (Directory.Exists(this.TempDirectory))
            {
                Directory.Delete(this.TempDirectory);
            }

            Directory.CreateDirectory(this.TempDirectory);
        }


        override public void Dispose()
        {
            if (Directory.Exists(this.TempDirectory))
            {
                Directory.Delete(this.TempDirectory);
            }
        }

        protected override IClientFileCacheHandler ClientFileCacheHandler => new LocalClientFileCacheHandler();
    }
}
