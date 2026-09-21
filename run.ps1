$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker is required.'
    }
    docker info --format '{{.ServerVersion}}'
    if ($LASTEXITCODE -ne 0) { throw 'Docker is not responding. Start Docker and retry.' }
    docker compose version
    if ($LASTEXITCODE -ne 0) { throw 'Docker Compose is required.' }
    docker compose -f docker-compose.yml up --build -d --wait --wait-timeout 120
    if ($LASTEXITCODE -ne 0) {
        docker compose -f docker-compose.yml ps
        docker compose -f docker-compose.yml logs --tail 80
        throw 'Application startup failed.'
    }
    Write-Host 'Application started successfully.'
    Write-Host 'Frontend:'
    Write-Host 'http://localhost:8081'
} catch {
    Write-Error $_
    exit 1
} finally {
    Pop-Location
}
