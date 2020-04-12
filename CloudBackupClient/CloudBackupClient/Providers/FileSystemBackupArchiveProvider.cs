using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloudBackupClient.Providers
{
    public class FileSystemBackupArchiveProvider : CloudBackupArchiveProvider
    {
        private bool isInitialized;
        private string baseBackupDir;

        ILogger logger;

        public bool ArchiveFile(BackupRun backupRun, BackupRunFileRef fileRef, FileInfo cacheFile)
        {
            Logger.LogDebug("Called FileSystemBackup.ArchiveFile");
            //TODO Add logging

            //Don't copy directories
            if (Directory.Exists(cacheFile.FullName))
            {
                Logger.LogInformation(String.Format("Skipping archive for directory reference: {0}", fileRef.FullFileName));

                return true;
            }
                                                           
            FileInfo archiveFile = new FileInfo(String.Format("{0}{1}{2}{3}{4}", 
                                                this.baseBackupDir, 
                                                Path.DirectorySeparatorChar, 
                                                backupRun.BackupRunID, 
                                                Path.DirectorySeparatorChar, 
                                                fileRef.FullFileName.Substring(3)));

            if (archiveFile.Exists)
            {
                Logger.LogInformation(String.Format("Deleting existing archive entry for file: {0}", fileRef.FullFileName));

                archiveFile.Delete();
            }

            if(archiveFile.Directory.Exists == false)
            {
                Logger.LogInformation(String.Format("Creating parent directory for archive entry: {0}", archiveFile.Directory.FullName));

                archiveFile.Directory.Create();
            }
                                    
            File.Copy(cacheFile.FullName, archiveFile.FullName);

            Logger.LogInformation(String.Format("Completed archive copy for entry: {0} with result: {1}", fileRef.FullFileName, archiveFile.Exists));

            return true;
       }

        public void Configure(IConfigurationSection configSection)
        {
            Logger.LogDebug("Called FileSystemBackup.Config");

            var baseDir = configSection.GetSection("BaseBackupDir").Value;

            if (String.IsNullOrWhiteSpace(baseDir))
            {
                throw new Exception("Base backup directory null or empty");                
            }
            else if(Directory.Exists(baseDir) == false)
            {
                Directory.CreateDirectory(baseDir);
            }

            this.baseBackupDir = baseDir;

            Logger.LogInformation(String.Format("Configured FileSystemBackup with base direcory: {0}", baseDir));

            this.isInitialized = true;
        }

        public bool Initialized
        {
            get { return isInitialized; }
        }

        private ILogger Logger
        {
            get
            {
                if (this.logger == null)
                {

                    var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder
                            .AddFilter("Microsoft", LogLevel.Warning)
                            .AddFilter("System", LogLevel.Warning)
                            .AddFilter("CloudBackupClient.Providers.FileSystemBackupArchiveProvider", LogLevel.Debug)
                            .AddConsole();
                    });

                    this.logger = loggerFactory.CreateLogger<BackupClient>();
                }

                return this.logger;
            }
        }
    }
}
