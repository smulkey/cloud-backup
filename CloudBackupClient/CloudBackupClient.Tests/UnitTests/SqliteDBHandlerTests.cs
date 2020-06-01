using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class SqliteDBHandlerTests : CloudBackupTestBase
    {
        protected override string ConfigurationJson => "{\"ConnectionStrings\": { \"SqliteConnString\": \"Data Source=:memory:\"} }";

        public SqliteDBHandlerTests()
        {

        }
                
        [Fact]
        public void AddBackupRunById()
        {            
            var backupRun = this.CreateBackupRun();

            ClientDBHandler.AddBackupRun(backupRun);
                        
            var requestedBackupRun = ClientDBHandler.GetBackupRun(backupRun.BackupRunID);

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
            var requestedBackupRun = ClientDBHandler.GetBackupRun(1001);

            Assert.Null(requestedBackupRun);            
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetOpenBackupRunTest(bool isException)
        {
            // Given
            var completedBackupRun = this.CreateBackupRun();

            this.ClientDBHandler.AddBackupRun(completedBackupRun);

            completedBackupRun.BackupRunStart = DateTime.Now.AddSeconds(-1);
            completedBackupRun.BackupRunCompleted = true;
            completedBackupRun.FailedWithException = isException;            
            completedBackupRun.BackupRunEnd = DateTime.Now;
            
            if (isException)            
            {
                completedBackupRun.ExceptionMessage = "Exception happened";
            }

            this.ClientDBHandler.UpdateBackupRun(completedBackupRun);            

            var openBackupRun = this.CreateBackupRun();

            this.ClientDBHandler.AddBackupRun(openBackupRun);

            // When
            IList<BackupRun> lstBrs = ClientDBHandler.GetOpenBackupRuns();
                        
            // Then
            Assert.NotNull(lstBrs);
            Assert.Equal(1, lstBrs.Count);
            Assert.Equal(openBackupRun.BackupRunID, lstBrs.First<BackupRun>().BackupRunID);
        }

        [Fact]
        public void UpdateBackupRun()
        {
            var backupRun = this.CreateBackupRun();

            ClientDBHandler.AddBackupRun(backupRun);

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

            ClientDBHandler.UpdateBackupRun(backupRun);

            var updatedBackupRun = ClientDBHandler.GetBackupRun(backupRun.BackupRunID);

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
            var backupRun = this.CreateBackupRun();

            ClientDBHandler.AddBackupRun(backupRun);

            var fileRef = backupRun.BackupFileRefs[0];

            fileRef.CopiedToCache = true;
            fileRef.CopiedToArchive = true;

            ClientDBHandler.UpdateBackupFileRef(fileRef);
            
            var updatedBackupRun = ClientDBHandler.GetBackupRun(backupRun.BackupRunID);

            Assert.True(updatedBackupRun.BackupFileRefs[0].CopiedToCache);
            Assert.True(updatedBackupRun.BackupFileRefs[0].CopiedToArchive);            
        }

        override public void Dispose()
        {
            this.ClientDBHandler.Dispose();
        }

        override protected IClientDBHandler ClientDBHandlerTemplate => new SqliteDBHandler();

        private IClientDBHandler ClientDBHandler => this.ServiceProvider.GetService<IClientDBHandler>();
    }


}
