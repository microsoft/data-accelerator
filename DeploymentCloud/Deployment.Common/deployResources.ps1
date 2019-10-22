param(
    [string]
    $ParamFile = "common.parameters.txt",

    [string]
    $subscriptionId,

    [string]
    $certPassword,

    [string]
    $sparkPassword,

    [string]
    $sparkSshPassword,

    [string]
    $sfPassword,

    [string]
    $resourceGroupName,
    
    [ValidatePattern('^[a-zA-Z0-9_.-]*$')]
    [ValidateLength(0, 40)]
    [string]
    $productName,

    [string]
    $resourceGroupLocation,

    [ValidateSet("EastUS", "SouthCentralUS", "NorthEurope", "WestEurope", "SoutheastAsia", "WestUS2", "CanadaCentral", "CentralIndia")]
    [string]
    $resourceLocationForMicrosoftInsights,

    [string]
    $resourceLocationForServiceFabric,

    [ValidateScript({Test-Path $_ })]
    [string]
    $deploymentCommonPath,

    [ValidateSet("y", "n")]
    [string]
    $generateAndUseSelfSignedCerts,

    [string]
    $mainCert,

    [string]
    $reverseProxyCert,

    [string]
    $sslCert,

    [ValidateSet("y", "n")]
    [string]
    $installModules,

    [ValidateSet("y", "n")]
    [string]
    $resourceCreation,

    [ValidateSet("y", "n")]
    [string]
    $sparkCreation,

    [ValidateSet("y", "n")]
    [string]
    $serviceFabricCreation,

    [ValidateSet("y", "n")]
    [string]
    $setupSecrets,

    [ValidateSet("y", "n")]
    [string]
    $setupCosmosDB,

    [ValidateSet("y", "n")]
    [string]
    $setupKVAccess
)

$user = [Security.Principal.WindowsIdentity]::GetCurrent();
$ret = (New-Object Security.Principal.WindowsPrincipal $user).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)  
if (!$ret) {
    Write-Host "The current command prompt session is not running as Administrator. Start command prompt by using the Run as Administrator option, and then try running the script again."
    Exit 50
}

$ErrorActionPreference = "stop"

Get-Content $ParamFile | Foreach-Object {
    $l = $_.Trim()
    if ($l.startsWith('#') -or $l.startsWith('//') -or !$l) {
        return    
    }

    $var = $l.Split('=', 2)
    set-Variable -Name $var[0] -Value $var[1]
}

if ($deployResources -ne 'y') {
    Write-Host "deployResources parameter value is not 'y'. This script will not execute."
    Exit 0
}

if ($generateNewSelfSignedCerts -eq 'n' -and !$certPassword) {
    Write-Host "Please provide certPassword to import the existing certs"
    Exit 40
}

Remove-Item -path ".\cachedVariables" -Force -ErrorAction SilentlyContinue
$rootFolderPath = $PSScriptRoot
Import-Module "..\Deployment.Common\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName, $productName, $sparkClusterName, $randomizeProductName, $serviceFabricClusterName, $serviceAppName, $clientAppName, $sparkPassword, $sparkSshPassword, $sfPassword, $certPassword, $redisCacheSize -WarningAction SilentlyContinue
Set-Content -Path ".\cachedVariables" -NoNewline -Value $name

function Install-Modules {
    Write-Host -ForegroundColor Green "Checking Module... "
    Write-Host -ForegroundColor Green "Estimated time to complete: 5 mins"
    Write-Host "Note: If any module is installed, you will have to close this prompt and restart deploy.bat as admin again... "
    
    $modules = New-Object 'System.Collections.Generic.Dictionary[String,String]'
    $modules.Add("azurerm", "6.13.1")
    $modules.Add("azuread", "2.0.2.4")
    $modules.Add("mdbc", " 5.1.4")
        
    $moduleInstalled = $false
    $modules.Keys | foreach {
            if (!(Get-installedModule -name $_ -MinimumVersion $modules.Item($_) -ErrorAction SilentlyContinue )) {
    
                Write-Host "Install Module: " $_
                $moduleInstalled = $true
                Install-Module -Name $_ -Force -AllowClobber -Scope CurrentUser -Repository PSGallery
            }
    }

    if ($moduleInstalled) {
        Write-Host -ForegroundColor Yellow "The script execution completed after one or more packages have been installed. In order to use the latest packages, please close this prompt, open a new command prompt as admin and run deploy.bat again"
        Exit 10
    }
}

# Check if file paths exist
function Check-FilePath {
    $notFound=
    $paths = @("$deploymentCommonPath\CosmosDB", "$deploymentCommonPath\Scripts", "$deploymentCommonPath\Resources")
    foreach ($p in $paths) {
        if (!(Test-Path $p)) {
            Write-Host "$p does not exist"
            Exit 20
        }
    }
}

