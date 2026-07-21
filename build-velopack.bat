@echo off
setlocal enabledelayedexpansion

set "PROJECT=TEMO.AI.csproj"
set "VERSION_FILE=AppVersion.props"
set "APP_ID=TEMO.AI"
set "APP_TITLE=TEMO.AI"
set "MAIN_EXE=TEMO.AI.exe"
set "RUNTIME=win-x64"
set "REPO_URL=https://github.com/Jareansuk14/TEMO.AI"
set "VPK_VERSION=1.2.0"
set "PUBLISH_DIR=%CD%\artifacts\publish"
set "RELEASES_DIR=%CD%\Releases"

for /f "usebackq delims=" %%v in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "[xml]$p=Get-Content '%VERSION_FILE%'; $p.Project.PropertyGroup.AppVersion"`) do set "VERSION=%%v"

if "%VERSION%"=="" (
  echo Cannot determine version from %VERSION_FILE%.
  exit /b 1
)

echo Building %APP_TITLE% %VERSION%
echo.

if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if exist "%RELEASES_DIR%" rmdir /s /q "%RELEASES_DIR%"
mkdir "%RELEASES_DIR%"

dotnet build "%PROJECT%" ^
  -c Release ^
  -r %RUNTIME%

if errorlevel 1 exit /b 1

set "CONFUSER_CLI=%~dp0..\tools\ConfuserEx\Confuser.CLI.exe"
if not exist "%CONFUSER_CLI%" (
  echo Installing ConfuserEx...
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\tools\install-confuser.ps1"
  if errorlevel 1 exit /b 1
)

if exist "%CONFUSER_CLI%" (
  echo.
  echo Obfuscating release assembly...
  set "CONFUSER_LOG=%TEMP%\confuser-%RANDOM%.log"
  "%CONFUSER_CLI%" -n "%~dp0build\confuser.crproj" > "!CONFUSER_LOG!" 2>&1
  set "CONFUSER_EXIT=!ERRORLEVEL!"
  findstr /V /C:"[DEBUG]" "!CONFUSER_LOG!" 2>nul
  del "!CONFUSER_LOG!" 2>nul
  if !CONFUSER_EXIT! neq 0 exit /b 1
) else (
  echo ConfuserEx not found. Skipping obfuscation.
)

echo.
echo Publishing single-file release...
dotnet publish "%PROJECT%" ^
  -c Release ^
  -r %RUNTIME% ^
  --no-build ^
  --self-contained true ^
  -o "%PUBLISH_DIR%" ^
  /p:PublishSingleFile=true ^
  /p:IncludeAllContentForSelfExtract=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:DebugType=None ^
  /p:DebugSymbols=false

if errorlevel 1 exit /b 1

dotnet tool update -g vpk --version %VPK_VERSION%
if errorlevel 1 dotnet tool install -g vpk --version %VPK_VERSION%
if errorlevel 1 exit /b 1

set "TOKEN_ARG="
if not "%GITHUB_TOKEN%"=="" set "TOKEN_ARG=--token %GITHUB_TOKEN%"

set "SIGN_ARG="
if not "%SIGN_PARAMS%"=="" set "SIGN_ARG=--signParams %SIGN_PARAMS%"

echo.
echo Downloading existing GitHub releases for delta packages...
vpk download github --repoUrl "%REPO_URL%" %TOKEN_ARG%
if errorlevel 1 echo No previous release downloaded. Continuing with a full package.

echo.
echo Packing Velopack release...
vpk pack ^
  --packId "%APP_ID%" ^
  --packTitle "%APP_TITLE%" ^
  --packVersion "%VERSION%" ^
  --packDir "%PUBLISH_DIR%" ^
  --mainExe "%MAIN_EXE%" ^
  --runtime %RUNTIME% ^
  --icon "Public\Logo.ico" ^
  --outputDir "%RELEASES_DIR%" ^
  %SIGN_ARG%

if errorlevel 1 exit /b 1

if not exist "%RELEASES_DIR%\%APP_ID%-win-Setup.exe" (
  echo Velopack setup was not created.
  exit /b 1
)

if "%GITHUB_TOKEN%"=="" (
  echo.
  echo Package created in:
  echo %RELEASES_DIR%
  exit /b 0
)

echo.
echo Uploading GitHub release...
vpk upload github ^
  --repoUrl "%REPO_URL%" ^
  %TOKEN_ARG% ^
  --publish ^
  --tag "v%VERSION%" ^
  --releaseName "%APP_TITLE% %VERSION%"

if errorlevel 1 exit /b 1

echo.
echo Done.
echo Installer and release assets are in:
echo %RELEASES_DIR%
