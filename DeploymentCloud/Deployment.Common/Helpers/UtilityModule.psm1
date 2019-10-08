param(
    [Parameter(Mandatory=$True)]
    [string]
    $rootFolderPath,

    [Parameter(Mandatory=$True)]
    [string]
    $resourceGroupName,

    [Parameter(Mandatory=$True)]
    [string]
    $productName,
    
    [Parameter(Mandatory=$True)]
    [string]
    $sparkClusterName,

    [Parameter(Mandatory=$True)]
    [string]
    $randomizeProductName,

    [Parameter(Mandatory=$True)]
    [string]
    $serviceFabricClusterName,    

    [Parameter(Mandatory=$True)]
    [string]
    $serviceAppName,

    [Parameter(Mandatory=$True)]
    [string]
    $clientAppName,

    [Parameter(Mandatory=$True)]
    [string]
    $sparkPassword,

    [Parameter(Mandatory=$True)]
    [string]
    $sparkSshPassword,

    [Parameter(Mandatory=$True)]
    [string]
    $sfPassword,

    [Parameter(Mandatory=$True)]
    [string]
    $certPassword,

    [Parameter(Mandatory=$True)]
    [string]
    $redisCacheSize
)

# Generate the random characters to randomize ProductName
# function Get-RandomName([string]$BaseName) {
function Get-RandomName([string]$BaseName) {
    $count = 32 - $BaseName.Length
    $rand = Get-Random -Maximum ([uint32]::MaxValue) -Minimum 0
    $rand = [math]::round($rand, 0)
    $rand
}

# Generate the random characters using the baseName to randomize ProductName
function Get-RandomNameWithLength([string]$BaseName, [Int]$Count) {
    $rand = -join (Get-Random -InputObject ([char[]]$BaseName) -Count $count -SetSeed $baseName.Length)
    $rand
}

# Generate the random password
function Get-Password {
    [Reflection.Assembly]::LoadWithPartialName("System.Web") | Out-Null

    $leng = Get-Random -Maximum 25 -Minimum 15
    $symleng = Get-Random -Maximum 8 -Minimum 2

    do {
        $pwd = [System.Web.Security.Membership]::GeneratePassword($leng, $symleng)
    } until ($pwd -match '(?=.*\d)(?=.*[A-Z])(?=.*[a-z])(?=.*\W)')

    $pwd
}

$cachedProductName = Get-Content -Raw -Path ".\cachedVariables" -ErrorAction SilentlyContinue
if ($cachedProductName) {
    $productName = $cachedProductName
    $randomizeProductName = 'n'
}
elseif (!$productName) {
    $productName = "dx" + (Get-RandomName -BaseName "dx") 
    $randomizeProductName = 'n'
}

# Variables
$name = $productName.ToLower();
if ($randomizeProductName -eq 'y'){
    $name = $name + (Get-RandomName -BaseName $name) 
}

if (!$serviceFabricClusterName) { 
    $serviceFabricName = "$name-sf"
}
else {
    $serviceFabricName = $serviceFabricClusterName.ToLower()
}

if (!$sparkClusterName) { 
    $sparkName = "$name"
}
else {
    $sparkName = $sparkClusterName.ToLower()
}

if (!$serviceAppName) {
    $serviceAppName = "serviceapp-$name" 
}

if (!$clientAppName) {
    $clientAppName = "clientapp-$name"
}

if (!$certPassword) {
    $certPwd = Get-Password
}
else {
    $certPwd = $certPassword
}

if (!$sparkPassword) {
    $sparkPwd = Get-Password
}
else {
    $sparkPwd = $sparkPassword
}

if (!$kafkaPassword) {
    $kafkaPwd = Get-Password
}
else {
    $kafkaPwd = $kafkaPassword
}

if (!$sparkSshPassword) {
    $sparkSshPwd = Get-Password
}
else {
    $sparkSshPwd = $sparkSshPassword
}

if (!$kafkaSshPassword) {
    $kafkaSshPwd = Get-Password
}
else {
    $kafkaSshPwd = $kafkaSshPassword
}

if (!$sfPassword) {
    $sfPwd = Get-Password
}
else {
    $sfPwd = $sfPassword
}