# Generate tokens with the actual values
function Get-Tokens {
    $tokens = Get-DefaultTokens

    # Template
    $tokens.Add('subscriptionId', $subscriptionId )
    $tokens.Add('resourceLocationForMicrosoftInsights', $resourceLocationForMicrosoftInsights )

    $tokens.Add('tenantId', $tenantId )
    $tokens.Add('userId', $userId )
	
    $sparkType = 'hdinsight'
    $keyvaultPrefix = 'keyvault'
	$dataxJobTemplate = 'DataXDirect'
	$dataxKafkaJobTemplate = 'kafkaDataXDirect'
	$dataxBatchJobTemplate = 'DataXBatch'
    if ($useDatabricks -eq 'y') {
        $sparkType = 'databricks' 
		$keyvaultPrefix = 'secretscope'
		$dataxJobTemplate = 'DataXDirectDatabricks'
		$dataxKafkaJobTemplate = 'kafkaDataXDirectDatabricks'
		$dataxBatchJobTemplate = 'DataXBatchDatabricks'
		$tokens.Add('databricksClusterSparkVersion', $databricksClusterSparkVersion)
		$tokens.Add('databricksClusterNodeType', $databricksClusterNodeType)
		$tokens.Add('databricksSku', $databricksSku)
		$tokens.Add('dbResourceGroupName', $resourceGroupName)
    } else {
        $tokens.Add('HDInsightVersion', $HDInsightVersion)
        $tokens.Add('sparkComponentVersion', $sparkComponentVersion)
        $tokens.Add('enableHDInsightAutoScaling', $enableHDInsightAutoScaling)
        if($enableHDInsightAutoScaling -eq 'y') {
            $tokens.Add('minNodesForHDInsightAutoScaling', $minNodesForHDInsightAutoScaling)
            $tokens.Add('maxNodesForHDInsightAutoScaling', $maxNodesForHDInsightAutoScaling)
        }
    }

	$tokens.Add('sparkType', $sparkType)
	$tokens.Add('keyvaultPrefix', $keyvaultPrefix)
	$tokens.Add('dataxJobTemplate', $dataxJobTemplate)
	$tokens.Add('dataxKafkaJobTemplate', $dataxKafkaJobTemplate)
	$tokens.Add('dataxBatchJobTemplate', $dataxBatchJobTemplate)
	
    # CosmosDB
    $tokens.Add('blobopsconnectionString', $blobopsconnectionString )
    $tokens.Add('configgenClientId', $azureADApplicationConfiggenApplicationId )
    $tokens.Add('configgenTenantId', $tenantName )
    
    # SF Template
    $tokens.Add('certPrimaryThumbprint', $certPrimary.Certificate.Thumbprint )
    $tokens.Add('certPrimarySecretId', $certPrimary.SecretId )
    $tokens.Add('certReverseProxyThumbprint', $certReverseProxy.Certificate.Thumbprint )
    $tokens.Add('certReverseProxySecretId', $certReverseProxy.SecretId )
    $tokens.Add('sslcertThumbprint', $certSSL.Certificate.Thumbprint )
    $tokens.Add('resourceLocationForServiceFabric', $resourceLocationForServiceFabric )
    $tokens.Add('vmNodeTypeSize', $vmNodeTypeSize )
    $tokens.Add('vmNodeinstanceCount', $vmNodeinstanceCount )
    
    # Spark Template
    $tokens.Add('vmSizeSparkHeadnode', $vmSizeSparkHeadnode )
    $tokens.Add('minInstanceCountSparkHeadnode', $minInstanceCountSparkHeadnode )
    $tokens.Add('targetInstanceCountSparkHeadnode', $targetInstanceCountSparkHeadnode )
    $tokens.Add('vmSizeSparkWorkernode', $vmSizeSparkWorkernode )
    $tokens.Add('targetInstanceCountSparkWorkernode', $targetInstanceCountSparkWorkernode )
    
    # Service param
    $tokens.Add('writerRole', $writerRole )
    $tokens.Add('readerRole', $readerRole )

    $tokens.Add('serviceSecretPrefix', $serviceSecretPrefix )
    $tokens.Add('clientSecretPrefix', $clientSecretPrefix )
    
    $tokens.Add('serviceAppId', $azureADApplicationConfiggenApplicationId )
    $tokens.Add('clientAppId', $azureADApplicationApplicationId )

    $tokens.Add('azureADApplicationConfiggenResourceId', $azureADApplicationConfiggenResourceId )

    $aiKey = ''
    $appInsight = Get-AzureRmApplicationInsights -resourceGroupName $resourceGroupName -Name $appInsightsName -ErrorAction SilentlyContinue
    if ($appInsight) {
        $aiKey = $appInsight.InstrumentationKey
    }

    $tokens.Add('appInsightKey', $aiKey )
    $tokens.Add('resourceLocation', $resourceGroupLocation )
    
    $certtype = '' 
    if ($useSelfSignedCerts -eq 'y') {
        $certtype = 'test' 
    }
    
    $tokens.Add('certtype', $certtype )
    
    $kafkaNativeConnectionString = ''
    $kafkaNativeTopics = ''

    if($enableKafkaSample -eq 'y') {
        $kafkaNativeConnectionString = "datagen-kafkaNativeConnectionString"
        $kafkaNativeTopics = "kafka1,kafka2"
    }
    
    $tokens.Add('kafkaNativeConnectionString', $kafkaNativeConnectionString)
    $tokens.Add('kafkaNativeTopics', $kafkaNativeTopics)

    $tokens
}

