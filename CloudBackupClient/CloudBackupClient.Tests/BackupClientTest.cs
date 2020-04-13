using NUnit.Framework;

namespace CloudBackupClient.Tests
{
    [TestFixture]
    public class CloudBackupClient_UseTestMethod
    {
        private BackupClient backupClient;

        [SetUp]
        public void Setup()
        {
            backupClient = new BackupClient();
        }

        [Test]
        public void TryStringMatch()
        {
            //var testVal = "myMethod";
            //string badVal = null;
            //var result = backupClient.ReturnForTest(testVal);

            //Assert.AreEqual(testVal, result);
            
            //Assert.Fail( testVal + badVal );
        }
    }
}