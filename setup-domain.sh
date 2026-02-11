#!/bin/bash
#
# setup-domain.sh - Configure skidjakt.linkasaurus.se with HTTPS
#
# This script:
#   1. Installs nginx and certbot
#   2. Configures nginx reverse proxy
#   3. Obtains Let's Encrypt SSL certificate
#   4. Updates docker-compose to use internal ports
#
# Prerequisites:
#   - DNS A record for skidjakt.linkasaurus.se pointing to this droplet
#   - Port 80 and 443 open in firewall
#
# Usage:
#   bash /opt/skidjakt/setup-domain.sh
#

set -euo pipefail

DOMAIN="skidjakt.linkasaurus.se"
NGINX_CONF="/etc/nginx/sites-available/$DOMAIN"
APP_DIR="/opt/skidjakt"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log()   { echo -e "${GREEN}--- $1${NC}"; }
warn()  { echo -e "${YELLOW}⚠ $1${NC}"; }
error() { echo -e "${RED}✗ $1${NC}"; exit 1; }

echo -e "${YELLOW}"
echo "╔════════════════════════════════════════════════════════╗"
echo "║  Setting up $DOMAIN with HTTPS  ║"
echo "╚════════════════════════════════════════════════════════╝"
echo -e "${NC}"

# -------------------------------------------------------------------
# Step 1: Check prerequisites
# -------------------------------------------------------------------
log "Checking prerequisites"

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   error "This script must be run as root (use sudo)"
fi

# Check DNS
echo -n "Checking DNS for $DOMAIN... "
if host "$DOMAIN" > /dev/null 2>&1; then
    IP=$(host "$DOMAIN" | grep "has address" | head -1 | awk '{print $4}')
    echo -e "${GREEN}✓ Resolves to $IP${NC}"
else
    error "DNS not configured. Create an A record for $DOMAIN first."
fi

# Check if Docker containers are running
if ! docker compose -f "$APP_DIR/docker-compose.yml" ps | grep -q "Up"; then
    warn "Docker containers not running. Deploy the app first."
fi

# -------------------------------------------------------------------
# Step 2: Install nginx and certbot
# -------------------------------------------------------------------
log "Installing nginx and certbot"

apt-get update
apt-get install -y nginx certbot python3-certbot-nginx

systemctl enable nginx
systemctl start nginx

# Ensure Docker and Docker Compose are installed
if ! command -v docker &> /dev/null; then
    log "Installing Docker"
    apt-get install -y docker.io
    systemctl enable docker
    systemctl start docker
fi

if ! docker compose version &> /dev/null && ! command -v docker-compose &> /dev/null; then
    log "Installing Docker Compose"
    if apt-cache show docker-compose-plugin &>/dev/null; then
        apt-get install -y docker-compose-plugin
    else
        COMPOSE_VERSION=$(curl -s https://api.github.com/repos/docker/compose/releases/latest | grep -Po '"tag_name": "\K.*?(?=")')
        curl -SL "https://github.com/docker/compose/releases/download/${COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
        chmod +x /usr/local/bin/docker-compose
        ln -sf /usr/local/bin/docker-compose /usr/bin/docker-compose
    fi
fi

# Detect Docker Compose command
if docker compose version &>/dev/null 2>&1; then
    COMPOSE="docker compose"
elif command -v docker-compose &>/dev/null; then
    COMPOSE="docker-compose"
else
    error "Neither 'docker compose' nor 'docker-compose' is available"
fi

# -------------------------------------------------------------------
# Step 3: Stop Docker containers on port 80 temporarily
# -------------------------------------------------------------------
log "Stopping Docker containers temporarily (for certbot)"

cd "$APP_DIR"
$COMPOSE down

# -------------------------------------------------------------------
# Step 4: Obtain SSL certificate
# -------------------------------------------------------------------
log "Obtaining Let's Encrypt certificate for $DOMAIN"

# Create temporary nginx config for certbot verification
cat > "$NGINX_CONF" <<EOF
server {
    listen 80;
    server_name $DOMAIN;

    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }
}
EOF

ln -sf "$NGINX_CONF" /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx

# Get certificate
certbot --nginx -d "$DOMAIN" --non-interactive --agree-tos --email admin@linkasaurus.se --redirect

# -------------------------------------------------------------------
# Step 5: Configure nginx reverse proxy
# -------------------------------------------------------------------
log "Configuring nginx reverse proxy"

# Copy the full proxy config
cp "$APP_DIR/nginx-proxy.conf" "$NGINX_CONF"

# Test and reload
nginx -t && systemctl reload nginx

# -------------------------------------------------------------------
# Step 6: Update Docker Compose to use internal ports
# -------------------------------------------------------------------
log "Updating Docker Compose to use internal ports"

cd "$APP_DIR"

# Use production compose file
if [[ -f docker-compose.prod.yml ]]; then
    ln -sf docker-compose.prod.yml docker-compose.override.yml
fi

# Start with internal ports only
$COMPOSE up -d --build

# -------------------------------------------------------------------
# Step 7: Verify
# -------------------------------------------------------------------
log "Waiting for services to start"
sleep 10

log "Testing backend health"
if curl -sf http://localhost:5000/api/health > /dev/null; then
    echo -e "${GREEN}✓ Backend is healthy${NC}"
else
    warn "Backend health check failed. Check logs: docker compose logs backend"
fi

log "Testing HTTPS"
if curl -sf "https://$DOMAIN/api/health" > /dev/null; then
    echo -e "${GREEN}✓ HTTPS is working${NC}"
else
    warn "HTTPS check failed. Check nginx logs: sudo tail -f /var/log/nginx/skidjakt.error.log"
fi

# -------------------------------------------------------------------
# Step 8: Set up auto-renewal
# -------------------------------------------------------------------
log "Configuring SSL certificate auto-renewal"

# Certbot installs a systemd timer automatically
systemctl enable certbot.timer
systemctl start certbot.timer

echo ""
echo -e "${GREEN}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  ✓ Setup complete!                                    ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "Your app is now running at: ${YELLOW}https://$DOMAIN${NC}"
echo ""
echo "Useful commands:"
echo "  View nginx logs:   sudo tail -f /var/log/nginx/skidjakt.error.log"
echo "  View app logs:     cd $APP_DIR && docker compose logs -f"
echo "  Reload nginx:      sudo systemctl reload nginx"
echo "  Renew cert:        sudo certbot renew"
echo ""
