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
Import-Module "..\Deployment.Common\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName, $productName, $sparkClusterName, $randomizeProductName, $serviceFabricClusterName -WarningAction SilentlyContinue

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

function Get-KafkaHosts {
    $login = Get-Secret -VaultName $sparkRDPKVName -SecretName "kafkaLogin"
    $pwd = Get-Secret -VaultName $sparkRDPKVName -SecretName "kafkaclusterloginpassword"

    $password = $pwd | ConvertTo-SecureString -asPlainText -Force
    $creds = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $login, $password
    $resp = Invoke-WebRequest -Uri "https://$kafkaName.azurehdinsight.net/api/v1/clusters/$kafkaName/services/KAFKA/components/KAFKA_BROKER" -Credential $creds
    $respObj = ConvertFrom-Json $resp.Content
    $brokerHosts = $respObj.host_components.HostRoles.host_name[0..1]
    $hosts = ($brokerHosts -join ":9092,") + ":9092"
    return $hosts
}

# Create secrets to keyVaults
function Setup-Secrets {
    $userContentContainerLocation = -join("wasbs://", $userContentContainerName, "@", $sparkBlobAccountName, ".blob.core.windows.net/iotsample/")
    
    # IotHub
    $hub =  Get-AzureRmIotHub -ResourceGroupName $resourceGroupName -Name $iotHubName
    $endPoint = $hub.Properties.EventHubEndpoints.events.Endpoint
    $key = (Get-AzureRmIotHubKey -ResourceGroupName $resourceGroupName -Name $iotHubName -KeyName "iothubowner").PrimaryKey
    
    $iotHubConnectionString ="Endpoint=$endPoint;SharedAccessKeyName=iothubowner;SharedAccessKey=$key;EntityPath=$iotHubName"

    $vaultName = "$sparkKVName"
    $secretName = "iotsample-input-eventhubconnectionstring"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $iotHubConnectionString
	
	$secretName = "iotsample-referencedata-devicesdata"
    $secretValue = -join($userContentContainerLocation, "devices.csv");
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue

    $secretName = "iotsample-jarpath-udfsample"
    $secretValue = -join($userContentContainerLocation, "udfsample.jar");
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue

    # EventHub
    $eventHubConnectionString = (Invoke-AzureRmResourceAction -ResourceGroupName $resourceGroupName -ResourceType Microsoft.EventHub/namespaces/eventhubs/authorizationRules -ResourceName "$kafkaEventHubNamespaceName/kafka1/listen" -Action listKeys -ApiVersion 2015-08-01 -Force).primaryConnectionString
    $secretName = "eventhub-input-eventhubconnectionstring"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $eventHubConnectionString
	
	$secretName = "eventhub-referencedata-devicesdata"
    $secretValue = -join($userContentContainerLocation, "devices.csv");
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue

    $secretName = "eventhub-jarpath-udfsample"
    $secretValue = -join($userContentContainerLocation, "udfsample.jar");
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue
	
    $storageAccount = Get-AzureRmStorageAccount -resourceGroupName $resourceGroupName -Name $sparkBlobAccountName
    $secretName = "datax-sa-fullconnectionstring-" + $sparkBlobAccountName    
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $storageAccount.Context.ConnectionString
	
    if($setupAdditionalResourcesForSample -eq 'y'){
		$sqlCosmosdbCon = Get-CosmosDBConnectionString -Name $sqlDocDBName
		$secretName = "output-samplecosmosdb"
	    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $sqlCosmosdbCon
    }

    # EventHub Batch sample
	$secretName = "eventhubbatch-referencedata-devicesdata"
    $secretValue = -join($userContentContainerLocation, "devices.csv");
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue

    $secretName = "eventhubbatch-jarpath-udfsample"
    $secretValue = -join($userContentContainerLocation, "udfsample.jar");
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue

    $blobsparkconnectionString = Get-StorageAccountConnectionString -Name $sparkBlobAccountName
    $secretName = "eventhubbatch-input-0-inputConnection"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $blobsparkconnectionString
    
    $secretName = "eventhubbatch-output-myazureblob"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $blobsparkconnectionString
    
    $eventHubBatchSamplePath = -join("wasbs://", $sampleContainerName, "@", $sparkBlobAccountName, ".blob.core.windows.net/eventhubbatch/")
    $secretName = "eventhubbatch-input-0-inputPath"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $eventHubBatchSamplePath

    $vaultName = "$servicesKVName"
    
    $secretName = "datagen-iotHubConnectionString"
    $iotHubConnectionString = (Get-AzureRmIotHubConnectionString -ResourceGroupName $resourceGroupName -Name $iotHubName -KeyName "iotHubowner").PrimaryConnectionString
    $value = $iotHubConnectionString + ";DeviceId={0}"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $value

    $secretName = "datagen-kafkaEventHubConnectionString"
    $kafkaEventHubConnectionString = (Invoke-AzureRmResourceAction -ResourceGroupName $resourceGroupName -ResourceType Microsoft.EventHub/namespaces/AuthorizationRules -ResourceName "$kafkaEventHubNamespaceName/listen" -Action listKeys -ApiVersion 2015-08-01 -Force).primaryConnectionString
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $kafkaEventHubConnectionString
    
    if($enableKafkaSample -eq 'y') {
        # Kafka
        # NativeKafka
        $vaultName = "$sparkKVName"
        $kafkaHosts = Get-KafkaHosts
        $secretName = "nativekafka-input-eventhubconnectionstring"
        Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $kafkaHosts
        
        $secretName = "nativekafka-referencedata-devicesdata"
        $secretValue = -join($userContentContainerLocation, "devices.csv");
        Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue

        $secretName = "nativekafka-jarpath-udfsample"
        $secretValue = -join($userContentContainerLocation, "udfsample.jar");
        Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue

        # EventHubKafka
        $secretName = "eventhubkafka-input-eventhubconnectionstring"
        Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $kafkaEventHubConnectionString
        
        $secretName = "eventhubkafka-referencedata-devicesdata"
        $secretValue = -join($userContentContainerLocation, "devices.csv");
        Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue

        $secretName = "eventhubkafka-jarpath-udfsample"
        $secretValue = -join($userContentContainerLocation, "udfsample.jar");
        Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $secretValue

        $vaultName = "$servicesKVName"

        $secretName = "datagen-kafkaNativeConnectionString"
        Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $kafkaHosts
    }
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

    $tokens.Add('subscriptionId', $subscriptionId)
    $tokens.Add('resourceLocation', $resourceGroupLocation)
    $tokens.Add('iotHubName', $iotHubName)
    $tokens.Add('kafkaName', $kafkaName)
    $tokens.Add('kafkaEventHubNamespaceName', $kafkaEventHubNamespaceName)
    $tokens.Add('clientSecretPrefix', $clientSecretPrefix)

    $deploymentDate = Get-Date -Format "yyyy-MM-dd"
    $tokens.Add('deploymentDate', $deploymentDate)
	
	$keyvaultPrefix = 'keyvault'
	if ($useDatabricks -eq 'y') {
		$keyvaultPrefix = 'secretscope'
	}
	$tokens.Add('keyvaultPrefix', $keyvaultPrefix)
	
	# Output section of DataX Query for samples
	$dataxQueryOutputForEventhubSample = '\n\nOUTPUT DoorUnlockedSimpleAlert TO myAzureBlob;\nOUTPUT GarageOpenForFiveMinsWindowAlert TO myAzureBlob;\nOUTPUT GarageOpenFor30MinutesInHourThresholdAlert TO myAzureBlob;\nOUTPUT WindowOpenFiveMinWhileHeaterOnCombinedAlert TO myAzureBlob;'
	$dataxQueryOutputForNativeKafkaSample = ''
	
	if ($setupAdditionalResourcesForSample -eq 'y') {
		$dataxQueryOutputForEventhubSample += '\n\nOUTPUT DoorUnlockedSimpleAlert TO myCosmosDB;\nOUTPUT GarageOpenForFiveMinsWindowAlert TO myCosmosDB;\nOUTPUT GarageOpenFor30MinutesInHourThresholdAlert TO myCosmosDB;\nOUTPUT WindowOpenFiveMinWhileHeaterOnCombinedAlert TO myCosmosDB;'
		$dataxQueryOutputForNativeKafkaSample += '\n\nOUTPUT DoorUnlockedSimpleAlert TO myCosmosDB;\nOUTPUT GarageOpenForFiveMinsWindowAlert TO myCosmosDB;\nOUTPUT GarageOpenFor30MinutesInHourThresholdAlert TO myCosmosDB;\nOUTPUT WindowOpenFiveMinWhileHeaterOnCombinedAlert TO myCosmosDB;'
	}
	
	$tokens.Add('dataxQueryOutputForEventhubSample', $dataxQueryOutputForEventhubSample)
	$tokens.Add('dataxQueryOutputForNativeKafkaSample', $dataxQueryOutputForNativeKafkaSample)
	
	# Output Sinks for samples
	$outputSinkForEventhubSample = '{"id" : "Metrics", "type" : "metric", "properties" : {}, "typeDisplay" : "Metrics"}, { "id" : "myAzureBlob", "type" : "blob", "properties": { "connectionString": "'+$keyvaultPrefix+'://'+$sparkKVName+'/datax-sa-fullconnectionstring-'+$sparkBlobAccountName+'", "containerName": "outputs", "blobPrefix": "EventHub", "blobPartitionFormat": "yyyy/MM/dd/HH", "format": "json", "compressionType": "none"}, "typeDisplay" : "Azure Blob"}'
	$outputSinkForNativeKafkaSample = '{"id" : "Metrics", "type" : "metric", "properties" : {}, "typeDisplay" : "Metrics"}'
			
	if ($setupAdditionalResourcesForSample -eq 'y') {
		$outputSinkForEventhubSample += ',{ "id" : "myCosmosDB", "type" : "cosmosdb", "properties" : { "connectionString" : "'+$keyvaultPrefix+'://'+$sparkKVName+'/output-samplecosmosdb", "db" : "dataxdb", "collection" : "eventhubsample"}, "typeDisplay" : "Cosmos DB"}'
		$outputSinkForNativeKafkaSample += ',{ "id" : "myCosmosDB", "type" : "cosmosdb", "properties" : { "connectionString" : "'+$keyvaultPrefix+'://'+$sparkKVName+'/output-samplecosmosdb", "db" : "dataxdb", "collection" : "nativekafkasample"}, "typeDisplay" : "Cosmos DB"}'
	}
	
	$tokens.Add('outputSinkForEventhubSample', $outputSinkForEventhubSample)
	$tokens.Add('outputSinkForNativeKafkaSample', $outputSinkForNativeKafkaSample)

    $tokens
}

