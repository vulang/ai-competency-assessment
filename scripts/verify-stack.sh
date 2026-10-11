#!/usr/bin/env bash
set -euo pipefail
repo_root=$(cd "$(dirname "$0")/.." && pwd)
cd "$repo_root"
validation_dir=$(mktemp -d)
project_name="ai-cas-phase0-check-$$"
cat > "$validation_dir/override.yml" <<'YAML'
services:
  postgres:
    ports: !override ["127.0.0.1::5432"]
  api:
    build:
      args:
        SDK_IMAGE: ${VALIDATION_SDK_IMAGE:-mcr.microsoft.com/dotnet/sdk:8.0}
    ports: !override ["127.0.0.1::8080"]
  frontend:
    ports: !override ["127.0.0.1::80"]
  ml-service:
    ports: !override ["127.0.0.1::8001"]
  question-generator:
    ports: !override ["127.0.0.1::8000"]
  mongo:
    ports: !override []
  rabbitmq:
    ports: !override []
  minio:
    ports: !override []
  qdrant:
    ports: !override []
YAML
if [[ ${VALIDATION_TMPFS:-0} == 1 ]]; then
  cat > "$validation_dir/memory.yml" <<'YAML'
services:
  postgres:
    volumes: !reset []
    tmpfs: ["/var/lib/postgresql/data:size=256m"]
  rabbitmq:
    tmpfs: ["/var/lib/rabbitmq:size=512m,mode=1777"]
  mongo:
    volumes: !reset []
    tmpfs: ["/data/db:size=512m"]
  minio:
    volumes: !reset []
    tmpfs: ["/data:size=2g"]
  qdrant:
    volumes: !reset []
    tmpfs: ["/qdrant/storage:size=128m"]
  api:
    tmpfs: ["/home/app:size=64m,mode=1777", "/tmp:size=64m,mode=1777"]
YAML
fi
compose=(docker compose --project-name "$project_name" --project-directory "$repo_root/infra" -f "$repo_root/infra/docker-compose.yml" -f "$validation_dir/override.yml")
if [[ ${VALIDATION_TMPFS:-0} == 1 ]]; then compose+=(-f "$validation_dir/memory.yml"); fi
cleanup() {
  "${compose[@]}" down --volumes --remove-orphans >/dev/null 2>&1 || true
  rm -rf "$validation_dir"
}
trap cleanup EXIT
# Force synthetic local settings; provider calls are not part of this smoke test.
export POSTGRES_DB=ai_competency POSTGRES_USER=postgres POSTGRES_PASSWORD=postgres
export AUTHENTICATION_ISSUER=http://localhost:5000/ OPENAI_API_KEY=
build_option=--build
if [[ ${VALIDATION_REUSE_IMAGES:-0} == 1 ]]; then build_option=--no-build; fi
if ! "${compose[@]}" up "$build_option" --detach --wait --wait-timeout 240; then
  "${compose[@]}" logs --no-color --tail 60
  exit 1
fi
api_address=$("${compose[@]}" port api 8080)
ui_address=$("${compose[@]}" port frontend 80)
ml_address=$("${compose[@]}" port ml-service 8001)
generator_address=$("${compose[@]}" port question-generator 8000)
python3 scripts/smoke-stack.py --api "http://$api_address" --ui "http://$ui_address" --ml "http://$ml_address" --generator "http://$generator_address"
