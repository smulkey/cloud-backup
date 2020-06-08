using CloudBackupClient.ClientFileCacheHandlers;
using CloudBackupClient.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class LocalClientFileCacheHandlerTests : CloudBackupTestBase
    {           
        private int MaxMBTestSize => 1;

        private string TestLargeFileNameFormat => @"\CloudBackupSource\Dir2\file{0}.big";
                
        public LocalClientFileCacheHandlerTests()
        {  
        }

        [Fact]
        public void InitializeBackupRunTest()
        {
            BackupRun backupRun = this.CreateBackupRun(this.MockFileSystem);
            
            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                this.MockFileSystem.AddFile(fileRef.FullFileName, fileRef.FullFileName);
            }

            //TODO move this
            backupRun.BackupFileRefs.Add(new BackupRunFileRef { FullFileName = string.Format(this.TestLargeFileNameFormat, "1") });
            backupRun.BackupFileRefs.Add(new BackupRunFileRef { FullFileName = string.Format(this.TestLargeFileNameFormat, "2") });

            MemoryStream ms = new MemoryStream();
            for (int i = 0; i < (MaxMBTestSize * 1000 * 1000) + 1; i++)
            {
                ms.WriteByte((byte)0);
            }
            this.MockFileSystem.AddFile(string.Format(this.TestLargeFileNameFormat, "1"), new MockFileData(ms.ToArray()));
            this.MockFileSystem.AddFile(string.Format(this.TestLargeFileNameFormat, "2"), new MockFileData(ms.ToArray()));

            var clientFileCacheHandler = CreateTestLocalCacheHandler();
            clientFileCacheHandler.InitializeBackupRun(backupRun);

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

                    var cacheFileName = ((LocalClientFileCacheHandler)clientFileCacheHandler).GetCacheEntryForFileRef(fileRef, backupRun);
                    Assert.True(this.MockFileSystem.FileInfo.FromFileName(cacheFileName).Exists);
                }
            }
        }

        [Fact]
        public void GetCacheStreamForItemTest()
        {            
            var backupRun = this.CreateBackupRun(this.MockFileSystem);

            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                this.MockFileSystem.AddFile(fileRef.FullFileName, fileRef.FullFileName);
            }

            var clientFileCacheHandler = CreateTestLocalCacheHandler();
            clientFileCacheHandler.InitializeBackupRun(backupRun);

            var backupRef = backupRun.BackupFileRefs.First();
            var writeStream = new MemoryStream();
                                    
            using (var readStream = this.MockFileSystem.OpenRead(backupRef.FullFileName))
            {
                writeStream.WriteByte((byte)readStream.ReadByte());
            }

            byte[] fileBytes = writeStream.ToArray();

            writeStream = new MemoryStream();

            using (var testStream = clientFileCacheHandler.GetCacheStreamForItem(backupRef, backupRun))
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
            BackupRun backupRun = this.CreateBackupRun(this.MockFileSystem);

            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                this.MockFileSystem.AddFile(fileRef.FullFileName, fileRef.FullFileName);
            }

            var clientFileCacheHandler = CreateTestLocalCacheHandler();
            clientFileCacheHandler.InitializeBackupRun(backupRun);

            var backupRef = backupRun.BackupFileRefs.First();

            clientFileCacheHandler.CompleteFileArchive(backupRef, backupRun);

            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                var cacheFileName = ((LocalClientFileCacheHandler)clientFileCacheHandler).GetCacheEntryForFileRef(fileRef, backupRun);

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

        [Fact]
        public void CacheFullStopsInitializeTest()
        {
            // Given backup directory exists            
            this.MockFileSystem.AddDirectory(@$"C:\\BackupCache\\BackupRun-0");

            // Given a backup run with source files
            BackupRun backupRun = this.CreateBackupRun(this.MockFileSystem);

            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                this.MockFileSystem.AddFile(fileRef.FullFileName, fileRef.FullFileName);
            }

            // When total allowed cache size is set to 0 and itialize is run
            var clientFileCacheHandler = CreateTestLocalCacheHandler(totalCacheGBSize: "0");
            clientFileCacheHandler.InitializeBackupRun(backupRun);
            
            // Then the cache should immediately read as full so no files are copied            
            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                Assert.False(fileRef.CopiedToCache);

                var cacheFileName = ((LocalClientFileCacheHandler)clientFileCacheHandler).GetCacheEntryForFileRef(fileRef, backupRun);

                Assert.False(this.MockFileSystem.FileInfo.FromFileName(cacheFileName).Exists);
            }
        }

        private IClientFileCacheHandler CreateTestLocalCacheHandler(string totalCacheGBSize = "1", string cacheDirectoryName = @"C:\BackupCache")
        {
            var config = new Dictionary<string, List<Dictionary<string, string>>>
            {
                ["LocalClientFileCacheConfig"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["MaxCachePerRunMB"] = "1" },
                    new Dictionary<string, string> { ["MaxTotalCacheSizeGB"] = totalCacheGBSize },
                    new Dictionary<string, string> { ["TempCopyDirectory"] = cacheDirectoryName}
                }
            };

            return new LocalClientFileCacheHandler(GenerateConfiguration(config), this.MockFileSystem, new Mock<ILogger<LocalClientFileCacheHandler>>().Object);
        }
    }
}