$EVENTHUBNAME_MAX_LENGTH = 50
$KVNAME_MAX_LENGTH = 24
$SANAME_MAX_LENGTH = 24
$IOTHUBDEVICENAME_MAX_LENGTH = 26

$sparkLogin = "user" + (Get-RandomName -BaseName "user")
$sparksshuser = "user" + (Get-RandomName -BaseName "user")
$kafkaLogin = "user" + (Get-RandomName -BaseName "user")
$kafkasshuser = "user" + (Get-RandomName -BaseName "user")
$sfadminuser = "user" + (Get-RandomName -BaseName "user")
 
$vmNodeTypeName = "vm" + (Get-RandomNameWithLength -BaseName $name -Count 7)

$redisSku = $redisCacheSize.Split('_', 3)
$redisSkuName = $redisSku[0]
$redisSkuFamily = $redisSku[1]
$redisSkuCapacity = $redisSku[2]

$websiteName = $name

$configBlobAccountName = "saspark" + (Get-RandomNameWithLength -BaseName $sparkName -Count ($SANAME_MAX_LENGTH-8))
$sparkBlobAccountName = "saspark" + (Get-RandomNameWithLength -BaseName $sparkName -Count ($SANAME_MAX_LENGTH-8))

$kvBaseName = (Get-RandomNameWithLength -BaseName $name -Count ($KVNAME_MAX_LENGTH-12))
$servicesKVName = "kvServices" + $kvBaseName
$sparkKVName = "kvSpark" + $kvBaseName
$sparkRDPKVName = "kvSparkRDP" + $kvBaseName
$fabricRDPKVName = "kvFabricRDP" + $kvBaseName
$sfKVName = "kvSF" + $kvBaseName

$docDBName = "$name"
$appInsightsName = "$name"
$redisName = "$name"
$eventHubNamespaceName = "eventhubmetric" + (Get-RandomNameWithLength -BaseName $name -Count ($EVENTHUBNAME_MAX_LENGTH-15))
$kafkaEventHubNamespaceName = "eventhubkafka" + (Get-RandomNameWithLength -BaseName $name -Count ($EVENTHUBNAME_MAX_LENGTH-15))
$sparkManagedIdentityName = "SparkManagedIdentity$sparkName"

$iotHubName = "iotDevice" + (Get-RandomNameWithLength -BaseName $name -Count ($IOTHUBDEVICENAME_MAX_LENGTH-10))
$kafkaName = "kafka$name"

# Paths
$packagesPath = Join-Path $rootFolderPath "Packages"
$parametersOutputPath = Join-Path $rootFolderPath "Outputs\Services\Parameters"
$resourcesTemplatePath = Join-Path $rootFolderPath "Resources"
$servicesTemplatePath = Join-Path $rootFolderPath "Services\Templates"
$adminParameterPath = Join-Path $rootFolderPath "Admin"
$tempPath = Join-Path $rootFolderPath "Temp"
$certPath = Join-Path $rootFolderPath "Temp\Certs"

$deployment = "deployment"

# Get the default output file path
function Get-OutputFilePath([string]$fileName = '') {
    New-Item -ItemType Directory -Force -Path $tempPath -ErrorAction SilentlyContinue | Out-Null
    $path = Join-Path $tempPath $fileName
    return $path
}

# Cleanup the folder
function CleanUp-Folder([string]$FolderName) {
    Remove-Item -path $FolderName -Recurse -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $FolderName -ErrorAction SilentlyContinue
}

# Replace token in template
Function Translate-Tokens([string]$Source = '', [system.collections.generic.dictionary[string,string]]$Tokens) {
    $newStr = $Source;

    if ($tokens) {
        $tokens.Keys | foreach { 
            $key = '$' + $_
            $newStr = $newStr.Replace($key, $tokens.Item($_) )
        }
    }
    
    $newStr
} 

# Initialize template by updating tokens there
function Prepare-Template([string]$templateName, [string]$templatePath, [system.collections.generic.dictionary[string,string]]$tokens) {
    $templateFullPath = Join-Path $templatePath $templateName
    $rawData = Get-Content -Raw -Path $templateFullPath
    $rawData = Translate-Tokens -Source $rawData -Tokens $tokens
    $filePath = Get-OutputFilePath -fileName "translated_$templateName"
    
    Set-Content -Path $filePath -Value $rawData
    $filePath
}

