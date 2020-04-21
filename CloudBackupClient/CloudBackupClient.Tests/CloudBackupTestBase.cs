using CloudBackupClient.ArchiveProviders;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.ClientFileCacheHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CloudBackupClient.Tests
{
    public abstract class CloudBackupTestBase
    {
        public CloudBackupTestBase()
        {   
            
        }

        protected void Initialize()
        {
            this.ServiceProvider = new ServiceCollection()
                                                .AddSingleton<ICloudBackupArchiveProvider>(this.CloudBackupArchiveProvider ?? new Mock<ICloudBackupArchiveProvider>().Object)
                                                .AddSingleton<IClientDBHandler>(this.ClientDBHandler ?? new Mock<IClientDBHandler>().Object)
                                                .AddSingleton<IClientFileCacheHandler>(this.ClientFileCacheHandler ?? new Mock<IClientFileCacheHandler>().Object)
                                                .AddSingleton<BackupClient>()
                                                .AddSingleton<IConfigurationRoot>(provider => new ConfigurationBuilder()
                                                                                                .AddJsonFile(new InMemoryFileProvider(this.ConfigurationJson), "appsettings.json", false, false)
                                                                                                .Build())
                                                .AddLogging(builder => builder.AddConsole())
                                                .BuildServiceProvider();

            this.ServiceProvider.GetService<ICloudBackupArchiveProvider>().Initialize(this.ServiceProvider);
            this.ServiceProvider.GetService<IClientDBHandler>().Initialize(this.ServiceProvider);
            this.ServiceProvider.GetService<IClientFileCacheHandler>().Initialize(this.ServiceProvider);
        }

        protected IServiceProvider ServiceProvider { get; set; }

        abstract protected string ConfigurationJson { get;  }

        virtual protected ICloudBackupArchiveProvider CloudBackupArchiveProvider { get; }

        virtual protected IClientDBHandler ClientDBHandler { get; }

        virtual protected IClientFileCacheHandler ClientFileCacheHandler { get; }

    }

}
