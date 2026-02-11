# Load-Env.ps1
# Helper function to load environment variables from .env file

function Get-EnvConfig {
    param(
        [string]$EnvFile = ".env"
    )

    $rootDir = Split-Path -Parent $PSScriptRoot
    $envPath = Join-Path $rootDir $EnvFile

    if (-not (Test-Path $envPath)) {
        Write-Host "Error: $EnvFile file not found at $envPath" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please create a .env file from the template:" -ForegroundColor Yellow
        Write-Host "  1. Copy .env.example to .env" -ForegroundColor White
        Write-Host "  2. Edit .env and fill in your droplet details" -ForegroundColor White
        Write-Host ""
        throw "$EnvFile file not found"
    }

    $config = @{}
    Get-Content $envPath | ForEach-Object {
        $line = $_.Trim()
        # Skip comments and empty lines
        if ($line -and -not $line.StartsWith('#')) {
            if ($line -match '^([^=]+)=(.*)$') {
                $key = $matches[1].Trim()
                $value = $matches[2].Trim()
                $config[$key] = $value
            }
        }
    }

    return $config
}

function Get-SshArgs {
    param(
        [hashtable]$Config,
        [string]$KeyFileOverride
    )

    $sshArgs = @()

    # Use override key file if provided, otherwise use config
    $keyFile = $KeyFileOverride
    if (-not $keyFile -and $Config['SSH_KEY_PATH']) {
        $keyFile = $Config['SSH_KEY_PATH']
    }

    if ($keyFile -and (Test-Path $keyFile)) {
        $sshArgs += "-i", $keyFile
    }

    $user = $Config['DROPLET_USER']
    if (-not $user) { $user = 'root' }

    $host = $Config['DROPLET_HOST']
    if (-not $host) {
        throw "DROPLET_HOST not configured in .env file"
    }

    $sshArgs += "$user@$host"

    return $sshArgs
}

Export-ModuleMember -Function Get-EnvConfig, Get-SshArgs
