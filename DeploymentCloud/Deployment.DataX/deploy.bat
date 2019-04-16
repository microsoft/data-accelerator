@echo off
echo Deploying resources
@set script=deployresources.ps1
powershell "&{..\Deployment.Common\deployresources.ps1 -ParamFile .\common.parameters.txt; exit $LastExitCode}"
if %errorlevel% neq 0 goto :errorHandler
echo Deploying resources succeeded

echo Deploying svc/apps
@set script=deployapps.ps1
powershell "&{.\deployapps.ps1 -ParamFile .\common.parameters.txt ;exit $LastExitCode}"
if %errorlevel% neq 0 goto :errorHandler
echo Deploying svc/apps succeeded

echo Deploying sample
@set script=deploySample.ps1
powershell "&{.\deploySample.ps1 -ParamFile .\common.parameters.txt ;exit $LastExitCode}"
if %errorlevel% neq 0 goto :errorHandler
echo Deploying sample succeeded
echo.

if exist cachedVariables (
    @set /p name=< cachedVariables
) else (
    @set name="your productname"
)
echo The deployment completed. Please open the DataX portal "https://%name%.azurewebsites.net/home"

goto :EOF

:errorHandler
echo Error occurred at %script%
exit /b %errorlevel%