<#
.SYNOPSIS
    Start Skidjakt locally for development.

.DESCRIPTION
    Runs the backend API and frontend dev server side-by-side.
    The Vite dev server proxies /api/* to the backend automatically.

.PARAMETER BackendOnly
    Only start the backend.

.PARAMETER FrontendOnly
    Only start the frontend.

.PARAMETER Docker
    Run everything via Docker Compose instead of native processes.

.EXAMPLE
    .\dev.ps1                   # Start both backend and frontend
    .\dev.ps1 -BackendOnly      # Only the .NET API
    .\dev.ps1 -FrontendOnly     # Only the React dev server
    .\dev.ps1 -Docker           # Run via Docker Compose
#>

param(
    [switch]$BackendOnly,
    [switch]$FrontendOnly,
    [switch]$Docker
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot

# -------------------------------------------------------------------
# Docker mode
# -------------------------------------------------------------------
if ($Docker) {
    Write-Host "Starting Skidjakt via Docker Compose..." -ForegroundColor Yellow
    Push-Location $ProjectRoot
    docker compose up --build
    Pop-Location
    exit 0
}

# -------------------------------------------------------------------
# Check prerequisites
# -------------------------------------------------------------------
function Test-Command($cmd) {
    return [bool](Get-Command $cmd -ErrorAction SilentlyContinue)
}

if (-not $FrontendOnly) {
    if (-not (Test-Command "dotnet")) {
        Write-Host "ERROR: dotnet SDK not found. Install .NET 10 from https://dot.net" -ForegroundColor Red
        exit 1
    }
    $dotnetVersion = dotnet --version
    Write-Host "dotnet: $dotnetVersion" -ForegroundColor DarkGray
}

if (-not $BackendOnly) {
    if (-not (Test-Command "node")) {
        Write-Host "ERROR: Node.js not found. Install from https://nodejs.org" -ForegroundColor Red
        exit 1
    }
    $nodeVersion = node --version
    Write-Host "node:   $nodeVersion" -ForegroundColor DarkGray
}

# -------------------------------------------------------------------
# Ensure frontend dependencies are installed
# -------------------------------------------------------------------
if (-not $BackendOnly) {
    $nodeModules = Join-Path $ProjectRoot "frontend\node_modules"
    if (-not (Test-Path $nodeModules)) {
        Write-Host "Installing frontend dependencies..." -ForegroundColor Yellow
        Push-Location (Join-Path $ProjectRoot "frontend")
        npm install
        Pop-Location
    }
}

# -------------------------------------------------------------------
# Start services
# -------------------------------------------------------------------
$jobs = @()

if (-not $FrontendOnly) {
    Write-Host "`nStarting backend on http://localhost:5000 ..." -ForegroundColor Green
    $backendJob = Start-Job -Name "Backend" -ScriptBlock {
        param($root)
        Set-Location (Join-Path $root "backend")
        # Ensure the data directory exists for SQLite
        $dataDir = Join-Path $root "backend\src\Skidjakt.Api\data"
        if (-not (Test-Path $dataDir)) { New-Item -ItemType Directory -Path $dataDir -Force | Out-Null }
        dotnet run --project src/Skidjakt.Api --urls "http://localhost:5000"
    } -ArgumentList $ProjectRoot
    $jobs += $backendJob
}

if (-not $BackendOnly) {
    Write-Host "Starting frontend on http://localhost:5173 ..." -ForegroundColor Green
    $frontendJob = Start-Job -Name "Frontend" -ScriptBlock {
        param($root)
        Set-Location (Join-Path $root "frontend")
        npm run dev
    } -ArgumentList $ProjectRoot
    $jobs += $frontendJob
}

# -------------------------------------------------------------------
# Stream output and wait
# -------------------------------------------------------------------
Write-Host "`n--------------------------------------------" -ForegroundColor DarkGray
Write-Host "  Skidjakt Development Server" -ForegroundColor Yellow
Write-Host "--------------------------------------------" -ForegroundColor DarkGray
if (-not $FrontendOnly) {
    Write-Host "  Backend API:  http://localhost:5000/api/health" -ForegroundColor Cyan
}
if (-not $BackendOnly) {
    Write-Host "  Frontend:     http://localhost:5173" -ForegroundColor Cyan
}
Write-Host "--------------------------------------------" -ForegroundColor DarkGray
Write-Host "  Press Ctrl+C to stop all services" -ForegroundColor DarkGray
Write-Host "--------------------------------------------`n" -ForegroundColor DarkGray

try {
    while ($true) {
        foreach ($job in $jobs) {
            Receive-Job $job -ErrorAction SilentlyContinue | ForEach-Object {
                $prefix = if ($job.Name -eq "Backend") { "[API]" } else { "[WEB]" }
                $color = if ($job.Name -eq "Backend") { "DarkCyan" } else { "DarkMagenta" }
                Write-Host "$prefix $_" -ForegroundColor $color
            }
        }
        Start-Sleep -Milliseconds 500
    }
}
finally {
    Write-Host "`nStopping services..." -ForegroundColor Yellow
    $jobs | ForEach-Object {
        Stop-Job $_ -ErrorAction SilentlyContinue
        Remove-Job $_ -Force -ErrorAction SilentlyContinue
    }
    Write-Host "All services stopped." -ForegroundColor Green
}
