using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.BackupClientController;
using CloudBackupClient.BackupRunController;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace CloudBackupClient.Tests.IntegrationTests
{
    public class BackupClientTests : CloudBackupTestBase
    {
        private static readonly string TestBackupDirectory = @"C:\\TestBackup";
        private static readonly string TestBackupSubDirectory = "files";
        private static readonly string TestFileName = "testFile";
                
        protected override string ConfigurationJson => "{"+
                                                            "\"BackupSettings\": {" +
                                                            "\"BackupClientID\": \"678385F0-AB93-434A-989D-9CD2649A457E\"," +
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
                                                           "}" +
                                                       "}";

        public BackupClientTests()
        {   
        }

        [Fact]
        public void BackupClientHappyPath()
        {
            // Given        
            var directoryInfo = this.MockFileSystem.Directory.CreateDirectory(TestBackupDirectory).CreateSubdirectory(TestBackupSubDirectory);

            var totalFiles = 3;

            for (int i = 1; i <= totalFiles; i++)
            {
                this.MockFileSystem.AddFile($"{directoryInfo.FullName}{Path.DirectorySeparatorChar}{TestFileName}{i}.txt", new MockFileData($"some content {i}"));
            }

            // When
            var backupClient = new BackupClient(this.ServiceProvider);

            backupClient.Start();

            // Then - No exception            
        }

        [Fact]
        public void BackupClientThrowsException()
        {
            // Given - Root directory will be missing
            

            // When / Then - throws exception
            var backupClient = new BackupClient(this.ServiceProvider);

            Assert.ThrowsAsync<Exception>(() => backupClient.Start());
        }

        protected override IClientDBHandler ClientDBHandlerTemplate => new SqliteDBHandler();

        override protected ICloudBackupArchiveProvider CloudBackupArchiveProviderTemplate => new FileSystemBackupArchiveProvider();

        protected override IBackupRunControl BackupRunControlTemplate => new BackupRunControl();

        protected override IClientFileCacheHandler ClientFileCacheHandlerTemplate => new LocalClientFileCacheHandler();

        protected override IBackupFileScanner BackupFileScannerTemplate => new BackupFileScanner();
    }

}
