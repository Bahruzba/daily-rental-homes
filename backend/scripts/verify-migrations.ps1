param(
    [string]$OutputPath = "artifacts/migrations-idempotent.sql"
)

$ErrorActionPreference = "Stop"

$backendRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $backendRoot
try {
    $outputFullPath = Join-Path $backendRoot $OutputPath
    $outputDirectory = Split-Path -Parent $outputFullPath
    if (-not (Test-Path $outputDirectory)) {
        New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    }

    dotnet ef migrations script --idempotent `
        --project src/DailyRentalHomes.Infrastructure `
        --startup-project src/DailyRentalHomes.Api `
        --output $outputFullPath

    if (-not (Test-Path $outputFullPath) -or (Get-Item $outputFullPath).Length -le 0) {
        throw "Migration script was not generated or is empty: $outputFullPath"
    }

    Write-Host "Migration script generated: $outputFullPath"
}
finally {
    Pop-Location
}