function Deploy-Resources([string]$templateName, [string]$paramName, [string]$templatePath, [system.collections.generic.dictionary[string,string]]$tokens) {
    # Initialize template
    $tPath = Join-Path $templatePath "Templates"
    $pPath = Join-Path $templatePath "Parameters"
    $paramFilePath = Prepare-Template -templateName $paramName -templatePath $pPath -tokens $tokens
    $templateFilePath = Join-Path $tPath $templateName

    $result = Test-AzureRmResourceGroupDeployment `
    -ResourceGroupName $resourceGroupName `
    -TemplateFile $templateFilePath `
    -TemplateParameterFile $paramFilePath `
    -ErrorAction stop
    
    if ($result) {
        Write-Host -ForegroundColor Red $result.Message
        Exit 50
    }   

    New-AzureRmResourceGroupDeployment `
    -Name "deployment-$templateName-$(get-date -f MM-dd-yyyy_HH_mm_ss)" `
    -ResourceGroupName $resourceGroupName `
    -TemplateFile $templateFilePath `
    -TemplateParameterFile $paramFilePath `
    -ErrorAction stop
}

# Upload files to StorageAccount
function Deploy-Files([string]$saName, [string]$containerName, [string]$filter, [string]$filesPath, [string]$targetPath = '', [bool]$translate, [System.Collections.Generic.Dictionary[String,String]]$tokens) {
    $saContext = Get-StorageAccountContext -Name $saName;

    if ($saContext)
    {
        $filesToUpload = Get-ChildItem -Path $filesPath -Include $filter -File -Recurse
        New-AzureStorageContainer -Name $containerName -Context $saContext -ErrorAction SilentlyContinue

        foreach ($f in $filesToUpload) {
            
            $blobpath = $f.Name
            if (!($targetPath -eq '')) {
                $blobpath = Join-Path $targetPath $f.Name
            }

            if (!$translate) {
                Set-AzureStorageBlobContent -File $f.fullname -Container $containerName -Blob $blobpath -Context $saContext -Force:$Force | Out-Null
            }
            else {

                $rawData = Get-Content -Raw -Path $f.fullname
                $filePath = Get-OutputFilePath -fileName $f.Name
                Translate-Tokens -Source $rawData -Tokens $tokens | Set-Content $filePath
                
                Set-AzureStorageBlobContent -File $filePath -Container $containerName -Blob $blobpath -Context $saContext -Force:$Force | Out-Null 
            }
        }
    }   
}

# Get the target storage account context
function Get-StorageAccountContext([string]$Name) {
    $storageAccount = Get-AzureRmStorageAccount -resourceGroupName $resourceGroupName -Name $Name
    $ctx = $storageAccount.Context
    $ctx
}

# Add a secret to Keyvault
Function Setup-Secret([String]$VaultName = '', [String]$SecretName = '', [String]$Value = '') {    
    $secret = ConvertTo-SecureString -String $Value -AsPlainText -Force
    Set-AzureKeyVaultSecret -VaultName $VaultName -Name $SecretName -SecretValue $secret -ErrorAction Stop | Out-Null
}

# Get a secret from Keyvault
Function Get-Secret([String]$VaultName, [String]$SecretName) {    
    $secret = Get-AzureKeyVaultSecret -VaultName $VaultName -Name $SecretName -ErrorAction Stop
    $secret.SecretValueText
}

# Deploy the service apps
function Deploy-Services([string[]]$packageNames, [string]$templatePath, [string]$parameterPath) {
    foreach ($packageName in $packageNames) {
        Deploy-Service -packageName $packageName -templatePath $templatePath -parameterPath $parameterPath
    }
}

# Helper function for Deploy-Services 
function Deploy-Service([string]$packageName, [string]$templatePath, [string]$parameterPath) {    
    $packageName = $packageName.Replace(".", "")
    $paramFile = Join-Path $parameterPath "$packageName.Parameters.json"
    $templateFile = Join-Path $templatePath "$packageName.Template.json"

    $result =  Test-AzureRmResourceGroupDeployment `
    -ResourceGroupName $resourceGroupName `
    -TemplateFile $templateFile `
    -TemplateParameterFile $paramFile `
    -ErrorAction stop

    if ($result) {
        Write-Host -ForegroundColor Red $result.Message
        Exit 50 
    }

    New-AzureRmResourceGroupDeployment `
    -Name "deployment-$packageName-$(get-date -f MM-dd-yyyy_HH_mm_ss)" `
    -ResourceGroupName "$resourceGroupName" `
    -TemplateFile $templateFile `
    -TemplateParameterFile $paramFile `
    -ErrorAction stop
}

