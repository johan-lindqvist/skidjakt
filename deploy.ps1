<#
.SYNOPSIS
    Deploy Skidjakt to a DigitalOcean droplet from Windows.

.DESCRIPTION
    Connects via SSH to the droplet and pulls the latest code, rebuilds
    Docker images, and restarts services. Assumes the repo is already
    cloned at /opt/skidjakt on the droplet.

.PARAMETER Host
    The droplet IP address or hostname.

.PARAMETER User
    SSH user (default: root).

.PARAMETER KeyFile
    Path to SSH private key (optional, uses default ssh agent if omitted).

.PARAMETER Setup
    Run first-time setup on the droplet (install Docker, clone repo).

.EXAMPLE
    .\deploy.ps1 -Host 123.45.67.89
    .\deploy.ps1 -Host 123.45.67.89 -Setup
    .\deploy.ps1 -Host ski.example.com -User deploy -KeyFile ~/.ssh/id_ed25519
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$HostName,

    [string]$User = "root",

    [string]$KeyFile,

    [string]$RepoUrl,

    [switch]$Setup
)

$ErrorActionPreference = "Stop"

function Invoke-Ssh {
    param([string]$Command)

    $sshArgs = @()
    if ($KeyFile) {
        $sshArgs += "-i", $KeyFile
    }
    $sshArgs += "-o", "StrictHostKeyChecking=accept-new"
    $sshArgs += "$User@$HostName"
    $sshArgs += $Command

    Write-Host ">> $Command" -ForegroundColor Cyan
    ssh @sshArgs
    if ($LASTEXITCODE -ne 0) {
        throw "SSH command failed with exit code $LASTEXITCODE"
    }
}

# -------------------------------------------------------------------
# First-time setup
# -------------------------------------------------------------------
if ($Setup) {
    if (-not $RepoUrl) {
        $RepoUrl = Read-Host "Git repo URL (e.g. git@github.com:user/skidjakt.git)"
    }

    Write-Host "`n=== First-time droplet setup ===" -ForegroundColor Yellow

    Write-Host "`n--- Installing Docker ---" -ForegroundColor Green
    Invoke-Ssh "apt-get update && apt-get install -y docker.io docker-compose-plugin"
    Invoke-Ssh "systemctl enable docker && systemctl start docker"

    Write-Host "`n--- Cloning repository ---" -ForegroundColor Green
    Invoke-Ssh "git clone $RepoUrl /opt/skidjakt"

    Write-Host "`n--- Building and starting services ---" -ForegroundColor Green
    Invoke-Ssh "cd /opt/skidjakt && docker compose up -d --build"

    Write-Host "`n=== Setup complete! ===" -ForegroundColor Yellow
    Write-Host "App should be running at http://$HostName" -ForegroundColor Green
    exit 0
}

# -------------------------------------------------------------------
# Normal deploy
# -------------------------------------------------------------------
Write-Host "`n=== Deploying Skidjakt to $HostName ===" -ForegroundColor Yellow

Write-Host "`n--- Pulling latest code ---" -ForegroundColor Green
Invoke-Ssh "cd /opt/skidjakt && git pull origin main"

Write-Host "`n--- Building Docker images ---" -ForegroundColor Green
Invoke-Ssh "cd /opt/skidjakt && docker compose build"

Write-Host "`n--- Restarting services ---" -ForegroundColor Green
Invoke-Ssh "cd /opt/skidjakt && docker compose up -d"

Write-Host "`n--- Cleaning up old images ---" -ForegroundColor Green
Invoke-Ssh "docker image prune -f"

Write-Host "`n--- Checking service health ---" -ForegroundColor Green
Invoke-Ssh "sleep 3 && curl -sf http://localhost:5000/api/health || echo 'Backend not ready yet (may need a few more seconds)'"

Write-Host "`n=== Deploy complete! ===" -ForegroundColor Yellow
Write-Host "App running at http://$HostName" -ForegroundColor Green
