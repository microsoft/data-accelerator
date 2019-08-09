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
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using DataX.Contract.Exception;

namespace DataX.Utilities.Blob
{
    public static class BlobHelper
    {
        /// <summary>
        /// Get blob content
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Delete all blobs in a container
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Helper to read a blob content
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Save a content to a blob
        /// </summary>
        public static void SaveContentToBlob(string connectionString, string blobUri, string content)
        {
            CloudBlockBlob blob = GetBlobReference(connectionString, blobUri);
            SaveToBlob(blob, content);
        }

        /// <summary>
        /// Get the contents of all blobs in a container
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Helper to get a blob reference
        /// </summary>
        /// <returns></returns>
        private static CloudBlockBlob GetBlobReference(string connectionString, string blobUri)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();
            // TODO Refactor
            return new CloudBlockBlob(new Uri(blobUri), client.Credentials);
        }

        /// <summary>
        /// Helper to save a content to a blob
        /// </summary>
        private static void SaveToBlob(CloudBlockBlob blockBlob, string content)
        {
            // TODO Refactor
            blockBlob.UploadTextAsync(content).Wait();
        }

        /// <summary>
        /// Get a content of a list of blobs which are sorted by last modified time
        /// </summary>
        /// <returns></returns>
        public static async Task<List<string>> GetLastModifiedBlobContentsInBlobPath(string connectionString, string containerName, string prefix, string blobPathPattern, int blobCount)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = cloudBlobClient.GetContainerReference(containerName);

            var allBlobs = await container.ListBlobsSegmentedAsync(prefix: prefix, useFlatBlobListing: true, blobListingDetails: BlobListingDetails.None, maxResults: null, currentToken: null, options: null, operationContext: null).ConfigureAwait(false);
            var filteredBlobs = allBlobs.Results.OfType<CloudBlockBlob>().OrderByDescending(m => m.Properties.LastModified);

            List<string> blobContents = new List<string>();
            int queueCount = 0;
            
            foreach (CloudBlockBlob blob in filteredBlobs)
            {
                if (ValidateBlobPath(blobPathPattern, blob.Uri.ToString()) && blob.Properties.Length > 0)
                {
                    using (var stream = await blob.OpenReadAsync().ConfigureAwait(false))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            int offset = 0;
                            while (blob.Properties.Length > offset)
                            {
                                // We can't read the entire blob if a blob is too big (1 > GB)
                                // So we just read it line by line until we have enough information (500 rows) to generate schema
                                var line = await sr.ReadLineAsync().ConfigureAwait(false);
                                if (!string.IsNullOrEmpty(line))
                                {
                                    if (ValidateJson(line))
                                    {
                                        blobContents.Add(line);

                                        if (++queueCount >= blobCount)
                                        {
                                            return blobContents;
                                        }
                                    }

                                    offset += line.Length;
                                }

                                if (sr.EndOfStream)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return blobContents;
        }

        /// <summary>
        /// Parse and get a prefix for a blob path
        /// </summary>
        /// <returns></returns>
        public static string ParsePrefix(string blobUri)
        {
            if (!Uri.TryCreate(blobUri, UriKind.Absolute, out var uri))
            {
                return blobUri;
            }

            var prefix = "";
            foreach (var seg in uri.Segments)
            {
                // It will keep adding every segment to compose a path prefix until we find the first segment which starts with "{".
                // e.g. When an input is mycontainer@mysa.blob.core.windows.net/mypath/mypath2/mypath3/{yyyy}/{MM}/{dd}, the output is mypath/mypath2/mypath3/
                if (seg.StartsWith("%7B")) // "{", '\u007B'
                {
                    break;
                }

                prefix = Path.Combine(prefix, seg);
            }

            return prefix.TrimStart('/');
        }

        /// <summary>
        /// Get a regex pattern for a blob path
        /// when an input is myoutputs@somesa.blob.core.windows.net/Test/{yyyy-MM-dd}
        /// the output is somesa.blob.core.windows.net/myoutputs/Test/(\w+)-(\w+)-(\w+)
        /// </summary>
        /// <returns></returns>
        public static string GenerateRegexPatternFromPath(string path)
        {
            path = NormalizeBlobPath(path);
            var mc = Regex.Matches(path, @"{(.*?)}");
            if (mc == null || mc.Count < 1)
            {
                return path;
            }

            foreach (Match m in mc)
            {
                var r3 = Regex.Match(m.Value, @"^({)*([yMdHhmsS\-\/.,: ]+)(})*$");
                if (!r3.Success)
                {
                    throw new GeneralException("Token in the blob path should be a data time format. e.g. {yyyy-MM-dd}");
                }

                path = path.Replace(m.Value, @"(\w+)", StringComparison.InvariantCulture);
            }

            return path;
        }

        /// <summary>
        /// Normalize a blob path
        /// when an input is myoutputs@somesa.blob.core.windows.net/Test/{yyyy-MM-dd}
        /// the output is myoutputs@somesa.blob.core.windows.net/Test/{yyyy}-{MM}-{dd}
        /// </summary>
        /// <returns></returns>
        private static string NormalizeBlobPath(string path)
        {
            path = path.TrimEnd('/');
            var mc = Regex.Matches(path, @"{(.*?)}");
            if (mc == null || mc.Count < 1 || mc.Count > 1)
            {
                return path;
            }

            var tokenValue = mc[0].Value.Trim(new char[] { '{', '}' });

            var mc2 = Regex.Matches(tokenValue, @"[A-Za-z]+");
            foreach (Match m in mc2)
            {
                tokenValue = tokenValue.Replace(m.Value, "{" + m.Value + "}", StringComparison.InvariantCulture);
            }

            path = path.Replace(mc[0].Value, tokenValue, StringComparison.InvariantCulture);

            return path;
        }

        /// <summary>
        /// Test if the input is a valid json
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Test if the blob path matches the expected blob pattern
        /// </summary>
        /// <returns></returns>
        private static bool ValidateBlobPath(string blobPathPattern, string blobFullPath)
        {
            return Regex.Match(blobFullPath, blobPathPattern).Success;
        }
    }
}
