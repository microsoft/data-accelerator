// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace DataX.Utilities.Blob
{
    public static class BlobHelper
    {
        public static string GetBlobContent(string connectionString, string blobUri)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();
            // TODO Refactor: Updated
            //CloudBlockBlob blob = new CloudBlockBlob(new Uri(blobUri), client);
            CloudBlockBlob blob = new CloudBlockBlob(new Uri(blobUri), client.Credentials);

            string text = ReadFromBlob(blob);

            return text;
        }

        /// <summary>
        /// Adding api for deleting a blob
        /// </summary>
        /// <param name="connectionString">connectionString</param>
        /// <param name="blobUri">blobUri</param>
        /// <returns>Returns ApiResult which success or failure as the case maybe</returns>
        public static async Task<ApiResult> DeleteBlob(string connectionString, string blobUri)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();

            CloudBlockBlob blob = new CloudBlockBlob(new Uri(blobUri), client.Credentials);
            await blob.DeleteIfExistsAsync();
            return ApiResult.CreateSuccess("Blob Deleted");
        }

        public static async Task<ApiResult> DeleteAllBlobsInAContainer(string storageConnectionString, string containerName, string blobDirectory)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            if (storageAccount == null)
            {
                return ApiResult.CreateSuccess("Nothing to delete");
            }

            CloudBlobContainer container = storageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
            bool exists = container.ExistsAsync().Result;
            if (!exists)
            {
                return ApiResult.CreateSuccess("Nothing to delete");
            }

            // Get all the blobs in the container
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> blobs = new List<IListBlobItem>();
            do
            {
                var response = container.GetDirectoryReference(blobDirectory).ListBlobsSegmentedAsync(continuationToken).Result;
                continuationToken = response.ContinuationToken;
                blobs.AddRange(response.Results);
            }
            while (continuationToken != null);

            // Delete the blobs
            if (blobs == null || blobs.Count <= 0)
            {
                return ApiResult.CreateSuccess("Nothing to delete");
            }

            foreach (IListBlobItem blob in blobs)
            {
                await container.GetBlobReference(((CloudBlockBlob)blob).Name).DeleteIfExistsAsync();
            }

            return ApiResult.CreateSuccess("Deleted Successfully");
        }


        private static string ReadFromBlob(CloudBlockBlob blockBlob)
        {
            string text = null;
            // TODO Refactor: Uses async
            //if (blockBlob != null && blockBlob.Exists())
            if (blockBlob != null && blockBlob.ExistsAsync().Result)
            {                
                // TODO Refactor
                //using (var blob = blockBlob.OpenRead())
                using (var blob = blockBlob.OpenReadAsync().Result)
                {
                    using (var sr = new StreamReader(blob))
                    {
                        text = sr.ReadToEnd();
                    }
                }
            }

            return text;
        }

        public static void SaveContentToBlob(string connectionString, string blobUri, string content)
        {
            CloudBlockBlob blob = GetBlobReference(connectionString, blobUri);
            SaveToBlob(blob, content);
        }

        public static Dictionary<string, string> GetBlobsFromContainer(string connectionString, string containerName, string folderPath)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();
            var blobd = client.GetContainerReference(containerName);

            var blobs = blobd.ListBlobsSegmentedAsync(prefix: folderPath, useFlatBlobListing: true, blobListingDetails: BlobListingDetails.None, maxResults: null, currentToken: null, options: null, operationContext: null).Result.Results;
            Dictionary<string, string> blobContents = new Dictionary<string, string>();
            if (blobs != null && blobs.Count() > 0)
            {
                foreach(CloudBlockBlob blob in blobs)
                {
                    blobContents.Add(blob.Name, blob.DownloadTextAsync().Result);
                }
            }

            return blobContents;
        }

        private static CloudBlockBlob GetBlobReference(string connectionString, string blobUri)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();
            // TODO Refactor
            return new CloudBlockBlob(new Uri(blobUri), client.Credentials);
        }

        private static void SaveToBlob(CloudBlockBlob blockBlob, string content)
        {
            // TODO Refactor
            blockBlob.UploadTextAsync(content).Wait();
        }

        public static async Task<List<string>> GetLastModifiedBlobContentsInBlobPath(string connectionString, string containerName, string prefix, string blobPathPattern, int blobCount)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = cloudBlobClient.GetContainerReference(containerName);

            var filteredBlobs = container.ListBlobsSegmentedAsync(prefix: prefix, useFlatBlobListing: true, blobListingDetails: BlobListingDetails.None, maxResults: null, currentToken: null, options: null, operationContext: null).Result.Results.OfType<CloudBlockBlob>()
                .Where(b => ValidateBlobPath(blobPathPattern, b.Uri.ToString()) && b.Properties.Length > 0 && ValidateJson(b.DownloadTextAsync().Result))
                .OrderByDescending(m => m.Properties.LastModified).ToList().Take(blobCount);

            List<string> blobContents = new List<string>();

            if (filteredBlobs != null && filteredBlobs.Count() > 0)
            {
                foreach (CloudBlockBlob blob in filteredBlobs)
                {
                    blobContents.Add(blob.DownloadTextAsync().Result);
                }
            }

            return blobContents;
        }

        private static bool ValidateJson(string input)
        {
            try
            {
                Newtonsoft.Json.Linq.JObject.Parse(input);
                return true;

            }
            catch
            {
                return false;
            }
        }

        private static bool ValidateBlobPath(string blobPathPattern, string blobFullPath)
        {
            return Regex.Match(blobFullPath, blobPathPattern).Success;
        }
    }
}
