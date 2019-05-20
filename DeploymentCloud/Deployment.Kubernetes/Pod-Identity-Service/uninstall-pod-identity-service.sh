#!/bin/bash

kubectl delete azureidentity managed-service-identity
kubectl delete azureidentitybinding managed-service-identity-binding
kubectl delete -f ./deployment.yaml 