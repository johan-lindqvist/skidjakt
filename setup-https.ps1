<#
.SYNOPSIS
    Set up HTTPS for skidjakt.linkasaurus.se on the droplet.

.DESCRIPTION
    This script runs the domain setup script on the droplet to:
    - Install nginx and certbot
    - Obtain Let's Encrypt SSL certificate
    - Configure reverse proxy
    - Restart services with internal ports

.PARAMETER HostName
    Droplet IP or hostname (optional, reads from .env if not specified).

.PARAMETER KeyFile
    SSH private key path (optional, reads from .env or uses ssh-agent).

.EXAMPLE
    .\setup-https.ps1
    .\setup-https.ps1 -HostName skidjakt.linkasaurus.se
    .\setup-https.ps1 -KeyFile "C:\path\to\key"
#>

param(
    [string]$HostName,
    [string]$KeyFile
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

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  Setting up HTTPS for skidjakt.linkasaurus.se         ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# Step 1: Check DNS
Write-Host "Step 1: Checking DNS configuration..." -ForegroundColor Cyan
try {
    $dns = Resolve-DnsName skidjakt.linkasaurus.se -ErrorAction Stop
    $resolvedIp = $dns.IPAddress
    Write-Host "✓ DNS resolves to: $resolvedIp" -ForegroundColor Green

    if ($resolvedIp -ne $HostName) {
        Write-Host "⚠ WARNING: DNS points to $resolvedIp but you're deploying to $HostName" -ForegroundColor Yellow
        $continue = Read-Host "Continue anyway? (y/n)"
        if ($continue -ne 'y') {
            Write-Host "Aborted." -ForegroundColor Red
            exit 1
        }
    }
} catch {
    Write-Host "✗ DNS lookup failed!" -ForegroundColor Red
    Write-Host "  Please create an A record for skidjakt.linkasaurus.se pointing to $HostName" -ForegroundColor Yellow
    Write-Host "  in your DigitalOcean DNS settings before continuing." -ForegroundColor Yellow
    $continue = Read-Host "Continue anyway? (y/n)"
    if ($continue -ne 'y') {
        Write-Host "Aborted." -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

# Step 2: Check if setup script exists on droplet
Write-Host "Step 2: Checking if setup script exists on droplet..." -ForegroundColor Cyan
$scriptExists = ssh @sshArgs "test -f /opt/skidjakt/setup-domain.sh && echo 'yes' || echo 'no'" 2>$null
if ($scriptExists -match "yes") {
    Write-Host "✓ Setup script found" -ForegroundColor Green
} else {
    Write-Host "✗ Setup script not found. Deploying code first..." -ForegroundColor Yellow
    ssh @sshArgs "cd /opt/skidjakt && git pull origin main"
    Write-Host "✓ Code updated" -ForegroundColor Green
}
Write-Host ""

# Step 3: Run the setup script
Write-Host "Step 3: Running HTTPS setup on droplet..." -ForegroundColor Cyan
Write-Host "This will:" -ForegroundColor White
Write-Host "  - Install nginx and certbot" -ForegroundColor White
Write-Host "  - Obtain Let's Encrypt SSL certificate" -ForegroundColor White
Write-Host "  - Configure reverse proxy" -ForegroundColor White
Write-Host "  - Restart Docker containers with internal ports" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to cancel, or Enter to continue..." -ForegroundColor Yellow
$null = Read-Host

ssh @sshArgs "cd /opt/skidjakt && bash setup-domain.sh"

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✓ HTTPS setup complete!                              ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "Your site should now be available at:" -ForegroundColor White
Write-Host "  https://skidjakt.linkasaurus.se" -ForegroundColor Cyan
Write-Host ""
Write-Host "Testing HTTPS connection..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

try {
    $response = Invoke-WebRequest -Uri "https://skidjakt.linkasaurus.se/api/health" -ErrorAction Stop
    Write-Host "✓ HTTPS is working! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "⚠ Could not connect via HTTPS yet: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "  It may take a minute for DNS/services to propagate." -ForegroundColor White
    Write-Host "  Try again in a few moments or run: .\diagnose-https.ps1" -ForegroundColor White
}
Write-Host ""
