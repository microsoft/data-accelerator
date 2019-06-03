// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.Management.EventHub;
using Microsoft.Azure.Management.EventHub.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using DataX.Config.ConfigDataModel;

namespace DataX.Utilities.EventHub
{
    public class EventHubUtil : IDisposable
    {
        private readonly string _clientId;
        private readonly string _tenantId;
        private readonly string _secretKey;
        private string _defaultLocation;
        private readonly string _keyvaultName;
        private readonly string _token;
        private readonly string _subscriptionId;

        private EventHubManagementClient _eventHubManagementClient;

        public EventHubUtil(string subscriptionId, string defaultLocation, string keyvaultName, string clientId, string tenantId, string secretPrefix)
        {
            _defaultLocation = defaultLocation;
            _keyvaultName = keyvaultName;
            _clientId = clientId;
            _tenantId = tenantId;

            _secretKey = KeyVault.KeyVault.GetSecretFromKeyvault(_keyvaultName, secretPrefix + "clientsecret");

            _token = GetAccessToken(_tenantId, _clientId, _secretKey);
            _subscriptionId = subscriptionId;
            var creds = new TokenCredentials(_token);
            _eventHubManagementClient = new EventHubManagementClient(creds)
            {
                SubscriptionId = subscriptionId
            };
        }

        public ApiResult CreateConsumerGroup(string resourceGroupName, string namespaceName, string eventHubName, string consumerGroupName, string inputType)
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new Exception("Namespace name is empty!");
            }

            var consumerGroupParams = new ConsumerGroup();

            Console.WriteLine("Creating Consumer Group...");

            string errorMessage = "";
            //TODO find a way to use only eventhub connection string (without using subscriptionId)
            List<string> names = resourceGroupName.Split(',').Select(s => s.Trim()).ToList();
            if (names != null && names.Count() > 0)
            {
                foreach (string name in names)
                {
                    try
                    {
                        if (inputType == Constants.InputType_IoTHub)
                        {
                            string endpoint = $"https://management.azure.com/subscriptions/{_subscriptionId}/resourceGroups/{name}/providers/Microsoft.Devices/IotHubs/{eventHubName}/eventHubEndpoints/events/ConsumerGroups/{consumerGroupName}?api-version=2018-04-01";
                            if (CreateIotHubEndpointConsumerGroup(endpoint))
                            {
                                return ApiResult.CreateSuccess("A consumer group is created successfully");
                            }
                        }
                        else
                        {
                            ConsumerGroup found = null;

                            try
                            {
                                found = _eventHubManagementClient.ConsumerGroups.GetAsync(name, namespaceName, eventHubName, consumerGroupName).Result;
                            }
                            catch (Exception)
                            {
                            }

                            if (found == null)
                            {
                                var r = _eventHubManagementClient.ConsumerGroups.CreateOrUpdateAsync(name, namespaceName, eventHubName, consumerGroupName, consumerGroupParams).Result;
                            }

                            return ApiResult.CreateSuccess("A consumer group is created successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    }
                }
            }

            return ApiResult.CreateError("Error encountered while creating consumer group: " + consumerGroupName + "  Error: " + errorMessage);
        }

        /// <summary>
        /// DeleteConsumerGroup
        /// </summary>
        /// <param name="resourceGroupName">resourceGroupName</param>
        /// <param name="namespaceName">namespaceName</param>
        /// <param name="eventHubName">eventHubName</param>
        /// <param name="consumerGroupName">consumerGroupName</param>
        /// <param name="isIotHub">isIotHub</param>
        /// <returns>ApiResult which contains error or successful result as the case maybe</returns>
        public ApiResult DeleteConsumerGroup(string resourceGroupName, string namespaceName, string eventHubName, string consumerGroupName, string inputType)
        {
            Console.WriteLine("Deleting Consumer Group...");

            //Validate parameters
            if (string.IsNullOrEmpty(resourceGroupName) || string.IsNullOrEmpty(namespaceName))
            {
                return ApiResult.CreateError("ResourceGroup or Namespace empty while deleting consumer group: " + consumerGroupName);
            }

            string errorMessage = "";

            List<string> names = resourceGroupName.Split(',').Select(s => s.Trim()).ToList();
            if (names != null && names.Count() > 0)
            {
                foreach (string name in names)
                {
                    try
                    {
                        if (inputType == Constants.InputType_IoTHub)
                        {
                            string endpoint = $"https://management.azure.com/subscriptions/{_subscriptionId}/resourceGroups/{name}/providers/Microsoft.Devices/IotHubs/{eventHubName}/eventHubEndpoints/events/ConsumerGroups/{consumerGroupName}?api-version=2018-04-01";
                            if (DeleteIotHubEndpointConsumerGroup(endpoint))
                            {
                                return ApiResult.CreateSuccess("A consumer group is deleted successfully");
                            }
                        }
                        else
                        {
                            ConsumerGroup found = null;

                            try
                            {
                                found = _eventHubManagementClient.ConsumerGroups.GetAsync(name, namespaceName, eventHubName, consumerGroupName).Result;
                            }
                            catch (Exception)
                            {
                            }

                            if (found != null)
                            {
                                var r = _eventHubManagementClient.ConsumerGroups.DeleteAsync(name, namespaceName, eventHubName, consumerGroupName);
                            }

                            return ApiResult.CreateSuccess("A consumer group is deleted successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    }
                }
            }

            return ApiResult.CreateError("Error encountered while deleting consumer group: " + consumerGroupName + "  Error: " + errorMessage);
        }

        public bool CreateIotHubEndpointConsumerGroup(string endpoint)
        {
            bool consumerGroupCreated = false;
            HttpResponseMessage response = null;
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);

                response = httpClient.PutAsync(endpoint, new StringContent("")).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    consumerGroupCreated = true;
                }
                else
                {
                    throw new Exception(response.Content.ReadAsStringAsync().Result);
                }
            }
            return consumerGroupCreated;
        }

        /// <summary>
        /// DeleteIotHubEndpointConsumerGroup api that deleted IotHub consumer group
        /// </summary>
        /// <param name="endpoint">endpoint</param>
        /// <returns>ApiResult which contains error or successful result as the case maybe</returns>
        public bool DeleteIotHubEndpointConsumerGroup(string endpoint)
        {
            bool consumerGroupDeleted = false;
            HttpResponseMessage response = null;
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);

                response = httpClient.DeleteAsync(endpoint).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    consumerGroupDeleted = true;
                }
                else
                {
                    throw new Exception(response.Content.ReadAsStringAsync().Result);
                }
            }
            return consumerGroupDeleted;
        }

        private string GetAccessToken(string tenantId, string clientId, string secretKey)
        {
            var authenticationContext = new AuthenticationContext($"https://login.windows.net/{tenantId}");
            var credential = new ClientCredential(clientId, secretKey);
            var result = authenticationContext.AcquireTokenAsync("https://management.core.windows.net/", credential).Result;

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            var token = result.AccessToken;
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
                _eventHubManagementClient.Dispose();
            }
        }
    }
}

