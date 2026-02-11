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
ARCH=$(uname -m)
case "$ARCH" in
    x86_64) ARCH="amd64" ;;
    aarch64) ARCH="arm64" ;;
esac
BUILDX_VERSION=$(curl -s https://api.github.com/repos/docker/buildx/releases/latest | grep -Po '"tag_name": "\K.*?(?=")')
curl -SL "https://github.com/docker/buildx/releases/download/${BUILDX_VERSION}/buildx-${BUILDX_VERSION}.linux-${ARCH}" -o ~/.docker/cli-plugins/docker-buildx 2>/dev/null || true
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

Write-Host "`n--- Building and restarting services ---" -ForegroundColor Green
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

$COMPOSE $COMPOSE_FILE build
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
