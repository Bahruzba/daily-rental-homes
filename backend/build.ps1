$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

dotnet restore DailyRentalHomes.slnx

dotnet build DailyRentalHomes.slnx --configuration Release --no-restore
