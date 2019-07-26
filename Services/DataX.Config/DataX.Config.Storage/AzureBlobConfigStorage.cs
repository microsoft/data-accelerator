// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Utility.Blob;
using DataX.Utility.KeyVault;
using System;
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config.Storage
{

    /// <summary>
    /// Runtime Storage implementation on Azure Blob Storage
    /// </summary>
    [Export(typeof(IRuntimeConfigStorage))]
    public class AzureBlobConfigStorage : IRuntimeConfigStorage
    {
        public const string ConfigSettingName_RuntimeStorageConnectionString = "runtimeStorageConnectionString";

        [ImportingConstructor]
        public AzureBlobConfigStorage(ConfigGenConfiguration configGenConfiguration, IKeyVaultClient keyvaultClient)
        {
            Configuration = configGenConfiguration;
            KeyVaultClient = keyvaultClient;
        }

        private ConfigGenConfiguration Configuration { get; }
        private IKeyVaultClient KeyVaultClient { get; }

        /// <summary>
        /// Save the file in Azure blob
        /// </summary>
        /// <param name="destinationPath"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<string> SaveFile(string destinationPath, string content)
        {
            string connectionStringRef = Configuration[ConfigSettingName_RuntimeStorageConnectionString];
            string connectionString = await KeyVaultClient.ResolveSecretUriAsync(connectionStringRef);
            var filePath = NormalizeFilePath(destinationPath);
            await BlobUtility.SaveContentToBlob(connectionString, filePath, content);
            return destinationPath;
        }

        public string NormalizeFilePath(string path)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                switch (uri.Scheme.ToLower())
                {
                    case "http":
                    case "https":
                        return path;
                    case "wasb":
                        return $"http://{uri.Host}/{uri.UserInfo}{uri.PathAndQuery}";
                    case "wasbs":
                        return $"https://{uri.Host}/{uri.UserInfo}{uri.PathAndQuery}";
                    default:
                        throw new ArgumentException($"Unsupported scheme type for output path:'{path}'");
                }
            }
            else
            {
                throw new ArgumentException($"Malformed Uri for output:'{path}'");
            }
        }

        public async Task<string> Delete(string destinationPath)
        {
            string connectionStringRef = Configuration[ConfigSettingName_RuntimeStorageConnectionString];
            string connectionString = await KeyVaultClient.ResolveSecretUriAsync(connectionStringRef);
            var filePath = NormalizeFilePath(destinationPath);
            await BlobUtility.DeleteBlob(connectionString, filePath);
            return destinationPath;
        }

        public async Task<string> DeleteAll(string destinationPath)
        {
            string connectionStringRef = Configuration[ConfigSettingName_RuntimeStorageConnectionString];
            string connectionString = await KeyVaultClient.ResolveSecretUriAsync(connectionStringRef);
            var filePath = NormalizeFilePath(destinationPath);
            if (Uri.TryCreate(destinationPath, UriKind.Absolute, out var uri))
            {
                await BlobUtility.DeleteAllBlobsInAContainer(connectionString, uri.UserInfo, uri.PathAndQuery.TrimStart('/'));
            }
            else
            {
                throw new ArgumentException($"Malformed Uri for output:'{destinationPath}'");
            }
            return destinationPath;
        }

    }
}
