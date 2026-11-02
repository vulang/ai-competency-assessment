#!/usr/bin/env bash
set -euo pipefail
repo_root=$(cd "$(dirname "$0")/.." && pwd)
if [[ ! -f "$repo_root/infra/.env" ]]; then
  cp "$repo_root/infra/.env.example" "$repo_root/infra/.env"
fi
docker compose --project-directory "$repo_root/infra" -f "$repo_root/infra/docker-compose.yml" up --build -d --wait --wait-timeout 180
echo 'UI: http://localhost:4200  API: http://localhost:5000  ML: http://localhost:8001'
