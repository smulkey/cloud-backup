using CloudBackupClient.ClientFileCacheHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CloudBackupClient.Tests.UnitTests
{
    [TestFixture]
    public class LocalClientFileCacheHandler_Test : CloudBackupTestBase
    {
        private string TempDirectory { get; set; }

        override protected string ConfigurationJson => $"{{\"LocalClientFileCacheConfig\": {{ \"TempCopyDirectory\": \"{this.TempDirectory}\", \"MaxCacheMB\": 1}} }}";
        
        
        [OneTimeSetUp]
        public void TestSetup()
        {
            this.TempDirectory = @"G:\CloudBackupTestTemp";

            if(Directory.Exists(this.TempDirectory))
            {
                Directory.Delete(this.TempDirectory);
            }

            Directory.CreateDirectory(this.TempDirectory);

            this.Initialize();
        }

        [Test]
        public void LocalFileCacheHandlerTest()
        {

        }

        [OneTimeTearDown]
        public void TestComplete()
        {
            if(Directory.Exists(this.TempDirectory))
            {
                Directory.Delete(this.TempDirectory);
            }
        }

        protected override IClientFileCacheHandler ClientFileCacheHandler => new LocalClientFileCacheHandler();
    }
}
