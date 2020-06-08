using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class FileSystemBackupArchiveProviderTests : CloudBackupTestBase
    {
        public FileSystemBackupArchiveProviderTests()
        {
        }

        [Fact]
        public async void ArchiveFileHappyPathTest()
        {
            var cloudBackupArchiveProvider = CreateTestFileSystemBackupArchiveProvider();
            BackupRun backupRun = this.CreateBackupRun();

            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileRef.FullFileName)))
                {
                    await cloudBackupArchiveProvider.ArchiveFileAsync(backupRun, fileRef, ms);
                }
            }
                        
            foreach (var fileRef in backupRun.BackupFileRefs)
            {                                
                string archiveFileName = ((FileSystemBackupArchiveProvider)cloudBackupArchiveProvider).GetArchiveFileName(fileRef, backupRun);

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

        private ICloudBackupArchiveProvider CreateTestFileSystemBackupArchiveProvider()
        {
            var config = new Dictionary<string, List<Dictionary<string, string>>>
            {
                ["FileSystemArchiveTestConfig"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["BaseBackupDir"] = @"\\Test\BackupArchive\" }
                }
            };

            return new FileSystemBackupArchiveProvider(GenerateConfiguration(config), this.MockFileSystem, new Mock<ILogger<FileSystemBackupArchiveProvider>>().Object);
        }
    }
}
