using CloudBackupClient.BackupRunController;
using CloudBackupClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace CloudBackupClient.Tests.UnitTests
{
    public class BackupFileScannerTests : CloudBackupTestBase
    {
        public BackupFileScannerTests()
        {
        }
        
        [Fact]
        public void PopulateFilesForBackupRunTest()
        {
            // Given            
            var backupFileScanner = new BackupFileScanner(this.MockFileSystem, new Mock<ILogger<BackupFileScanner>>().Object);

            var backupRun = new BackupRun() 
                                { 
                                    BackupRunID = 1,
                                    BackupClientID = this.TestBackupClientID 
                                };
            
            var backupDirs = new string[]
                                {
                                    @"C:\TestDir1\files",
                                    @"C:\TestDir1\files\morefiles",
                                    @"C:\TestDir2\otherfiles"
                                };

            var backupFiles = new string[]
                {
                    $@"{backupDirs[0]}\testfile1.txt",
                    $@"{backupDirs[0]}\testfile2.txt",
                    $@"{backupDirs[1]}\testfilea.txt",
                    $@"{backupDirs[1]}testfileb.txt",
                    $@"{backupDirs[1]}testfilec.txt",
                    $@"{backupDirs[2]}\testfilez.txt"
                };

            backupRun.BackupDirectories = new List<BackupDirectoryRef>();
            backupRun.BackupFileRefs = new List<BackupRunFileRef>();

            for (int i = 0; i < backupDirs.Length; i++)
            {
                this.MockFileSystem.AddDirectory(backupDirs[i]);
                backupRun.BackupDirectories.Add(new BackupDirectoryRef { DirectoryFullFileName = backupDirs[i] });
            }

            for (int i = 0; i < backupFiles.Length; i++)
            {
                this.MockFileSystem.AddFile(backupFiles[i], new MockFileData(backupFiles[i]));
            }
                                    
            // When
            backupFileScanner.PopulateFilesForBackupRun(backupRun);

            // Then
            Assert.Equal(backupDirs.Length + backupFiles.Length, backupRun.BackupFileRefs.Count);

            var foundFiles = 0;
            bool fileFound;

            foreach (var fileRef in backupRun.BackupFileRefs)
            {
                fileFound = false;
                
                for(int i = 0; i < backupFiles.Length; i++)
                {
                    if (fileRef.FullFileName.Equals(backupFiles[i]))
                    {
                        fileFound = true;
                        break;
                    }
                }

                if( !fileFound )
                {
                    for (int i = 0; i < backupDirs.Length; i++)
                    {
                        if (fileRef.FullFileName.Equals(backupDirs[i]))
                        {
                            fileFound = true;
                            break;
                        }
                    }
                }

                if (fileFound)
                {
                    foundFiles += 1;
                }
            }

            Assert.Equal(backupDirs.Length + backupFiles.Length, foundFiles);
        }

        [Fact]
        public void BackupRunFileCollectorMissingDirectoryTest()
        {
            // Given
            var backupFileScanner = new BackupFileScanner(this.MockFileSystem, new Mock<ILogger<BackupFileScanner>>().Object);

            var backupRun = new BackupRun()
            {
                BackupRunID = 1,
                BackupClientID = this.TestBackupClientID,
                BackupDirectories = new List<BackupDirectoryRef>()
                                        {
                                            new BackupDirectoryRef { DirectoryFullFileName = "" }
                                        }
            };

            // When / Then            
            Assert.Throws<Exception>(() => backupFileScanner.PopulateFilesForBackupRun(backupRun))
                                            .Message.Equals("No root directory provided");
        }

        [Fact]
        public void BackupRunFileCollectorEmptyDirectoryTest()
        {
            // Given
            var backupFileScanner = new BackupFileScanner(this.MockFileSystem, new Mock<ILogger<BackupFileScanner>>().Object);

            var backupRun = new BackupRun()
                                {
                                    BackupRunID = 1,
                                    BackupClientID = this.TestBackupClientID
                                };

            var backupDirs = new string[]
                                {
                                    @"C:\TestDir1\files",
                                    @"C:\TestDir1\files\morefiles"
                                };

            this.MockFileSystem.AddDirectory(backupDirs[0]);

            backupRun.BackupDirectories = new List<BackupDirectoryRef>();

            for (int i = 0; i < backupDirs.Length; i++)
            {
                backupRun.BackupDirectories.Add(new BackupDirectoryRef { DirectoryFullFileName = backupDirs[i] });
            }

            backupRun.BackupFileRefs = new List<BackupRunFileRef>();

            // When / Then            
            Assert.Throws<Exception>(() => backupFileScanner.PopulateFilesForBackupRun(backupRun))
                                                                    .Message.Equals($"Root directory {backupDirs[1]} doesn't exist");
        }
    }    
}
