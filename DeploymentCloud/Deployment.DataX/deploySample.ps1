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

if ($deploySample -ne 'y') {
    Write-Host "deploySample parameter value is not 'y'. This script will not execute."
    Exit 0
}

$rootFolderPath = $PSScriptRoot
Import-Module "..\Deployment.Common\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName, $productName, $sparkClusterName -WarningAction SilentlyContinue

# Check if file paths exist
function Check-FilePath {    
    $paths = @("$packageFileFolder\MicrosoftDataXSimulatedData")
    foreach ($p in $paths) {
        if (!(Test-Path $p)) {
            Write-Host "$p does not exist"
            Exit 20
        }
    }
}

# Setup IotThub
# Add devices to IotHub
function Setup-IotHub {
    az account set --subscription $subscriptionId
    try {
        az extension add --name azure-cli-iot-ext
    } 
    catch {}

    For ($i=1; $i -le 9; $i++) {
        $devId = "sensortestdevice$i" + "_0"
        try {
            az iot hub device-identity create --hub-name $iotHubName --auth-method shared_private_key --device-id $devId > $null 2>&1
        }
        catch {}
    }
}

# Create secrets to keyVaults
function Setup-Secrets {
    $hub =  Get-AzureRmIotHub -ResourceGroupName $resourceGroupName -Name $iotHubName
    $endPoint = $hub.Properties.EventHubEndpoints.events.Endpoint
    $key = (Get-AzureRmIotHubKey -ResourceGroupName $resourceGroupName -Name $iotHubName -KeyName "iothubowner").PrimaryKey
    
    $iotHubConnectionString ="Endpoint=$endPoint;SharedAccessKeyName=iothubowner;SharedAccessKey=$key;EntityPath=$iotHubName"
    
    $vaultName = "$sparkKVName"
    $secretName = "iotsample-input-eventhubconnectionstring"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $iotHubConnectionString
	
	$userContentContainerLocation = -join("wasbs://", $userContentContainerName, "@", $sparkBlobAccountName, ".blob.core.windows.net/iotsample/")
    $secretName = "iotsample-referencedata-devicesdata"
    $secretValue = -join($userContentContainerLocation, "devices.csv");
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue

    $secretName = "iotsample-jarpath-udfsample"
    $secretValue = -join($userContentContainerLocation, "udfsample.jar");
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue
    
    $vaultName = "$servicesKVName"

    $secretName = "datagen-iotHubConnectionString"
    $iotHubConnectionString = (Get-AzureRmIotHubConnectionString -ResourceGroupName $resourceGroupName -Name $iotHubName -KeyName "iotHubowner").PrimaryConnectionString
    $value = $iotHubConnectionString + ";DeviceId={0}"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $value
}

function Add-CosmosDBData ([string]$filePath) {
    $fileName = (Get-Item $filePath).BaseName
    $tokens = Get-Tokens
    $outputPath = Get-OutputFilePath
    $outputFile = Join-Path $outputPath "_$fileName"
    $rawData = Get-Content -Raw -Path $filePath
    $rawData = Translate-Tokens -Source $rawData -Tokens $tokens
    $json = ConvertFrom-Json -InputObject $rawData

    try
    {
        Import-Module -Name Mdbc  
        $dbCon = Get-CosmosDBConnectionString -Name $docDBName
        Connect-Mdbc -ConnectionString $dbCon -DatabaseName "production"
        $collection1 = $Database.GetCollection("flows")

        $json | ConvertTo-Json -Depth 8 | Set-Content -Encoding Unicode $outputFile
        $input = Import-MdbcData $outputFile -FileFormat Json
        $response = Add-MdbcData -InputObject $input -Collection $collection1 -NewId
        if (!$response) {
            throw
        }
    }
    catch{}
    
    Remove-Module Mdbc  
}

function Get-Tokens {
    $tokens = Get-DefaultTokens

    $tokens.Add('resourceLocation', $resourceGroupLocation)
    $tokens.Add('iotHubName', $iotHubName)
    $tokens.Add('clientSecretPrefix', $clientSecretPrefix)

    $tokens
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

# Deploy IotHub and add devices to the IotHub
# This will take about 5 mins 
$userContentContainerName = "usercontent"
$templatePath = $resourcesTemplatePath
$tokens = Get-Tokens

Write-Host -ForegroundColor Green "Deploying Resources... (14/16 steps)"
Write-Host -ForegroundColor Green "Estimated time to complete: 6 mins"
Deploy-Resources -templateName "Sample-Template.json" -paramName "Sample-parameter.json" -templatePath $templatePath -tokens $tokens
Setup-IotHub
# Add secret for IotHub connection string to Keyvault  
Setup-Secrets

# Install nuget packages and prepare to deploy
# This will take about 2 mins 
$packageFileFolder = $packagesPath

Write-Host -ForegroundColor Green "Downloading package... (15/16 steps)"
Write-Host -ForegroundColor Green "Estimated time to complete: 1 min"
$packageNames = New-Object 'System.Collections.Generic.Dictionary[String,String]'
$packageNames.Add('Microsoft.DataX.SimulatedData', $MicrosoftDataXSimulatedData)
Install-Packages -packageNames $packageNames -targetPath $packageFileFolder -source $feedUrl
Check-FilePath
Prepare-Packages -packageFilePath $packageFileFolder

# Fix parameters
$parameterFileFolder = $parametersOutputPath
$parameterPath = Fix-ApplicationTypeVersion -parametersPath $parameterFileFolder -packagesPath $packageFileFolder

# Copy files Simulated service needs
Add-CosmosDBData -filePath ".\Samples\flows\iotsample-product.json"
Deploy-Files -saName $sparkBlobAccountName -containerName "samples" -filter "iotsample.json" -filesPath ".\Samples\samples\iotDevice" -targetPath "iotdevice"
Deploy-Files -saName $sparkBlobAccountName -containerName "samples" -filter "iotsample-091610A6B869B8C9318E988F390F46AB.json" -filesPath ".\Samples\samples" -targetPath ""
Deploy-Files -saName $sparkBlobAccountName -containerName $userContentContainerName -filter "*.*" -filesPath ".\Samples\usercontent" -targetPath "iotsample"

# Deploy Simulated service
# This will take about 10 min
Write-Host -ForegroundColor Green "Deploying package... (16/16 steps)"
Write-Host -ForegroundColor Green "Estimated time to complete: 15 mins for a clean install. For an upgrade, it will take longer."
$svcPackageNames = @("DataX.SimulatedData")
$templatePath = $servicesTemplatePath
Deploy-Services -packageNames $svcPackageNames -templatePath $templatePath -parameterPath $parameterPath

# Clean up \Temp folder
Write-Host "Cleaning up the cache files..."
CleanUp-Folder -FolderName $parameterPath
CleanUp-Folder -FolderName $packageFileFolder

$websiteUrl = "https://$name.azurewebsites.net/home"
start $websiteUrl

Exit 0