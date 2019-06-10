// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.Management.EventHub;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataX.Flow.Common;

namespace DataX.Utility.EventHub
{
    public static class EventHubUtil
    {
        public static async Task<Result> CreateIotHubConsumerGroup(string clientId,
                                                    string tenantId,
                                                    string secretKey,
                                                    string subscriptionId,
                                                    string resourceGroupName,
                                                    string hubName,
                                                    string consumerGroupName)
        {
            return await UpsertHubConsumerGroup(
                clientId: clientId,
                tenantId: tenantId,
                secretKey: secretKey,
                subscriptionId: subscriptionId,
                hubName: hubName,
                consumerGroupName: consumerGroupName,
                resourceGroupName: resourceGroupName,
                op: (client) => client.CreateIotHubEndpointConsumerGroup(
                   subscriptionId: subscriptionId,
                   resourceGroupName: resourceGroupName,
                   hubName: hubName,
                   consumerGroupName: consumerGroupName));
        }

       public static async Task<Result> CreateEventHubConsumerGroups(string clientId,
                                                    string tenantId,
                                                    string secretKey,
                                                    string subscriptionId,
                                                    string resourceGroupName,
                                                    string hubNamespace,
                                                    string hubNames,
                                                    string consumerGroupName)
        {
            Ensure.NotNull(hubNamespace, "hubNamespace");

            Result result = null;
            var ehNames = Helper.ParseEventHubNames(hubNames);

            foreach(var ehName in ehNames)
            {
                result = await UpsertHubConsumerGroup(
                    clientId: clientId,
                    tenantId: tenantId,
                    secretKey: secretKey,
                    subscriptionId: subscriptionId,
                    hubName: ehName,
                    consumerGroupName: consumerGroupName,
                    resourceGroupName: resourceGroupName,
                    op: (client) => client.UpsertEventHubConsumerGroup(
                        resourceGroupName: resourceGroupName,
                        hubNamespace: hubNamespace,
                        hubName: ehName,
                        consumerGroupName: consumerGroupName));

                Ensure.IsSuccessResult(result);
            }

            return result;
        }

        public static async Task<Result> UpsertHubConsumerGroup(string clientId,
                                                    string tenantId,
                                                    string secretKey,
                                                    string subscriptionId,
                                                    string hubName,
                                                    string consumerGroupName,
                                                    string resourceGroupName,
                                                    Func<EventHubManagementRestClient, Task<bool>> op)
        {
            Ensure.NotNull(clientId, "clientId");
            Ensure.NotNull(tenantId, "tenantId");
            Ensure.NotNull(secretKey, "secretKey");
            Ensure.NotNull(subscriptionId, "subscriptionId");
            Ensure.NotNull(hubName, "hubName");
            Ensure.NotNull(consumerGroupName, "consumerGroupName");
            Ensure.NotNull(resourceGroupName, "resourceGroupName");
            
            var client = new EventHubManagementRestClient(clientId, tenantId, secretKey, subscriptionId);
            var result = await op(client);

            var consumerGroupString = $"consumer group '{consumerGroupName}' for hub '{hubName}', resource group:'{resourceGroupName}'";
            if (!result)
            {
                return new FailedResult($"failed to create {consumerGroupString}.");
            }

            return new SuccessResult($"successfully created {consumerGroupString}.");
        }

    }
}