# Get appRole definition
function Create-AppRole([string] $Name, [string] $AppName, [string] $Description) {
    $appRole = New-Object Microsoft.Open.AzureAD.Model.AppRole
    $appRole.AllowedMemberTypes = New-Object System.Collections.Generic.List[string]
    $appRole.AllowedMemberTypes.Add("User");
    if (($Name -eq $writerRole) -and ($AppName -eq $serviceAppName)) {
        $appRole.AllowedMemberTypes.Add("Application");
    }
    $appRole.DisplayName = $Name
    $appRole.Id = New-Guid
    $appRole.IsEnabled = $true
    $appRole.Description = $Description
    $appRole.Value = $Name
    $appRole
}

# Add appRoles to AAD app
function Set-AzureAADAppRoles([string]$AppName) {
    $role_r = Create-AppRole -Name $readerRole -AppName $AppName -Description $readerRole + " have ability to view flows"
    $role_w = Create-AppRole -Name $writerRole -AppName $AppName -Description $writerRole + " can manage flows"
    $roles = @($role_r, $role_W)
    
    $app = Get-AzureADApplication -Filter "DisplayName eq '$AppName'"
    if ($app.AppRoles) {
        foreach($r in $roles)
        {
            $role = $app.AppRoles | Where-Object { $_.Value -match $r.Value }
            
            if (!$role) {
                $app.AppRoles.Add($r)
                Set-AzureADApplication -ObjectId $app.ObjectId -AppRoles $app.AppRoles
            }
        }
    }
    else {
        foreach($r in $roles)
        {
            $app.AppRoles.Add($r) | Out-Null
        }   
    
        Set-AzureADApplication -ObjectId $app.ObjectId -AppRoles $app.AppRoles
    }
}

# Add user with appRoles to service principal
function Add-UserAppRole([string]$AppName) {
    $sp = Get-AzureADServicePrincipal -Filter "DisplayName eq '$AppName'"
    $appRole = $sp.AppRoles | Where-Object { $_.Value -match $writerRole }
    
    try {
        New-AzureADUserAppRoleAssignment -ObjectId $userId -PrincipalId $userId -ResourceId $sp.ObjectId -Id $appRole.Id
    }
    catch {}
}

# Set secret to AAD app
function Set-AzureAADAppSecret([string]$AppName) {
    $app = Get-AzureRmADApplication -DisplayName $AppName
    if ($app)
    {
        $startDate = Get-Date
        $endDate = $startDate.AddYears(2)
        
        $keyValue = New-AzureADApplicationPasswordCredential -ObjectId $app.ObjectId -StartDate $startDate -EndDate $endDate
    }
    
    $keyValue
}

# Set credential to AAD app
function Set-AzureAADAppCert([string]$AppName) {
    $app = Get-AzureRmADApplication -DisplayName $AppName
    if ($app)
    {
        $cer = $certPrimary.Certificate
        $certValue = [System.Convert]::ToBase64String($cer.GetRawCertData())

        az ad app credential reset --append --id $app.ApplicationId --cert $certValue
    }
}

# Set secret to AAD app
function Generate-AADApplication([string]$appName, [string]$websiteName) {
    $app = Get-AzureRmADApplication -DisplayName $appName
    if (!$app)
    {
        if ($websiteName){
            $app = New-AzureRmADApplication  -DisplayName $appName -IdentifierUris "https://$tenantName/$appName" -ReplyUrls "https://$websiteName.azurewebsites.net/authReturn"
        }
        else {
            $app = New-AzureRmADApplication  -DisplayName $appName -IdentifierUris "https://$tenantName/$appName" 
        }
    }

    if ($app)
    {
        $urls = $app.IdentifierUris
        if ($urls.Count -eq 0) {
            Set-AzureRmADApplication -ObjectId $app.ObjectId -IdentifierUris "https://$tenantName/$appName"  -ErrorAction SilentlyContinue        
        }
    }
    
    if ($websiteName)
    {
        $urls = $app.ReplyUrls
        $urls.Add("https://$websiteName.azurewebsites.net/authReturn")
        Set-AzureRmADApplication -ObjectId $app.ObjectId -ReplyUrl $urls -ErrorAction SilentlyContinue
    }

    $servicePrincipal = Get-AzureRmADServicePrincipal -ApplicationId $app.ApplicationId
    if (!$servicePrincipal)
    {
         $servicePrincipal = New-AzureRmADServicePrincipal -ApplicationId $app.ApplicationId
    }

    $app
}


