param(
    [string]
    $tenantId,
    [string]
    $subscriptionId,
    [string]
    $resourceGroupName,
    [string]
    $applicationId,
    [string]
    $appSecretKey,
    [string]
    $kvBaseNamePrefix='configgen',
    [string]
    $flowName='scenariotest',
    [string]
    $skipServerCertificateValidation='false'
)
Import-Module ./utilities.psm1

$ErrorActionPreference = "stop"

Push-Location $PSScriptRoot

$scenarioTestKVBaseName = $flowName

$jobRunnerBasePath = $PSScriptRoot
$resourcesPath = $jobRunnerBasePath + "\Resources"
$templatesPath = $resourcesPath + "\Templates"
$parametersPath = $resourcesPath + "\Parameters"
$jobRunnerTemplateFile = $templatesPath + "\jobRunner-Template.json"
$jobRunnerParameterFile = $parametersPath + "\JobRunner-Parameter.json"

Write-Host "Template json path: $jobRunnerTemplateFile"

Write-Host -ForegroundColor Green "Total estimated time to complete: 10 minutes"

$login = Login -subscriptionId $subscriptionId -tenantId $tenantId
$tenantName = $login.tenantName

$appAccInfo = Get-AppInfo -applicationId $applicationId -tenantName $tenantName

$stInfo = Get-ScenarioTesterInfo `
    -subscriptionId $subscriptionId `
    -resourceGroupName $resourceGroupName `
    -flowName $flowName `
    -scenarioTestKVBaseName $scenarioTestKVBaseName

$kvInfo = Get-KVKeyInfo -baseNamePrefix $kvBaseNamePrefix

$params = @{
    tenantId = $tenantId
    productName = $stInfo.productName
    applicationObjectId = $appAccInfo.objectId
    applicationId = $appAccInfo.applicationId
    applicationIdentifierUri = $appAccInfo.identifierUri
    authorityUri = $appAccInfo.authorityUri
    serviceUrl = $stInfo.serviceUrl
    secretKey = $appSecretKey
    kvServicesName = $stInfo.kvServicesName
    kvSparkName = $stInfo.kvSparkName
    blobUri = $stInfo.blobUri
    sparkStorageAccountName = $stInfo.sparkStorageAccountName
    eventHubName = $stInfo.eventHubName 
    eventHubConnectionString = $stInfo.eventHubConnectionString
    isIotHub = $stInfo.isIotHub
    jobRunnerName = $stInfo.jobRunnerName
    databricksToken = $stInfo.databricksToken
    referenceDataUri = $stInfo.referenceDataUri
    udfSampleUri = $stInfo.udfSampleUri
    skipServerCertificateValidation = $skipServerCertificateValidation
    eventHubConnectionStringKVName = $stInfo.eventHubConnectionStringKVName
    referenceDataKVName = $stInfo.referenceDataKVName
    udfSampleKVName = $stInfo.udfSampleKVName
    databricksTokenKVName = $stInfo.databricksTokenKVName
    secretKeyKVName = $kvInfo.secretKeyKVName
    clientIdKVName = $kvInfo.clientIdKVName
    testClientId = $kvInfo.testClientId
}

$templateName = $flowName

New-AzureRmResourceGroupDeployment `
-Name "deployment-$templateName-$(get-date -f MM-dd-yyyy_HH_mm_ss)" `
-ResourceGroupName $resourceGroupName `
-TemplateFile $jobRunnerTemplateFile `
-TemplateParameterObject $params `
-Verbose `
-ErrorAction stop