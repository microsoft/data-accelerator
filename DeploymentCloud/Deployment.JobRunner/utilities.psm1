#  Stop execution if any exception happens at some step of the script
$ErrorActionPreference = "stop"

#
#  Login
#
#  Prompts the caller authentication in order to be granted access to the target subscription id and tenant id
#
function Login([string]$subscriptionId, [string]$tenantId)
{
    # sign in
    Write-Host "Logging in..."

    # select subscription
    Write-Host "Selecting subscription '$subscriptionId'"
    Write-Host "Checking credential..."
    Write-Host "Logging in for AzureRM"
    $azurermsub = Select-AzureRmSubscription -SubscriptionId $subscriptionId -ErrorAction SilentlyContinue
    if (!($azurermsub)) {
        Connect-AzureRmAccount -TenantId $tenantId
        $azurermsub = Select-AzureRmSubscription -SubscriptionId $subscriptionId
    }
    try {
        az account set --subscription $subscriptionId > $null 2>&1
    }
    catch {}

    if (!$?) {
        Write-Host "Logging in for AzureCLI"
        az login --tenant $tenantId
        az account set --subscription $subscriptionId
        if (!$?)
        {
            Write-Host "Can't access to the subscription. Please run 'az login' to sign in and try again"
            Exit 40
        }
    }

    Write-Host "Logging in for AzureAD"
    $acc = Connect-AzureAD -TenantId $tenantId
    $tenantName = $acc.Tenant.Domain
    $tenantId = $acc.Tenant.Id.Guid
    $userId = (az ad signed-in-user show --query 'objectId').Replace("""", "")

    # Check login info
    if (!($tenantName) -or !($tenantId) -or !($userId)) {
        Write-Host "Error on getting tenantName, tenantId or userId"
        Exit 100
    }

    # Output login info
    Write-Host "tenantId: " $tenantId
    Write-Host "tenantName: " $tenantName
    Write-Host "userId: " $userId

    @{
        subscriptionId = $subscriptionId
        tenantId = $tenantId
        tenantName = $tenantName
        userId = $userId
    }
}

#
#  Get-AppInfo
#
#  Retrieves additional identifiers from a given application id and tenant such object, authorityuri and app name
#
function Get-AppInfo([string]$applicationId, [string]$tenantName) 
{
    $app = Get-AzureRmADApplication -ApplicationId $applicationId

    if (!$app)
    {
        Write-Host "Error on getting the service application"
        Exit 30
    }

    $applicationId = $app.ApplicationId

    $servicePrincipal = Get-AzureRmADServicePrincipal -ApplicationId $applicationId
    if (!$servicePrincipal)
    {
        Write-Host "Error on getting the service application principal"
        Exit 30
    }

    
    $applicationId = $applicationId
    $objectId = $servicePrincipal.Id
    $identifierUri =  $app.IdentifierUris[0]
    $authorityUri = "https://login.microsoftonline.com/$tenantName"
    $appName = $app.DisplayName

    Write-Host "Application Id: $applicationId"
    Write-Host "Application name: $appName"
    Write-Host "Application Object Id: $objectId"
    Write-Host "Application identifier uri: $identifierUri"
    Write-Host "Authority Uri: $authorityUri"

    @{
        applicationId = $applicationId
        appName = $appName
        objectId = $objectId
        identifierUri = $identifierUri
        authorityUri = $authorityUri
    }
}

#
#  Get-ScenarioTesterInfo
#
#  Gathers information of existing deployed resources from a previous data-accelerator deployment. Information from this step will be used
#  to generate the resources used by Scenario Tester Job Runner
#
function Get-ScenarioTesterInfo
{
    param(
        [string]$subscriptionId, 
        [string]$resourceGroupName,
        [string]$flowName,
        [string]$scenarioTestKVBaseName
    )
    $blobContainerName="samples"
    $blobPath="iotdevice/ScenarioTest.json"
    $eventHubConnectionStringSource="iotsample-input-eventhubconnectionstring"
    $databricksTokenSource="iotsample-info-databricksToken"
    $referenceDataSource="iotsample-referencedata-devicesdata"
    $udfSampleSource="iotsample-jarpath-udfsample"
    $isIotHub = "true"
    $eventHubType = "iothub"

    $deployment = (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName | where DeploymentName -Like 'deployment-Resource-Template.json*')

    $productName = $deployment.Parameters.sites_web_name.Value

    $kvSparkName = $deployment.Parameters.vaults_sparkKV_name.Value

    $kvServicesName = $deployment.Parameters.vaults_servicesKV_name.Value

    $sparkStorageAccountName = $deployment.Parameters.storageAccounts_spark_name.Value

    $sparkType = $deployment.Parameters.sparkType.Value

    $sfClusterName = $deployment.Parameters.clusterName.Value

    $sfCluster = Get-AzureRmResource -ResourceGroupName $resourceGroupName -ResourceType Microsoft.ServiceFabric/clusters -Name $sfClusterName
    $sfClusterManageEndpoint = [uri]$sfCluster.Properties.managementEndpoint
    $sfClusterServiceUrl = $sfClusterManageEndpoint.scheme + "://" +$sfClusterManageEndpoint.Host
    Write-Host "SF Cluster management endpoint: $sfClusterServiceUrl"

    $blobStorageAccount = Get-AzureRmResource -ResourceGroupName $resourceGroupName -ResourceType Microsoft.Storage/storageAccounts -Name $sparkStorageAccountName 
    $blobPrimaryEndpoint = $blobStorageAccount.Properties.primaryEndpoints.blob
    $blobUri = $blobPrimaryEndpoint + $blobUriPath
    Write-Host "Blob uri path: $blobUri"

    $eventHubConnectionString = (Get-AzureKeyVaultSecret -VaultName $kvSparkName -Name $eventHubConnectionStringSource).SecretValueText
    Write-Host "Event hub connection string: $eventHubConnectionString"

    $csAsObject = $eventHubConnectionString -replace ";", "`n" | ConvertFrom-StringData
    $eventHubName = $csAsObject.EntityPath
    Write-Host "Event Hub name: $eventHubName"

    $referenceDataUri = (Get-AzureKeyVaultSecret -VaultName $kvSparkName -Name $referenceDataSource).SecretValueText
    Write-Host "Reference data uri: $referenceDataUri"

    $udfSampleUri = (Get-AzureKeyVaultSecret -VaultName $kvSparkName -Name $udfSampleSource).SecretValueText
    Write-Host "Udf sample uri: $udfSampleUri"

    Write-Host "Spark type: $sparkType"
    $databricksToken = ""

    if($sparkType.Equals("databricks"))
    {
        Write-Host "Fetching databricks token from secret"
        $databricksToken = (Get-AzureKeyVaultSecret -VaultName $kvSparkName -Name $databricksTokenSource).SecretValueText
    }
    else {
        Write-Host "Not fetching token, is not databricks"
    }

    Write-Host "Databricks token: $databricksToken"

    if($sparkType.Equals("databricks")) 
    {
        $jsonFileName = "ScenarioTestDatabricks.json"
    }
    else
    {
        $jsonFileName = "ScenarioTestHDInsights.json"
    }

    Write-Host "Json file name $jsonFileName"

    if($sparkType.Equals("databricks")) 
    {
        $kvSparkBase = "secretscope://$kvSparkName"
    }
    else
    {
        $kvSparkBase = "keyvault://$kvSparkName"
    }

    $eventHubConnectionStringKVName = "${scenarioTestKVBaseName}-input-eventhubconnectionstring"
    $referenceDataKVName = "${scenarioTestKVBaseName}-referencedata-devicesdata"
    $udfSampleKVName = "${scenarioTestKVBaseName}-jarpath-udfsample"

    $jsonInputFilePath = $PSScriptRoot + "\" + $jsonFileName
    $scenarioTestJson = Get-Content -Raw -Path $jsonInputFilePath
    $scenarioTestJson = $scenarioTestJson.Replace('$flowName', $flowName)
    $scenarioTestJson = $scenarioTestJson.Replace('$eventHubName', $eventHubName)
    $scenarioTestJson = $scenarioTestJson.Replace('$eventHubType', $eventHubType)
    $scenarioTestJson = $scenarioTestJson.Replace('$referenceDataUri', "$kvSparkBase/$referenceDataKVName")
    $scenarioTestJson = $scenarioTestJson.Replace('$udfSampleUri', "$kvSparkBase/$udfSampleKVName")
    $scenarioTestJson = $scenarioTestJson.Replace('$subscriptionId', $subscriptionId)
    $scenarioTestJson = $scenarioTestJson.Replace('$eventHubConnectionString', "$kvSparkBase/$eventHubConnectionStringKVName")
    if($sparkType.Equals("databricks")) 
    {
        $scenarioTestJson = $scenarioTestJson.Replace('$databricksToken', $databricksToken)
    }
    $jsonOutputFilePath = $PSScriptRoot + "\ScenarioTest.json"
    Set-Content -Path $jsonOutputFilePath -Value $scenarioTestJson
    $storageAccountKey = (Get-AzureRmStorageAccountKey -ResourceGroupName $resourceGroupName -AccountName $sparkStorageAccountName).Value[0]
    Write-Host "Storage account Key: $storageAccountKey"
    $ctx = New-AzureStorageContext -StorageAccountName $sparkStorageAccountName -StorageAccountKey $storageAccountKey
    $blobUri = (Set-AzureStorageBlobContent -File "ScenarioTest.json" -Force `
        -Container $blobContainerName `
        -Blob $blobPath `
        -Context $ctx).ICloudBlob.uri.AbsoluteUri
    Write-Host "Blob uri: $blobUri"

    @{
        productName = $productName
        blobUri = $blobUri
        eventHubName = $eventHubName
        eventHubConnectionString = $eventHubConnectionString
        isIotHub = $isIotHub
        serviceUrl = $sfClusterServiceUrl
        jobRunnerName = $productName + $flowName + $sparkType
        databricksToken = $databricksToken
        referenceDataUri = $referenceDataUri
        udfSampleUri = $udfSampleUri
        kvSparkName = $kvSparkName
        kvServicesName = $kvServicesName
        sparkStorageAccountName = $sparkStorageAccountName
        eventHubConnectionStringKVName = $eventHubConnectionStringKVName
        referenceDataKVName = $referenceDataKVName
        udfSampleKVName = $udfSampleKVName
        databricksTokenKVName = "${scenarioTestKVBaseName}-info-databricksToken"
    }
}

#
#  Get-KVKeyInfo
#
#  Utility function to easily retrieve secrets from a data-accelerator deployment given a base name prefix. This prefix is part of the initial parameters
#  of the previous data-accelerator deployment.
#
function Get-KVKeyInfo {
    param(
        [string]$baseNamePrefix
    )

    return @{
        testClientId = "$baseNamePrefix-testClientId"
        secretKeyKVName = "$baseNamePrefix-scenarioTester-secretKey"
        clientIdKVName = "$baseNamePrefix-scenarioTester-clientId"
    }
}

Export-ModuleMember -Function *