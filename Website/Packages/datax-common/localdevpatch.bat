@echo on
REM Local Development Patch

@echo off
REM Patch this package's dist bits to Website's node_module/this_package_name/dist folder
REM Change destination path as needed if website location is different

@echo on
REM Patching dist folder to Website
xcopy dist ..\..\Website\node_modules\datax-common\dist /i /s /y

REM Patching dist folder to home
xcopy dist ..\datax-home\node_modules\datax-common\dist /i /s /y

REM Patching dist folder to pipeline
xcopy dist ..\datax-pipeline\node_modules\datax-common\dist /i /s /y

REM Patching dist folder to metrics
xcopy dist ..\datax-metrics\node_modules\datax-common\dist /i /s /y

REM Patching dist folder to jobs
xcopy dist ..\datax-jobs\node_modules\datax-common\dist /i /s /y

REM Patching dist folder to query
xcopy dist ..\datax-query\node_modules\datax-common\dist /i /s /y

@echo off
REM Patch all other dist folders that you want to patch here
REM For example, you may want to patch the feature packages that uses this package