# Start Database Only - Quick access to database without full stack
# Use this when you need database access while troubleshooting other services

Write-Host "Starting PostgreSQL database only..." -ForegroundColor Yellow

docker-compose up -d postgres

Write-Host "`nWaiting for database to be ready..." -ForegroundColor Cyan
Start-Sleep -Seconds 5

Write-Host "`nDatabase status:" -ForegroundColor Green
docker-compose ps postgres

Write-Host "`nDatabase connection details:" -ForegroundColor Cyan
Write-Host "Host: localhost"
Write-Host "Port: 5434"
Write-Host "Database: predict_lotto_nz"
Write-Host "User: postgres"
Write-Host "Password: password"

Write-Host "`nConnection string:" -ForegroundColor Yellow
Write-Host "Host=localhost;Port=5434;Database=predict_lotto_nz;Username=postgres;Password=password"

Write-Host "`nTo connect via psql:" -ForegroundColor Cyan
Write-Host "docker exec -it predict-lotto-postgres psql -U postgres -d predict_lotto_nz"
