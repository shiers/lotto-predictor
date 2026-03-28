# Docker Rebuild Script - Fixes hanging builds
# This script cleans up Docker resources and rebuilds with proper settings

Write-Host "Stopping all containers..." -ForegroundColor Yellow
docker-compose down

Write-Host "`nRemoving predictor image..." -ForegroundColor Yellow
docker rmi predict-lotto-predictor -f 2>$null

Write-Host "`nPruning Docker build cache..." -ForegroundColor Yellow
docker builder prune -f

Write-Host "`nRebuilding with no cache..." -ForegroundColor Yellow
docker-compose build --no-cache predictor

Write-Host "`nStarting services..." -ForegroundColor Green
docker-compose up -d

Write-Host "`nChecking service status..." -ForegroundColor Cyan
Start-Sleep -Seconds 5
docker-compose ps

Write-Host "`nDocker rebuild complete!" -ForegroundColor Green
Write-Host "Run 'docker-compose logs -f predictor' to monitor the predictor service" -ForegroundColor Cyan
