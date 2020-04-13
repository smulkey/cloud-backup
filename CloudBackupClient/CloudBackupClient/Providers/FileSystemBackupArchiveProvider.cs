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
            if( false == isInitialized )
            {
                this.Initialize();
            }

            Logger.LogDebug("Called FileSystemBackup.ArchiveFile");
            
            //TODO Move direcory check
            //Don't copy directories            
            //if (Directory.Exists(cacheFile.FullName))
            //{
            //    Logger.LogInformation(String.Format("Skipping archive for directory reference: {0}", fileRef.FullFileName));

            //    return true;
            //}
                                                           
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

            byte[] buffer = new byte[1024];
            int offset = 0;
            int count;

            while((count = cacheFileStream.Read(buffer, offset, buffer.Length)) > 0)
            {
                offset += count;  
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
            else if(Directory.Exists(baseDir) == false)
            {
                Directory.CreateDirectory(baseDir);
            }

            this.baseBackupDir = baseDir;

            Logger.LogInformation(String.Format("Configured FileSystemBackup with base direcory: {0}", baseDir));

            this.isInitialized = true;
        }

        private ILogger Logger => this.serviceProvider.GetService<ILogger<FileSystemBackupArchiveProvider>>();
    }
}
