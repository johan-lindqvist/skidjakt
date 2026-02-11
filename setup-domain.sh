#!/bin/bash
#
# setup-domain.sh - Configure skidjakt.linkasaurus.se with HTTPS
#
# This script:
#   1. Installs nginx and certbot
#   2. Obtains Let's Encrypt SSL certificate
#   3. Configures nginx reverse proxy
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
warn()  { echo -e "${YELLOW}--- $1${NC}"; }
error() { echo -e "${RED}--- $1${NC}"; exit 1; }

echo -e "${YELLOW}"
echo "========================================================"
echo "  Setting up $DOMAIN with HTTPS"
echo "========================================================"
echo -e "${NC}"

# -------------------------------------------------------------------
# Step 1: Check prerequisites
# -------------------------------------------------------------------
log "Checking prerequisites"

if [[ $EUID -ne 0 ]]; then
   error "This script must be run as root (use sudo)"
fi

# Detect Docker Compose command early
if docker compose version &>/dev/null 2>&1; then
    COMPOSE="docker compose"
elif command -v docker-compose &>/dev/null; then
    COMPOSE="docker-compose"
else
    error "Neither 'docker compose' nor 'docker-compose' is available. Install Docker first."
fi
log "Using compose command: $COMPOSE"

# Check DNS
echo -n "Checking DNS for $DOMAIN... "
if command -v host &>/dev/null && host "$DOMAIN" > /dev/null 2>&1; then
    IP=$(host "$DOMAIN" | grep "has address" | head -1 | awk '{print $4}')
    echo -e "${GREEN}Resolves to $IP${NC}"
elif command -v dig &>/dev/null && dig +short "$DOMAIN" | head -1 | grep -q .; then
    IP=$(dig +short "$DOMAIN" | head -1)
    echo -e "${GREEN}Resolves to $IP${NC}"
else
    warn "Could not verify DNS. Make sure an A record for $DOMAIN exists."
fi

# -------------------------------------------------------------------
# Step 2: Stop Docker containers on port 80 temporarily
# -------------------------------------------------------------------
log "Stopping Docker containers temporarily (freeing port 80 for certbot)"

cd "$APP_DIR"
$COMPOSE down || true

# -------------------------------------------------------------------
# Step 3: Install nginx and certbot
# -------------------------------------------------------------------
log "Installing nginx and certbot"

apt-get update
apt-get install -y nginx certbot python3-certbot-nginx

systemctl enable nginx
systemctl start nginx

# -------------------------------------------------------------------
# Step 4: Obtain SSL certificate
# -------------------------------------------------------------------
log "Obtaining Let's Encrypt certificate for $DOMAIN"

# Remove default site if it conflicts
rm -f /etc/nginx/sites-enabled/default

# Create temporary nginx config for certbot verification
cat > "$NGINX_CONF" <<EOF
server {
    listen 80;
    server_name $DOMAIN;

    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    location / {
        return 200 'Setting up...';
        add_header Content-Type text/plain;
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

# Overwrite with the full proxy config (references the certs certbot just created)
cp "$APP_DIR/nginx-proxy.conf" "$NGINX_CONF"

# Test and reload
if nginx -t; then
    systemctl reload nginx
    echo -e "${GREEN}nginx config is valid${NC}"
else
    warn "nginx config test failed, check $NGINX_CONF"
fi

# -------------------------------------------------------------------
# Step 6: Start Docker with internal ports
# -------------------------------------------------------------------
log "Starting Docker containers with internal ports"

cd "$APP_DIR"

# Use production compose file (internal ports only, nginx handles external)
if [[ -f docker-compose.prod.yml ]]; then
    cp docker-compose.prod.yml docker-compose.override.yml
    log "Using docker-compose.prod.yml as override"
fi

$COMPOSE up -d --build

# -------------------------------------------------------------------
# Step 7: Verify
# -------------------------------------------------------------------
log "Waiting for services to start..."
sleep 10

log "Testing backend health"
if curl -sf http://localhost:5000/api/health > /dev/null; then
    echo -e "${GREEN}Backend is healthy${NC}"
else
    warn "Backend health check failed. Check: $COMPOSE logs backend"
fi

log "Testing HTTPS"
if curl -sf "https://$DOMAIN/api/health" > /dev/null; then
    echo -e "${GREEN}HTTPS is working!${NC}"
else
    warn "HTTPS check failed. Check: tail -f /var/log/nginx/skidjakt.error.log"
fi

# -------------------------------------------------------------------
# Step 8: Set up auto-renewal
# -------------------------------------------------------------------
log "Configuring SSL certificate auto-renewal"

if systemctl list-unit-files | grep -q certbot.timer; then
    systemctl enable certbot.timer
    systemctl start certbot.timer
fi

echo ""
echo -e "${GREEN}========================================================"
echo -e "  Setup complete!"
echo -e "========================================================${NC}"
echo ""
echo -e "Your app is now running at: ${YELLOW}https://$DOMAIN${NC}"
echo ""
echo "Useful commands:"
echo "  View nginx logs:   tail -f /var/log/nginx/skidjakt.error.log"
echo "  View app logs:     cd $APP_DIR && $COMPOSE logs -f"
echo "  Reload nginx:      systemctl reload nginx"
echo "  Renew cert:        certbot renew"
echo ""
