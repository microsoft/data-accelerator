#!/bin/bash

set -ex

ClientId="$(az identity list --resource-group $AzureResourceGroupName --subscription $SubscriptionId --query "[0].clientId" --output tsv)"
ManagedServiceIdentityName="$(az identity list --resource-group $AzureResourceGroupName --subscription $SubscriptionId --query "[0].name" --output tsv)"

sed -i 's/{SubscriptionId}/'$SubscriptionId'/g' managed-service-identity.yaml
sed -i 's/{ResourceGroupName}/'$AzureResourceGroupName'/g' managed-service-identity.yaml
sed -i 's/{ManagedIdentityName}/'$ManagedServiceIdentityName'/g' managed-service-identity.yaml
sed -i 's/{ClientId}/'$ClientId'/g' managed-service-identity.yaml

cat managed-service-identity.yaml

## todo use helm chart

kubectl apply -f deployment.yaml
kubectl apply -f managed-service-identity.yaml
kubectl apply -f managed-service-identity-binding.yaml