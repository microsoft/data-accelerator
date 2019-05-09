#!/bin/bash
export AzureServicesAuthConnectionString="RunAs=App;AppId=${AppId};TenantId=${TenantId};AppKey=${ClientSecret}"
dotnet Flow.ManagementService.dll