$ErrorActionPreference = "Stop"

$rootDirectory = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $rootDirectory "deploy/local/compose.yaml"

Push-Location $rootDirectory
try {
    docker compose -f $composeFile up -d postgres s3

    dotnet tool restore
    dotnet restore HouseKeeper.slnx

    Write-Host -NoNewline "Waiting for PostgreSQL"
    do {
        docker compose -f $composeFile exec -T postgres `
            pg_isready -U housekeeper -d housekeeper *> $null

        if ($LASTEXITCODE -ne 0) {
            Write-Host -NoNewline "."
            Start-Sleep -Seconds 1
        }
    } while ($LASTEXITCODE -ne 0)
    Write-Host " ready"

    dotnet ef database update `
        --project src/Modules/HouseKeeper.Modules.Households `
        --startup-project src/HouseKeeper.Api `
        --context HouseholdsDbContext

    $api = Start-Process dotnet `
        -ArgumentList @(
            "run",
            "--project", "src/HouseKeeper.Api",
            "--launch-profile", "http"
        ) `
        -NoNewWindow `
        -PassThru

    $web = Start-Process dotnet `
        -ArgumentList @(
            "run",
            "--project", "src/HouseKeeper.Web",
            "--launch-profile", "http"
        ) `
        -NoNewWindow `
        -PassThru

    Write-Host ""
    Write-Host "HouseKeeper is starting:"
    Write-Host "  Web: http://localhost:5136"
    Write-Host "  API: http://localhost:5287"
    Write-Host "  S3: http://localhost:9000"
    Write-Host ""

    try {
        Wait-Process -Id @($api.Id, $web.Id)
    }
    finally {
        Stop-Process -Id @($api.Id, $web.Id) -ErrorAction SilentlyContinue
    }
}
finally {
    Pop-Location
}
