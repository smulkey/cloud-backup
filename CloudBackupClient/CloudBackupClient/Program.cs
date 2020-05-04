using System;
using System.Collections.Generic;
using CloudBackupClient.ArchiveProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using System.IO.Abstractions;

namespace CloudBackupClient
{
    public class Program
    {   

        static void Main(string[] args)
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
                                                .AddSingleton<BackupClient>()
                                                .AddSingleton<IConfigurationRoot>(provider => appConfig)
                                                .AddLogging(builder => builder.AddConsole())
                                                .BuildServiceProvider();


                serviceProvider.GetService<ICloudBackupArchiveProvider>().Initialize(serviceProvider);                
                serviceProvider.GetService<IClientDBHandler>().Initialize(serviceProvider);
                serviceProvider.GetService<IClientFileCacheHandler>().Initialize(serviceProvider);

                serviceProvider.GetService<BackupClient>().Start();

            } catch(Exception ex)
            {                
                Console.WriteLine("Program fail with exception: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }


        
    }
}
