@FOR /F "TOKENS=1* DELIMS= " %%A IN ('DATE/T') DO @SET CDATE=%%B
@FOR /F "TOKENS=1,2 eol=/ DELIMS=/ " %%A IN ('DATE/T') DO @SET mm=%%B
@FOR /F "TOKENS=1,2 DELIMS=/ eol=/" %%A IN ('echo %CDATE%') DO @SET dd=%%B
@FOR /F "TOKENS=2,3 DELIMS=/ " %%A IN ('echo %CDATE%') DO @SET yyyy=%%B
SET date=%yyyy%%mm%%dd%
SET dataxver=1.2.0
SET module_name=%1
SET dst_name=%2

@GOTO :setsrc
@IF ERRORLEVEL 1 GOTO help
@GOTO end

:setsrc
SET src_module=%module_name%
SET src_filepath=%src_module%\target\%src_module%-%dataxver%
@GOTO :step2

:step2
@GOTO :setdst_%dst_name%
@IF ERRORLEVEL 1 GOTO help
@GOTO end

:setdst_staging
SET filename=%src_module%-%dataxver%
SET dst_sa=%3
SET dst_container=%4
SET dst_filepath=%5/%filename%
@GOTO deployment

:deployment
call az storage blob upload --auth-mode login --account-name %dst_sa% -f %src_filepath%.jar -c %dst_container% -n %dst_filepath%.jar
call az storage blob snapshot --auth-mode login --account-name %dst_sa% -c %dst_container% -n %dst_filepath%.jar
@echo deployment is done.
GOTO end

:help
@echo usage: "deploy <module> <destination>"
@echo example: deploy core staging

:end
