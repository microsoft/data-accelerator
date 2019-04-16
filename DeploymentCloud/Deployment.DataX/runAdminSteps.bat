@echo off
echo Setting up Admin steps
@set script=adminSteps.ps1
powershell "&{.\adminSteps.ps1 -ParamFile .\adminsteps.parameters.txt; exit $LastExitCode}"
if %errorlevel% neq 0 goto :errorHandler
echo Setting up Admin steps completed

goto :EOF

:errorHandler
echo Error occurred at %script%
exit /b %errorlevel%