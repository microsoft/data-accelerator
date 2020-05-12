// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Utilities.Blob
{
    public static class BlobStorage
    {
        public static void UploadConfigsToBlobStorage(string subscriptionId, string keyvaultName, string resourceGroupName, string defaultLocation, string storageAccountName, string containerName, string fileName, string content, string clientId, string tenantId, string secretPrefix)
        {
            AzureStorageUtility storageUtility = new AzureStorageUtility(subscriptionId, defaultLocation, keyvaultName, clientId, tenantId, secretPrefix);
            storageUtility.UploadConfigsToBlobStorage(resourceGroupName, storageAccountName, containerName, fileName, content);
            storageUtility.Dispose();
        }

        public static void DeleteConfigFromBlobStorage(string subscriptionId, string keyvaultName, string resourceGroupName, string defaultLocation, string storageAccountName, string containerName, string fileName, string clientId, string tenantId, string secretPrefix)
        {
            AzureStorageUtility storageUtility = new AzureStorageUtility(subscriptionId, defaultLocation, keyvaultName, clientId, tenantId, secretPrefix);
            storageUtility.DeleteConfigFromBlobStorage(resourceGroupName, storageAccountName, containerName, fileName);
            storageUtility.Dispose();
        }

        public static void DeleteAllConfigsFromBlobStorage(string subscriptionId, string keyvaultName, string resourceGroupName, string defaultLocation, string storageAccountName, string containerName, string containerPath, string clientId, string tenantId, string secretPrefix)
        {
            AzureStorageUtility storageUtility = new AzureStorageUtility(subscriptionId, defaultLocation, keyvaultName, clientId, tenantId, secretPrefix);
            storageUtility.DeleteAllConfigsFromBlobStorage(resourceGroupName, storageAccountName, containerName, containerPath).Wait();
            storageUtility.Dispose();
        }

        public static string LoadConfigFromBlobStorage(string subscriptionId, string keyvaultName, string resourceGroupName, string defaultLocation, string storageAccountName, string containerName, string fileName, string clientId, string tenantId, string secretPrefix)
        {
            AzureStorageUtility storageUtility = new AzureStorageUtility(subscriptionId, defaultLocation, keyvaultName, clientId, tenantId, secretPrefix);
            var result = storageUtility.LoadConfigsFromBlobStorage(resourceGroupName, storageAccountName, containerName, fileName);
            storageUtility.Dispose();
            return result;
        }

        public static List<string> LoadAllConfigsFromBlobStorage(string subscriptionId, string keyvaultName, string resourceGroupName, string defaultLocation, string storageAccountName, string containerName, string containerPath, string fileName, string clientId, string tenantId, string secretPrefix)
        {
            AzureStorageUtility storageUtility = new AzureStorageUtility(subscriptionId, defaultLocation, keyvaultName, clientId, tenantId, secretPrefix);
            var result = storageUtility.LoadAllConfigsFromBlobStorage(resourceGroupName, storageAccountName, containerName, containerPath, fileName);
            storageUtility.Dispose();
            return result;
        }
    }
}
