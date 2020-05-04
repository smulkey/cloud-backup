using CloudBackupClient.Models;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;

namespace CloudBackupClient
{
    public static class FileSystemHelper
    {
        public static bool CheckFileRefExists(this IFileSystem fileSystem, BackupRunFileRef fileRef) => fileSystem.FileInfo.FromFileName(fileRef.FullFileName).Exists;

        public static bool CheckFileRefIsDirectory(this IFileSystem fileSystem, BackupRunFileRef fileRef) => fileSystem.DirectoryInfo.FromDirectoryName(fileRef.FullFileName).Exists;

        public static string GetFileParentDirectoryName(this IFileSystem fileSystem, string fileName) => fileSystem.FileInfo.FromFileName(fileName).DirectoryName;

        public static void CreateDirectory(this IFileSystem fileSystem, string dirName) => fileSystem.DirectoryInfo.FromDirectoryName(dirName).Create();

        public static bool CheckFileExists(this IFileSystem fileSystem, string fileName) => fileSystem.FileInfo.FromFileName(fileName).Exists;

        public static void DeleteFile(this IFileSystem fileSystem, string fileName) => fileSystem.FileInfo.FromFileName(fileName).Delete();

        public static void CopyFileRef(this IFileSystem fileSystem, BackupRunFileRef fileRef, string targetFile) => fileSystem.File.Copy(fileRef.FullFileName, targetFile);

        public static long GetFileLength(this IFileSystem fileSystem, string fullFileName) => fileSystem.FileInfo.FromFileName(fullFileName).Length;

        
    }
}
