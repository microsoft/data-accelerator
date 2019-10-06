// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace DataX.Utility.Blob
{
    /// <summary>
    /// Utility class to interact with Azure Blob Storage
    /// </summary>
    public static class BlobUtility
    {
        /// <summary>
        /// Get the blob content
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="blobUri"></param>
        /// <returns></returns>
        public static async Task<string> GetBlobContent(string connectionString, string blobUri)
        {
            string text = "";

            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();
            CloudBlockBlob blob = new CloudBlockBlob(new Uri(blobUri), client.Credentials);

            text = await ReadFromBlob(blob);

            return text;
        }

        /// <summary>
        /// Adding api for deleting a blob
        /// </summary>
        /// <param name="connectionString">connectionString</param>
        /// <param name="blobUri">blobUri</param>
        /// <returns>Returns ApiResult which success or failure as the case maybe</returns>
        public static async Task<bool> DeleteBlob(string connectionString, string blobUri)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();

            CloudBlockBlob blob = new CloudBlockBlob(new Uri(blobUri), client.Credentials);
            return await blob.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Delete all blobs in the given directory inside the container
        /// </summary>
        /// <param name="storageConnectionString"></param>
        /// <param name="containerName"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteAllBlobsInAContainer(string storageConnectionString, string containerName, string directory)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            if (storageAccount == null)
            {
                return true;
            }

            CloudBlobContainer container = storageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
            bool exists = await container.ExistsAsync();
            if (!exists)
            {
                return true;
            }

            // Get all the blobs in the container
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> blobs = new List<IListBlobItem>();
            do
            {
                var response = container.GetDirectoryReference(directory).ListBlobsSegmentedAsync(continuationToken).Result;
                continuationToken = response.ContinuationToken;
                blobs.AddRange(response.Results);
            }
            while (continuationToken != null);

            // Delete the blobs
            if (blobs == null || blobs.Count <= 0)
            {
                return true;
            }

            foreach (IListBlobItem blob in blobs)
            {
                await container.GetBlobReference(((CloudBlockBlob)blob).Name).DeleteIfExistsAsync();
            }
            return true;
        }

        /// <summary>
        /// Upload the content to the blob storage
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="blobUri"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static async Task SaveContentToBlob(string connectionString, string blobUri, string content)
        {
            CloudBlockBlob blockBlob = GetBlobReference(connectionString, blobUri);
            await blockBlob.UploadTextAsync(content);
        }

        /// <summary>
        /// Get all the blobs 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="blobUri"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<List<string>> GetBlobs(string connectionString, string blobUri, string fileName)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();
            var blobd = new CloudBlobContainer(new Uri(blobUri));

            var result = await blobd.ListBlobsSegmentedAsync(prefix: null, useFlatBlobListing: true, blobListingDetails: BlobListingDetails.None, maxResults: null, currentToken: null, options: null, operationContext: null);
            var blobs = result.Results.Where(b => b.Uri.Segments.Last().EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            List<string> blobContents = new List<string>();
            if (blobs != null && blobs.Count() > 0)
            {
                blobContents.AddRange(blobs.Where(b => b is CloudBlockBlob).Select(b => ((CloudBlockBlob)b).DownloadTextAsync().Result));
            }

            return blobContents;
        }

        private static CloudBlockBlob GetBlobReference(string connectionString, string blobUri)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();
            return new CloudBlockBlob(new Uri(blobUri), client.Credentials);
        }

        private static async Task<string> ReadFromBlob(CloudBlockBlob blockBlob)
        {
            string text = null;
            if (blockBlob != null && await blockBlob.ExistsAsync())
            {
                using (var blob = await blockBlob.OpenReadAsync())
                {
                    using (var sr = new StreamReader(blob))
                    {
                        text = await sr.ReadToEndAsync();
                    }
                }
            }

            return text;
        }
    }
}
