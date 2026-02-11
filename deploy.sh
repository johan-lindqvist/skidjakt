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
COMPOSE="docker compose"

# Use production compose file if it exists (for reverse proxy setup)
if [[ -f "$APP_DIR/docker-compose.override.yml" ]]; then
    COMPOSE="docker compose"  # Will automatically pick up override file
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

log "Pulling latest code"
git pull origin main

log "Building Docker images"
$COMPOSE build

log "Stopping old containers"
$COMPOSE down

log "Starting services"
$COMPOSE up -d

log "Cleaning up old images"
docker image prune -f

log "Waiting for backend to start"
sleep 5

log "Health check"
if curl -sf http://localhost:5000/api/health; then
    echo ""
    echo -e "${GREEN}Backend is healthy!${NC}"
else
    echo -e "${YELLOW}Backend not responding yet. Check logs: docker compose logs backend${NC}"
fi

echo ""
echo -e "${YELLOW}=== Deploy complete ===${NC}"
echo -e "Logs:   ${CYAN}cd $APP_DIR && docker compose logs -f${NC}"
echo -e "Status: ${CYAN}cd $APP_DIR && docker compose ps${NC}"
