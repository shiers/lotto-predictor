# Rebuild Development Service
# Use this when you add new dependencies (npm packages, NuGet packages, pip packages)

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('backend', 'frontend', 'predictor', 'all')]
    [string]$service
)

Write-Host "`n=== Rebuilding Development Service ===" -ForegroundColor Cyan

if ($service -eq "all") {
    Write-Host "Rebuilding all services..." -ForegroundColor Yellow
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml build
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ All services rebuilt successfully" -ForegroundColor Green
        Write-Host "Restarting services..." -ForegroundColor Yellow
        docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
        
        Write-Host "`nService status:" -ForegroundColor Cyan
        docker-compose ps
    }
} else {
    Write-Host "Rebuilding $service..." -ForegroundColor Yellow
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml build $service
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ $service rebuilt successfully" -ForegroundColor Green
        Write-Host "Restarting $service..." -ForegroundColor Yellow
        docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d $service
        
        Write-Host "`nShowing logs (Ctrl+C to exit)..." -ForegroundColor Cyan
        Start-Sleep -Seconds 2
        docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f --tail=50 $service
    } else {
        Write-Host "`n❌ Failed to rebuild $service" -ForegroundColor Red
    }
}
