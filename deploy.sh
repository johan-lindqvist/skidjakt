#!/bin/bash
set -e
cd /opt/skidjakt
git pull origin main
docker compose build
docker compose up -d
docker image prune -f
echo "Deploy complete!"
