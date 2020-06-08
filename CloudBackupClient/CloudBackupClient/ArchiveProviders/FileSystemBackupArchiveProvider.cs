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
                
        private readonly IFileSystem fileSystem;

        private readonly ILogger<FileSystemBackupArchiveProvider> logger;

        private const string BaseBackupDirKey = "BaseBackupDir";

        private const string FileSystemArchiveTestConfigKey = "FileSystemArchiveTestConfig";

        public FileSystemBackupArchiveProvider(IConfiguration configuration,
                                               IFileSystem fileSystem,
                                               ILogger<FileSystemBackupArchiveProvider> logger)
        {
            this.fileSystem = fileSystem;
            this.logger = logger;
        
            var configSection = configuration.GetSection(FileSystemArchiveTestConfigKey);

            this.logger.LogDebug("Called FileSystemBackupArchiveProvider.Initialize");

            var baseDir = configSection[BaseBackupDirKey];

            if (String.IsNullOrWhiteSpace(baseDir))
            {
                throw new Exception("Base backup directory null or empty");
            }
            else if (this.fileSystem.CheckFileExists(baseDir) == false)
            {
                this.fileSystem.CreateDirectory(baseDir);                
            }

            this.baseBackupDir = baseDir;

            this.logger.LogInformation(String.Format("Configured FileSystemBackupArchiveProvider with base direcory: {0}", baseDir));
        }

        public async Task<bool> ArchiveFileAsync(BackupRun backupRun, BackupRunFileRef fileRef, Stream cacheFileStream)
        {
            this.logger.LogDebug("Called FileSystemBackup.ArchiveFile");

            var archiveFileName = GetArchiveFileName(fileRef, backupRun);

            //// TODO: Find way to archive empty directories
            //if( this.fileSystem.CheckFileIsDirectory(archiveFileName) )
            //{
            //    return true;
            //}

            var dirName = this.fileSystem.GetFileParentDirectoryName(archiveFileName);

            if (this.fileSystem.CheckFileExists(archiveFileName))
            {
                //TODO Check with checksum to potentially skip copy instead of automatically deleting  

                this.fileSystem.DeleteFile(archiveFileName);
            }
            else if (this.fileSystem.CheckFileExists(dirName) == false)
            {
                this.logger.LogInformation(String.Format("Creating parent directory for archive entry: {0}", dirName));

                this.fileSystem.CreateDirectory(dirName);                
            }

            if (cacheFileStream != null)
            {
                this.logger.LogInformation(String.Format("Creating archive entry for file: {0}", fileRef.FullFileName));
                
                using (var outputFileStream = this.fileSystem.CreateFile(archiveFileName))
                {
                    await cacheFileStream.CopyToAsync(outputFileStream);
                }                
            }

            this.logger.LogInformation(String.Format("Completed archive copy for entry: {0}", fileRef.FullFileName));

            return true;
        }
        
        //TODO Share for testing
        public string GetArchiveFileName(BackupRunFileRef backupRunFileRef, BackupRun backupRun) => String.Format("{0}{1}{2}{3}{4}",
                                                                                                                    this.baseBackupDir,
                                                                                                                    Path.DirectorySeparatorChar,
                                                                                                                    backupRun.BackupRunID,
                                                                                                                    Path.DirectorySeparatorChar,
                                                                                                                    backupRunFileRef.FullFileName.Substring(3));        
    }
}
