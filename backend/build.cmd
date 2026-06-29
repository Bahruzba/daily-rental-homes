@echo off
cd /d %~dp0

dotnet restore DailyRentalHomes.slnx
if errorlevel 1 exit /b %errorlevel%

dotnet build DailyRentalHomes.slnx --configuration Release --no-restore
if errorlevel 1 exit /b %errorlevel%