# Install app/service nuget packages
function Install-Packages([system.collections.generic.dictionary[string,string]]$packageNames, [string]$targetPath, [string]$source) {
    CleanUp-Folder -folderName $targetPath

    $packageNames.Keys | foreach {
        $path = Join-Path $targetPath $_.Replace(".", "")
        $version = $packageNames.Item($_)

        if($version) {
            ..\Deployment.Common\nuget\nuget.exe install $_ -version $version -PreRelease -Source $source -OutputDirectory $path | Out-Null
        }
        else {
            ..\Deployment.Common\nuget\nuget.exe install $_ -PreRelease -Source $source -OutputDirectory $path | Out-Null
        }
    }
}

# Prepare app/service nuget packages to deploy
function Prepare-Packages([string]$packageFilePath) {
    Add-Type -Assembly System.IO.Compression.FileSystem
    $storageAccountContext = Get-StorageAccountContext -Name $sparkBlobAccountName;

    $allPackages = Get-ChildItem -Path $packageFilePath -Include *.sfpkg -File -Recurse
    foreach ($package in $allPackages) {
        $parentFolderName = ''
        if ($package.FullName -match '(Deployment.DataX\\Packages\\)(.*)(\\Microsoft.DataX)')
        {
            $parentFolderName = $Matches[2]
        } else {
            $parentFolderName = $package.BaseName
        } 
        
        $parentZipName = $package.BaseName + ".zip"
        $zipPath = [System.IO.Path]::Combine($packageFilePath, $parentFolderName, $parentZipName)
        Copy-Item $package.Fullname $zipPath -Force

        Set-AzureStorageBlobContent -File $package.Fullname -Container $deployment -Blob $package.Name -Context $storageAccountContext -Force:$Force
    }
}

# Get content of Zip file
function Get-ZipArchiveEntryText([IO.Compression.ZipArchiveEntry]$entry) {
    try {
        $stream = $entry.Open()
        $reader = New-Object IO.StreamReader($stream)
        return $reader.ReadToEnd()
    }
    finally {
        if ($stream) { $stream.Dispose() }
    }
}

# Replace tokens in parameter file with the actuall values
function Fix-ApplicationTypeVersion([string] $parametersPath, [string] $packagesPath) {
    $allParameters = Get-ChildItem -Path $parametersPath -Filter *.json -File -Recurse
    $storageAccountContext = Get-StorageAccountContext -Name $sparkBlobAccountName;
    $paramOutputPath = Get-OutputFilePath;

    foreach($parameterFile in $allParameters) {
        $paramJsonFile = Get-Content($parameterFile.fullname) | ConvertFrom-Json
        $packageName = "Microsoft" + $paramJsonFile.parameters.sfPkgName.value

        $archivePaths = Get-ChildItem -Path $packagesPath -Filter "$packageName.zip" -File -Recurse
        
        if ($archivePaths.count -gt 0) {
            $archivePath = $archivePaths[0].FullName
      
            $packageVersion = Get-PackageVersion -archivePath $archivePath
            $paramFile = Get-Content $parameterFile.fullname -Raw
            $replacedContent = $paramFile.Replace('$version', $packageVersion) 
                
            $pkgName = "$packageName.sfpkg";
            $token = New-AzureStorageBlobSASToken -Context $storageAccountContext -Container "deployment" -Blob $pkgName -Permission r
            $sasurl = "https://$sparkBlobAccountName.blob.core.windows.net/deployment/$pkgName" + $token

            $replacedContent = $replacedContent.Replace('$appPackageUrl', $sasurl) 
            $fileName = Join-Path $paramOutputPath $parameterFile.Name
            Set-Content -Path $fileName -Value $replacedContent
        }
    }

    $paramOutputPath
}

