<#
.SYNOPSIS
    Check Skidjakt logs on the droplet.

.PARAMETER HostName
    Droplet IP or hostname (optional, reads from .env if not specified).

.PARAMETER KeyFile
    SSH private key path (optional, reads from .env or uses ssh-agent).

.PARAMETER Follow
    Tail logs in real-time.

.PARAMETER Errors
    Show only errors.

.PARAMETER Scraping
    Show only scraping activity.

.EXAMPLE
    .\check-logs.ps1
    .\check-logs.ps1 -Follow
    .\check-logs.ps1 -Errors
    .\check-logs.ps1 -Scraping
    .\check-logs.ps1 -HostName custom-host -KeyFile "C:\path\to\key"
#>

param(
    [string]$HostName,
    [string]$KeyFile,
    [switch]$Follow,
    [switch]$Errors,
    [switch]$Scraping
)

# Load environment configuration
Import-Module "$PSScriptRoot\scripts\Load-Env.ps1" -Force
try {
    $config = Get-EnvConfig
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Use command-line parameter or fall back to .env
if (-not $HostName) {
    $HostName = $config['DROPLET_HOST']
}
if (-not $HostName) {
    Write-Host "Error: DROPLET_HOST not configured in .env file" -ForegroundColor Red
    exit 1
}

# Build SSH arguments
$sshArgs = @()
$keyPath = $KeyFile
if (-not $keyPath -and $config['SSH_KEY_PATH']) {
    $keyPath = $config['SSH_KEY_PATH']
}
if ($keyPath -and (Test-Path $keyPath)) {
    $sshArgs += "-i", $keyPath
}
$user = $config['DROPLET_USER']
if (-not $user) { $user = 'root' }
$sshArgs += "$user@$HostName"

# Detect which docker compose command is available on the droplet
$detectCmd = 'if docker compose version &>/dev/null 2>&1; then echo "docker compose"; elif command -v docker-compose &>/dev/null; then echo "docker-compose"; fi'
$composeCmd = ssh @sshArgs $detectCmd 2>$null
if (-not $composeCmd) {
    $composeCmd = "docker-compose"  # fallback
}

if ($Follow) {
    Write-Host "Following logs (Ctrl+C to stop)..." -ForegroundColor Yellow
    ssh @sshArgs "cd /opt/skidjakt && $composeCmd logs -f backend"
}
elseif ($Errors) {
    Write-Host "Showing errors..." -ForegroundColor Red
    ssh @sshArgs "cd /opt/skidjakt && $composeCmd logs backend | grep -iE 'error|exception|fail|warn'"
}
elseif ($Scraping) {
    Write-Host "Showing scraping activity..." -ForegroundColor Green
    ssh @sshArgs "cd /opt/skidjakt && $composeCmd logs backend | grep -iE 'scrap|deal|agency'"
}
else {
    Write-Host "Showing last 50 log lines..." -ForegroundColor Cyan
    ssh @sshArgs "cd /opt/skidjakt && $composeCmd logs --tail=50 backend"
}
