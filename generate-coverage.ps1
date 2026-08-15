param(
    [bool]$Open = $true,
    [bool]$IncludeE2E = $false
)

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "       TicketHub - Code Coverage Pipeline        " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Restore local dotnet tools (ReportGenerator)
Write-Host "`n[1/4] Restoring .NET local tools..." -ForegroundColor Yellow
dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to restore tools." -ForegroundColor Red
    exit $LASTEXITCODE
}

# 2. Clean old test results
Write-Host "`n[2/4] Cleaning previous TestResults..." -ForegroundColor Yellow
if (Test-Path "./TestResults") {
    Remove-Item -Recurse -Force "./TestResults"
}

# 3. Run tests with Coverlet code coverage collector
Write-Host "`n[3/4] Running bUnit test suite with Coverlet coverage..." -ForegroundColor Yellow
dotnet test "TicketHub.Tests.bUnit/TicketHub.Tests.bUnit.csproj" `
    --collect:"XPlat Code Coverage" `
    --settings "coverage.runsettings" `
    --results-directory "./TestResults" `
    --logger "console;verbosity=minimal"

if ($IncludeE2E) {
    Write-Host "`nRunning E2E tests with coverage..." -ForegroundColor Yellow
    dotnet test "TicketHub.Tests.E2E/TicketHub.Tests.E2E.csproj" `
        --collect:"XPlat Code Coverage" `
        --settings "coverage.runsettings" `
        --results-directory "./TestResults" `
        --logger "console;verbosity=minimal"
}

# 4. Generate visual HTML Report using ReportGenerator
Write-Host "`n[4/4] Generating HTML Code Coverage Report..." -ForegroundColor Yellow

$reportDir = "./TestResults/CoverageReport"
dotnet tool run reportgenerator `
    "-reports:./TestResults/**/coverage.cobertura.xml" `
    "-targetdir:$reportDir" `
    "-sourcedirs:./" `
    "-reporttypes:Html;TextSummary;Badges" `
    "-assemblyfilters:+TicketHub.Core;+TicketHub.Application;+TicketHub.Infrastructure;+TicketHub.Web;-*Tests*" `
    "-title:TicketHub Coverage Dashboard" `
    "-verbosity:Warning"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n=================================================" -ForegroundColor Green
    Write-Host " Coverage Report generated successfully!       " -ForegroundColor Green
    Write-Host " Location: $reportDir/index.html               " -ForegroundColor Green
    Write-Host "=================================================`n" -ForegroundColor Green

    # Output text summary to console
    if (Test-Path "$reportDir/Summary.txt") {
        Get-Content "$reportDir/Summary.txt"
    }

    if ($Open) {
        $htmlPath = (Resolve-Path "$reportDir/index.html").Path
        Start-Process $htmlPath
    }
} else {
    Write-Host "Failed to generate coverage report." -ForegroundColor Red
}