# Get applicationTypeVersion from ApplicationManifest
function Get-PackageVersion([string]$archivePath) {
    try {
        $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    }
    catch {
        throw "Unable to open archive $archivePath. The file passed to the PackagePath parameter must be a zip archive or a folder.`n  $_"
    }

    $appManifestEntry = $archive.Entries | Where-Object { $_.FullName -ieq 'ApplicationManifest.xml' }

    if (!$appManifestEntry) {
        throw "The entry ApplicationManifest.xml could not be found."
    }

    [xml]$applicationManifest = Get-ZipArchiveEntryText $appManifestEntry
    $applicationTypeVersion = $applicationManifest.ApplicationManifest.ApplicationTypeVersion
    $archive.Dispose()
    $applicationTypeVersion
}

# Get StorageAccount connectionstring
function Get-StorageAccountConnectionString([string]$Name) {
    $connectionString = ""
    $storageAccountContext = Get-StorageAccountContext -Name $Name;
    if ($storageAccountContext.ConnectionString -match '(AccountName=.*)')
    {
        $partConnectionString = $Matches[0]
        $connectionString = "DefaultEndpointsProtocol=https;$partConnectionString;EndpointSuffix=core.windows.net"
    }    

    $connectionString
}

# Get CosmosDB connectionstring
function Get-CosmosDBConnectionString([string]$Name) {
    $connectionString = ""
    $dbConRaw = Invoke-AzureRmResourceAction -Action listConnectionStrings `
    -ResourceType "Microsoft.DocumentDb/databaseAccounts" `
    -ApiVersion "2015-04-08" `
    -resourceGroupName $resourceGroupName `
    -Name $Name `
    -force

    $connectionString = $dbConRaw.connectionStrings[0].connectionString
    $connectionString
}

# Delete existing SF stuffs
function CleanUp-ExistingSF {
    $res = New-Object 'System.Collections.Generic.Dictionary[String,String]'
    
    $res.Add('Microsoft.ServiceFabric/clusters', $serviceFabricName)
    $res.Add('Microsoft.Compute/virtualMachineScaleSets', $vmNodeTypeName)
    $lbName = "LB-" + $serviceFabricName + "-" + $vmNodeTypeName
    $res.Add('Microsoft.Network/loadBalancers', $lbName)
    
    foreach ($r in $res.Keys) {
        $rType = $r
        $rName = $res.Item($r)
        
        $found = Get-AzureRmResource | Where {$_.resourceGroupName -eq $resourceGroupName -and $_.Name -match $rName -and $_.ResourceType -eq $rType}
        
        if ($found) {
            Remove-AzureRmResource -ResourceId $found.ResourceId -Force | Out-Null
        }
    }
}

# Make sure the user has access to subscription 
function Check-Credential([string]$SubscriptionId, [string]$TenantId) {
    Write-Host "Checking credential..."
    Write-Host "Logging in for AzureRM"
    $azurermsub = Select-AzureRmSubscription -SubscriptionId $subscriptionId -ErrorAction SilentlyContinue
    if (!($azurermsub)) {
        Connect-AzureRmAccount -TenantId $TenantId
        $azurermsub = Select-AzureRmSubscription -SubscriptionId $subscriptionId
    }

    try {
        az account set --subscription $subscriptionId > $null 2>&1
    }
    catch {}

    if (!$?) {
        Write-Host "Logging in for AzureCLI"
        az login --tenant $TenantId
        az account set --subscription $subscriptionId
        if (!$?)
        {
            Write-Host "Can't access to the subscription. Please run 'az login' to sign in and try again"
            Exit 40
        }
    }
}

function Set-AzureAADAccessControl([string]$AppId) {
    $ErrorActionPreference = "SilentlyContinue"
    
    Write-Host -ForegroundColor Yellow  "Adding the service app as a contributor to the subscription. This requires the subscription admin privilege. If this fails, please refer to the manual steps and ask a subscription admin"
    # Add the service app as a contributor
    
    $added = $false
    # Sometimes this failed. Retry up to 5 times. 
    For ($i=0; $i -le 4; $i++) {
        try {
            az role assignment create --role "contributor" --assignee $AppId --resource-group $resourceGroupName > $null 2>&1

            if ($?) {
                $added = $true
                break
            }
        }
        catch {}
    }

    if (!$added) {
        Write-Host -ForegroundColor Yellow "Please make sure the service app is added as a contributor to the subscription"
    }
    $ErrorActionPreference = "stop"
}

function Set-AzureAADApiPermission([string]$ServiceAppId, [string]$ClientAppId, [string]$RoleName) {
    $ErrorActionPreference = "SilentlyContinue"

    Write-Host -ForegroundColor Yellow  "Setting up App Api Permissions. This requires the subscription admin privilege. If this fails, please refer to the manual steps and ask a subscription admin"
    $ServiceAppPermId = az ad app show --id $ServiceAppId --query oauth2Permissions[0].id
    $aadCommandId = "00000002-0000-0000-c000-000000000000"
    $permissionId = "311a71cc-e848-46a1-bdf8-97ff7156d8e6"
    
    if ($RoleName) {
        $appRoles =  az ad app show --id $ServiceAppId --query appRoles | ConvertFrom-Json

        $role = $appRoles | Where-Object { $_.Value -match $RoleName }
        if ($role) {
            $roleId = $role.Id
            az ad app permission add --id $ServiceAppId --api $ServiceAppId --api-permissions $roleId=Role > $null 2>&1
            az ad app permission grant --id $ServiceAppId --api $ServiceAppId --scope $roleId > $null 2>&1
        }
        else 
        {
            Write-Host -ForegroundColor Red  "$RoleName is not defined in the app $ServiceAppId"
        }
    }
    
    az ad app permission add --id $ServiceAppId --api $aadCommandId --api-permissions $permissionId=Scope > $null 2>&1
    az ad app permission add --id $ClientAppId --api $aadCommandId --api-permissions $permissionId=Scope > $null 2>&1
    az ad app permission add --id $ClientAppId --api $ServiceAppId --api-permissions $ServiceAppPermId=Scope > $null 2>&1
    az ad app permission grant --id $ServiceAppId --api $aadCommandId --scope $permissionId > $null 2>&1
    az ad app permission grant --id $ClientAppId --api $aadCommandId --scope $permissionId > $null 2>&1
    az ad app permission grant --id $ClientAppId --api $ServiceAppId --scope $ServiceAppPermId > $null 2>&1
    
    if (!$?) {
        Write-Host -ForegroundColor Yellow  "Please make sure the permissions are admin consented. If not, you need to use the grant permissions button in the Azure portal to grant them. Please ask a subscription owner to do so"
    }

    $ErrorActionPreference = "stop"    
}

# Generate tokens with the actual values
function Get-DefaultTokens {
    $tokens = New-Object 'System.Collections.Generic.Dictionary[String,String]'
    
    $tokens.Add('websiteName', $websiteName)
    $tokens.Add('sparkName', $sparkName)

    $tokens.Add('appInsightsName', $appInsightsName )
    $tokens.Add('redisName', $redisName )
    $tokens.Add('sparkRDPKVName', $sparkRDPKVName )
    $tokens.Add('fabricRDPKVName', $fabricRDPKVName )
    $tokens.Add('servicesKVName', $servicesKVName )
    $tokens.Add('sparkKVName', $sparkKVName )
    $tokens.Add('sfKVName', $sfKVName )
    $tokens.Add('docDBName', $docDBName )
    $tokens.Add('eventHubNamespaceName', $eventHubNamespaceName )
    $tokens.Add('serviceFabricName', $serviceFabricName )
    $tokens.Add('sparkBlobAccountName', $sparkBlobAccountName )
    $tokens.Add('configBlobAccountName', $configBlobAccountName )
    $tokens.Add('sparkManagedIdentityName', $sparkManagedIdentityName )

    # Template
    $tokens.Add('redisSkuName', $redisSkuName )
    $tokens.Add('redisSkuFamily', $redisSkuFamily )
    $tokens.Add('redisSkuCapacity', $redisSkuCapacity )

    # Blob
    $aiResource = Get-AzureRmApplicationInsights -resourceGroupName $resourceGroupName -Name $appInsightsName -ErrorAction SilentlyContinue
    $tokens.Add('appinsightkey', $aiResource.InstrumentationKey )    
    
    # SF Template
    $tokens.Add('vmNodeTypeName', $vmNodeTypeName )
    
    # Parameters
    $tokens.Add('serviceAppName', $serviceAppName )
    $tokens.Add('resourceGroup', $resourceGroupName )

    $tokens.Add('sparkPwd', $sparkPwd )
    $tokens.Add('sparkSshPwd', $sparkSshPwd )
    $tokens.Add('sfPwd', $sfPwd )
    $tokens.Add('name', $name )
    
    $tokens
}

# Variables
Export-ModuleMember -Variable "name"
Export-ModuleMember -Variable "serviceFabricName"
Export-ModuleMember -Variable "sparkName"
Export-ModuleMember -Variable "websiteName"
Export-ModuleMember -Variable "configBlobAccountName"
Export-ModuleMember -Variable "sparkBlobAccountName"
Export-ModuleMember -Variable "servicesKVName"
Export-ModuleMember -Variable "sparkKVName"
Export-ModuleMember -Variable "sparkRDPKVName"
Export-ModuleMember -Variable "fabricRDPKVName"
Export-ModuleMember -Variable "sfKVName"

Export-ModuleMember -Variable "appInsightsName"
Export-ModuleMember -Variable "docDBName"
Export-ModuleMember -Variable "eventHubNamespaceName"
Export-ModuleMember -Variable "redisName"

Export-ModuleMember -Variable "redisSkuName"
Export-ModuleMember -Variable "redisSkuFamily"
Export-ModuleMember -Variable "redisSkuCapacity"

Export-ModuleMember -Variable "sparkManagedIdentityName"
Export-ModuleMember -Variable "serviceAppName"
Export-ModuleMember -Variable "clientAppName"
Export-ModuleMember -Variable "vmNodeTypeName"

Export-ModuleMember -Variable "iotHubName"
Export-ModuleMember -Variable "kafkaEventHubNamespaceName"
Export-ModuleMember -Variable "kafkaName"
Export-ModuleMember -Variable "sparkPwd"
Export-ModuleMember -Variable "sparkSshPwd"
Export-ModuleMember -Variable "kafkaPwd"
Export-ModuleMember -Variable "kafkaSshPwd"
Export-ModuleMember -Variable "sfPwd"
Export-ModuleMember -Variable "certPwd"

Export-ModuleMember -Variable "sparkLogin"
Export-ModuleMember -Variable "sparksshuser"
Export-ModuleMember -Variable "kafkaLogin"
Export-ModuleMember -Variable "kafkasshuser"
Export-ModuleMember -Variable "sfadminuser"

# Paths
Export-ModuleMember -Variable "rootFolderPath"
Export-ModuleMember -Variable "resourcesTemplatePath"
Export-ModuleMember -Variable "servicesTemplatePath"
Export-ModuleMember -Variable "packagesPath"
Export-ModuleMember -Variable "adminParameterPath"
Export-ModuleMember -Variable "parametersOutputPath"
Export-ModuleMember -Variable "tempPath"
Export-ModuleMember -Variable "certPath"

# Functions
Export-ModuleMember -Function "Get-OutputFilePath"
Export-ModuleMember -Function "Get-PackageVersion"
Export-ModuleMember -Function "Get-StorageAccountConnectionString"
Export-ModuleMember -Function "Get-CosmosDBConnectionString"
Export-ModuleMember -Function "Deploy-Resources"
Export-ModuleMember -Function "Deploy-Services"
Export-ModuleMember -Function "Deploy-Files"
Export-ModuleMember -Function "Fix-ApplicationTypeVersion"
Export-ModuleMember -Function "CleanUp-Folder"
Export-ModuleMember -Function "Install-Packages"
Export-ModuleMember -Function "Translate-Tokens"
Export-ModuleMember -Function "Prepare-Template"
Export-ModuleMember -Function "Prepare-Packages"
Export-ModuleMember -Function "Setup-Secret"
Export-ModuleMember -Function "Get-Secret"
Export-ModuleMember -Function "CleanUp-ExistingSF"
Export-ModuleMember -Function "Check-Credential"
Export-ModuleMember -Function "Get-DefaultTokens"
Export-ModuleMember -Function "Set-AzureAADAccessControl"
Export-ModuleMember -Function "Set-AzureAADApiPermission"

