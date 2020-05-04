using Xunit;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using CloudBackupClient.Models;
using CloudBackupClient.ClientDBHandlers;

namespace CloudBackupClient.Tests.UnitTests
{
    public class SqliteDBHandler_Tests : CloudBackupTestBase
    {
        protected override string ConfigurationJson => "{\"ConnectionStrings\": { \"SqliteConnString\": \"Data Source=:memory:\"} }";

        public SqliteDBHandler_Tests()
        {

        }
                
        [Fact]
        public void AddBackupRunById()
        {            
            ClientDBHandler.AddBackupRun(this.BackupRun);
                        
            var requestedBackupRun = ClientDBHandler.GetBackupRun(this.BackupRun.BackupRunID);

            Assert.NotNull(requestedBackupRun);
            Assert.Equal(this.BackupRun.BackupRunID, requestedBackupRun.BackupRunID);
            Assert.NotNull(requestedBackupRun.BackupDirectories);
            Assert.Equal(this.BackupRun.BackupDirectories.Count, requestedBackupRun.BackupDirectories.Count);
            Assert.Equal(this.BackupRun.BackupDirectories[0].DirectoryFullFileName, requestedBackupRun.BackupDirectories[0].DirectoryFullFileName);
            Assert.Equal(this.BackupRun.BackupFileRefs.Count, requestedBackupRun.BackupFileRefs.Count);
            Assert.Equal(this.BackupRun.BackupFileRefs[0].FullFileName, requestedBackupRun.BackupFileRefs[0].FullFileName);
        }

        [Fact]
        public void GetBackupRunByIdNotFound()
        {            
            var requestedBackupRun = ClientDBHandler.GetBackupRun(1001);

            Assert.Null(requestedBackupRun);            
        }

        [Fact]
        public void GetOpenBackupRunTest()
        {
            ClientDBHandler.AddBackupRun(this.BackupRun);

            IList<BackupRun> lstBrs = ClientDBHandler.GetOpenBackupRuns();

            Assert.NotNull(lstBrs);
            Assert.Equal(1, lstBrs.Count);
            Assert.Equal(this.BackupRun.BackupRunID, lstBrs[0].BackupRunID);
        }

        [Fact]
        public void UpdateBackupRun()
        {   
            ClientDBHandler.AddBackupRun(this.BackupRun);

            BackupRunFileRef newFileRef = new BackupRunFileRef
            {
                FullFileName = @"C:\Test2\file2.txt"
            };
;
            this.BackupRun.BackupFileRefs.Add(newFileRef);

            var backupRefFileCount = this.BackupRun.BackupFileRefs.Count;

            this.BackupRun.BackupRunStart = DateTime.Now.AddSeconds(-10);
            this.BackupRun.BackupRunEnd = DateTime.Now.AddSeconds(10);
            this.BackupRun.BackupRunCompleted = false;
            this.BackupRun.FailedWithException = true;
            this.BackupRun.ExceptionMessage = "test exception";

            this.BackupRun.BackupRunCompleted = true;

            ClientDBHandler.UpdateBackupRun(this.BackupRun);

            var updatedBackupRun = ClientDBHandler.GetBackupRun(this.BackupRun.BackupRunID);

            Assert.Equal(backupRefFileCount, updatedBackupRun.BackupFileRefs.Count);
            Assert.True(updatedBackupRun.BackupRunCompleted);
            Assert.Equal(this.BackupRun.BackupRunStart, updatedBackupRun.BackupRunStart);
            Assert.Equal(this.BackupRun.BackupRunEnd, updatedBackupRun.BackupRunEnd);            
            Assert.True(updatedBackupRun.FailedWithException);
            Assert.Equal(this.BackupRun.ExceptionMessage, updatedBackupRun.ExceptionMessage);
        }

        [Fact]
        public void UpdateBackupRunFileRefs()
        {            
            ClientDBHandler.AddBackupRun(this.BackupRun);

            var fileRef = this.BackupRun.BackupFileRefs[0];

            fileRef.CopiedToCache = true;
            fileRef.CopiedToArchive = true;

            ClientDBHandler.UpdateBackupFileRef(fileRef);
            
            var updatedBackupRun = ClientDBHandler.GetBackupRun(this.BackupRun.BackupRunID);

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
