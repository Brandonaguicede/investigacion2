#!/bin/sh
set -eu
cd "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
command -v docker >/dev/null 2>&1 || { echo 'Docker is required.' >&2; exit 1; }
docker info --format '{{.ServerVersion}}' || { echo 'Docker is not responding. Start Docker and retry.' >&2; exit 1; }
docker compose version || { echo 'Docker Compose is required.' >&2; exit 1; }
if ! docker compose -f docker-compose.yml up --build -d --wait --wait-timeout 120; then
    docker compose -f docker-compose.yml ps
    docker compose -f docker-compose.yml logs --tail 80
    echo 'Application startup failed.' >&2
    exit 1
fi
printf 'Application started successfully.\nFrontend:\nhttp://localhost:8081\n'
