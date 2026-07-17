param(
    [string]$ApiUrl = "http://127.0.0.1:5099",
    [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection,
    [string]$TokenKey = $env:Token__Key,
    [string]$StorageRoot = "uploads-smoke",
    [switch]$SkipFrontendBuild
)

$ErrorActionPreference = "Stop"

function Wait-ForEndpoint {
    param(
        [string]$Url,
        [int]$Attempts = 30
    )

    for ($index = 1; $index -le $Attempts; $index++) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                return
            }
        }
        catch {
            if ($index -eq $Attempts) {
                throw "Endpoint did not become healthy: $Url"
            }
            Start-Sleep -Seconds 1
        }
    }
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "ConnectionStrings__DefaultConnection is required for deployment smoke validation."
}

if ([string]::IsNullOrWhiteSpace($TokenKey) -or [Text.Encoding]::UTF8.GetByteCount($TokenKey) -lt 32) {
    throw "Token__Key is required and must be at least 32 bytes for deployment smoke validation."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$backendRoot = Join-Path $repoRoot "backend"
$apiProject = Join-Path $backendRoot "src\DailyRentalHomes.Api"
$frontendRoot = Join-Path $repoRoot "clients\web-app"
$smokeStorageRoot = Join-Path $apiProject "wwwroot\$StorageRoot"
$process = $null

Push-Location $backendRoot
try {
    & "$PSScriptRoot\verify-migrations.ps1" -OutputPath "artifacts/migrations-smoke.sql"

    $env:ASPNETCORE_ENVIRONMENT = "Production"
    $env:ASPNETCORE_URLS = $ApiUrl
    $env:ConnectionStrings__DefaultConnection = $ConnectionString
    $env:DAILY_RENTAL_HOMES_CONNECTION_STRING = $ConnectionString
    $env:Token__Issuer = "DailyRentalHomes"
    $env:Token__Audience = "DailyRentalHomesClients"
    $env:Token__Key = $TokenKey
    $env:NotificationDelivery__Provider = "Fake"
    $env:Notifications__WorkerEnabled = "false"
    $env:BackgroundWorkers__DistributedLocking__Enabled = "true"
    $env:BackgroundWorkers__DistributedLocking__LeaseSeconds = "120"
    $env:FileStorage__Provider = "Local"
    $env:FileStorage__Local__RootPath = $StorageRoot
    $env:FileStorage__Local__PublicBasePath = "/$StorageRoot"

    dotnet build DailyRentalHomes.slnx --no-restore

    $process = Start-Process dotnet `
        -ArgumentList @("run", "--no-build", "--no-launch-profile", "--project", $apiProject, "--urls", $ApiUrl) `
        -NoNewWindow `
        -PassThru

    Wait-ForEndpoint "$ApiUrl/health"
    Wait-ForEndpoint "$ApiUrl/health/ready"

    if (-not (Test-Path $smokeStorageRoot)) {
        throw "Local file storage root was not created: $smokeStorageRoot"
    }

    Invoke-WebRequest -UseBasicParsing -Uri "$ApiUrl/api/rental-homes" -TimeoutSec 5 | Out-Null

    if (-not $SkipFrontendBuild) {
        Push-Location $frontendRoot
        try {
            npm run build
        }
        finally {
            Pop-Location
        }
    }

    Write-Host "Deployment smoke validation passed."
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
    Pop-Location
}
