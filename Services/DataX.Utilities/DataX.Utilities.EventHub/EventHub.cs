// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;

namespace DataX.Utilities.EventHub
{
    public static class EventHub
    {
         public static ApiResult CreateConsumerGroup(string subscriptionId, string keyvaultName, string resourceGroup, string resourceGroupLocation, string eventHubNamespace, string eventHubName, string consumerGroupName, string inputType, string clientId, string tenantId, string secretPrefix)
        {
            using (EventHubUtil eventHubUtil = new EventHubUtil(subscriptionId, resourceGroupLocation, keyvaultName, clientId, tenantId, secretPrefix))
            {
                return eventHubUtil.CreateConsumerGroup(resourceGroup, eventHubNamespace, eventHubName, consumerGroupName, inputType);
            }
        }

        public static ApiResult DeleteConsumerGroup(string subscriptionId, string keyvaultName, string resourceGroup, string resourceGroupLocation, string eventHubNamespace, string eventHubName, string consumerGroupName, string inputType, string clientId, string tenantId, string secretPrefix)
        {
            using (EventHubUtil eventHubUtil = new EventHubUtil(subscriptionId, resourceGroupLocation, keyvaultName, clientId, tenantId, secretPrefix))
            {
                return eventHubUtil.DeleteConsumerGroup(resourceGroup, eventHubNamespace, eventHubName, consumerGroupName, inputType);
            }
        }
    }
}
