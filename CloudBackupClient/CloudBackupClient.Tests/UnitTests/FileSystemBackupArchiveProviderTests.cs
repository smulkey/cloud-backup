using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.Models;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class FileSystemBackupArchiveProviderTests : CloudBackupTestBase
    {
        override protected string ConfigurationJson => "{\"FileSystemArchiveTestConfig\": { \"BaseBackupDir\": \"\\\\Test\\\\BackupArchive\"}}";

        public FileSystemBackupArchiveProviderTests()
        {

        }

        [Fact]
        public async void ArchiveFileHappyPathTest()
        {
            BackupRun backupRun = this.CreateBackupRun();

            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileRef.FullFileName)))
                {
                    await this.CloudBackupArchiveProvider.ArchiveFileAsync(backupRun, fileRef, ms);
                }
            }
                        
            foreach (var fileRef in backupRun.BackupFileRefs)
            {                                
                string archiveFileName = ((FileSystemBackupArchiveProvider)this.CloudBackupArchiveProvider).GetArchiveFileName(fileRef, backupRun);

                Assert.True(this.MockFileSystem.CheckFileExists(archiveFileName));

                using (var fs = this.MockFileSystem.OpenRead(archiveFileName))
                {
                    MemoryStream ms = new MemoryStream();
                    int val;

                    while((val = fs.ReadByte()) != -1)
                    {
                        ms.WriteByte((byte)val);
                    }

                    byte[] streamBuffer = ms.GetBuffer();                    

                    Assert.Equal(fileRef.FullFileName, Encoding.UTF8.GetString(streamBuffer).Replace("\0",""));
                }
            }
        }

        override public void Dispose()
        {
            this.CloudBackupArchiveProvider.Dispose();
        }

        override protected ICloudBackupArchiveProvider CloudBackupArchiveProviderTemplate => new FileSystemBackupArchiveProvider();

        private ICloudBackupArchiveProvider CloudBackupArchiveProvider => this.ServiceProvider.GetService<ICloudBackupArchiveProvider>();
    }
}
