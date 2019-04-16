function checkEqual([string]$actual, [string]$expected) {
    if ($actual -ceq $expected) {
        return $true
    }
    else {
        Write-Host $expected "is expected. But the actual value is" $actual
        return $false
    }
}

function checkNotEqual([string]$actual, [string]$expected) {
    if (!($actual -ceq $expected)) {
        return $true
    }
    else {
        Write-Host $expected "is expected. But the actual value is" $actual
        return $false
    }
}

function checkNotNull([string]$value) {
    if ($value) {
        return $true
    }
    else {
        Write-Host "The value is null or empty, but it is not expected."
        return $false
    }
}

function ImportModule ([string]$resourceGroupName, [string]$productName, [string]$sparkClusterName, [string]$randomizeProductName, [string]$serviceFabricClusterName, [string]$serviceAppName, [string]$clientAppName) {
    Import-Module "..\Deployment.Common\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName, $productName, $sparkClusterName, $randomizeProductName, $serviceFabricClusterName, $serviceAppName, $clientAppName -WarningAction SilentlyContinue
}

function TestNameWithCacheNameAndEmptyProductName {    
    Write-Host "TestNameWithCacheNameAndEmptyProductName"
    Set-Content -Path ".\cachedVariables" -NoNewline -Value "myProd12345"
    $rootFolderPath = $PSScriptRoot
    $resourceGroupName = "DataX"; $productName = ""; $sparkClusterName = ""; 
    $randomizeProductName = "y"; $serviceFabricClusterName = ""; $serviceAppName = "configgen1"; $clientAppName = "app1"
    ImportModule -ResourceGroupName $resourceGroupName -productName $productName -sparkClusterName $sparkClusterName -randomizeProductName $randomizeProductName `
    -serviceFabricClusterName $serviceFabricClusterName -serviceAppName $serviceAppName -clientAppName $clientAppName
    
    checkEqual -actual $resourceGroupName -expected "DataX"
    checkEqual -actual $name -expected "myprod12345"
    checkEqual -actual $sparkName -expected "myprod12345"
    checkEqual -actual $serviceFabricName -expected "myprod12345-sf"
    checkEqual -actual $vmNodeTypeName -expected "vmd13o5rm"
    
    Remove-Module UtilityModule
    Remove-Item -path ".\cachedVariables" -Force -ErrorAction SilentlyContinue
}

function TestNameWithCacheName {    
    Write-Host "TestNameWithCacheName"
    Set-Content -Path ".\cachedVariables" -NoNewline -Value "myProd12345"
    $rootFolderPath = $PSScriptRoot
    $resourceGroupName = "DataX"; $productName = "test"; $sparkClusterName = ""; 
    $randomizeProductName = "y"; $serviceFabricClusterName = ""; $serviceAppName = "configgen1"; $clientAppName = "app1"
    # Import-Module ".\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName, $productName, $sparkClusterName, $randomizeProductName, $serviceFabricClusterName, $serviceAppName, $clientAppName -WarningAction SilentlyContinue
    ImportModule -ResourceGroupName $resourceGroupName -productName $productName -sparkClusterName $sparkClusterName -randomizeProductName $randomizeProductName `
    -serviceFabricClusterName $serviceFabricClusterName -serviceAppName $serviceAppName -clientAppName $clientAppName
    
    checkEqual -actual $resourceGroupName -expected "DataX"
    checkEqual -actual $name -expected "myprod12345"
    checkEqual -actual $sparkName -expected "myprod12345"
    checkEqual -actual $serviceFabricName -expected "myprod12345-sf"
    checkEqual -actual $vmNodeTypeName -expected "vmd13o5rm"
    
    Remove-Module UtilityModule
    Remove-Item -path ".\cachedVariables" -Force -ErrorAction SilentlyContinue
}
    
