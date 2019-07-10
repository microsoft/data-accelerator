@rem node deploy/zipsite.js
set webappName=%1
set subscriptionName=%2
if [%3] == [] (set resourceGroup=DataX) else (set resourceGroup=%3)

:deploy
cmd /c az account set -s %subscriptionName%
cmd /c az webapp deployment source config-zip --resource-group %resourceGroup% --name %webappName% --src deploy/deployment.zip
cmd /c az webapp restart --resource-group %resourceGroup% --name %webappName%

:end
@echo done.