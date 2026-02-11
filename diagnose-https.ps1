<#
.SYNOPSIS
    Diagnose HTTPS issues on the droplet.

.PARAMETER HostName
    Droplet IP or hostname (optional, reads from .env if not specified).

.PARAMETER KeyFile
    SSH private key path (optional, reads from .env or uses ssh-agent).

.EXAMPLE
    .\diagnose-https.ps1
    .\diagnose-https.ps1 -HostName skidjakt.linkasaurus.se
    .\diagnose-https.ps1 -KeyFile "C:\path\to\key"
#>

param(
    [string]$HostName,
    [string]$KeyFile
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

Write-Host "=== HTTPS Diagnostics ===" -ForegroundColor Cyan
Write-Host ""

# Function to run SSH command and display result
function Test-Service {
    param($Name, $Command)
    Write-Host "Checking $Name..." -ForegroundColor Yellow
    $result = ssh @sshArgs $Command 2>&1
    Write-Host $result
    Write-Host ""
}

# 1. Check if nginx is installed and running
Test-Service "nginx status" "systemctl is-active nginx 2>/dev/null || echo 'nginx not running'"

# 2. Check if SSL certificate exists
Test-Service "SSL certificate" "ls -lh /etc/letsencrypt/live/skidjakt.linkasaurus.se/ 2>/dev/null || echo 'SSL certificate not found'"

# 3. Check nginx configuration
Test-Service "nginx config" "nginx -t 2>&1 || echo 'nginx config error'"

# 4. Check if nginx sites are enabled
Test-Service "nginx sites-enabled" "ls -lh /etc/nginx/sites-enabled/ 2>/dev/null"

# 5. Check which docker-compose file is active
Test-Service "docker-compose override" "ls -lh /opt/skidjakt/docker-compose.override.yml 2>/dev/null || echo 'No override file'"

# 6. Check Docker container ports
Test-Service "docker ports" "cd /opt/skidjakt && docker compose ps"

# 7. Check if ports 80 and 443 are listening
Test-Service "listening ports" "ss -tlnp | grep ':80\|:443' || echo 'Ports 80/443 not listening'"

# 8. Check DNS resolution
Write-Host "Checking DNS from your machine..." -ForegroundColor Yellow
try {
    $dns = Resolve-DnsName skidjakt.linkasaurus.se -ErrorAction Stop
    Write-Host "DNS resolves to: $($dns.IPAddress)" -ForegroundColor Green
} catch {
    Write-Host "DNS lookup failed: $_" -ForegroundColor Red
}
Write-Host ""

# 9. Test HTTP connection
Write-Host "Testing HTTP connection..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://skidjakt.linkasaurus.se" -MaximumRedirection 0 -ErrorAction SilentlyContinue
    Write-Host "HTTP Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "HTTP Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 10. Test HTTPS connection
Write-Host "Testing HTTPS connection..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://skidjakt.linkasaurus.se" -ErrorAction Stop
    Write-Host "HTTPS Status: $($response.StatusCode) - Working!" -ForegroundColor Green
} catch {
    Write-Host "HTTPS Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 11. Check nginx error logs
Write-Host "Recent nginx errors:" -ForegroundColor Yellow
ssh @sshArgs "tail -20 /var/log/nginx/skidjakt.error.log 2>/dev/null || echo 'No nginx error log found'"
Write-Host ""

Write-Host "=== Diagnosis Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If HTTPS is not working, you may need to:" -ForegroundColor Yellow
Write-Host "  1. Run the setup script: ssh root@$HostName 'cd /opt/skidjakt && bash setup-domain.sh'" -ForegroundColor White
Write-Host "  2. Check DNS is pointing to droplet IP: $HostName" -ForegroundColor White
Write-Host "  3. Ensure ports 80/443 are open in DigitalOcean firewall" -ForegroundColor White
Write-Host ""
