using CloudBackupClient.BackupClientController;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class BackupRunControlTests : CloudBackupTestBase
    {        
        protected override string ConfigurationJson => $"{{\"BackupSettings\": {{\"BackupClientID\": \"A3B078F9-D348-4483-8FA3-FC0D5BD90DAE\",\"BackupDirectories\": \"{TestBackupDirectory}}}\",\"RunTimeLimitSeconds\": 3600 }}}}";

        private readonly string TestBackupDirectory = @"C:\\TestBackup";

        private readonly Mock<IClientDBHandler> mockDBHandler = new Mock<IClientDBHandler>();

        public BackupRunControlTests()
        {

        }

        [Fact]
        public void GetNextBackupRunNoActiveRunsTest()
        {
            // Given
            this.mockDBHandler.Setup(mock => mock.GetOpenBackupRuns())
                .Returns(new List<BackupRun>());

            this.mockDBHandler.Setup(mock => mock.AddBackupRun(It.IsAny<BackupRun>()));

            this.MockFileSystem.AddDirectory(this.TestBackupDirectory);
            
            // When
            var backupRun = this.BackupRunControl.GetNextBackupRun();

            // Then
            Assert.NotNull(backupRun);
            Assert.Equal(0, backupRun.BackupRunID);            
        }

        [Fact]
        public void GetNextBackupRunExistingRunTest()
        {
            // Given
            var backupRunId = 99;

            this.mockDBHandler.Setup(mock => mock.GetOpenBackupRuns())
                .Returns(new List<BackupRun>()
                {
                    new BackupRun()
                    {                        
                        BackupRunID = backupRunId,
                        BackupClientID = this.TestBackupClientID
                    }
                });
                        
            // When
            var backupRun = this.BackupRunControl.GetNextBackupRun();

            // Then
            Assert.NotNull(backupRun);
            Assert.Equal(backupRunId, backupRun.BackupRunID);
        }

        [Fact]
        public void GetNextBackupRunMultipoleExistingTest()
        {
            // Given
            this.mockDBHandler.Setup(mock => mock.GetOpenBackupRuns())
                .Returns(new List<BackupRun>()
                {
                    new BackupRun()
                    {                        
                        BackupRunID = 1,
                        BackupClientID = this.TestBackupClientID
                    },
                    new BackupRun()
                    {
                        BackupRunID = 2,
                        BackupClientID = this.TestBackupClientID
                    }
                }); ;

            // When / Then
            Assert.Throws<Exception>(() => this.BackupRunControl.GetNextBackupRun())
                .Message.Equals("More than one open backup");
        }
        
        protected override IClientDBHandler ClientDBHandlerTemplate => this.mockDBHandler.Object;

        protected override IBackupRunControl BackupRunControlTemplate => new BackupRunControl();

        private IBackupRunControl BackupRunControl => this.ServiceProvider.GetService<IBackupRunControl>();
    }    

}
