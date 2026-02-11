#!/bin/bash
#
# deploy.sh - Runs ON the DigitalOcean droplet.
#
# Usage:
#   From Windows:  ssh root@YOUR_DROPLET "bash /opt/skidjakt/deploy.sh"
#   On the droplet: bash /opt/skidjakt/deploy.sh
#
# First-time setup:
#   bash /opt/skidjakt/deploy.sh --setup
#
set -euo pipefail

APP_DIR="/opt/skidjakt"

# Detect which Docker Compose command to use
if docker compose version &>/dev/null 2>&1; then
    COMPOSE="docker compose"
elif command -v docker-compose &>/dev/null; then
    COMPOSE="docker-compose"
else
    echo "ERROR: Neither 'docker compose' nor 'docker-compose' is available"
    exit 1
fi

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

log()  { echo -e "${GREEN}--- $1${NC}"; }
info() { echo -e "${CYAN}$1${NC}"; }

# -------------------------------------------------------------------
# First-time setup
# -------------------------------------------------------------------
if [[ "${1:-}" == "--setup" ]]; then
    echo -e "${YELLOW}=== First-time droplet setup ===${NC}"

    log "Installing Docker"
    apt-get update

    # Install Docker
    apt-get install -y docker.io curl git
    systemctl enable docker
    systemctl start docker

    # Install Docker Compose (v2 via plugin or standalone)
    if apt-cache show docker-compose-plugin &>/dev/null; then
        log "Installing docker-compose-plugin"
        apt-get install -y docker-compose-plugin
    else
        log "Installing docker-compose standalone"
        COMPOSE_VERSION=$(curl -s https://api.github.com/repos/docker/compose/releases/latest | grep -Po '"tag_name": "\K.*?(?=")')
        curl -SL "https://github.com/docker/compose/releases/download/${COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
        chmod +x /usr/local/bin/docker-compose
        ln -sf /usr/local/bin/docker-compose /usr/bin/docker-compose
    fi

    # Verify installation
    log "Verifying Docker installation"
    docker --version
    docker compose version || docker-compose version

    # Install buildx plugin to silence warnings (optional but recommended)
    log "Installing Docker buildx plugin"
    mkdir -p ~/.docker/cli-plugins
    BUILDX_VERSION=$(curl -s https://api.github.com/repos/docker/buildx/releases/latest | grep -Po '"tag_name": "\K.*?(?=")')
    curl -SL "https://github.com/docker/buildx/releases/download/${BUILDX_VERSION}/buildx-$(uname -s | tr '[:upper:]' '[:lower:]')-$(uname -m)" -o ~/.docker/cli-plugins/docker-buildx 2>/dev/null || true
    chmod +x ~/.docker/cli-plugins/docker-buildx 2>/dev/null || true

    log "Creating app directory"
    mkdir -p "$APP_DIR"

    echo -e "${YELLOW}=== Setup complete ===${NC}"
    echo "Next steps:"
    echo "  1. Clone your repo:  git clone <repo-url> $APP_DIR"
    echo "  2. Deploy:           bash $APP_DIR/deploy.sh"
    exit 0
fi

# -------------------------------------------------------------------
# Normal deploy
# -------------------------------------------------------------------
echo -e "${YELLOW}=== Deploying Skidjakt ===${NC}"

cd "$APP_DIR"

# Detect if running behind nginx (use prod compose file with localhost-only ports)
if systemctl is-active --quiet nginx 2>/dev/null && [[ -f docker-compose.prod.yml ]]; then
    COMPOSE_FILE="-f docker-compose.prod.yml"
    log "Detected nginx - using production compose file (localhost-only ports)"
else
    COMPOSE_FILE=""
    log "No nginx detected - using default compose file (exposed ports)"
fi

log "Pulling latest code"
git pull origin main

# Remove stale override files (override merges with base, causing port conflicts)
rm -f docker-compose.override.yml

log "Stopping old containers (images are pre-loaded via deploy.ps1)"
$COMPOSE $COMPOSE_FILE down

log "Starting services"
$COMPOSE $COMPOSE_FILE up -d

log "Cleaning up old images"
docker image prune -f

log "Waiting for backend to start"
sleep 5

log "Health check"
if curl -sf http://localhost:5000/api/health; then
    echo ""
    echo -e "${GREEN}Backend is healthy!${NC}"
else
    echo -e "${YELLOW}Backend not responding yet. Check logs: $COMPOSE $COMPOSE_FILE logs backend${NC}"
fi

echo ""
echo -e "${YELLOW}=== Deploy complete ===${NC}"
echo -e "Logs:   ${CYAN}cd $APP_DIR && $COMPOSE $COMPOSE_FILE logs -f${NC}"
echo -e "Status: ${CYAN}cd $APP_DIR && $COMPOSE $COMPOSE_FILE ps${NC}"
