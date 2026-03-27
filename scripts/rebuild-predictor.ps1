# Rebuild Predictor Service Only
# Use this to rebuild just the predictor service without affecting database

Write-Host "Stopping predictor service..." -ForegroundColor Yellow
docker-compose stop predictor

Write-Host "`nRemoving predictor container and image..." -ForegroundColor Yellow
docker-compose rm -f predictor
docker rmi predict-lotto-predictor -f 2>$null

Write-Host "`nPruning build cache..." -ForegroundColor Yellow
docker builder prune -f

Write-Host "`nRebuilding predictor with no cache..." -ForegroundColor Cyan
Write-Host "This may take 5-10 minutes depending on network speed..." -ForegroundColor Gray
docker-compose build --no-cache --progress=plain predictor

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild successful! Starting predictor..." -ForegroundColor Green
    docker-compose up -d predictor
    
    Write-Host "`nWaiting for service to start..." -ForegroundColor Cyan
    Start-Sleep -Seconds 10
    
    Write-Host "`nService status:" -ForegroundColor Green
    docker-compose ps predictor
    
    Write-Host "`nTesting health endpoint..." -ForegroundColor Cyan
    Start-Sleep -Seconds 5
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8000/health" -UseBasicParsing -TimeoutSec 5
        Write-Host "Health check: OK" -ForegroundColor Green
        Write-Host $response.Content
    } catch {
        Write-Host "Health check failed - service may still be starting" -ForegroundColor Yellow
        Write-Host "Check logs with: docker-compose logs -f predictor" -ForegroundColor Cyan
    }
} else {
    Write-Host "`nBuild failed! Check the output above for errors." -ForegroundColor Red
    Write-Host "Try using requirements.minimal.txt if the issue persists." -ForegroundColor Yellow
}
