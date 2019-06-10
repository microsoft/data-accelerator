param(
    [string]
    $ParamFile = "common.parameters.txt"
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

if ($deployApps -ne 'y') {
    Write-Host "deployApps parameter value is not 'y'. This script will not execute."
    Exit 0
}

$rootFolderPath = $PSScriptRoot
Import-Module "..\Deployment.Common\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName, $productName, $sparkClusterName -WarningAction SilentlyContinue 

# Check if file paths exist
function Check-FilePath {    
    $paths = @("$packageFileFolder\MicrosoftDataXFlow", "$packageFileFolder\MicrosoftDataXGateway", "$packageFileFolder\MicrosoftDataXMetrics", "$packageFileFolder\MicrosoftDataXWeb", "$packageFileFolder\MicrosoftDataXSpark")

    foreach ($p in $paths) {
        if (!(Test-Path $p)) {
            Write-Host "$p does not exist"
            Exit 20
        }
    }
}

# Deploy the frontend web app
function Deploy-Web([string]$packagesPath) {
    $package = Get-ChildItem -Path $packagesPath -Filter deployment.zip -File -Recurse
    
    [xml]$profile = [xml] (Get-AzureRmWebAppPublishingProfile -ResourceGroupName $resourceGroupName -Name $websiteName)
    
    $password = $profile.publishData.publishProfile[1].userPWD
    $token = "`$$websiteName" + ":$password"
    $apiUrl = "https://$websiteName.scm.azurewebsites.net/api/zipdeploy"
    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}" -f $token)))
    $userAgent = "powershell/1.0"
    Invoke-RestMethod -Uri $apiUrl -Headers @{Authorization = ("Basic {0}" -f $base64AuthInfo)} -UserAgent $userAgent -Method POST -InFile $package.FullName -ContentType "application/octet-steam" -TimeoutSec 3600    
}

#******************************************************************************
# Script body
# Execution begins here
#******************************************************************************
$ErrorActionPreference = "stop"

Push-Location $PSScriptRoot

# select subscription
Write-Host "Selecting subscription '$subscriptionId'"
Check-Credential -SubscriptionId $subscriptionId -TenantId $tenantId

# Install nuget packages and prepare to deploy
# This will take about 2 mins 
$packageFileFolder = $packagesPath

$packageNames = New-Object 'System.Collections.Generic.Dictionary[String,String]'
$packageNames.Add('Microsoft.DataX.Flow', $MicrosoftDataXFlow)
$packageNames.Add('Microsoft.DataX.Gateway', $MicrosoftDataXGateway)
$packageNames.Add('Microsoft.DataX.Metrics', $MicrosoftDataXMetrics)
$packageNames.Add('Microsoft.DataX.Web', $MicrosoftDataXWeb)
$packageNames.Add('Microsoft.DataX.Spark', $MicrosoftDataXSpark)

Write-Host -ForegroundColor Green "Downloading packages... (10/16 steps)"
Write-Host -ForegroundColor Green "Estimated time to complete: 5 mins"
Install-Packages -packageNames $packageNames -targetPath $packageFileFolder -source $feedUrl
Check-FilePath
Prepare-Packages -packageFilePath $packageFileFolder

# Fix parameters
$parameterFileFolder = $parametersOutputPath
$parameterPath = Fix-ApplicationTypeVersion -parametersPath $parameterFileFolder -packagesPath $packageFileFolder

# Deploy service fabric apps: DataX.Flow, DataX.Gateway and DataX.Metrics
Write-Host -ForegroundColor Green "Deploying packages... (11/16 steps)"
Write-Host -ForegroundColor Green "Estimated time to complete: 30 mins for a clean install. For an upgrade, it will take longer."
$svcPackageNames = @("DataX.Flow", "DataX.Gateway", "DataX.Metrics")
$templatePath = $servicesTemplatePath
Deploy-Services -packageNames $svcPackageNames -templatePath $templatePath -parameterPath $parameterPath

# Deploy web app
Write-Host -ForegroundColor Green "Deploying webapp... (12/16 steps)"
Write-Host -ForegroundColor Green "Estimated time to complete: 10 mins"
Deploy-Web -packagesPath $packageFileFolder

# Deploy DataX jar files
Write-Host -ForegroundColor Green "Deploying jar files... (13/16 steps)"
Write-Host -ForegroundColor Green "Estimated time to complete: 1 min"
Deploy-Files -saName $sparkBlobAccountName -containerName "defaultdx" -filter "*.jar" -filesPath $packageFileFolder -targetPath "datax\bin"

Exit 0