function Setup-SecretsForKafka {
    $vaultName = "$sparkRDPKVName"
    
    $secretName = "kafkaLogin" 
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $kafkaLogin

    $secretName = "kafkaclusterloginpassword"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $kafkaPwd

    $secretName = "kafkasshuser" 
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $kafkasshuser

    $secretName = "kafkasshpassword" 
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $kafkaSshPwd
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
$sampleContainerName = "samples"
$templatePath = $resourcesTemplatePath
$tokens = Get-Tokens

$sqlDocDBName = 'sql' + $docDBName
$tokens.Add('sqlDocDBName', $sqlDocDBName)

Write-Host -ForegroundColor Green "Deploying Resources... (14/16 steps)"
Write-Host -ForegroundColor Green "Estimated time to complete: 6 mins"

Write-Host -ForegroundColor Green "For the sample Flow, deploying IotHub..."
Deploy-Resources -templateName "IotHub-Template.json" -paramName "IotHub-parameter.json" -templatePath $templatePath -tokens $tokens
Setup-IotHub

if($setupAdditionalResourcesForSample -eq 'y') {
    Write-Host -ForegroundColor Green "Deploying SQL type CosmosDB for output sample"
    Write-Host -ForegroundColor Green "Estimated time to complete: 7 mins"
    Deploy-Resources -templateName "SampleOutputs-Template.json" -paramName "SampleOutputs-Parameter.json"  -templatePath $templatePath -tokens $tokens
}

if($enableKafkaSample -eq 'y') {
    Setup-SecretsForKafka
    Write-Host -ForegroundColor Green "For the sample Flow, deploying Kafka..."
    Deploy-Resources -templateName "Kafka-Template.json" -paramName "Kafka-parameter.json" -templatePath $templatePath -tokens $tokens
}

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
Add-CosmosDBData -filePath ".\Samples\flows\eventhub-product.json"
Add-CosmosDBData -filePath ".\Samples\flows\eventhubbatch-product.json"

if($enableKafkaSample -eq 'y') {
    Add-CosmosDBData -filePath ".\Samples\flows\nativekafka-product.json"
    Add-CosmosDBData -filePath ".\Samples\flows\eventhubkafka-product.json"
}

Deploy-Files -saName $sparkBlobAccountName -containerName $sampleContainerName -filter "iotsample.json" -filesPath ".\Samples\samples\iotDevice" -targetPath "iotdevice"
Deploy-Files -saName $sparkBlobAccountName -containerName $sampleContainerName -filter "eventhubbatch-sample.json" -filesPath ".\Samples\samples\eventHubBatch" -targetPath "eventhubbatch"
Deploy-Files -saName $sparkBlobAccountName -containerName $sampleContainerName -filter "*.json" -filesPath ".\Samples\samples" -targetPath ""
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