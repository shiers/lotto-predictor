# View Development Logs
# Shows logs from all services or a specific service

param(
    [string]$service = "",
    [int]$lines = 100
)

Write-Host "`n=== Development Logs ===" -ForegroundColor Cyan

if ($service -eq "") {
    Write-Host "Showing logs for all services (Ctrl+C to exit)" -ForegroundColor Yellow
    Write-Host ""
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f --tail=$lines
} else {
    Write-Host "Showing logs for: $service (Ctrl+C to exit)" -ForegroundColor Yellow
    Write-Host ""
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f --tail=$lines $service
}
