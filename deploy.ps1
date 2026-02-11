<#
.SYNOPSIS
    Deploy Skidjakt to a DigitalOcean droplet from Windows.

.DESCRIPTION
    Builds Docker images locally, transfers them to the droplet via
    docker save + scp + docker load, and restarts services. This avoids
    resource-intensive builds on the small droplet.

.PARAMETER HostName
    The droplet IP address or hostname (optional, reads from .env if not specified).

.PARAMETER User
    SSH user (optional, reads from .env or defaults to root).

.PARAMETER KeyFile
    Path to SSH private key (optional, reads from .env or uses ssh-agent).

.PARAMETER RepoUrl
    Git repository URL (optional, reads from .env if not specified).

.PARAMETER Setup
    Run first-time setup on the droplet (install Docker, clone repo).

.EXAMPLE
    .\deploy.ps1
    .\deploy.ps1 -Setup
    .\deploy.ps1 -HostName custom-host -KeyFile "C:\path\to\key"
#>

param(
    [string]$HostName,
    [string]$User,
    [string]$KeyFile,
    [string]$RepoUrl,
    [switch]$Setup
)

$ErrorActionPreference = "Stop"

# Load environment configuration
. "$PSScriptRoot\scripts\Load-Env.ps1"
try {
    $config = Get-EnvConfig
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Use command-line parameters or fall back to .env
if (-not $HostName) {
    $HostName = $config['DROPLET_HOST']
}
if (-not $User) {
    $User = $config['DROPLET_USER']
}
if (-not $User) {
    $User = 'root'
}
if (-not $KeyFile -and $config['SSH_KEY_PATH']) {
    $KeyFile = $config['SSH_KEY_PATH']
}
if (-not $RepoUrl -and $config['GIT_REPO_URL']) {
    $RepoUrl = $config['GIT_REPO_URL']
}

if (-not $HostName) {
    Write-Host "Error: DROPLET_HOST not configured in .env file" -ForegroundColor Red
    exit 1
}

function Get-SshBaseArgs {
    $args_ = @()
    if ($KeyFile) {
        $args_ += "-i", $KeyFile
    }
    $args_ += "-o", "StrictHostKeyChecking=accept-new"
    return $args_
}

function Invoke-Ssh {
    param([string]$Command)

    $sshArgs = Get-SshBaseArgs
    $sshArgs += "$User@$HostName"
    $sshArgs += $Command

    Write-Host ">> $Command" -ForegroundColor Cyan
    ssh @sshArgs
    if ($LASTEXITCODE -ne 0) {
        throw "SSH command failed with exit code $LASTEXITCODE"
    }
}

function Invoke-Scp {
    param([string]$LocalPath, [string]$RemotePath)

    $scpArgs = Get-SshBaseArgs
    $scpArgs += $LocalPath
    $scpArgs += "${User}@${HostName}:${RemotePath}"

    Write-Host ">> scp $LocalPath -> ${HostName}:${RemotePath}" -ForegroundColor Cyan
    scp @scpArgs
    if ($LASTEXITCODE -ne 0) {
        throw "SCP failed with exit code $LASTEXITCODE"
    }
}

function Build-AndTransferImages {
    $imageTar = Join-Path $env:TEMP "skidjakt-images.tar"

    Write-Host "`n--- Building backend image locally ---" -ForegroundColor Green
    docker build -t skidjakt-backend:latest ./backend
    if ($LASTEXITCODE -ne 0) { throw "Backend build failed" }

    Write-Host "`n--- Building frontend image locally ---" -ForegroundColor Green
    docker build -t skidjakt-frontend:latest ./frontend
    if ($LASTEXITCODE -ne 0) { throw "Frontend build failed" }

    Write-Host "`n--- Saving images to tar ---" -ForegroundColor Green
    docker save skidjakt-backend:latest skidjakt-frontend:latest -o $imageTar
    if ($LASTEXITCODE -ne 0) { throw "Docker save failed" }

    $sizeMB = [math]::Round((Get-Item $imageTar).Length / 1MB, 1)
    Write-Host "Image tar size: ${sizeMB} MB" -ForegroundColor Cyan

    Write-Host "`n--- Transferring images to droplet ---" -ForegroundColor Green
    Invoke-Scp -LocalPath $imageTar -RemotePath "/tmp/skidjakt-images.tar"

    Write-Host "`n--- Loading images on droplet ---" -ForegroundColor Green
    Invoke-Ssh "docker load -i /tmp/skidjakt-images.tar"

    Write-Host "`n--- Cleaning up ---" -ForegroundColor Green
    Invoke-Ssh "rm -f /tmp/skidjakt-images.tar"
    Remove-Item -Force $imageTar -ErrorAction SilentlyContinue
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
    Invoke-Ssh "apt-get update && apt-get install -y docker.io curl git"
    Invoke-Ssh "systemctl enable docker && systemctl start docker"

    Write-Host "`n--- Installing Docker Compose ---" -ForegroundColor Green
    $composeScript = @'
if apt-cache show docker-compose-plugin &>/dev/null; then
    apt-get install -y docker-compose-plugin
else
    COMPOSE_VERSION=$(curl -s https://api.github.com/repos/docker/compose/releases/latest | grep -Po '"tag_name": "\K.*?(?=")')
    curl -SL "https://github.com/docker/compose/releases/download/${COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
    ln -sf /usr/local/bin/docker-compose /usr/bin/docker-compose
fi
'@
    Invoke-Ssh $composeScript

    Write-Host "`n--- Cloning repository ---" -ForegroundColor Green
    Invoke-Ssh "rm -rf /opt/skidjakt && git clone $RepoUrl /opt/skidjakt"

    # Build locally and transfer images instead of building on the droplet
    Build-AndTransferImages

    Write-Host "`n--- Starting services ---" -ForegroundColor Green
    $startScript = @'
cd /opt/skidjakt
if docker compose version &>/dev/null 2>&1; then
    COMPOSE="docker compose"
elif command -v docker-compose &>/dev/null; then
    COMPOSE="docker-compose"
else
    echo "ERROR: No docker compose found"; exit 1
fi
$COMPOSE -f docker-compose.prod.yml up -d
'@
    Invoke-Ssh $startScript

    Write-Host "`n--- Checking service health ---" -ForegroundColor Green
    Invoke-Ssh "sleep 5 && curl -sf http://localhost:5000/api/health || echo 'Backend not ready yet (may need a few more seconds)'"

    Write-Host "`n=== Setup complete! ===" -ForegroundColor Yellow
    Write-Host "App should be running at http://$HostName" -ForegroundColor Green
    exit 0
}

# -------------------------------------------------------------------
# Normal deploy
# -------------------------------------------------------------------
Write-Host "`n=== Deploying Skidjakt to $HostName ===" -ForegroundColor Yellow

# Build locally and transfer images
Build-AndTransferImages

Write-Host "`n--- Pulling latest code (for config changes) ---" -ForegroundColor Green
Invoke-Ssh "cd /opt/skidjakt && git pull origin main"

Write-Host "`n--- Restarting services ---" -ForegroundColor Green
$deployScript = @'
cd /opt/skidjakt

# Detect compose command
if docker compose version &>/dev/null 2>&1; then
    COMPOSE="docker compose"
elif command -v docker-compose &>/dev/null; then
    COMPOSE="docker-compose"
else
    echo "ERROR: No docker compose found"; exit 1
fi

# Use prod compose file if nginx is running (localhost-only ports)
COMPOSE_FILE=""
if systemctl is-active --quiet nginx 2>/dev/null && [ -f docker-compose.prod.yml ]; then
    COMPOSE_FILE="-f docker-compose.prod.yml"
    echo "--- Detected nginx, using production compose file"
fi

# Remove stale override files (override merges with base, causing port conflicts)
rm -f docker-compose.override.yml

$COMPOSE $COMPOSE_FILE down
$COMPOSE $COMPOSE_FILE up -d
'@
Invoke-Ssh $deployScript

Write-Host "`n--- Cleaning up old images ---" -ForegroundColor Green
Invoke-Ssh "docker image prune -f"

Write-Host "`n--- Checking service health ---" -ForegroundColor Green
Invoke-Ssh "sleep 3 && curl -sf http://localhost:5000/api/health || echo 'Backend not ready yet (may need a few more seconds)'"

Write-Host "`n=== Deploy complete! ===" -ForegroundColor Yellow
Write-Host "App running at http://$HostName" -ForegroundColor Green