# Generate SelfSigned Certs
function Generate-SelfSignedCert([string] $certFileName, [string] $outputPath) {
    $todaydt = Get-Date
    $2years = $todaydt.AddYears(2)

    $clustername = "$serviceFabricName" 
    $subject = "CN=$clustername"+ ".$resourceLocationForServiceFabric" + ".cloudapp.azure.com"     

    $certFilePath = Join-Path $outputPath $certFileName
    $password = ConvertTo-SecureString $certPwd -AsPlainText -Force

    $cert = New-SelfSignedCertificate -Subject $subject -notafter $2years  -CertStoreLocation cert:\LocalMachine\My
    
    # Export the cert to a PFX with password
    Export-PfxCertificate -Cert "cert:\LocalMachine\My\$($cert.Thumbprint)" -FilePath $certFilePath -Password $password | Out-Null
    
    Import-PfxCertificate -FilePath $certFilePath -CertStoreLocation cert:\CurrentUser\My -Password $password | Out-Null

    $certFilePath
}

# Import Certs to keyVault
function Import-CertsToKeyVault([string]$certPath) {
    $certPath = $certPath.Replace("""", "")
    $certBaseName = ((Get-Item $certPath).Name).Replace(".", "")
    $password = ConvertTo-SecureString $certPwd -AsPlainText -Force

    # Upload to Key Vault
    if ($serviceFabricCreation -eq 'y') {
        $cert = Import-AzureKeyVaultCertificate -VaultName $sfKVName -Name $certBaseName -FilePath $certPath -Password $password
    }
    else {
        $cert = Get-AzureKeyVaultCertificate -VaultName $sfKVName -Name $certBaseName
    }
    $cert
}

# Add script actions to Spark
function Add-ScriptActions {
    $clusterName = "$sparkName"
    $scriptActionName = "StartMSIServer"

    $scAction = Get-AzureRmHDInsightScriptActionHistory -ClusterName $clusterName
    if (($scAction.Name -eq "$scriptActionName") -and ($scAction.Status -eq 'succeeded')) {
        return
    }

    $tokens = Get-Tokens
    Deploy-Files -saName $sparkBlobAccountName -containerName "scripts" -filter "*.*" -filesPath "$deploymentCommonPath\Scripts" -targetPath "" -translate $True -tokens $tokens
            
    $scriptActionUri = "https://$sparkBlobAccountName.blob.core.windows.net/scripts/startmsiserverservice.sh"
    $nodeTypes = "headnode", "workernode"
    Submit-AzureRmHDInsightScriptAction -ClusterName $clusterName `
        -Name $scriptActionName `
        -Uri $scriptActionUri `
        -NodeTypes $nodeTypes `
        -PersistOnSuccess
}

# Setup cosmosDB
function Setup-CosmosDB {
    Connect-Mdbc -ConnectionString $dbCon -DatabaseName "production"
    $colnames = @(
        "commons",
        "configgenConfigs",
        "flows",
        "sparkClusters"
        "sparkJobs"
    )

    $colnames | foreach {
        try{
             $response = Add-MdbcCollection -Name $_
                if (!$response) {
                throw
            }
        }
        catch
        {}
    }
    
    $templatePath = "$deploymentCommonPath\CosmosDB"
    $outputPath = Get-OutputFilePath
    $qry = New-MdbcQuery -Name "_id" -Exists
    
    $colnames | foreach{
        $colName = $_
        $collection1 = $Database.GetCollection($colName)
        
        Remove-MdbcData -Query $qry -Collection $collection1
        
        $templateName = "$colName.json"
        $outputFile = Join-Path $outputPath "_$templateName"
        $filePath = Join-Path $templatePath $templateName
        $tokens = Get-Tokens
        $rawData = Get-Content -Raw -Path $filePath
        $rawData = Translate-Tokens -Source $rawData -Tokens $tokens
        $json = ConvertFrom-Json -InputObject $rawData

        $json | foreach {
            try
            {
                $_ | ConvertTo-Json -Depth 10 | Set-Content -Encoding Unicode $outputFile
                $input = Import-MdbcData $outputFile -FileFormat Json
                $response = Add-MdbcData -InputObject $input -Collection $collection1 -NewId
                if (!$response) {
                    throw
                }
            }
            catch{}
        }
    }

    Remove-Module Mdbc  
}


# Create secrets to keyVaults for Spark
function Setup-SecretsForCert {
    $vaultName = "$sfKVName"
    
    $secretName = "certpassword" 
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $certPwd
}

# Create secrets to keyVaults for Spark
function Setup-SecretsForSpark {
    $vaultName = "$sparkRDPKVName"
    
    $secretName = "sparkLogin" 
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $sparkLogin

    $secretName = "sparkclusterloginpassword"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $sparkPwd

    $secretName = "sparksshuser" 
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $sparksshuser

    $secretName = "sparksshpassword" 
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $sparkSshPwd
    
    $vaultName = "$servicesKVName"
    $prefix = "$serviceSecretPrefix-"

    $secretName = $prefix + "livyconnectionstring-" + $sparkName    
    $tValue = "endpoint=https://$sparkName.azurehdinsight.net/livy;username=$sparkLogin;password=$sparkPwd"
	if ($useDatabricks -eq 'y') {
        $tValue = "endpoint=https://$resourceGroupLocation.azuredatabricks.net/api/2.0/;dbtoken=" 
    }
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tValue
}

# Create secrets to keyVaults for SF
function Setup-SecretsForServiceFabric {
    $vaultName = "$fabricRDPKVName"
    
    $secretName = "sfadminpassword" 
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $sfPwd
    
    $secretName = "sfadminuser" 
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $sfadminuser    
}

# Create secrets to keyVaults
function Setup-Secrets {
    $vaultName = "$servicesKVName"
    $prefix = "$serviceSecretPrefix-"

    $secretName = $prefix + "configgenconfigs"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $dbCon

    $secretName = $prefix + "configgenconfigsdatabasename"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value "production"

    $secretName = $prefix + $configBlobAccountName + "-blobconnectionstring"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $blobopsconnectionString

    $secretName = $prefix + $sparkBlobAccountName + "-blobconnectionstring"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $blobsparkconnectionString

    $secretName = $prefix + "aiInstrumentationKey"    
    $aiKey = (Get-AzureRmApplicationInsights -resourceGroupName $resourceGroupName -Name $appInsightsName).InstrumentationKey
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $aiKey

    $secretName = $prefix + "tenantid"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tenantName

    $secretName = $prefix + "clientId"
    $tValue = ($azureADApplicationConfiggenApplicationId.Split(" "))[0]
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tValue

    $secretName = $prefix + "clientsecret"    
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $azureADAppSecretConfiggenValue
    
    $secretName = $prefix + "eventhubnamespaceconnectionstring"     
    $tValue = (Invoke-AzureRmResourceAction -ResourceGroupName $resourceGroupName -ResourceType Microsoft.EventHub/namespaces/AuthorizationRules -ResourceName "$eventHubNamespaceName/listen" -Action listKeys -ApiVersion 2015-08-01 -Force).primaryConnectionString
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tValue
    
    $secretName = $prefix + "azureservicesauthconnectionstring"    
    $tValue = "<Parameter Name=""AzureServicesAuthConnectionString"" Value=""RunAs=App;AppId=" + $azureADApplicationConfiggenApplicationId + ";TenantId=" + $tenantId + ";CertificateThumbprint=" + $certPrimary.Certificate.Thumbprint + ";CertificateStoreLocation=LocalMachine""/>"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tValue

    $secretName = $prefix + "cacertificatelocation"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $caCertificateLocation

    $prefix = "$clientSecretPrefix-"
    $secretName = $prefix + "aiKey"    
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $aiKey

    $secretName = $prefix + "clientId"
    $tValue = ($azureADApplicationApplicationId.Split(" "))[0]
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tValue

    $secretName = $prefix + "clientSecret"    
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $azureADAppSecretValue

    $secretName = $prefix + "serviceClusterUrl"    
    $sfName = "$serviceFabricName"
    $tValue = "https://$sfName"+ ".$resourceLocationForServiceFabric" + ".cloudapp.azure.com"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tValue

    $secretName = $prefix + "serviceResourceId"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $azureADApplicationConfiggenResourceId

    $secretName = $prefix + "mongoDbUrl"    
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value "test"

    $secretName = $prefix + "redisDataConnectionString" 
    $redisKey = (Get-AzureRmRedisCacheKey -Name $redisName -resourceGroupName $resourceGroupName).PrimaryKey
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value "$redisName.redis.cache.windows.net:6380,password=$redisKey,ssl=True,abortConnect=False"
    
    $secretName = $prefix + "sessionSecret"       
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value "test"

    $secretName = $prefix + "subscriptionId"       
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $subscriptionId

    $secretName = $prefix + "tenantName"    
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tenantName

    $prefix = ""

    $storageAccount = Get-AzureRmStorageAccount -resourceGroupName $resourceGroupName -Name $sparkBlobAccountName
    $tValue = ""
    if ($storageAccount.Context.ConnectionString -match 'AccountKey=(.*)')
    {
        $tValue = $Matches[1].Replace("AccountKey=", "")
    }
    
    $secretName = $prefix + "datagen-storageaccountAccesskey"
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tValue
    
    $vaultName = "$sparkKVName"
    
    $secretName = $prefix + "datax-sa-" + $configBlobAccountName    
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tValue
    
    $storageAccount = Get-AzureRmStorageAccount -resourceGroupName $resourceGroupName -Name $sparkBlobAccountName
    $tValue = ""
    if ($storageAccount.Context.ConnectionString -match 'AccountKey=(.*)')
    {
        $tValue = $Matches[1].Replace("AccountKey=", "")
    }

    $secretName = $prefix + "datax-sa-" + $sparkBlobAccountName    
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tValue
    
    $secretName = $prefix + "metric-eventhubconnectionstring"    
    $tValue = (Get-AzureRmEventHubKey -resourceGroupName $resourceGroupName -NamespaceName "$eventHubNamespaceName" -EventHubName metricseventhub -AuthorizationRuleName send).PrimaryConnectionString
    Setup-Secret -VaultName $vaultName -SecretName $secretName -Value $tValue
}

# Setup keyVault access
function Setup-KVAccess {
    # Get ObjectId of web app
    $servicePrincipalId = az resource show -g $resourceGroupName --name $websiteName --resource-type Microsoft.Web/sites --query identity.principalId
    
    # Get ObjectId of vmss
    $vmssId = az resource show -g $resourceGroupName --name $vmNodeTypeName --resource-type Microsoft.Compute/virtualMachineScaleSets --query identity.principalId
    
    # Get ObjectId of Service app
    try {
        $servicePrincipalConfiggenId = az ad sp list --display-name $serviceAppName --query [0].objectId 
    }
    catch {
            Write-Host "Error on getting the service principal objectId"
            Exit 30
    }

    az keyvault set-policy --name $servicesKVName --object-id $servicePrincipalId --secret-permissions get, list, set > $null 2>&1
    az keyvault set-policy --name $servicesKVName --object-id $servicePrincipalConfiggenId --secret-permissions get, list, set > $null 2>&1 
    az keyvault set-policy --name $servicesKVName --object-id $vmssId --secret-permissions get, list, set > $null 2>&1

    az keyvault set-policy --name $sparkKVName --object-id $servicePrincipalId --secret-permissions get, list, set > $null 2>&1
    az keyvault set-policy --name $sparkKVName --object-id $servicePrincipalConfiggenId --secret-permissions get, list, set, delete > $null 2>&1
    az keyvault set-policy --name $sparkKVName --object-id $vmssId --secret-permissions get, list, set > $null 2>&1
	if($useDatabricks -eq 'n') {
		# Get ObjectId of sparkManagedIdentityName  
		$SparkManagedIdentityId = az resource show -g $resourceGroupName --name $sparkManagedIdentityName --resource-type Microsoft.ManagedIdentity/userAssignedIdentities --query properties.principalId
		az keyvault set-policy --name $servicesKVName --object-id $SparkManagedIdentityId --secret-permissions get, list, set > $null 2>&1
		az keyvault set-policy --name $sparkKVName --object-id $SparkManagedIdentityId --secret-permissions get, list, set > $null 2>&1
	}
}

# Import SSL Cert To Service Fabric
function Import-SSLCertToSF([string]$certPath) {
    $certPath = $certPath.Replace("""", "")
    $certBaseName = (Get-Item $certPath).BaseName
    $clustername = "$serviceFabricName" 
    $bytes = [System.IO.File]::ReadAllBytes($certPath)
    $base64 = [System.Convert]::ToBase64String($bytes)

    $jsonBlob = @{
       data = $base64
       dataType = 'pfx'
       password = $certPwd
       } | ConvertTo-Json

    $contentbytes = [System.Text.Encoding]::UTF8.GetBytes($jsonBlob)
    $content = [System.Convert]::ToBase64String($contentbytes)

    $secretValue = ConvertTo-SecureString -String $content -AsPlainText -Force

    # Upload the certificate to the key vault as a secret
    Write-Host "Writing secret to $certBaseName in vault $sfKVName"

    $secret = Set-AzureKeyVaultSecret -VaultName $sfKVName -Name $certBaseName -SecretValue $secretValue

    # Add a certificate to all the VMs in the cluster.
    Add-AzureRmServiceFabricApplicationCertificate -resourceGroupName $resourceGroupName -Name $clustername -SecretIdentifier $secret.Id | Out-Null
}

# Open 443 port
function Open-Port {
    $probename = "AppPortProbe6"
	$rulename = "AppPortLBRule6"
	$port = 443
	
	# Get the load balancer resource
	$resource = Get-AzureRmResource | Where {$_.resourceGroupName -eq $resourceGroupName -and $_.ResourceType -eq "Microsoft.Network/loadBalancers" -and $_.Name -like "LB-*"}
	$slb = Get-AzureRmLoadBalancer -Name $resource.Name -resourceGroupName $resourceGroupName
    
	$probe = Get-AzureRmLoadBalancerProbeConfig -Name $probename -LoadBalancer $slb -ErrorAction SilentlyContinue
    if (!($probe)) {
        # Add a new probe configuration to the load balancer
        $slb | Add-AzureRmLoadBalancerProbeConfig -Name $probename -Protocol Tcp -Port $port -IntervalInSeconds 15 -ProbeCount 2 | Out-Null
        $probe = Get-AzureRmLoadBalancerProbeConfig -Name $probename -LoadBalancer $slb
    }
    
    $rule = get-AzureRmLoadBalancerRuleConfig -LoadBalancer $slb -Name $rulename -ErrorAction SilentlyContinue
    if (!($rule)) {
        # Add rule configuration to the load balancer
        $slb | Add-AzureRmLoadBalancerRuleConfig -Name $rulename -BackendAddressPool $slb.BackendAddressPools[0] -FrontendIpConfiguration $slb.FrontendIpConfigurations[0] -Probe $probe -Protocol Tcp -FrontendPort $port -BackendPort $port | Out-Null
    }
	
	# Set the goal state for the load balancer
    $slb | Set-AzureRmLoadBalancer | Out-Null
}

# Setup Service Fabric cluster. 
# Import SSL cert and open 443 port
function Setup-SF {
    Import-SSLCertToSF -certPath "$sslCert"
    Open-Port
}

# Generate paramter files for the service deployment 
function Translate-ParameterFiles([string] $ParameterFilePath, [system.collections.generic.dictionary[string,string]]$Tokens) {
    $rawData = Get-Content -Raw -Path $ParameterFilePath
    $rawData = Translate-Tokens -Source $rawData -Tokens $Tokens
    $rawData
}

# Generate paramter files for the service deployment 
function Generate-ParameterFiles([string] $parametersFolder = '', [string] $parametersOutputFolder = '') {
    New-Item -ItemType Directory -Force -Path $parametersOutputFolder -ErrorAction SilentlyContinue
    $allParameters = Get-ChildItem -Path $parametersFolder -Filter *.json -File -Recurse

    $tokens = Get-Tokens
    foreach($parameterFile in $allParameters) {
        $translatedData = Translate-ParameterFiles -ParameterFilePath $parameterFile.FullName -Token $tokens
        $filePath = [System.IO.Path]::Combine($parametersOutputFolder, $parameterFile.Name)
        Set-Content -Path $filePath -Value $translatedData
    }
}

# Prepare steps for the service deployment
function Prepare-AppDeployment {
    $parametersFolder = Join-Path -Path $deploymentAppPath -ChildPath "Services\Parameters"
    $parametersOutputFolder = Join-Path -Path $deploymentAppPath -ChildPath "Outputs\Services\Parameters"
    Generate-ParameterFiles -parametersFolder $parametersFolder -parametersOutputFolder $parametersOutputFolder
}

# Prepare steps for the service deployment
function Prepare-AdminSteps {
    $parameterFile = Join-Path $adminParameterPath "adminsteps.parameters.txt"
    $parameterFileOutput = Join-Path $deploymentAppPath "adminsteps.parameters.txt"

    $tokens = Get-Tokens
    $translatedData = Translate-ParameterFiles -ParameterFilePath $parameterFile -Token $tokens
    Set-Content -Path $parameterFileOutput -Value $translatedData
}

#******************************************************************************
# Script body
# Execution begins here
#******************************************************************************
$ErrorActionPreference = "stop"

Push-Location $PSScriptRoot

Write-Host -ForegroundColor Green "Total estimated time to complete: 2 to 4 hours"

if ($installModules -eq  'y') {
    Install-Modules
}

Check-FilePath

# sign in
Write-Host "Logging in..."

# select subscription
Write-Host "Selecting subscription '$subscriptionId'"
Check-Credential -SubscriptionId $subscriptionId -TenantId $tenantId

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

# Create or check for existing resource group
$resourceGroup = Get-AzureRmResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue
if(!$resourceGroup) {
    Write-Host "Resource group '$resourceGroupName' does not exist"
    if(!$resourceGroupLocation) {
        $resourceGroupLocation = Read-Host "resourceGroupLocation"
    }
    Write-Host "Creating resource group '$resourceGroupName' in location '$resourceGroupLocation'"
    New-AzureRmResourceGroup -Name $resourceGroupName -Location $resourceGroupLocation
}
else {
    Write-Host "Using existing resource group '$resourceGroupName'"
    $resourceGroupLocation = $resourceGroup.Location
}

$templatePath = $resourcesTemplatePath

Write-Host -ForegroundColor Green "Starting deployment..."

if($resourceCreation -eq 'y') {
    Write-Host -ForegroundColor Green "Deploying resources (1/16 steps): All resources except HDInsight and Service Fabric clusters will be deployed"
    Write-Host -ForegroundColor Green "Estimated time to complete: 40 mins"
    
    $tokens = Get-Tokens
    Deploy-Resources -templateName "Resource-Template.json" -paramName "Resource-parameter.json"  -templatePath $templatePath -tokens $tokens
}

if($sparkCreation -eq 'y') {
    Write-Host -ForegroundColor Green "Deploying resources (2/16 steps): A spark cluster will be deployed"   
    Setup-SecretsForSpark

    $tokens = Get-Tokens
	if ($useDatabricks -eq 'n') {

        $sparkTemplate = "Spark-Template.json"
        $sparkParameter = "Spark-parameter.json"

        $version = ($HDInsightVersion -split '\.')[0]
        $version = [int]$version
        if ($version -ge 4 -and $enableHDInsightAutoScaling -eq 'y') {
            $sparkTemplate = "Spark-AutoScale-Template.json"
            $sparkParameter = "Spark-AutoScale-parameter.json"
        }      
        Write-Host "sparkTemplate: '$sparkTemplate' ; sparkParameter: '$sparkParameter'"

		Write-Host -ForegroundColor Green "Estimated time to complete: 20 mins"
		Deploy-Resources -templateName  $sparkTemplate -paramName $sparkParameter -templatePath $templatePath -tokens $tokens
	}
	else {
		Write-Host -ForegroundColor Green "Estimated time to complete: 5 mins"
		Deploy-Resources -templateName "Databricks-Template.json" -paramName "Databricks-Parameter.json" -templatePath $templatePath -tokens $tokens
	}
}

# Preparing certs...
if ($generateNewSelfSignedCerts -eq 'y') {
    Write-Host "Generating SelfSigned certs..."
    New-Item -ItemType Directory -Force -Path $certPath -ErrorAction SilentlyContinue

    $mainCert = Generate-SelfSignedCert -certFileName "certprimary$name.pfx" -outputPath $certPath
    $reverseProxyCert = Generate-SelfSignedCert -certFileName "certreverseproxy$name.pfx" -outputPath $certPath
    $sslCert = Generate-SelfSignedCert -certFileName "certssl$name.pfx" -outputPath $certPath
}

Write-Host "processing certs..."
$certPrimary = Import-CertsToKeyVault -certPath $mainCert
$certReverseProxy = Import-CertsToKeyVault -certPath $reverseProxyCert
$certSSL = Import-CertsToKeyVault -certPath $sslCert

#aad
Write-Host -ForegroundColor Green "processing AAD... (3/16 steps)"
Write-Host -ForegroundColor Green "Estimated time to complete: 2 mins"

$azureADApplication = Generate-AADApplication -appName $clientAppName -websiteName $websiteName
$azureADApplicationConfiggen = Generate-AADApplication -appName $serviceAppName

$azureADApplicationApplicationId = $azureADApplication.ApplicationId.Guid
$azureADApplicationConfiggenApplicationId = $azureADApplicationConfiggen.ApplicationId.Guid

$azureADApplicationConfiggenResourceId = $azureADApplicationConfiggen.IdentifierUris[0]

$azureADAppSecret = Set-AzureAADAppSecret -AppName $clientAppName
$azureADAppSecretConfiggen = Set-AzureAADAppSecret -AppName $serviceAppName

Set-AzureAADAppCert -AppName $serviceAppName

$azureADAppSecretValue = $azureADAppSecret.Value 
$azureADAppSecretConfiggenValue = $azureADAppSecretConfiggen.Value

Set-AzureAADAppRoles -AppName $clientAppName
Set-AzureAADAppRoles -AppName $serviceAppName
Add-UserAppRole -AppName $clientAppName
Add-UserAppRole -AppName $serviceAppName

Set-AzureAADAccessControl -AppId $azureADApplicationConfiggenApplicationId
Set-AzureAADApiPermission -ServiceAppId $azureADApplicationConfiggenApplicationId -ClientAppId $azureADApplicationApplicationId -RoleName $writerRole

if($serviceFabricCreation -eq 'y') {
    Write-Host -ForegroundColor Green "Deploying resources (4/16 steps): A Service fabric cluster will be deployed"
    Write-Host -ForegroundColor Green "Estimated time to complete: 20 mins"

    Setup-SecretsForServiceFabric
    Setup-SecretsForCert

    $tokens = Get-Tokens
    Deploy-Resources -templateName "SF-Template.json" -paramName "SF-parameter.json" -templatePath $templatePath -tokens $tokens
}

# Processing
$dbCon = Get-CosmosDBConnectionString -Name $docDBName

$blobopsconnectionString = Get-StorageAccountConnectionString -Name $configBlobAccountName
$blobsparkconnectionString = Get-StorageAccountConnectionString -Name $sparkBlobAccountName

Write-Host "Prepare for the service deployment..."
Prepare-AppDeployment

Write-Host "Prepare for the admin steps..."
Prepare-AdminSteps

# Secrets
if ($setupSecrets -eq 'y') {
    Write-Host -ForegroundColor Green "Setting up Secrets... (5/16 steps)"
    Write-Host -ForegroundColor Green "Estimated time to complete: 1 min"
    Setup-Secrets
}

# Spark
if ($sparkCreation -eq 'y') {
    Write-Host -ForegroundColor Green "Setting up ScriptActions... (6/16 steps)"   
	if ($useDatabricks -eq 'n') {
		Write-Host -ForegroundColor Green "Estimated time to complete: 2 mins"
        Add-ScriptActions 
    }
}

# cosmosDB
if ($setupCosmosDB -eq 'y') {
    Write-Host -ForegroundColor Green "Setting up CosmosDB... (7/16 steps)"
    Write-Host -ForegroundColor Green "Estimated time to complete: 1 min"
    Setup-CosmosDB
}

# Access Policies
if ($setupKVAccess -eq 'y') {
    Write-Host -ForegroundColor Green "Setting up KV access... (8/16 steps)"
    Write-Host -ForegroundColor Green "Estimated time to complete: 2 mins"
    Setup-KVAccess
}

# setup SF
if ($serviceFabricCreation -eq 'y') {
    Write-Host -ForegroundColor Green "Setting up SF... (9/16 steps)"
    Write-Host -ForegroundColor Green "Estimated time to complete: 60 mins"
    Setup-SF
}

# Clean up \Temp folder
Write-Host "Cleaning up the cache files..."
CleanUp-Folder -FolderName $tempPath

Exit 0