using System;
using System.Collections.Generic;
using CloudBackupClient.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;

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
                                                .AddSingleton<IConfigurationRoot>(provider => appConfig)
                                                .AddSingleton<IClientFileCacheHandler, LocalClientFileCacheHandler>()
                                                .AddSingleton<BackupClient>()
                                                .AddLogging(builder => builder.AddConsole())
                                                .BuildServiceProvider();

                //TODO Get from properties file
                var dbProperties = new Dictionary<string, string>();
                dbProperties[nameof(SqliteDBHandler.ConnectionString)] = serviceProvider.GetService<IConfigurationRoot>().GetSection("LocalDBTestConfig").GetSection("BackupRunConnStr").Value;
                                
                ((SqliteDBHandler)serviceProvider.GetService<IClientDBHandler>()).Initialize(dbProperties);

                serviceProvider.GetService<BackupClient>().Start();

            } catch(Exception ex)
            {                
                Console.WriteLine("Program fail with exception: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }


        
    }
}
