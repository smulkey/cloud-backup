using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudBackupClient.Tests
{
    public abstract class CloudBackupTestBase
    {
        public CloudBackupTestBase()
        {
            var appConfig = new ConfigurationBuilder()
                                      .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                                      .AddJsonFile(this.ConfigurationFileJson, optional: false, reloadOnChange: false).Build();

            this.ServiceProvider = new ServiceCollection()
                                            .AddSingleton<ICloudBackupArchiveProvider>((new Mock<ICloudBackupArchiveProvider>()).Object)
                                            .AddSingleton<IClientDBHandler, SqliteDBHandler>()
                                            .AddSingleton(provider => appConfig)
                                            .AddSingleton<IClientFileCacheHandler, TestFileCacheHandler>()
                                            .AddSingleton<BackupClient>()
                                            .AddLogging(builder => builder.AddConsole())
                                            .BuildServiceProvider();

            var dbProperties = new Dictionary<string, string>();
            dbProperties[nameof(SqliteDBHandler.ConnectionString)] = appConfig.GetSection("ConnectionString").Value;

            ((SqliteDBHandler)this.ServiceProvider.GetService<IClientDBHandler>()).Initialize(dbProperties);
        }

        public IServiceProvider ServiceProvider { get; }

        abstract protected string ConfigurationFileJson { get;  }
    }
}
