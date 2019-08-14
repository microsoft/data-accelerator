using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DataX.Flow.InteractiveQuery.Tests
{
    [TestClass]
    public class InteractiveQueryTests
    {
        [TestMethod]
        public void ConvertWasbsToDbfsFilePath()
        {
            //Test ConvertToDbfsFilePath with both filePath and fileName parameters
            string wasbsPath = "wasbs://mycontainer@mystorageaccount.blob.core.windows.net/";
            string fileName = "testFile.json";
            string actualValue = InteractiveQueryManager.ConvertToDbfsFilePath(wasbsPath, fileName);
            string expectedValue = "dbfs:/mnt/livequery/mycontainer/testFile.json";
            Assert.AreEqual(expectedValue, actualValue, "DBFS file path is incorrect");

            //Test ConvertToDbfsFilePath with only filePath parameter
            wasbsPath = "wasbs://mycontainer@mystorageaccount.blob.core.windows.net/testfolder/testFile.json";
            actualValue = InteractiveQueryManager.ConvertToDbfsFilePath(wasbsPath);
            expectedValue = "dbfs:/mnt/livequery/mycontainer/testfolder/testFile.json";
            Assert.AreEqual(expectedValue, actualValue, "DBFS file path is incorrect");
        }

        [TestMethod]
        public void TestMountCode()
        {
            string expectedValue = "dbutils.fs.mount(source = \"wasbs://mycontainer@teststorageaccount.blob.core.windows.net/\", mountPoint = \"/mnt/livequery//mycontainer\", extraConfigs = Map(\"fs.azure.account.key.teststorageaccount.blob.core.windows.net\"->dbutils.secrets.get(scope = \"testkeyvault\", key = \"datax-sa-teststorageaccount\")))";

            //Test unnested file path
            string dbfsPath = "dbfs:/mnt/livequery/mycontainer/testFile.json";
            string opsStorageAccountName = "teststorageaccount";
            string sparkKeyVaultName = "testkeyvault";
            string actualValue = KernelService.CreateMountCode(dbfsPath, opsStorageAccountName, sparkKeyVaultName);
            Assert.AreEqual(expectedValue, actualValue, "Mount code is incorrect");

            //Test nested file path
            dbfsPath = "dbfs:/mnt/livequery/mycontainer/folder1/folder2/testFile.json";
            actualValue = KernelService.CreateMountCode(dbfsPath, opsStorageAccountName, sparkKeyVaultName);
            Assert.AreEqual(expectedValue, actualValue, "Mount code is incorrect");
        }
    }
}
