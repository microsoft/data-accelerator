param(
    [string]
    $ParamFile = "adminsteps.parameters.txt"
)

$ErrorActionPreference = "stop"

# Initialize names
Get-Content $ParamFile | Foreach-Object{
    $l = $_.Trim()
    if ($l.startsWith('#') -or $l.startsWith('//') -or !$l) {
        return    
    }

    $var = $l.Split('=', 2)
    set-Variable -Name $var[0] -Value $var[1]
}

$rootFolderPath = $PSScriptRoot

Import-Module "..\Deployment.Common\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName -WarningAction SilentlyContinue 

function IsToken([string] $value) {
    return $value.StartsWith("$")
}

#******************************************************************************
# Script body
# Execution begins here
#******************************************************************************
$ErrorActionPreference = "stop"

Push-Location $PSScriptRoot

if ((IsToken -value $tenantId) -or (IsToken -value $serviceAppId) -or (IsToken -value $clientAppId)) {
    Write-Host "tenantId, serviceAppId and clientAppId should not be empty. Please run deployResources.ps1 or set the values manualy. For the manual steps, please refer to <Manual Steps: Admin steps (Get ApplicationId of AAD app)>"
    Exit 20
}

Write-Host "Signing in '$tenantId'"
az login --tenant $tenantId

Set-AzureAADAccessControl -AppId $serviceAppId
Set-AzureAADApiPermission -ServiceAppId $serviceAppId -ClientAppId $clientAppId -RoleName $writerRole

Exit 0