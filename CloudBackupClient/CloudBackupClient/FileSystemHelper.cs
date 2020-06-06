using CloudBackupClient.Models;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace CloudBackupClient
{
    public static class FileSystemHelper
    {
        public static bool CheckFileRefExists(this IFileSystem fileSystem, BackupRunFileRef fileRef) => fileSystem.FileInfo.FromFileName(fileRef.FullFileName).Exists;

        public static bool CheckFileRefIsDirectory(this IFileSystem fileSystem, BackupRunFileRef fileRef) => fileSystem.DirectoryInfo.FromDirectoryName(fileRef.FullFileName).Exists;

        public static string GetFileParentDirectoryName(this IFileSystem fileSystem, string fileName) => fileSystem.FileInfo.FromFileName(fileName).DirectoryName;

        public static void CreateDirectory(this IFileSystem fileSystem, string dirName) => fileSystem.DirectoryInfo.FromDirectoryName(dirName).Create();

        public static bool CheckFileExists(this IFileSystem fileSystem, string fileName) => fileSystem.FileInfo.FromFileName(fileName).Exists;

        public static bool CheckFileIsDirectory(this IFileSystem fileSystem, string directoryPath) => fileSystem.DirectoryInfo.FromDirectoryName(directoryPath).Exists;
        
        public static IList<string> GetChildDirectories(this IFileSystem fileSystem, string directoryPath)
        {
            var childDirs = new List<string>();
          
            foreach (var dirInfo in fileSystem.DirectoryInfo.FromDirectoryName(directoryPath).GetDirectories())
            {
                childDirs.Add(dirInfo.FullName);
            }

            return childDirs;
        }

        public static IList<string> GetFilesInDirectory(this IFileSystem fileSystem, string directoryPath)
        {
            var childFiles = new List<string>();
                        
            foreach (var fileInfo in fileSystem.DirectoryInfo.FromDirectoryName(directoryPath).GetFiles())
            {
                childFiles.Add(fileInfo.FullName);
            }

            return childFiles;
        }

        public static void DeleteFile(this IFileSystem fileSystem, string fileName) => fileSystem.File.Delete(fileName);

        public static void CopyFileRef(this IFileSystem fileSystem, BackupRunFileRef fileRef, string targetFile) => fileSystem.File.Copy(fileRef.FullFileName, targetFile);

        public static long GetFileLength(this IFileSystem fileSystem, string fullFileName) => fileSystem.FileInfo.FromFileName(fullFileName).Length;

        public static Stream  CreateFile(this IFileSystem fileSystem, string fullFileName) => fileSystem.File.Create(fullFileName);

        public static Stream OpenRead(this IFileSystem fileSystem, string fullFileName) => fileSystem.File.OpenRead(fullFileName);

        public static Stream OpenWrite(this IFileSystem fileSystem, string fullFileName) => fileSystem.File.OpenWrite(fullFileName);


    }
}
