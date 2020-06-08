using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class SqliteDBHandlerTests : CloudBackupTestBase
    {        
        public SqliteDBHandlerTests()
        {
        }

        [Fact]
        public void AddBackupRunById()
        {
            var clientDbHandler = CreateTestDBHandler();
            var backupRun = this.CreateBackupRun();

            clientDbHandler.AddBackupRun(backupRun);
                        
            var requestedBackupRun = clientDbHandler.GetBackupRun(backupRun.BackupRunID);

            Assert.NotNull(requestedBackupRun);            
            Assert.Equal(backupRun.BackupRunID, requestedBackupRun.BackupRunID);
            Assert.Equal(backupRun.BackupClientID, requestedBackupRun.BackupClientID);
            Assert.NotNull(requestedBackupRun.BackupDirectories);
            Assert.Equal(backupRun.BackupDirectories.Count, requestedBackupRun.BackupDirectories.Count);
            Assert.Equal(backupRun.BackupDirectories[0].DirectoryFullFileName, requestedBackupRun.BackupDirectories[0].DirectoryFullFileName);
            Assert.Equal(backupRun.BackupFileRefs.Count, requestedBackupRun.BackupFileRefs.Count);
            Assert.Equal(backupRun.BackupFileRefs[0].FullFileName, requestedBackupRun.BackupFileRefs[0].FullFileName);
        }

        [Fact]
        public void GetBackupRunByIdNotFound()
        {
            var clientDbHandler = CreateTestDBHandler();
            var requestedBackupRun = clientDbHandler.GetBackupRun(1001);

            Assert.Null(requestedBackupRun);            
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetOpenBackupRunTest(bool isException)
        {
            // Given
            var clientDbHandler = CreateTestDBHandler();
            var completedBackupRun = this.CreateBackupRun();

            clientDbHandler.AddBackupRun(completedBackupRun);

            completedBackupRun.BackupRunStart = DateTime.Now.AddSeconds(-1);
            completedBackupRun.BackupRunCompleted = true;
            completedBackupRun.FailedWithException = isException;            
            completedBackupRun.BackupRunEnd = DateTime.Now;
            
            if (isException)            
            {
                completedBackupRun.ExceptionMessage = "Exception happened";
            }

            clientDbHandler.UpdateBackupRun(completedBackupRun);            

            var openBackupRun = this.CreateBackupRun();

            clientDbHandler.AddBackupRun(openBackupRun);

            // When
            IList<BackupRun> lstBrs = clientDbHandler.GetOpenBackupRuns();
                        
            // Then
            Assert.NotNull(lstBrs);
            Assert.Equal(1, lstBrs.Count);
            Assert.Equal(openBackupRun.BackupRunID, lstBrs.First<BackupRun>().BackupRunID);
        }

        [Fact]
        public void UpdateBackupRun()
        {
            var clientDbHandler = CreateTestDBHandler();
            var backupRun = this.CreateBackupRun();

            clientDbHandler.AddBackupRun(backupRun);

            BackupRunFileRef newFileRef = new BackupRunFileRef
            {
                FullFileName = @"C:\Test2\file2.txt"
            };
;
            backupRun.BackupFileRefs.Add(newFileRef);

            var backupRefFileCount = backupRun.BackupFileRefs.Count;

            backupRun.BackupRunStart = DateTime.Now.AddSeconds(-10);
            backupRun.BackupRunEnd = DateTime.Now.AddSeconds(10);
            backupRun.BackupRunCompleted = false;
            backupRun.FailedWithException = true;
            backupRun.ExceptionMessage = "test exception";

            backupRun.BackupRunCompleted = true;

            clientDbHandler.UpdateBackupRun(backupRun);

            var updatedBackupRun = clientDbHandler.GetBackupRun(backupRun.BackupRunID);

            Assert.Equal(backupRefFileCount, updatedBackupRun.BackupFileRefs.Count);
            Assert.True(updatedBackupRun.BackupRunCompleted);
            Assert.Equal(backupRun.BackupRunStart, updatedBackupRun.BackupRunStart);
            Assert.Equal(backupRun.BackupRunEnd, updatedBackupRun.BackupRunEnd);            
            Assert.True(updatedBackupRun.FailedWithException);
            Assert.Equal(backupRun.ExceptionMessage, updatedBackupRun.ExceptionMessage);
        }

        [Fact]
        public void UpdateBackupRunFileRefs()
        {
            var clientDbHandler = CreateTestDBHandler();
            var backupRun = this.CreateBackupRun();

            clientDbHandler.AddBackupRun(backupRun);

            var fileRef = backupRun.BackupFileRefs[0];

            fileRef.CopiedToCache = true;
            fileRef.CopiedToArchive = true;

            clientDbHandler.UpdateBackupFileRef(fileRef);
            
            var updatedBackupRun = clientDbHandler.GetBackupRun(backupRun.BackupRunID);

            Assert.True(updatedBackupRun.BackupFileRefs[0].CopiedToCache);
            Assert.True(updatedBackupRun.BackupFileRefs[0].CopiedToArchive);            
        }

        private IClientDBHandler CreateTestDBHandler()
        {
            var config = new Dictionary<string, List<Dictionary<string, string>>>
            {
                ["ConnectionStrings"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["SqliteConnString"] = "Data Source=:memory:" }
                }
            };

            return new SqliteDBHandler(this.GenerateConfiguration(config), new Mock<ILogger<SqliteDBHandler>>().Object); ;
        }

    }
}
