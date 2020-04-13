using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloudBackupClient.ClientFileCacheHandlers
{
    class LocalClientFileCacheHandler : IClientFileCacheHandler
    {
        private IServiceProvider serviceProvider;

        public LocalClientFileCacheHandler(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void PopulateFilesForBackupRun(BackupRun br)
        {
            List<FileInfo> directoryFiles = new List<FileInfo>();

            Logger.LogInformation("Starting directory scan for backup run");
                        
            foreach (var dirRef in br.BackupDirectories)
            {                
                var rootDir = dirRef.DirectoryFullFileName;

                if (String.IsNullOrWhiteSpace(rootDir))
                {
                    throw new Exception("No root directory provided");
                }
                else if (!Directory.Exists(rootDir))
                {
                    throw new Exception(String.Format("Root directory {0} doesn't exist", rootDir));
                }

                PopulateFileList(rootDir, directoryFiles);
            }

            foreach (FileInfo info in directoryFiles)
            {
                //Make sure file wasn't added in an ealier scan for this run
                if (br.BackupFileRefs.Where<BackupRunFileRef>(r => r.FullFileName.ToLower().Equals(info.FullName.ToLower())).ToList().Count == 0)
                {
                    Logger.LogInformation(String.Format("Adding file ref {0} to backup run ID {1}", info.FullName, br.BackupRunID));
                    br.BackupFileRefs.Add(new BackupRunFileRef { FullFileName = info.FullName });
                }

            }

            string backupCacheFullDir = String.Format("{0}{1}", this.BackupCacheDirectory, br.BackupRunID);

            if (Directory.Exists(backupCacheFullDir) == false)
            {
                Logger.LogInformation(String.Format("Creating new backup cache directory at {0}", backupCacheFullDir));

                Directory.CreateDirectory(backupCacheFullDir);
            }

        }

        private void PopulateFileList(string fileName, List<FileInfo> fileList)
        {
            if (File.Exists(fileName) || Directory.Exists(fileName))
            {
                fileList.Add(new FileInfo(fileName));

                if (Directory.Exists(fileName))
                {
                    foreach (string dirFile in Directory.EnumerateFiles(fileName))
                    {
                        fileList.Add(new FileInfo(dirFile));
                    }

                    foreach (string dirFile in Directory.EnumerateDirectories(fileName))
                    {
                        PopulateFileList(dirFile, fileList);
                    }
                }
            }
        }

        public Stream GetCacheStreamForItem(BackupRunFileRef item, BackupRun br)
        {
            var cacheFile = new FileInfo(GetCacheEntryForFile(item.FullFileName, br));

            if( File.Exists(cacheFile.FullName) == false)
            {
                cacheFile.Create();
            }

            return cacheFile.OpenRead();
        }

        public void CompleteFileArchive(BackupRunFileRef backupRef, BackupRun br)
        {
            var cacheFileName = GetCacheEntryForFile(backupRef.FullFileName, br);

            if (File.Exists(cacheFileName))
            {
                Logger.LogDebug("Deleting cache file after archive: {0}", cacheFileName);
                                
                File.Delete(cacheFileName);
            }
        }


        public void Dispose()
        {
            //noop
        }


        private string GetCacheEntryForFile(string fullFileName, BackupRun br) => String.Format("{0}{1}{2}{3}", this.BackupCacheDirectory, br.BackupRunID, Path.DirectorySeparatorChar, fullFileName.Substring(fullFileName.IndexOf(":") + 1));

        private string BackupCacheDirectory => this.serviceProvider.GetService<IConfigurationRoot>().GetSection("BackupSettings").GetSection("TempCopyDirectory").Value;
        
        private ILogger Logger => this.serviceProvider.GetService<ILogger<LocalClientFileCacheHandler>>();
    }
}
