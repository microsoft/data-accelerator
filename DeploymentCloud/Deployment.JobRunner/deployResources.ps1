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
    $flowName='scenariotest'
)
Import-Module ./utilities.psm1

$ErrorActionPreference = "stop"

Push-Location $PSScriptRoot

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
    -flowName $flowName

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
}

$templateName = $flowName

New-AzureRmResourceGroupDeployment `
-Name "deployment-$templateName-$(get-date -f MM-dd-yyyy_HH_mm_ss)" `
-ResourceGroupName $resourceGroupName `
-TemplateFile $jobRunnerTemplateFile `
-TemplateParameterObject $params `
-Verbose `
-ErrorAction stop