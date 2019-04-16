// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataX.Utilities.Blob
{
    public class BlobStorageMSI
    {
        public CloudBlobClient _cloudBlobClient;

        public BlobStorageMSI(string storageAccountName)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var storageUri = $"https://{storageAccountName}.blob.core.windows.net/";
            var AccessToken = azureServiceTokenProvider.GetAccessTokenAsync(storageUri).Result;
            StorageCredentials storageCredentials = new StorageCredentials(new TokenCredential(AccessToken));

            _cloudBlobClient = new CloudBlobClient(new Uri(storageUri), storageCredentials);
        }

        public CloudBlobContainer GetBlobContainer(string containerName)
        {
            return _cloudBlobClient.GetContainerReference(containerName);
        }

        public CloudBlockBlob GetBlob(string containerName, string blobRef)
        {
            var container = GetBlobContainer(containerName);
            return container.GetBlockBlobReference(blobRef);
        }

        public IEnumerable<CloudBlockBlob> GetCloudBlockBlobs(string containerName, string blobPrefix)
        {
            var container = GetBlobContainer(containerName);
            return container.ListBlobsSegmentedAsync(prefix: blobPrefix, useFlatBlobListing: true, blobListingDetails: BlobListingDetails.None, maxResults: null, currentToken: null, options: null, operationContext: null).Result.Results.OfType<CloudBlockBlob>();
        }

    }
}
