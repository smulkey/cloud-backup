using CloudBackupClient.ClientFileCacheHandlers;
using CloudBackupClient.Models;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class LocalClientFileCacheHandlerTests : CloudBackupTestBase
    {       
        override protected string ConfigurationJson => $"{{\"LocalClientFileCacheConfig\": {{ \"TempCopyDirectory\": \"\\\\TempBackup\", \"MaxCacheMB\": {this.MaxMBTestSize}}} }}";
        
        public LocalClientFileCacheHandlerTests()
        {
            BackupRun backupRun = this.CreateBackupRun();

            backupRun.BackupFileRefs.Add(new BackupRunFileRef { FullFileName = string.Format(this.TestLargeFileNameFormat, "1") });
            backupRun.BackupFileRefs.Add(new BackupRunFileRef { FullFileName = string.Format(this.TestLargeFileNameFormat, "2") });

            MemoryStream ms = new MemoryStream();
            for (int i = 0; i < (MaxMBTestSize * 1000 * 1000) + 1; i++)
            {
                ms.WriteByte((byte)0);
            }

            this.MockFileSystem.AddFile(string.Format(this.TestLargeFileNameFormat, "1"), new MockFileData(ms.ToArray()));
            this.MockFileSystem.AddFile(string.Format(this.TestLargeFileNameFormat, "2"), new MockFileData(ms.ToArray()));
        }

        [Fact]
        public void InitializeBackupRunTest()
        {
            BackupRun backupRun = this.CreateBackupRun();

            this.ClientFileCacheHandler.InitializeBackupRun(backupRun);

            foreach(var fileRef in backupRun.BackupFileRefs)
            {
                Assert.False(fileRef.CopiedToArchive);

                if (fileRef.FullFileName.Equals(string.Format(this.TestLargeFileNameFormat, "2")))
                {
                    Assert.False(fileRef.CopiedToCache);
                }
                else
                {
                    Assert.True(fileRef.CopiedToCache);

                    var cacheFileName = ((LocalClientFileCacheHandler)this.ClientFileCacheHandler).GetCacheEntryForFileRef(fileRef, backupRun);
                    Assert.True(this.MockFileSystem.FileInfo.FromFileName(cacheFileName).Exists);
                }
            }
        }

        [Fact]
        public void GetCacheStreamForItemTest()
        {            
            var backupRun = this.CreateBackupRun();

            this.ClientFileCacheHandler.InitializeBackupRun(backupRun);

            var backupRef = backupRun.BackupFileRefs.First<BackupRunFileRef>();
            var writeStream = new MemoryStream();
                                    
            using (var readStream = this.MockFileSystem.OpenRead(backupRef.FullFileName))
            {
                writeStream.WriteByte((byte)readStream.ReadByte());
            }

            byte[] fileBytes = writeStream.ToArray();

            writeStream = new MemoryStream();

            using (var testStream = this.ClientFileCacheHandler.GetCacheStreamForItem(backupRef, backupRun))
            {
                writeStream.WriteByte((byte)testStream.ReadByte());    
            }

            byte[] cacheBytes = writeStream.ToArray();

            Assert.Equal(fileBytes.Length, cacheBytes.Length);

            for(int i = 0; i < fileBytes.Length; i++)
            {
                Assert.Equal(fileBytes[i], cacheBytes[i]);
            }
        }

        [Fact]
        public void CompleteFileArchiveTest()
        {
            InitializeBackupRunTest();

            var backupRun = this.CreateBackupRun();

            var backupRef = backupRun.BackupFileRefs.First<BackupRunFileRef>();

            this.ClientFileCacheHandler.CompleteFileArchive(backupRef, backupRun);

            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                var cacheFileName = ((LocalClientFileCacheHandler)this.ClientFileCacheHandler).GetCacheEntryForFileRef(fileRef, backupRun);

                if (
                    fileRef.FullFileName.Equals(backupRef.FullFileName) ||
                    fileRef.FullFileName.Equals(string.Format(this.TestLargeFileNameFormat, "2"))
                   )
                {                    
                    Assert.False(this.MockFileSystem.FileInfo.FromFileName(cacheFileName).Exists);                    
                }
                else
                {
                    Assert.True(this.MockFileSystem.FileInfo.FromFileName(cacheFileName).Exists);
                }
            }
        }

        override public void Dispose()
        {
            this.ClientFileCacheHandler.Dispose();
        }

        private int MaxMBTestSize => 1;

        private string TestLargeFileNameFormat => @"\CloudBackupSource\Dir2\file{0}.big";

        protected override IClientFileCacheHandler ClientFileCacheHandlerTemplate => new LocalClientFileCacheHandler();

        private IClientFileCacheHandler ClientFileCacheHandler => this.ServiceProvider.GetService<IClientFileCacheHandler>();
    }
}
