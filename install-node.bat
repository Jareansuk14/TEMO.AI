@echo off
where winget >nul 2>&1
if %errorlevel% neq 0 exit /b 1

winget install OpenJS.NodeJS.LTS ^
--accept-package-agreements ^
--accept-source-agreements ^
--silent
