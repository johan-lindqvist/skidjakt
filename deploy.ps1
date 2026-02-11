<#
.SYNOPSIS
    Deploy Skidjakt to a DigitalOcean droplet from Windows.

.DESCRIPTION
    Connects via SSH to the droplet and pulls the latest code, rebuilds
    Docker images, and restarts services. Reads configuration from .env file
    if parameters are not provided.

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
Import-Module "$PSScriptRoot\scripts\Load-Env.ps1" -Force
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

    Write-Host "`n--- Installing Docker buildx (optional) ---" -ForegroundColor Green
    $buildxScript = @'
mkdir -p ~/.docker/cli-plugins
BUILDX_VERSION=$(curl -s https://api.github.com/repos/docker/buildx/releases/latest | grep -Po '"tag_name": "\K.*?(?=")')
curl -SL "https://github.com/docker/buildx/releases/download/${BUILDX_VERSION}/buildx-$(uname -s | tr '[:upper:]' '[:lower:]')-$(uname -m)" -o ~/.docker/cli-plugins/docker-buildx 2>/dev/null || true
chmod +x ~/.docker/cli-plugins/docker-buildx 2>/dev/null || true
'@
    Invoke-Ssh $buildxScript

    Write-Host "`n--- Cloning repository ---" -ForegroundColor Green
    Invoke-Ssh "rm -rf /opt/skidjakt && git clone $RepoUrl /opt/skidjakt"

    Write-Host "`n--- Building and starting services ---" -ForegroundColor Green
    $startScript = @'
cd /opt/skidjakt
if docker compose version &>/dev/null 2>&1; then
    docker compose up -d --build
else
    docker-compose up -d --build
fi
'@
    Invoke-Ssh $startScript

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
$buildScript = @'
cd /opt/skidjakt
if docker compose version &>/dev/null 2>&1; then
    docker compose build
else
    docker-compose build
fi
'@
Invoke-Ssh $buildScript

Write-Host "`n--- Restarting services ---" -ForegroundColor Green
$upScript = @'
cd /opt/skidjakt
if docker compose version &>/dev/null 2>&1; then
    docker compose up -d
else
    docker-compose up -d
fi
'@
Invoke-Ssh $upScript

Write-Host "`n--- Cleaning up old images ---" -ForegroundColor Green
Invoke-Ssh "docker image prune -f"

Write-Host "`n--- Checking service health ---" -ForegroundColor Green
Invoke-Ssh "sleep 3 && curl -sf http://localhost:5000/api/health || echo 'Backend not ready yet (may need a few more seconds)'"

Write-Host "`n=== Deploy complete! ===" -ForegroundColor Yellow
Write-Host "App running at http://$HostName" -ForegroundColor Green
