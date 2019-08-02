// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.Threading.Tasks;

namespace DataX.Config
{
    public interface IKeyVaultClient
    {
        /// <summary>
        /// Resolve the keyvault secret uri in format 'keyvault://{keyvaultname}/{secretname}'
        /// </summary>
        /// <param name="secretUri">secret uri</param>
        /// <returns>value of the secert if the given id matches the keyvault id format, or else return the given string</returns>
        Task<string> ResolveSecretUriAsync(string secretUri);

        /// <summary>
        /// Get secret value with given keyvault name and secret name
        /// </summary>
        /// <param name="keyvaultName">name of the keyvault service</param>
        /// <param name="secretName">name of the specified secret</param>
        /// <returns>value of the secret or throw</returns>
        Task<string> GetSecretFromKeyVaultAsync(string keyvaultName, string secretName);

        /// <summary>
        /// Save the secret and return the secret uri
        /// </summary>
        /// <param name="keyvaultName">keyvault to save the secret</param>
        /// <param name="secretName">name of the secret</param>
        /// <param name="secretValue">value of the secret</param>
        /// <param name="uriPrefix">value of the uriPrefix</param>
        /// <param name="hashSuffix">specify whether the generated secret uri has a hashed suffix, by default it is false</param>
        /// <returns>secret id</returns>
        Task<string> SaveSecretAsync(string keyvaultName, string secretName, string secretValue, string sparkType, bool hashSuffix = false);

        /// <summary>
        /// Save the secret and return the secret uri
        /// </summary>
        /// <param name="secretUri">Uri of the secret</param>
        /// <param name="secretValue">value of the secret</param>
        /// <returns>secret id</returns>
        Task<string> SaveSecretAsync(string secretUri, string secretValue);

        /// <summary>
        /// Generate a secret uri prefix based on sparkType
        /// </summary>
        /// <param name="sparkType">spark type</param>
        /// <returns></returns>
        string GetUriPrefix(string sparkType);
    }
}
