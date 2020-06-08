using CloudBackupClient.ClientFileCacheHandlers;
using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class LocalClientFileCacheHandlerTests : CloudBackupTestBase
    {       
        //override protected string ConfigurationJson => $"{{\"LocalClientFileCacheConfig\": {{ \"TempCopyDirectory\": \"\\\\TempBackup\", \"MaxCacheMB\": {this.MaxMBTestSize}}} }}";
        
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

        private IClientFileCacheHandler CreateTestLocalCacheHandler()
        {
            var config = new Dictionary<string, List<Dictionary<string, string>>>
            {
                ["LocalClientFileCacheConfig"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["MaxCacheMBSetting"] = "1" },
                    new Dictionary<string, string> { ["TempCopyDirectory"] = @"C:\\BackupCache\" }
                }
            };

            return new LocalClientFileCacheHandler(GenerateConfiguration(config), this.MockFileSystem, new Mock<ILogger<LocalClientFileCacheHandler>>().Object);
        }
    }
}
