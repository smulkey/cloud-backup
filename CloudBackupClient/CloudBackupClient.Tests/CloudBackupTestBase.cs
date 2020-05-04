using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using CloudBackupClient.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace CloudBackupClient.Tests
{
    public abstract class CloudBackupTestBase : IDisposable
    {
        virtual protected BackupRun BackupRun { get; set; }

        public CloudBackupTestBase()
        {   
            byte[] configBytes = System.Text.Encoding.UTF8.GetBytes(this.ConfigurationJson ?? "{}");
                            
            this.ServiceProvider = new ServiceCollection()
                                                .AddSingleton<ICloudBackupArchiveProvider>(this.CloudBackupArchiveProviderTemplate ?? new Mock<ICloudBackupArchiveProvider>().Object)
                                                .AddSingleton<IClientDBHandler>(this.ClientDBHandlerTemplate ?? new Mock<IClientDBHandler>().Object)
                                                .AddSingleton<IClientFileCacheHandler>(this.ClientFileCacheHandlerTemplate ?? new Mock<IClientFileCacheHandler>().Object)
                                                .AddSingleton<IFileSystem>(new MockFileSystem())
                                                .AddSingleton<BackupClient>()
                                                .AddSingleton<IConfigurationRoot>(provider => new ConfigurationBuilder()
                                                                .AddJsonStream(new MemoryStream(configBytes))
                                                                .Build())
                                                .AddLogging(builder => builder.AddConsole())
                                                .BuildServiceProvider();

            this.ServiceProvider.GetService<ICloudBackupArchiveProvider>().Initialize(this.ServiceProvider);
            this.ServiceProvider.GetService<IClientDBHandler>().Initialize(this.ServiceProvider);
            this.ServiceProvider.GetService<IClientFileCacheHandler>().Initialize(this.ServiceProvider);

            this.CreateBackupRun();
        }

        private void CreateBackupRun()
        {
            this.BackupRun = new BackupRun()
            {
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

            foreach (var backupDir in this.BackupRun.BackupDirectories)
            {
                this.MockFileSystem.AddDirectory(backupDir.DirectoryFullFileName);
            }

            foreach (var backupFileRef in this.BackupRun.BackupFileRefs)
            {
                if (backupFileRef.FullFileName.Contains("Dir1"))
                {
                    this.MockFileSystem.AddFile(backupFileRef.FullFileName, new MockFileData(backupFileRef.FullFileName.ToUpper()));
                }
                else
                {
                    this.MockFileSystem.AddFile(backupFileRef.FullFileName, new MockFileData(System.Text.Encoding.UTF8.GetBytes(backupFileRef.FullFileName.ToLower())));
                }
            }
        }

        virtual public void Dispose()
        {

        }
                
        protected IServiceProvider ServiceProvider { get; set; }

        abstract protected string ConfigurationJson { get; }

        virtual protected ICloudBackupArchiveProvider CloudBackupArchiveProviderTemplate { get; }

        virtual protected IClientDBHandler ClientDBHandlerTemplate { get; }

        virtual protected IClientFileCacheHandler ClientFileCacheHandlerTemplate { get; }

        virtual protected MockFileSystem MockFileSystem => (MockFileSystem)this.ServiceProvider.GetService<IFileSystem>();
    }

}
