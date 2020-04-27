using Xunit;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using CloudBackupClient.Models;
using CloudBackupClient.ClientDBHandlers;

namespace CloudBackupClient.Tests.UnitTests
{
    public class SqliteDBHandler_UnitTest : CloudBackupTestBase
    {
        protected override string ConfigurationJson => "{\"ConnectionStrings\": { \"SqliteConnString\": \"Data Source=:memory:\"} }";

        public SqliteDBHandler_UnitTest()
        {

        }

        [Fact]
        private BackupRun CreateBackupRun()
        {
            var dirList = new List<BackupDirectoryRef>
                {
                    new BackupDirectoryRef { DirectoryFullFileName = "C:\\Test1" }
                };

            var backupRun = new BackupRun
                            {
                                BackupDirectories = dirList,
                                BackupFileRefs = new List<BackupRunFileRef>() 
                                {
                                    new BackupRunFileRef { FullFileName = @"C:\Test1\file1.txt" }
                                }
                            };

            return backupRun;
        }

        [Fact]
        public void AddBackupRunById()
        {
            var backupRun = CreateBackupRun();

            dbHandler.AddBackupRun(backupRun);
                        
            var requestedBackupRun = dbHandler.GetBackupRun(backupRun.BackupRunID);

            Assert.NotNull(requestedBackupRun);
            Assert.Equal(backupRun.BackupRunID, requestedBackupRun.BackupRunID);
            Assert.NotNull(requestedBackupRun.BackupDirectories);
            Assert.Equal(backupRun.BackupDirectories.Count, requestedBackupRun.BackupDirectories.Count);
            Assert.Equal(backupRun.BackupDirectories[0].DirectoryFullFileName, requestedBackupRun.BackupDirectories[0].DirectoryFullFileName);
            Assert.Equal(backupRun.BackupFileRefs.Count, requestedBackupRun.BackupFileRefs.Count);
            Assert.Equal(backupRun.BackupFileRefs[0].FullFileName, requestedBackupRun.BackupFileRefs[0].FullFileName);
        }

        [Fact]
        public void GetBackupRunByIdNotFound()
        {            
            var requestedBackupRun = dbHandler.GetBackupRun(1001);

            Assert.Null(requestedBackupRun);            
        }

        [Fact]
        public void QueryOpenBackupRunTest()
        {
            var backupRun = CreateBackupRun();
        
            dbHandler.AddBackupRun(backupRun);

            IList<BackupRun> lstBrs = dbHandler.GetOpenBackupRuns();

            Assert.NotNull(lstBrs);
            Assert.Equal(1, lstBrs.Count);
            Assert.Equal(backupRun.BackupRunID, lstBrs[0].BackupRunID);
        }

        [Fact]
        public void UpdateBackupRun()
        {
            var backupRun = CreateBackupRun();

            dbHandler.AddBackupRun(backupRun);

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

            dbHandler.UpdateBackupRun(backupRun);

            var updatedBackupRun = dbHandler.GetBackupRun(backupRun.BackupRunID);

            Assert.Equal(backupRefFileCount, updatedBackupRun.BackupFileRefs.Count);
            Assert.True(updatedBackupRun.BackupRunCompleted);
            Assert.Equal(updatedBackupRun.BackupRunStart, backupRun.BackupRunStart);
            Assert.Equal(updatedBackupRun.BackupRunEnd, backupRun.BackupRunEnd);            
            Assert.True(updatedBackupRun.FailedWithException);
            Assert.Equal(updatedBackupRun.ExceptionMessage, backupRun.ExceptionMessage);
        }

        [Fact]
        public void UpdateBackupRunFileRefs()
        {
            var backupRun = CreateBackupRun();

            dbHandler.AddBackupRun(backupRun);

            var fileRef = backupRun.BackupFileRefs [0];

            fileRef.CopiedToCache = true;
            fileRef.CopiedToArchive = true;

            dbHandler.UpdateBackupFileRef(fileRef);
            
            var updatedBackupRun = dbHandler.GetBackupRun(backupRun.BackupRunID);

            Assert.True(updatedBackupRun.BackupFileRefs[0].CopiedToCache);
            Assert.True(updatedBackupRun.BackupFileRefs[0].CopiedToArchive);            
        }

        override public void Dispose()
        {
            this.ServiceProvider.GetService<IClientDBHandler>().Dispose();
        }

        override protected IClientDBHandler ClientDBHandler => new SqliteDBHandler();

        private IClientDBHandler dbHandler => this.ServiceProvider.GetService<IClientDBHandler>();
    }


}
