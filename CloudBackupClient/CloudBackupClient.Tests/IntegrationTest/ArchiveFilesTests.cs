using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.BackupClientController;
using CloudBackupClient.BackupRunController;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace CloudBackupClient.Tests.IntegrationTests
{
    public class ArchiveFileTests : CloudBackupTestBase
    {
        private static readonly string TestBackupDirectory = @"C:\\TestBackup";
        private static readonly string TestBackupSubDirectory = "files";
        private static readonly string TestFileName = "testFile";

        private IConfiguration configuration;
                
        private IClientDBHandler clientDBHandler;

        private ICloudBackupArchiveProvider cloudBackupArchiveProvider;

        public ArchiveFileTests()
        {   
        }
                
        [Fact]
        public async void ArchiveBackupRunHappyPath()
        {
            // Given       
            var backupRunControl = CreateBackupRunControl(false);
            var directoryInfo = this.MockFileSystem.Directory.CreateDirectory(TestBackupDirectory).CreateSubdirectory(TestBackupSubDirectory);

            var totalFiles = 3;

            for (int i = 1; i <= totalFiles; i++)
            {
                this.MockFileSystem.AddFile($"{directoryInfo.FullName}{Path.DirectorySeparatorChar}{TestFileName}{i}.txt", new MockFileData($"some content {i}"));
            }

            // When
            var backupRun = backupRunControl.GetNextBackupRun();

            await backupRunControl.ArchiveBackupRunAsync(backupRun);

            // Then
            var updatedBackupRun = this.clientDBHandler.GetBackupRun(1);

            Assert.True(updatedBackupRun.BackupRunCompleted);
            Assert.False(updatedBackupRun.FailedWithException);
            Assert.Null(updatedBackupRun.ExceptionMessage);
            Assert.NotNull(updatedBackupRun.BackupRunEnd);

            var completedCount = 0;

            foreach (var fileRef in updatedBackupRun.BackupFileRefs)
            {
                Assert.True(fileRef.CopiedToCache);

                if (this.MockFileSystem.CheckFileIsDirectory(fileRef.FullFileName))
                {
                    Assert.False(fileRef.CopiedToArchive);
                }
                else
                {
                    Assert.True(fileRef.CopiedToArchive);

                    var backupFilePath = ((FileSystemBackupArchiveProvider)this.cloudBackupArchiveProvider).GetArchiveFileName(fileRef, updatedBackupRun);

                    Assert.True(this.MockFileSystem.CheckFileExists(backupFilePath));

                    completedCount += 1;
                }
            }

            Assert.Equal(3, completedCount);
        }

        [Fact]
        public async void ArchiveBackupRunTimeout()
        {
            // Given
            var backupRunControl = CreateBackupRunControl(true);
            var directoryInfo = this.MockFileSystem.Directory.CreateDirectory(TestBackupDirectory).CreateSubdirectory(TestBackupSubDirectory);

            var totalFiles = 3;

            for (int i = 1; i <= totalFiles; i++)
            {
                this.MockFileSystem.AddFile($"{directoryInfo.FullName}{Path.DirectorySeparatorChar}{TestFileName}{i}.txt", new MockFileData($"some content {i}"));
            }

            // When
            var backupRun = backupRunControl.GetNextBackupRun();
                                                
            await backupRunControl.ArchiveBackupRunAsync(backupRun);

            // Then
            var updatedBackupRun = this.clientDBHandler.GetBackupRun(1);

            Assert.False(updatedBackupRun.BackupRunCompleted);
            Assert.False(updatedBackupRun.FailedWithException);
            Assert.Null(updatedBackupRun.ExceptionMessage);
            Assert.Null(updatedBackupRun.BackupRunEnd);

            var completedCount = 0;

            foreach (var fileRef in updatedBackupRun.BackupFileRefs)
            {
                if (fileRef.CopiedToArchive)
                {
                    completedCount += 1;
                }
            }

            Assert.Equal(1, completedCount);
        }
                
        [Fact]
        public async void ArchiveBackupRunRestart()
        {
            // Given
            var backupRunControl = CreateBackupRunControl(true);

            var directoryInfo = this.MockFileSystem.Directory.CreateDirectory(TestBackupDirectory).CreateSubdirectory(TestBackupSubDirectory);

            var totalFiles = 3;

            for (int i = 1; i <= totalFiles; i++)
            {
                this.MockFileSystem.AddFile($"{directoryInfo.FullName}{Path.DirectorySeparatorChar}{TestFileName}{i}.txt", new MockFileData($"some content {i}"));
            }

            // When
            var firstBackupRun = backupRunControl.GetNextBackupRun();
            
            await backupRunControl.ArchiveBackupRunAsync(firstBackupRun);

            // Emulate application restart
            //CreateBackupRunControl();

            // Then - Get same open backup run
            var secondBackupRun = backupRunControl.GetNextBackupRun();

            Assert.Equal(firstBackupRun.BackupRunID, secondBackupRun.BackupRunID);
            Assert.NotNull(secondBackupRun.BackupDirectories);
            Assert.Equal(firstBackupRun.BackupDirectories.Count, secondBackupRun.BackupDirectories.Count);
            Assert.NotNull(secondBackupRun.BackupFileRefs);
            Assert.Equal(firstBackupRun.BackupFileRefs.Count, secondBackupRun.BackupFileRefs.Count);
        }

        private BackupRunControl CreateBackupRunControl(bool forceTimeout)
        {
            var configDef = new Dictionary<string, List<Dictionary<string, string>>>
            {
                ["BackupSettings"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["BackupClientID"] = this.TestBackupClientID.ToString() },
                    new Dictionary<string, string> { ["BackupDirectories"] = @"C:\TestBackup" },
                    new Dictionary<string, string> { ["RunTimeLimitSeconds"] = forceTimeout ? "-1" : "3600" },
                },
                ["ConnectionStrings"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["SqliteConnString"] = "Data Source=:memory:" }
                },
                ["LocalClientFileCacheConfig"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["MaxCachePerRunMB"] = "1" },
                    new Dictionary<string, string> { ["MaxTotalCacheSizeGB"] = "1" },
                    new Dictionary<string, string> { ["TempCopyDirectory"] = @"C:\BackupCache" }
                },
                ["FileSystemArchiveTestConfig"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["BaseBackupDir"] = @"\Test\BackupArchive" }
                }
            };

            this.configuration = GenerateConfiguration(configDef);

            var backupFileScanner = new BackupFileScanner(this.MockFileSystem, new Mock<ILogger<BackupFileScanner>>().Object);
            var clientClientFileCacheHandler = new LocalClientFileCacheHandler(this.configuration, this.MockFileSystem, new Mock<ILogger<LocalClientFileCacheHandler>>().Object);

            this.cloudBackupArchiveProvider = new FileSystemBackupArchiveProvider(this.configuration, this.MockFileSystem, new Mock<ILogger<FileSystemBackupArchiveProvider>>().Object);
            this.clientDBHandler = new SqliteDBHandler(configuration, new Mock<ILogger<SqliteDBHandler>>().Object);

            return new BackupRunControl(backupFileScanner,
                                        this.clientDBHandler,
                                        clientClientFileCacheHandler,
                                        this.cloudBackupArchiveProvider,
                                        this.configuration,
                                        this.MockFileSystem,
                                        new Mock<ILogger<BackupRunControl>>().Object);
        }
    }
}
