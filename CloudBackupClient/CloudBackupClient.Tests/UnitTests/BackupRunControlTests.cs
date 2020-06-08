using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.BackupClientController;
using CloudBackupClient.BackupRunController;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using CloudBackupClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class BackupRunControlTests : CloudBackupTestBase
    {   
        private readonly string TestBackupDirectory = @"C:\\TestBackup";

        private readonly Mock<IClientDBHandler> mockDBHandler = new Mock<IClientDBHandler>();

        public BackupRunControlTests()
        {

        }

        [Fact]
        public void GetNextBackupRunNoActiveRunsTest()
        {
            // Given
            var backupRunControl = CreateTestBackupRunControl();

            this.mockDBHandler.Setup(mock => mock.GetOpenBackupRuns())
                .Returns(new List<BackupRun>());

            this.mockDBHandler.Setup(mock => mock.AddBackupRun(It.IsAny<BackupRun>()));

            this.MockFileSystem.AddDirectory(this.TestBackupDirectory);
            
            // When
            var backupRun = backupRunControl.GetNextBackupRun();

            // Then
            Assert.NotNull(backupRun);
            Assert.Equal(0, backupRun.BackupRunID);            
        }

        [Fact]
        public void GetNextBackupRunExistingRunTest()
        {
            // Given
            var backupRunControl = CreateTestBackupRunControl();

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
            var backupRun = backupRunControl.GetNextBackupRun();

            // Then
            Assert.NotNull(backupRun);
            Assert.Equal(backupRunId, backupRun.BackupRunID);
        }

        [Fact]
        public void GetNextBackupRunMultipoleExistingTest()
        {
            // Given
            var backupRunControl = CreateTestBackupRunControl();

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
            Assert.Throws<Exception>(() => backupRunControl.GetNextBackupRun())
                .Message.Equals("More than one open backup");
        }

        private IBackupRunControl CreateTestBackupRunControl()
        {
            var config = new Dictionary<string, List<Dictionary<string, string>>>
            {
                ["BackupSettings"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["BackupClientID"] = "678385F0-AB93-434A-989D-9CD2649A457E" },
                    new Dictionary<string, string> { ["BackupDirectories"] = @"C:\\TestBackup" },
                    new Dictionary<string, string> { ["RunTimeLimitSeconds"] = "3600" },
                }
            };

            return new BackupRunControl(new Mock<IBackupFileScanner>().Object,
                                        this.mockDBHandler.Object, 
                                        new Mock<IClientFileCacheHandler>().Object, 
                                        new Mock<ICloudBackupArchiveProvider>().Object, 
                                        GenerateConfiguration(config), 
                                        this.MockFileSystem, 
                                        new Mock<Microsoft.Extensions.Logging.ILogger<BackupRunControl>>().Object);
        }
    }    

}
