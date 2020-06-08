using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace CloudBackupClient.Tests
{
    public abstract class CloudBackupTestBase
    {
        protected readonly Guid TestBackupClientID = new Guid("A69FFFE3-BD3B-447F-9791-50CFF5E0D7A4");

        private readonly MockFileSystem mockFileSystem;

        public CloudBackupTestBase()
        { 
            this.mockFileSystem = new MockFileSystem();
        }

        protected BackupRun CreateBackupRun()
        {
            return this.CreateBackupRun(this.MockFileSystem);
        }

        protected BackupRun CreateBackupRun(MockFileSystem mockFileSystem)
        {
            var backupRun = new BackupRun()
            {
                BackupClientID = this.TestBackupClientID,
                BackupDirectories = new List<BackupDirectoryRef>
                                {
                                    new BackupDirectoryRef { DirectoryFullFileName = @"\CloudBackupSource\Dir1" },
                                    new BackupDirectoryRef { DirectoryFullFileName = @"\CloudBackupSource\Dir2" }
                                },
                BackupFileRefs = new List<BackupRunFileRef>
                                {
                                    new BackupRunFileRef { FullFileName = @"\CloudBackupSource\Dir1\file1.txt" },
                                    new BackupRunFileRef { FullFileName = @"\CloudBackupSource\Dir1\file2.txt" },
                                    new BackupRunFileRef { FullFileName = @"\CloudBackupSource\Dir2\filea.out" },
                                    new BackupRunFileRef { FullFileName = @"\CloudBackupSource\Dir2\fileb.data"},                                    
                                }
            };

            foreach (var backupDir in backupRun.BackupDirectories)
            {
                mockFileSystem.AddDirectory(backupDir.DirectoryFullFileName);
            }

            foreach (var backupFileRef in backupRun.BackupFileRefs)
            {
                if (backupFileRef.FullFileName.Contains("Dir1"))
                {
                    mockFileSystem.AddFile(backupFileRef.FullFileName, new MockFileData(backupFileRef.FullFileName.ToUpper()));
                }
                else
                {
                    mockFileSystem.AddFile(backupFileRef.FullFileName, new MockFileData(Encoding.UTF8.GetBytes(backupFileRef.FullFileName.ToLower())));
                }
            }

            return backupRun;
        }
                
        protected IConfiguration GenerateConfiguration(Dictionary<string, List<Dictionary<string, string>>> config)
        {
            Mock<IConfiguration> configurationRoot = new Mock<IConfiguration>();


            foreach (var section in config.Keys)
            {
                var values = config[section];

                Mock<IConfigurationSection> configurationSection = new Mock<IConfigurationSection>();

                foreach (var valueSet in values)
                {
                    foreach (var valueEntry in valueSet)
                    {
                        configurationSection.Setup(mock => mock[It.Is<string>(s => s == valueEntry.Key)]).Returns(valueEntry.Value);
                    }
                }

                configurationRoot.Setup(mock => mock.GetSection(section)).Returns(configurationSection.Object);
            }

            return configurationRoot.Object;
        }
       
        virtual protected MockFileSystem MockFileSystem => this.mockFileSystem;
    }
}
