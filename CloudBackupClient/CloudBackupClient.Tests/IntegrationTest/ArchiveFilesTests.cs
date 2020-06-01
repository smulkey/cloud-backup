using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.BackupClientController;
using CloudBackupClient.BackupRunController;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                
        protected override string ConfigurationJson => "{"+
                                                            "\"BackupSettings\": {" +
                                                            "\"BackupClientID\": \"DBAEC131-E186-4C68-8E12-86548107D7E2\"," +
                                                            $"\"BackupDirectories\": \"{TestBackupDirectory}\"," +
                                                            "\"RunTimeLimitSeconds\": 3600" +
                                                           "}," +
                                                           "\"LocalClientFileCacheConfig\": {" +
                                                                "\"TempCopyDirectory\": \"C:\\\\BackupCache\"," +
                                                                "\"MaxCacheMB\": 1" +
                                                           "}," +
                                                           "\"FileSystemArchiveTestConfig\": {" +
                                                            "\"BaseBackupDir\": \"C:\\\\BackupArchive\"" +
                                                           "}," +
                                                           "\"ConnectionStrings\": {" +
                                                                "\"SqliteConnString\": \"Data Source=:memory:\"" +
                                                                //"\"SqliteConnString\": \"Data Source=CloudBackupClientTestDB.sdf\""+
                                                           "}" +
                                                       "}";

        public ArchiveFileTests()
        {   
        }

        [Fact]
        public async void ArchiveBackupRunHappyPath()
        {
            // Given        
            var directoryInfo = this.MockFileSystem.Directory.CreateDirectory(TestBackupDirectory).CreateSubdirectory(TestBackupSubDirectory);

            var totalFiles = 3;

            for (int i = 1; i <= totalFiles; i++)
            {
                this.MockFileSystem.AddFile($"{directoryInfo.FullName}{Path.DirectorySeparatorChar}{TestFileName}{i}.txt", new MockFileData($"some content {i}"));
            }            
            
            // When
            var backupRun = this.BackupRunControl.GetNextBackupRun();

            await this.BackupRunControl.ArchiveBackupRunAsync(backupRun);

            // Then
            var updatedBackupRun = this.ServiceProvider.GetService<IClientDBHandler>().GetBackupRun(1);

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

                    var backupFilePath = this.ConcreteFileArchiveProvider.GetArchiveFileName(fileRef, updatedBackupRun);

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
            var directoryInfo = this.MockFileSystem.Directory.CreateDirectory(TestBackupDirectory).CreateSubdirectory(TestBackupSubDirectory);

            var totalFiles = 3;

            for (int i = 1; i <= totalFiles; i++)
            {
                this.MockFileSystem.AddFile($"{directoryInfo.FullName}{Path.DirectorySeparatorChar}{TestFileName}{i}.txt", new MockFileData($"some content {i}"));
            }

            // When
            var backupRun = this.BackupRunControl.GetNextBackupRun();
            
            var configurationRoot = this.ServiceProvider.GetService<IConfigurationRoot>();

            var backupSettings = configurationRoot.GetSection(BackupClientConfigurationKeys.RootConfigurationSection);

            //Will cause save loop to run once and then time out
            backupSettings[BackupClientConfigurationKeys.RunTimeLimitSeconds] = "-10";

            await this.BackupRunControl.ArchiveBackupRunAsync(backupRun);

            // Then
            var updatedBackupRun = this.ServiceProvider.GetService<IClientDBHandler>().GetBackupRun(1);

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
            var directoryInfo = this.MockFileSystem.Directory.CreateDirectory(TestBackupDirectory).CreateSubdirectory(TestBackupSubDirectory);

            var totalFiles = 3;

            for (int i = 1; i <= totalFiles; i++)
            {
                this.MockFileSystem.AddFile($"{directoryInfo.FullName}{Path.DirectorySeparatorChar}{TestFileName}{i}.txt", new MockFileData($"some content {i}"));
            }

            // When
            var firstBackupRun = this.BackupRunControl.GetNextBackupRun();

            var configurationRoot = this.ServiceProvider.GetService<IConfigurationRoot>();

            var backupSettings = configurationRoot.GetSection(BackupClientConfigurationKeys.RootConfigurationSection);

            //Will cause save loop to run once and then time out
            backupSettings[BackupClientConfigurationKeys.RunTimeLimitSeconds] = "-10";

            await this.BackupRunControl.ArchiveBackupRunAsync(firstBackupRun);

            // TODO: This is super hacky, need a way to swap in test using actual DB file/instance
            if (!this.ConfigurationJson.Contains("Data Source=:memory:"))
            {
                // Close and re-open database connection
                this.ServiceProvider.GetService<IClientDBHandler>().Dispose();
                this.ServiceProvider.GetService<IClientDBHandler>().Initialize(this.ServiceProvider);
            }

            // Then - Get same open backup run
            var secondBackupRun = this.BackupRunControl.GetNextBackupRun();
            
            Assert.Equal(firstBackupRun.BackupRunID, secondBackupRun.BackupRunID);
            Assert.NotNull(secondBackupRun.BackupDirectories);
            Assert.Equal(firstBackupRun.BackupDirectories.Count, secondBackupRun.BackupDirectories.Count);
            Assert.NotNull(secondBackupRun.BackupFileRefs);
            Assert.Equal(firstBackupRun.BackupFileRefs.Count, secondBackupRun.BackupFileRefs.Count);
        }

        private FileSystemBackupArchiveProvider ConcreteFileArchiveProvider => (FileSystemBackupArchiveProvider)this.ServiceProvider.GetService<ICloudBackupArchiveProvider>();

        protected override IClientDBHandler ClientDBHandlerTemplate => new SqliteDBHandler();

        override protected ICloudBackupArchiveProvider CloudBackupArchiveProviderTemplate => new FileSystemBackupArchiveProvider();

        protected override IBackupRunControl BackupRunControlTemplate => new BackupRunControl();

        protected override IClientFileCacheHandler ClientFileCacheHandlerTemplate => new LocalClientFileCacheHandler();

        protected override IBackupFileScanner BackupFileScannerTemplate => new BackupFileScanner();

        private IBackupRunControl BackupRunControl => this.ServiceProvider.GetService<IBackupRunControl>();
    }

}
