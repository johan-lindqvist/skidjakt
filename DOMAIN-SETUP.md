# Domain Setup Guide - skidjakt.linkasaurus.se

This guide shows how to set up `skidjakt.linkasaurus.se` with HTTPS on your DigitalOcean droplet.

## Overview

The setup uses:
- **nginx** as a reverse proxy (handles HTTPS, routes traffic)
- **Let's Encrypt** for free SSL/TLS certificates
- **Docker containers** running on internal ports only (not exposed to internet)

```
Internet → nginx (port 443) → Docker containers (localhost:5000, localhost:8080)
```

## Prerequisites

1. **DNS A record**: Create an A record for `skidjakt.linkasaurus.se` pointing to your droplet's IP
   - Go to your DNS provider (DigitalOcean, Cloudflare, etc.)
   - Add: `skidjakt IN A YOUR_DROPLET_IP`
   - Wait 5-10 minutes for propagation

2. **Firewall**: Ensure ports 80 and 443 are open
   ```bash
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw enable
   ```

3. **App deployed**: The app should already be running via the regular deploy process
   ```powershell
   .\deploy.ps1 -Host YOUR_DROPLET_IP -Setup -RepoUrl <your-repo>
   ```

## Setup Steps

### Option A: Automated (recommended)

1. **Push the new files to your repo:**
   ```powershell
   git add .
   git commit -m "Add domain setup"
   git push origin main
   ```

2. **SSH into your droplet and run the setup script:**
   ```bash
   ssh root@YOUR_DROPLET_IP
   cd /opt/skidjakt
   git pull origin main
   sudo bash setup-domain.sh
   ```

   This script will:
   - Install nginx and certbot
   - Obtain an SSL certificate from Let's Encrypt
   - Configure nginx as a reverse proxy
   - Update Docker to use internal ports
   - Set up automatic certificate renewal

3. **Done!** Visit `https://skidjakt.linkasaurus.se`

### Option B: Manual

If you prefer to do it step-by-step:

#### 1. Install nginx and certbot

```bash
ssh root@YOUR_DROPLET_IP

apt-get update
apt-get install -y nginx certbot python3-certbot-nginx
```

#### 2. Stop Docker containers temporarily

```bash
cd /opt/skidjakt
docker compose down
```

#### 3. Get SSL certificate

```bash
certbot --nginx -d skidjakt.linkasaurus.se \
  --non-interactive \
  --agree-tos \
  --email admin@linkasaurus.se \
  --redirect
```

#### 4. Configure nginx reverse proxy

```bash
cd /opt/skidjakt
cp nginx-proxy.conf /etc/nginx/sites-available/skidjakt.linkasaurus.se
ln -s /etc/nginx/sites-available/skidjakt.linkasaurus.se /etc/nginx/sites-enabled/
nginx -t
systemctl reload nginx
```

#### 5. Update Docker to use internal ports

```bash
cd /opt/skidjakt
ln -s docker-compose.prod.yml docker-compose.override.yml
docker compose up -d --build
```

#### 6. Verify

```bash
# Check backend
curl http://localhost:5000/api/health

# Check HTTPS
curl https://skidjakt.linkasaurus.se/api/health
```

## Deployment After Domain Setup

Once the domain is set up, your normal deploy process still works:

```powershell
# From Windows
.\deploy.ps1 -Host YOUR_DROPLET_IP
```

The `deploy.sh` script automatically detects `docker-compose.override.yml` and uses it.

## Troubleshooting

### Certificate not working

```bash
# Check certbot status
sudo certbot certificates

# Try renewing manually
sudo certbot renew --dry-run
```

### Backend not responding

```bash
# Check if containers are running
docker compose ps

# Check logs
docker compose logs backend

# Check if backend is listening
curl http://localhost:5000/api/health
```

### nginx errors

```bash
# Check nginx config
sudo nginx -t

# View error logs
sudo tail -f /var/log/nginx/skidjakt.error.log

# Reload nginx
sudo systemctl reload nginx
```

### DNS not resolving

```bash
# Check DNS from the droplet
host skidjakt.linkasaurus.se

# Check from your machine
nslookup skidjakt.linkasaurus.se
```

## SSL Certificate Renewal

Certbot automatically renews certificates. To check:

```bash
# View renewal timer status
systemctl status certbot.timer

# Test renewal
sudo certbot renew --dry-run
```

## Maintenance

### View logs

```bash
# nginx access log
sudo tail -f /var/log/nginx/skidjakt.access.log

# nginx error log
sudo tail -f /var/log/nginx/skidjakt.error.log

# App logs
cd /opt/skidjakt && docker compose logs -f
```

### Restart services

```bash
# Restart nginx
sudo systemctl restart nginx

# Restart app
cd /opt/skidjakt && docker compose restart
```

### Backup SSL certificate

```bash
sudo tar -czf letsencrypt-backup.tar.gz /etc/letsencrypt/
```

## Architecture After Setup

```
┌─────────────────────────────────────────────────────────────┐
│ Internet                                                     │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            │ HTTPS (port 443)
                            ▼
                ┌───────────────────────┐
                │  nginx reverse proxy  │
                │  - SSL termination    │
                │  - Routing            │
                └───────────┬───────────┘
                            │
            ┌───────────────┴────────────────┐
            │                                │
            │ localhost:8080                 │ localhost:5000
            ▼                                ▼
┌──────────────────────┐          ┌──────────────────────┐
│  Docker: frontend    │          │  Docker: backend     │
│  - nginx             │          │  - .NET 10           │
│  - Static files      │◄─────────│  - API endpoints     │
│  - SPA routing       │  proxies │  - SQLite DB         │
└──────────────────────┘   /api/* └──────────────────────┘
```

## Ports Summary

| Service | External | Internal | Access |
|---------|----------|----------|--------|
| nginx | 80 (HTTP) | - | Redirects to 443 |
| nginx | 443 (HTTPS) | - | Public access |
| frontend | - | 127.0.0.1:8080 | Via nginx only |
| backend | - | 127.0.0.1:5000 | Via nginx only |
