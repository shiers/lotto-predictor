# Start Development Environment with Hot-Reload
# This script starts all services in development mode with automatic code reloading

Write-Host "`n=== Starting Development Environment ===" -ForegroundColor Cyan
Write-Host "This mode enables hot-reload for all services" -ForegroundColor Gray
Write-Host ""

# Stop any running production containers
Write-Host "Stopping production containers..." -ForegroundColor Yellow
docker-compose down 2>$null

# Start development environment
Write-Host "`nStarting development containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Development environment started successfully!" -ForegroundColor Green
    
    Start-Sleep -Seconds 5
    
    Write-Host "`n=== Service Status ===" -ForegroundColor Cyan
    docker-compose ps
    
    Write-Host "`n=== Development Features ===" -ForegroundColor Cyan
    Write-Host "✅ Backend: Hot-reload enabled (dotnet watch)" -ForegroundColor Green
    Write-Host "✅ Frontend: Hot-reload enabled (Vite HMR)" -ForegroundColor Green
    Write-Host "✅ Predictor: Hot-reload enabled (uvicorn --reload)" -ForegroundColor Green
    
    Write-Host "`n=== Service URLs ===" -ForegroundColor Cyan
    Write-Host "Frontend:  http://localhost:3000" -ForegroundColor Gray
    Write-Host "Backend:   http://localhost:5000" -ForegroundColor Gray
    Write-Host "Predictor: http://localhost:8000" -ForegroundColor Gray
    Write-Host "Database:  localhost:5434" -ForegroundColor Gray
    
    Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
    Write-Host "1. Edit your code - changes will auto-reload!" -ForegroundColor Gray
    Write-Host "2. View logs: .\scripts\dev-logs.ps1" -ForegroundColor Gray
    Write-Host "3. Restart service: .\scripts\dev-restart.ps1 [service-name]" -ForegroundColor Gray
    Write-Host "4. Stop all: docker-compose down" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "`n❌ Failed to start development environment" -ForegroundColor Red
    Write-Host "Check the error messages above" -ForegroundColor Yellow
}
