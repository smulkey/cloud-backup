using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using CloudBackupClient.Models;
using CloudBackupClient.ClientDBHandlers;

namespace CloudBackupClient.Tests.ComponentTests
{
    [TestFixture]
    public class SqliteDBHandler_Test : CloudBackupTestBase
    {
        protected override string ConfigurationFileJson => "appsettings_test1.json";

        public void TestSetup()
        {

        }

        [Test]
        public void BackupRunDataTest()
        {

            var dirList = new List<BackupDirectoryRef>
            {
                new BackupDirectoryRef { DirectoryFullFileName = "C:\\Test1" },
                new BackupDirectoryRef { DirectoryFullFileName = "C:\\Test2" }
            };

            BackupRun br1 = new BackupRun
            {
                BackupDirectories = dirList,
                BackupFileRefs = new List<BackupRunFileRef>()
                
            };

            var dbHandler = this.ServiceProvider.GetService<IClientDBHandler>();

            dbHandler.AddBackupRun(br1);

            IList<BackupRun> lstBrs = dbHandler.GetOpenBackupRuns();

            Assert.AreEqual(lstBrs.Count, 1);

            var backupRun = lstBrs[0];

            Assert.AreEqual(backupRun.BackupDirectories.Count, 2);
            Assert.AreEqual(backupRun.BackupDirectories[0].BackupRunID, backupRun.BackupRunID);
            Assert.AreEqual(backupRun.BackupDirectories[1].BackupRunID, backupRun.BackupRunID);
            Assert.AreEqual(backupRun.BackupDirectories[0].DirectoryFullFileName, dirList[0].DirectoryFullFileName);
            Assert.AreEqual(backupRun.BackupDirectories[1].DirectoryFullFileName, dirList[1].DirectoryFullFileName);

            var fileName1 = "C:\\Test1\\file1.txt";
            var fileName2 = "C:\\Test2\\file2.data";

            BackupRunFileRef fileRef1 = new BackupRunFileRef
            {
                FullFileName = fileName1
            };

            BackupRunFileRef fileRef2 = new BackupRunFileRef
            {
                FullFileName = fileName2
            };

            backupRun.BackupFileRefs.Add(fileRef1);
            backupRun.BackupFileRefs.Add(fileRef2);

            dbHandler.UpdateBackupRun(backupRun);

            var updatedBackupRun = dbHandler.GetBackupRun(backupRun.BackupRunID);

            Assert.AreEqual(updatedBackupRun.BackupFileRefs.Count, 2);
            Assert.AreEqual(updatedBackupRun.BackupFileRefs[0].FullFileName, fileName1);
            Assert.AreEqual(updatedBackupRun.BackupFileRefs[1].FullFileName, fileName2);

            var updatedBackupFileRun1 = updatedBackupRun.BackupFileRefs[0];
            var updatedBackupFileRun2 = updatedBackupRun.BackupFileRefs[1];

            updatedBackupFileRun1.CopiedToCache = true;
            
            updatedBackupFileRun2.CopiedToCache = true;
            updatedBackupFileRun2.CopiedToArchive = true;

            dbHandler.UpdateBackupFileRef(updatedBackupFileRun1);
            dbHandler.UpdateBackupFileRef(updatedBackupFileRun2);

            updatedBackupRun = dbHandler.GetBackupRun(updatedBackupRun.BackupRunID);

            Assert.AreEqual(updatedBackupRun.BackupFileRefs[0].FullFileName, updatedBackupFileRun1.FullFileName);
            Assert.AreEqual(updatedBackupRun.BackupFileRefs[1].FullFileName, updatedBackupFileRun2.FullFileName);

            updatedBackupRun.BackupRunStart = DateTime.Now.AddSeconds(-10);
            updatedBackupRun.BackupRunEnd = DateTime.Now.AddSeconds(10);

            updatedBackupRun.BackupRunCompleted = true;
            
            var finalBackupRun1 = dbHandler.GetBackupRun(updatedBackupRun.BackupRunID);

            //TODO replace with member compare
            Assert.AreEqual(updatedBackupRun, finalBackupRun1);

            finalBackupRun1.BackupRunCompleted = false;
            finalBackupRun1.FailedWithException = true;
            finalBackupRun1.ExceptionMessage = "test exception";

            var finalBackupRun2 = dbHandler.GetBackupRun(updatedBackupRun.BackupRunID);

            //TODO replace with member compare
            Assert.AreEqual(updatedBackupRun, finalBackupRun2);
        }    
    }
}
