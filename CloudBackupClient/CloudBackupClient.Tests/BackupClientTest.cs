using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;

namespace CloudBackupClient.Tests
{
    [TestFixture]
    public class CloudBackupClient_UseTestMethod : CloudBackupTestBase
    {        
        protected override string ConfigurationFileJson => "appsettings_test1.json";
             
        [Test]
        public void TryStringMatch()
        {
            //var testVal = "myMethod";
            //string badVal = null;
            //var result = backupClient.ReturnForTest(testVal);
            
            var backupClient = this.ServiceProvider.GetService<BackupClient>();
            var result = ((BackupClient)this.ServiceProvider.GetService(typeof(BackupClient))).TestMethod();
            
            Assert.IsTrue(result);
            //Assert.AreEqual(testVal, result);
            
            //Assert.Fail( testVal + badVal );
        }
    }
}