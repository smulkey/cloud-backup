using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.BackupClientController;
using CloudBackupClient.BackupRunController;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Xunit;

namespace CloudBackupClient.Tests.IntegrationTests
{
    public class BackupClientTests : CloudBackupTestBase
    {
        private static readonly string TestBackupDirectory = @"C:\\TestBackup";
        private static readonly string TestBackupSubDirectory = "files";
        private static readonly string TestFileName = "testFile";
      
        public BackupClientTests()
        {   
        }

        [Fact]
        public async Task BackupClientHappyPath()
        {
            // Given 
            var directoryInfo = this.MockFileSystem.Directory.CreateDirectory(TestBackupDirectory).CreateSubdirectory(TestBackupSubDirectory);

            var totalFiles = 3;

            for (int i = 1; i <= totalFiles; i++)
            {
                this.MockFileSystem.AddFile($"{directoryInfo.FullName}{Path.DirectorySeparatorChar}{TestFileName}{i}.txt", new MockFileData($"some content {i}"));
            }

            // When
            var backupClient = CreateTestBackupClient();

            await backupClient.Start();

            // Then - No exception            
        }

        [Fact]
        public void BackupClientThrowsException()
        {
            // Given - Root directory will be missing


            // When / Then - throws exception
            var backupClient = CreateTestBackupClient();

            Assert.ThrowsAsync<Exception>(() => backupClient.Start());
        }
               
        private BackupClient CreateTestBackupClient()
        {
            var configDef = new Dictionary<string, List<Dictionary<string, string>>>
            {
                ["BackupSettings"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["BackupClientID"] = this.TestBackupClientID.ToString() },
                    new Dictionary<string, string> { ["BackupDirectories"] = @"C:\\TestBackup" },
                    new Dictionary<string, string> { ["RunTimeLimitSeconds"] = "3600" },
                },
                ["ConnectionStrings"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["SqliteConnString"] = "Data Source=:memory:" }
                },
                ["LocalClientFileCacheConfig"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["MaxCacheMBSetting"] = "1" },
                    new Dictionary<string, string> { ["TempCopyDirectory"] = @"C:\\BackupCache\" }
                },
                ["FileSystemArchiveTestConfig"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["BaseBackupDir"] = @"\\Test\BackupArchive\" }
                }
            };

            var configuration = GenerateConfiguration(configDef);

            var backupFileScanner = new BackupFileScanner(this.MockFileSystem, new Mock<ILogger<BackupFileScanner>>().Object);
            var clientClientFileCacheHandler = new LocalClientFileCacheHandler(configuration, this.MockFileSystem, new Mock<ILogger<LocalClientFileCacheHandler>>().Object);

            var cloudBackupArchiveProvider = new FileSystemBackupArchiveProvider(configuration, this.MockFileSystem, new Mock<ILogger<FileSystemBackupArchiveProvider>>().Object);
            var clientDBHandler = new SqliteDBHandler(configuration, new Mock<ILogger<SqliteDBHandler>>().Object);

            var backupRunControl = new BackupRunControl(backupFileScanner,
                                                        clientDBHandler,
                                                        clientClientFileCacheHandler,
                                                        cloudBackupArchiveProvider,
                                                        configuration,
                                                        MockFileSystem,
                                                        new Mock<ILogger<BackupRunControl>>().Object);

            return new BackupClient(clientDBHandler, backupRunControl, configuration, new Mock<ILogger<BackupClient>>().Object);
        }
    }
}
