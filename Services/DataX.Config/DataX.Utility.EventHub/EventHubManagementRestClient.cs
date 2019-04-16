// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.Management.EventHub;
using Microsoft.Azure.Management.EventHub.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using DataX.Contract.Exception;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Utility.EventHub
{
    public class EventHubManagementRestClient
    {
        private readonly string _clientId;
        private readonly string _tenantId;
        private readonly string _token;

        private Lazy<EventHubManagementClient> _managementClient;

        public EventHubManagementRestClient(string clientId, string tenantId, string secretKey, string subscriptionId)
        {
            _clientId = clientId;
            _tenantId = tenantId;

            _token = GetAccessToken(_tenantId, _clientId, secretKey);
            var creds = new TokenCredentials(_token);

            _managementClient = new Lazy<EventHubManagementClient>(
                () => new EventHubManagementClient(creds) { SubscriptionId = subscriptionId },
                isThreadSafe: true);
        }

        private static string GetAccessToken(string tenantId, string clientId, string secretKey)
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

        public async Task<bool> CreateIotHubEndpointConsumerGroup(string subscriptionId, string resourceGroupName, string hubName, string consumerGroupName)
        {
            string endpoint = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Devices/IotHubs/{hubName}/eventHubEndpoints/events/ConsumerGroups/{consumerGroupName}?api-version=2018-04-01";

            HttpResponseMessage response = null;
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);

                response = await httpClient.PutAsync(endpoint, new StringContent(""));
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    throw new GeneralException(msg);
                }
            }
        }

        public async Task<bool> UpsertEventHubConsumerGroup(string resourceGroupName, string hubNamespace, string hubName, string consumerGroupName)
        {
            var found = await TryGetEventHubConsumerGroup(resourceGroupName: resourceGroupName,
                            hubNamespace: hubNamespace,
                            hubName: hubName,
                            consumerGroupName: consumerGroupName);

            if (found != null)
            {
                return true;
            }

            var consumerGroupParams = new ConsumerGroup();
            var client = _managementClient.Value;

            try
            {
                var result = await client.ConsumerGroups.CreateOrUpdateAsync(resourceGroupName: resourceGroupName,
                                    namespaceName: hubNamespace,
                                    eventHubName: hubName,
                                    consumerGroupName: consumerGroupName,
                                    parameters: consumerGroupParams);

                return result != null;
            }
            catch(ErrorResponseException ere)
            {
                throw new GeneralException($"Failed to create consumer group '{consumerGroupName}' for eventhub '{hubName}' due to server returned error: {ere.Message}, detailed response:{ere.Response.Content}", ere);
            }
        }

        public async Task<ConsumerGroup> TryGetEventHubConsumerGroup(string resourceGroupName, string hubNamespace, string hubName, string consumerGroupName)
        {
            var client = _managementClient.Value;

            try
            {
                return await client.ConsumerGroups.GetAsync(resourceGroupName: resourceGroupName,
                                namespaceName: hubNamespace,
                                eventHubName: hubName,
                                consumerGroupName: consumerGroupName);
            }
            catch
            {
                return null;
            }
        }
    }
}
