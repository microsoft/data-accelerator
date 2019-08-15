@echo on
REM Local Development Patch

@echo off
REM Patch this package's dist bits to Website's node_module/this_package_name/dist folder
REM Change destination path as needed if website location is different

@echo on
REM Patching dist folder to pipeline
xcopy dist ..\datax-pipeline\node_modules\datax-query\dist /i /s /y

@echo off
REM Patch all other dist folders that you want to patch here
REM For example, you may want to patch the feature packages that uses this package