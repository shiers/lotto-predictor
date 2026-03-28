# Restart Development Service
# Restarts a specific service and shows its logs

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('backend', 'frontend', 'predictor', 'postgres')]
    [string]$service
)

Write-Host "`n=== Restarting $service ===" -ForegroundColor Cyan

docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart $service

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ $service restarted successfully" -ForegroundColor Green
    Write-Host "`nShowing logs (Ctrl+C to exit)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 2
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f --tail=50 $service
} else {
    Write-Host "❌ Failed to restart $service" -ForegroundColor Red
}
