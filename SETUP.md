# Skidjakt Setup Guide

## Initial Setup

### 1. Clone the Repository

```bash
git clone <your-repo-url> skidjakt
cd skidjakt
```

### 2. Configure Environment

Copy the example environment file and fill in your details:

```powershell
# Copy the template
Copy-Item .env.example .env

# Edit .env with your favorite editor
notepad .env
```

Fill in your configuration:

```env
# Droplet Configuration
DROPLET_HOST=your-droplet-ip-or-skidjakt.linkasaurus.se
DROPLET_USER=root

# SSH Configuration (optional - leave empty to use ssh-agent)
SSH_KEY_PATH=C:\path\to\your\ssh\key

# Git Repository (for deployment)
GIT_REPO_URL=https://github.com/yourusername/skidjakt.git
```

### 3. First-Time Droplet Setup

Run the deployment script with the `-Setup` flag to install Docker and set up the droplet:

```powershell
.\deploy.ps1 -Setup
```

This will:
- Install Docker and Docker Compose on the droplet
- Clone the repository to `/opt/skidjakt`
- Build and start the containers

### 4. Set Up HTTPS (Optional but Recommended)

If you want to run the site with HTTPS on a domain:

1. Configure DNS in DigitalOcean:
   - Create an A record for `skidjakt.linkasaurus.se` pointing to your droplet IP

2. Run the HTTPS setup script:
   ```powershell
   .\setup-https.ps1
   ```

This will:
- Install nginx and certbot
- Obtain a Let's Encrypt SSL certificate
- Configure reverse proxy
- Set up automatic certificate renewal

## Daily Usage

### Deploy Changes

After making code changes, deploy to the droplet:

```powershell
.\deploy.ps1
```

### Check Logs

View application logs:

```powershell
# Show last 50 lines
.\check-logs.ps1

# Follow logs in real-time
.\check-logs.ps1 -Follow

# Show only errors
.\check-logs.ps1 -Errors

# Show only scraping activity
.\check-logs.ps1 -Scraping
```

### Diagnose HTTPS Issues

If HTTPS is not working:

```powershell
.\diagnose-https.ps1
```

### Run Locally

For local development:

```powershell
# Run backend and frontend together
.\dev.ps1

# Run only backend
.\dev.ps1 -BackendOnly

# Run only frontend
.\dev.ps1 -FrontendOnly

# Run in Docker locally
.\dev.ps1 -Docker
```

## File Structure

- `.env` - Your local configuration (not in git)
- `.env.example` - Template for .env file (in git)
- `deploy.ps1` - Deploy to droplet
- `setup-https.ps1` - Set up HTTPS with Let's Encrypt
- `check-logs.ps1` - View application logs
- `diagnose-https.ps1` - Diagnose HTTPS issues
- `dev.ps1` - Run locally for development

## Security Notes

- **Never commit `.env`** - It's in `.gitignore` for a reason
- The `.env` file contains sensitive information (IP addresses, SSH keys)
- Keep your SSH keys secure
- Use strong passwords for your droplet

## Troubleshooting

### "DROPLET_HOST not configured" Error

You forgot to create the `.env` file. Copy `.env.example` to `.env` and fill it in.

### SSH Permission Denied

Either:
- Set `SSH_KEY_PATH` in your `.env` file to point to your SSH private key
- Set up ssh-agent with your key loaded
- Use password authentication (you'll be prompted)

### Docker Compose Command Not Found

The scripts auto-detect whether to use `docker compose` or `docker-compose`. If neither is found, run `.\deploy.ps1 -Setup` to install Docker Compose.
