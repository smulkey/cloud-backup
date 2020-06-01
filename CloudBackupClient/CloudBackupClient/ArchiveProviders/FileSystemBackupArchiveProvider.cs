using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

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
            else if (this.FileSystem.CheckFileExists(baseDir) == false)
            {
                this.FileSystem.CreateDirectory(baseDir);                
            }

            this.baseBackupDir = baseDir;

            Logger.LogInformation(String.Format("Configured FileSystemBackupArchiveProvider with base direcory: {0}", baseDir));
        }

        public async Task<bool> ArchiveFileAsync(BackupRun backupRun, BackupRunFileRef fileRef, Stream cacheFileStream)
        {
            Logger.LogDebug("Called FileSystemBackup.ArchiveFile");

            var archiveFileName = GetArchiveFileName(fileRef, backupRun);

            //// TODO: Find way to archive empty directories
            //if( this.FileSystem.CheckFileIsDirectory(archiveFileName) )
            //{
            //    return true;
            //}

            var dirName = this.FileSystem.GetFileParentDirectoryName(archiveFileName);

            if (this.FileSystem.CheckFileExists(archiveFileName))
            {
                //TODO Check with checksum to potentially skip copy instead of automatically deleting  

                this.FileSystem.DeleteFile(archiveFileName);
            }
            else if (this.FileSystem.CheckFileExists(dirName) == false)
            {
                Logger.LogInformation(String.Format("Creating parent directory for archive entry: {0}", dirName));

                this.FileSystem.CreateDirectory(dirName);                
            }

            if (cacheFileStream != null)
            {
                Logger.LogInformation(String.Format("Creating archive entry for file: {0}", fileRef.FullFileName));
                
                using (var outputFileStream = this.FileSystem.CreateFile(archiveFileName))
                {
                    await cacheFileStream.CopyToAsync(outputFileStream);
                }                
            }

            Logger.LogInformation(String.Format("Completed archive copy for entry: {0}", fileRef.FullFileName));

            return true;
        }
        
        //TODO Share for testing
        public string GetArchiveFileName(BackupRunFileRef backupRunFileRef, BackupRun backupRun) => String.Format("{0}{1}{2}{3}{4}",
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
