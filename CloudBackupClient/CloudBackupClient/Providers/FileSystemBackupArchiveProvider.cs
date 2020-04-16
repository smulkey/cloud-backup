using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloudBackupClient.Providers
{
    public class FileSystemBackupArchiveProvider : ICloudBackupArchiveProvider
    {
        private bool isInitialized;
        private string baseBackupDir;
        private IServiceProvider serviceProvider;

        public FileSystemBackupArchiveProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public bool ArchiveFile(BackupRun backupRun, BackupRunFileRef fileRef, Stream cacheFileStream)
        {

            if (false == isInitialized)
            {
                this.Initialize();
            }

            Logger.LogDebug("Called FileSystemBackup.ArchiveFile");

            FileInfo archiveFile = GetArchiveFile(backupRun, fileRef);

            if (archiveFile.Directory.Exists == false)
            {
                Logger.LogInformation(String.Format("Creating parent directory for archive entry: {0}", archiveFile.Directory.FullName));

                archiveFile.Directory.Create();
            }

            if (cacheFileStream != null)
            {
                Logger.LogInformation(String.Format("Creating archive entry for file: {0}", fileRef.FullFileName));

                //TODO Check with checksum to potentially skip copy                

                using (var outputFileStream = new FileStream(archiveFile.FullName, FileMode.Create))
                {
                    cacheFileStream.CopyTo(outputFileStream);
                }                
            }

            Logger.LogInformation(String.Format("Completed archive copy for entry: {0} with result: {1}", fileRef.FullFileName, archiveFile.Exists));

            return true;
        }

        public void Initialize()
        {
            var appConfigRoot = this.serviceProvider.GetService<IConfigurationRoot>();
            var configSection = appConfigRoot.GetSection("FileSystemArchiveTestConfig");

            Logger.LogDebug("Called FileSystemBackup.Config");

            var baseDir = configSection.GetSection("BaseBackupDir").Value;

            if (String.IsNullOrWhiteSpace(baseDir))
            {
                throw new Exception("Base backup directory null or empty");
            }
            else if (Directory.Exists(baseDir) == false)
            {
                Directory.CreateDirectory(baseDir);
            }

            this.baseBackupDir = baseDir;

            Logger.LogInformation(String.Format("Configured FileSystemBackup with base direcory: {0}", baseDir));

            this.isInitialized = true;
        }

        private FileInfo GetArchiveFile(BackupRun backupRun, BackupRunFileRef backupRunFileRef) => new FileInfo(String.Format("{0}{1}{2}{3}{4}",
                                                                                                                    this.baseBackupDir,
                                                                                                                    Path.DirectorySeparatorChar,
                                                                                                                    backupRun.BackupRunID,
                                                                                                                    Path.DirectorySeparatorChar,
                                                                                                                    backupRunFileRef.FullFileName.Substring(3)));
        private ILogger Logger => this.serviceProvider.GetService<ILogger<FileSystemBackupArchiveProvider>>();
    }
}
