using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloudBackupClient.ArchiveProviders
{
    public class FileSystemBackupArchiveProvider : ICloudBackupArchiveProvider
    {
        private string baseBackupDir;
        private IServiceProvider serviceProvider;
        
        public FileSystemBackupArchiveProvider()
        {

        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            var appConfigRoot = this.serviceProvider.GetService<IConfigurationRoot>();
            var configSection = appConfigRoot.GetSection("FileSystemArchiveTestConfig");

            Logger.LogDebug("Called FileSystemBackupArchiveProvider.Initialize");

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

            Logger.LogInformation(String.Format("Configured FileSystemBackupArchiveProvider with base direcory: {0}", baseDir));
        }

        public bool ArchiveFile(BackupRun backupRun, BackupRunFileRef fileRef, Stream cacheFileStream)
        {
            Logger.LogDebug("Called FileSystemBackup.ArchiveFile");

            var archiveFileName = GetArchiveFileName(fileRef, backupRun);
            var dirName = this.FileSystem.GetFileParentDirectoryName(archiveFileName);

            if (this.FileSystem.CheckFileExists(archiveFileName))
            {
                //TODO Check with checksum to potentially skip copy instead of automatically deleting  

                this.FileSystem.DeleteFile(archiveFileName);
            }
            else if (this.FileSystem.CheckFileExists(dirName) == false)
            {
                Logger.LogInformation(String.Format("Creating parent directory for archive entry: {0}", dirName));

                this.FileSystem.CreateDirectory(archiveFileName);
            }

            if (cacheFileStream != null)
            {
                Logger.LogInformation(String.Format("Creating archive entry for file: {0}", fileRef.FullFileName));

                using (var outputFileStream = this.FileSystem.FileInfo.FromFileName(archiveFileName).OpenWrite())
                {
                    cacheFileStream.CopyTo(outputFileStream);
                }                
            }

            Logger.LogInformation(String.Format("Completed archive copy for entry: {0}", fileRef.FullFileName));

            return true;
        }
        
        private string GetArchiveFileName(BackupRunFileRef backupRunFileRef, BackupRun backupRun) => String.Format("{0}{1}{2}{3}{4}",
                                                                                                                    this.baseBackupDir,
                                                                                                                    Path.DirectorySeparatorChar,
                                                                                                                    backupRun.BackupRunID,
                                                                                                                    Path.DirectorySeparatorChar,
                                                                                                                    backupRunFileRef.FullFileName.Substring(3));

        public void Dispose() 
        {
            //no op
        }

        private IFileSystem FileSystem => this.serviceProvider.GetService<IFileSystem>();

        private ILogger Logger => this.serviceProvider.GetService<ILogger<FileSystemBackupArchiveProvider>>();
    }
}
