using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.BackupClientController;
using CloudBackupClient.BackupRunController;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Abstractions;

namespace CloudBackupClient
{
    public class Program
    {   

        static void Main()
        {
            try
            {
                var appConfig = new ConfigurationBuilder()
                                        .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

                var serviceProvider = new ServiceCollection()
                                                .AddSingleton<ICloudBackupArchiveProvider, FileSystemBackupArchiveProvider>()
                                                .AddSingleton<IClientDBHandler, SqliteDBHandler>()                                                
                                                .AddSingleton<IClientFileCacheHandler, LocalClientFileCacheHandler>()
                                                .AddSingleton<IFileSystem, FileSystem>()
                                                .AddSingleton<IBackupRunControl, BackupRunControl>()
                                                .AddSingleton<IBackupFileScanner, BackupFileScanner>()                                                
                                                .AddSingleton<IConfigurationRoot>(provider => appConfig)
                                                .AddLogging(builder => builder.AddConsole())
                                                .BuildServiceProvider();


                serviceProvider.GetService<ICloudBackupArchiveProvider>().Initialize(serviceProvider);                
                serviceProvider.GetService<IClientDBHandler>().Initialize(serviceProvider);
                serviceProvider.GetService<IClientFileCacheHandler>().Initialize(serviceProvider);
                serviceProvider.GetService<IBackupRunControl>().Initialize(serviceProvider);
                serviceProvider.GetService<IBackupFileScanner>().Initialize(serviceProvider);

                var backupClient = new BackupClient(serviceProvider);

                backupClient.Start().GetAwaiter().GetResult();

            } catch(Exception ex)
            {                
                Console.WriteLine("Program fail with exception: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
