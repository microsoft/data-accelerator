// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataX.Utilities.Blob
{
    public class AzureStorageUtility : IDisposable
    {
        private readonly string _clientId;
        private readonly string _tenantId;
        private readonly string _secretKey;
        private string _defaultLocation;
        private readonly string _keyvaultName;
        private static Sku _DefaultSku = new Sku(SkuName.StandardLRS);
        private static Kind _DefaultKind = Kind.BlobStorage;
        private static Dictionary<string, string> _DefaultTags = new Dictionary<string, string>
        {
            { "key1","value1"},
            {"key2","value2"}
        };

        private StorageManagementClient _storageMgmtClient;


        public AzureStorageUtility(string subscriptionId, string defaultLocation, string keyvaultName, string clientId, string tenantId, string secretPrefix)
        {
            _defaultLocation = defaultLocation;
            _keyvaultName = keyvaultName;
            _clientId = clientId;
            _tenantId = tenantId;

            _secretKey = KeyVault.KeyVault.GetSecretFromKeyvault(_keyvaultName, secretPrefix + "clientsecret");

            string token = GetAccessToken(_tenantId, clientId, _secretKey);
            TokenCredentials credential = new TokenCredentials(token);
            
            _storageMgmtClient = new StorageManagementClient(credential) { SubscriptionId = subscriptionId };
        }

        /// <summary>
        /// Create a new Storage Account. If one already exists then the request still succeeds
        /// </summary>
        /// <param name="resourceGroupName">Resource Group Name</param>
        /// <param name="accountName">Account Name</param>
        /// <param name="useCoolStorage">Use Cool Storage</param>
        /// <param name="useEncryption">Use Encryption</param>       
        public void CreateStorageAccount(string resourceGroupName, string accountName)
        {

            StorageAccountCreateParameters parameters = GetDefaultStorageAccountParameters();

            Console.WriteLine("Creating a storage account...");

            var storageAccount = _storageMgmtClient.StorageAccounts.Create(resourceGroupName, accountName, parameters);

            Console.WriteLine("Storage account created with name " + storageAccount.Name);
        }

        /// <summary>
        /// //Get the storage account keys for a given account and resource group
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="accountName"></param>
        /// <returns></returns>
        public string GetStorageAccountKey(string resourceGroupName, string accountName)
        {
            // -----------------------------------------------------------------------------------------------------------------------------
            // To get all storage accounts using the subscription
            //var acs = storageMgmtClient.StorageAccounts.List();
            //foreach(var ac in acs)
            //{
            //}

            IList<StorageAccountKey> acctKeys = _storageMgmtClient.StorageAccounts.ListKeys(resourceGroupName, accountName).Keys;
            string storageAccountKey = (acctKeys != null && acctKeys.Count > 0) ? acctKeys[0].Value : "";

            return storageAccountKey;
        }

        public CloudBlobContainer GetBlobContainerReference(string connectionString, string containerName)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            return container;
        }

        public CloudBlobContainer GetBlobContainerReference(string accountName, string key, string containerName)
        {
            string connStr = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={key};EndpointSuffix=core.windows.net";

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            
            return container;
        }


        public CloudBlobContainer CreateBlobContainer(string connectionString, string containerName)
        {
            // Retrieve a reference to a container.
            CloudBlobContainer container = GetBlobContainerReference(connectionString, containerName);

            // Create the container if it doesn't already exist.
            // TODO Refactor: Updated to Async. Validate
            // container.CreateIfNotExists();
            bool result = container.CreateIfNotExistsAsync().Result;

            return container;
        }

        public CloudBlobContainer CreateBlobContainer(string accountName, string key, string containerName)
        {
            // Retrieve a reference to a container.
            CloudBlobContainer container = GetBlobContainerReference(accountName, key, containerName);

            // Create the container if it doesn't already exist.
            // TODO Refactor: Updated to Async. Validate
            // container.CreateIfNotExists();
            bool result = container.CreateIfNotExistsAsync().Result;

            return container;
        }

        /// <summary>
        /// Deletes a storage account for the specified account name
        /// </summary>
        /// <param name="rgname"></param>
        /// <param name="accountName"></param>
        public void DeleteStorageAccount(string resourceGroupName, string accountName)
        {
            Console.WriteLine("Deleting a storage account...");
            _storageMgmtClient.StorageAccounts.Delete(resourceGroupName, accountName);
            Console.WriteLine("Storage account " + accountName + " deleted");
        }

        /// <summary>
        /// Updates the storage account
        /// </summary>
        /// <param name="resourceGroupName">Resource Group Name</param>
        /// <param name="aacountName">Account Name</param>
        public void UpdateStorageAccountSku(string resourceGroupName, string aacountName, SkuName skuName)
        {

            Console.WriteLine("Updating storage account...");

            // Update storage account sku

            var parameters = new StorageAccountUpdateParameters
            {
                Sku = new Sku(skuName)
            };

            var storageAccount = _storageMgmtClient.StorageAccounts.Update(resourceGroupName, aacountName, parameters);

            Console.WriteLine("Sku on storage account updated to " + storageAccount.Sku.Name);
        }
        
        public List<string> LoadAllConfigsFromBlobStorage(string resourceGroupName, string storageAccountName, string containerName, string containerPath, string fileName)
        {
            var storageAccountKey = GetStorageAccountKey(resourceGroupName, storageAccountName);
            var container = GetBlobContainerReference(storageAccountName, storageAccountKey, containerName);

            // TODO Refactor: Updated this line. Validate
            //var blobs = container.ListBlobs(prefix: containerPath, useFlatBlobListing: true).Where(b => b.Uri.Segments.Last().EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
            var blobs =  container.ListBlobsSegmentedAsync(prefix: containerPath, useFlatBlobListing: true, blobListingDetails: BlobListingDetails.None, maxResults: null, currentToken: null, options: null, operationContext: null).Result.Results.Where(b => b.Uri.Segments.Last().EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            List<string> blobContents = new List<string>();
            if (blobs != null && blobs.Count() > 0)
            {
                // TODO Refactor: Updated this call to Async. Validate
                //blobContents.AddRange(blobs.Where(b => b is CloudBlockBlob).Select(b => ((CloudBlockBlob)b).DownloadText()));
                blobContents.AddRange(blobs.Where(b => b is CloudBlockBlob).Select(b => ((CloudBlockBlob)b).DownloadTextAsync().Result));
            }

            return blobContents;
        }

        public string LoadConfigsFromBlobStorage(string resourceGroupName, string storageAccountName, string containerName, string fileName)
        {
            var storageAccountKey = GetStorageAccountKey(resourceGroupName, storageAccountName);
            var container = GetBlobContainerReference(storageAccountName, storageAccountKey, containerName);
            return DownloadBlobStorage(container, fileName);
        }

        public void UploadConfigsToBlobStorage(string resourceGroupName, string storageAccountName, string containerName, string fileName, string content)
        {
            var storageAccountKey = GetStorageAccountKey(resourceGroupName, storageAccountName);
            var container = GetBlobContainerReference(storageAccountName, storageAccountKey, containerName);
            UploadToStorageContainer(container, fileName, content);
        }

        /// <summary>
        /// Delete a config from blob storage
        /// </summary>
        /// <param name="resourceGroupName">resourceGroupName</param>
        /// <param name="storageAccountName">storageAccountName</param>
        /// <param name="containerName">containerName</param>
        /// <param name="fileName">fileName</param>
        public void DeleteConfigFromBlobStorage(string resourceGroupName, string storageAccountName, string containerName, string fileName)
        {
            var storageAccountKey = GetStorageAccountKey(resourceGroupName, storageAccountName);
            var container = GetBlobContainerReference(storageAccountName, storageAccountKey, containerName);
            DeleteBlobInStorageContainer(container, fileName).Wait();
        }

        /// <summary>
        /// This function deletes all blob files under a folder in Azure in a recursive manner
        /// </summary>
        /// <param name="resourceGroupName">resourceGroupName</param>
        /// <param name="storageAccountName">storageAccountName</param>
        /// <param name="containerName">containerName</param>
        /// <param name="containerPath">containerPath</param>
        public async Task<ApiResult> DeleteAllConfigsFromBlobStorage(string resourceGroupName, string storageAccountName, string containerName, string containerPath)
        {
            var storageAccountKey = GetStorageAccountKey(resourceGroupName, storageAccountName);
            var container = GetBlobContainerReference(storageAccountName, storageAccountKey, containerName);

            bool exists = container.ExistsAsync().Result;
            if (!exists)
            {
                return ApiResult.CreateSuccess("Nothing to delete");
            }

            /// Azure does not like a mix of directory separators whereas Path.Combine gives a mixture of Path.AltDirectorySeparatorChar(/) and Path.DirectorySeparatorChar (\)
            containerPath = containerPath.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);            
            IEnumerable<IListBlobItem> listBlobs = ListBlobsAsync(container, containerPath).GetAwaiter().GetResult();

            if (listBlobs == null || listBlobs.Count() == 0)
            {
                return ApiResult.CreateSuccess("Nothing to delete");
            }

            foreach (CloudBlockBlob cloudBlockBlob in listBlobs)
            {
                await DeleteBlobInStorageContainer(cloudBlockBlob.Container, cloudBlockBlob.Name);
            }
            return ApiResult.CreateSuccess("Deleted successfully");
        }

        /// <summary>
        /// This function returns a list of IListBlobItem(s) in a container
        /// </summary>
        /// <param name="container"></param>
        /// <param name="containerPath"></param>
        /// <returns></returns>
        private async Task<List<IListBlobItem>> ListBlobsAsync(CloudBlobContainer container, string containerPath)
        {
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> results = new List<IListBlobItem>();
            do
            {
                var response = await container.ListBlobsSegmentedAsync(prefix: containerPath, useFlatBlobListing: true, blobListingDetails: BlobListingDetails.None, maxResults: null, currentToken: null, options: null, operationContext: null);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            }
            while (continuationToken != null);
            return results;
        }

        private string DownloadBlobStorage(CloudBlobContainer container, string fileName)
        {
            var blockBlob = container.GetBlockBlobReference(fileName);
            Console.WriteLine("{0} is uploaded to {1}", fileName, container.Name);

            try
            {
                // TODO Refactor: Updated to use Async
                //return blockBlob.DownloadText();
                return blockBlob.DownloadTextAsync().Result;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        /// <summary>
        /// DeleteBlobInStorageContainer deletes the blob in a storage container
        /// </summary>
        /// <param name="container">CloudBlobContainer container</param>
        /// <param name="fileName">fileName</param>
        /// <returns></returns>
        private async Task<ApiResult> DeleteBlobInStorageContainer(CloudBlobContainer container, string fileName)
        {
            bool exists = container.ExistsAsync().Result;
            if (!exists)
            {
                return ApiResult.CreateSuccess("Container does not exist. Nothing to delete");
            }

            var blockBlob = container.GetBlockBlobReference(fileName);

            bool x = await blockBlob.DeleteIfExistsAsync();
            if(x)
            {
                return ApiResult.CreateSuccess($"Deleted blob: {fileName}");
            }
            else
            {
                return ApiResult.CreateError($"Could not delete blob: {fileName}");
            }
        }

        private void UploadToStorageContainer(CloudBlobContainer container, string fileName, string content)
        {
            var blockBlob = container.GetBlockBlobReference(fileName);

            // TODO Refactor: Updated to use async.
            //blockBlob.UploadText(content);
            blockBlob.UploadTextAsync(content).Wait();
            
            Console.WriteLine("{0} is uploaded to {1}", fileName, container.Name);
        }

        /// <summary>
        /// Returns default values to create a storage account
        /// </summary>
        /// <returns>The parameters to provide for the account</returns>
        private StorageAccountCreateParameters GetDefaultStorageAccountParameters()
        {

            StorageAccountCreateParameters account = new StorageAccountCreateParameters
            {
                Location = _defaultLocation,
                Kind = _DefaultKind,
                Tags = _DefaultTags,
                Sku = _DefaultSku,
                AccessTier=AccessTier.Hot
            };

            return account;
        }

        private string GetAccessToken(string tenantId, string clientId, string secretKey)
        {
            var authenticationContext = new AuthenticationContext($"https://login.windows.net/{tenantId}");
            var credential = new ClientCredential(clientId, secretKey);
            var result = authenticationContext.AcquireTokenAsync("https://management.core.windows.net/",
                credential);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            var token = result.Result.AccessToken;
            return token;
        }

        /// <summary>
        /// Dispose object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _storageMgmtClient.Dispose();
            }
        }
    }
}
