@echo off
rem TB-SHIPPING-MVP-DEV-ADMIN-UI-065 -- recorded launch command for the user's Development host.
rem Reconstructed from the positively identified pre-restart process:
rem   PID 36540  dotnet run --project src\backend\Host\Tooba.Host --no-launch-profile --no-build
rem   PID 37028  src\backend\Host\Tooba.Host\bin\Debug\net8.0\Tooba.Host.exe
rem Equivalent to the Tooba.Host launch profile, expressed without it (as the user ran it):
rem   ASPNETCORE_ENVIRONMENT=Development   (profile: Tooba.Host / environmentVariables)
rem   ASPNETCORE_URLS=http://localhost:5088 (profile: applicationUrl)
rem Rollback: stop the Tooba.Host process and re-run this file from the repository root.
setlocal
cd /d "%~dp0..\..\..\.."
set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5088"
dotnet run --project src\backend\Host\Tooba.Host --no-launch-profile --no-build
exit /b %ERRORLEVEL%
