using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.BackupClientController;
using CloudBackupClient.BackupRunController;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
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

                var serviceProvider = new ServiceCollection();

                Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(appConfig).CreateLogger();

                serviceProvider.AddLogging(builder => builder.AddSerilog(Log.Logger, dispose: true));

                serviceProvider.AddSingleton<ICloudBackupArchiveProvider, FileSystemBackupArchiveProvider>();
                serviceProvider.AddSingleton(typeof(IClientDBHandler), typeof(SqliteDBHandler));
                serviceProvider.AddSingleton(typeof(IClientFileCacheHandler), typeof(LocalClientFileCacheHandler));                
                serviceProvider.AddSingleton<IFileSystem, FileSystem>();
                serviceProvider.AddSingleton<IBackupRunControl, BackupRunControl>();
                serviceProvider.AddSingleton<IBackupFileScanner, BackupFileScanner>();
                serviceProvider.AddSingleton(typeof(BackupClient));
                serviceProvider.AddSingleton(provider => appConfig);
    
                var services = serviceProvider.BuildServiceProvider();
                
                services.GetService<BackupClient>().Start().GetAwaiter().GetResult();

            } catch(Exception ex)
            {                
                Console.WriteLine("Program fail with exception: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
