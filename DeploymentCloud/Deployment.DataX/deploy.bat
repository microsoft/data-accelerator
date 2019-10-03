@echo off
echo Deploying resources
@set script=deployresources.ps1
powershell "&{..\Deployment.Common\deployresources.ps1 -ParamFile .\common.parameters.txt; exit $LastExitCode}"
if %errorlevel% neq 0 goto :errorHandler
echo Deploying resources succeeded

REM echo Deploying svc/apps
REM @set script=deployapps.ps1
REM powershell "&{.\deployapps.ps1 -ParamFile .\common.parameters.txt ;exit $LastExitCode}"
REM if %errorlevel% neq 0 goto :errorHandler
REM echo Deploying svc/apps succeeded

REM echo Deploying sample
REM @set script=deploySample.ps1
REM powershell "&{.\deploySample.ps1 -ParamFile .\common.parameters.txt ;exit $LastExitCode}"
REM if %errorlevel% neq 0 goto :errorHandler
REM echo Deploying sample succeeded
REM echo.

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