# Test the default product name
function TestNameWithDefaultRandomName {   
    Write-Host "TestNameWithDefaultRandomName" 
    $rootFolderPath = $PSScriptRoot
    $resourceGroupName = "DataX"; $productName = ""; $sparkClusterName = ""; 
    $randomizeProductName = "y"; $serviceFabricClusterName = ""; $serviceAppName = "configgen1"; $clientAppName = "app1"
    # Import-Module ".\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName, $productName, $sparkClusterName, $randomizeProductName, $serviceFabricClusterName, $serviceAppName, $clientAppName -WarningAction SilentlyContinue
    ImportModule -ResourceGroupName $resourceGroupName -productName $productName -sparkClusterName $sparkClusterName -randomizeProductName $randomizeProductName `
    -serviceFabricClusterName $serviceFabricClusterName -serviceAppName $serviceAppName -clientAppName $clientAppName
    
    checkEqual -actual $resourceGroupName -expected "DataX"
    checkNotNull -value $name
    checkEqual -actual $sparkName -expected $name
    checkEqual -actual $serviceFabricName -expected "$name-sf"
    checkEqual -actual $vmNodeTypeName -expected "vmd708343"
    
    Remove-Module UtilityModule
}

# Test the randomized product name, the custom spark name, the custom SF name 
function TestNameWithRandomName {    
    Write-Host "TestNameWithRandomName" 
    $rootFolderPath = $PSScriptRoot
    $resourceGroupName = "DataX"; $productName = "myProd"; $sparkClusterName = "mySpark"; 
    $randomizeProductName = "y"; $serviceFabricClusterName = "mySF"; $serviceAppName = "configgen1"; $clientAppName = "app1"
    # Import-Module ".\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName, $productName, $sparkClusterName, $randomizeProductName, $serviceFabricClusterName, $serviceAppName, $clientAppName -WarningAction SilentlyContinue
    ImportModule -ResourceGroupName $resourceGroupName -productName $productName -sparkClusterName $sparkClusterName -randomizeProductName $randomizeProductName `
    -serviceFabricClusterName $serviceFabricClusterName -serviceAppName $serviceAppName -clientAppName $clientAppName
    
    checkEqual -actual $resourceGroupName -expected "DataX"
    checkNotEqual -actual $name -expected "myprod"
    checkEqual -actual $sparkName -expected "myspark"
    checkEqual -actual $serviceFabricName -expected "mysf"
    checkNotEqual -actual $vmNodeTypeName -expected "D3myprod"
    
    Remove-Module UtilityModule
}

# Test the non-randomized product name, the custom spark name, the custom SF name 
function TestNameWithNoRandomName {    
    Write-Host "TestNameWithNoRandomName" 
    $rootFolderPath = $PSScriptRoot
    $resourceGroupName = "DataX"; $productName = "myProd"; $sparkClusterName = "mySpark"; 
    $randomizeProductName = "n"; $serviceFabricClusterName = "mySF"; $serviceAppName = "configgen1"; $clientAppName = "app1"
    # Import-Module ".\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName, $productName, $sparkClusterName, $randomizeProductName, $serviceFabricClusterName, $serviceAppName, $clientAppName -WarningAction SilentlyContinue
    ImportModule -ResourceGroupName $resourceGroupName -productName $productName -sparkClusterName $sparkClusterName -randomizeProductName $randomizeProductName `
    -serviceFabricClusterName $serviceFabricClusterName -serviceAppName $serviceAppName -clientAppName $clientAppName
    
    checkEqual -actual $resourceGroupName -expected "DataX"
    checkEqual -actual $name -expected "myprod"
    checkEqual -actual $sparkName -expected "myspark"
    checkEqual -actual $serviceFabricName -expected "mysf"
    checkEqual -actual $vmNodeTypeName -expected "vmopdmry"

    Remove-Module UtilityModule
}

# Test the non-randomized product name
function TestvmNameWithProductNameAndNoRandomName {   
    Write-Host "TestvmNameWithProductNameAndNoRandomName"  
    $rootFolderPath = $PSScriptRoot
    $resourceGroupName = "DataX"; $productName = "myprod78794"; $sparkClusterName = ""; 
    $randomizeProductName = "n"; $serviceFabricClusterName = ""; $serviceAppName = "configgen1"; $clientAppName = "app1"
    # Import-Module ".\Helpers\UtilityModule" -ArgumentList $rootFolderPath, $resourceGroupName, $productName, $sparkClusterName, $randomizeProductName, $serviceFabricClusterName, $serviceAppName, $clientAppName -WarningAction SilentlyContinue
    ImportModule -ResourceGroupName $resourceGroupName -productName $productName -sparkClusterName $sparkClusterName -randomizeProductName $randomizeProductName `
    -serviceFabricClusterName $serviceFabricClusterName -serviceAppName $serviceAppName -clientAppName $clientAppName
    
    checkEqual -actual $resourceGroupName -expected "DataX"
    checkEqual -actual $name -expected "myprod78794"
    checkEqual -actual $sparkName -expected "myprod78794"
    checkEqual -actual $serviceFabricName -expected "myprod78794-sf"
    checkEqual -actual $vmNodeTypeName -expected "vmd77o4rm"

    Remove-Module UtilityModule
}

TestNameWithCacheNameAndEmptyProductName
TestNameWithCacheName
TestNameWithDefaultRandomName
TestNameWithRandomName
TestNameWithNoRandomName
TestvmNameWithProductNameAndNoRandomName