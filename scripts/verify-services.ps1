# Verify All Services Are Running
# Quick health check for all Docker services

Write-Host "`n=== Docker Services Health Check ===" -ForegroundColor Cyan
Write-Host ""

# Check if services are running
Write-Host "Checking service status..." -ForegroundColor Yellow
docker-compose ps

Write-Host "`n=== Testing Service Endpoints ===" -ForegroundColor Cyan

# Test Predictor
Write-Host "`n1. Predictor Service (port 8000):" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8000/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "   ✅ Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "   Response: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Backend
Write-Host "`n2. Backend API (port 5001):" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5001/api/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "   ✅ Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "   Response: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   ⚠️  Backend may still be starting: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   Check logs with: docker-compose logs backend" -ForegroundColor Cyan
}

# Test Frontend
Write-Host "`n3. Frontend (port 3000):" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000" -UseBasicParsing -TimeoutSec 5
    Write-Host "   ✅ Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Database
Write-Host "`n4. Database (port 5434):" -ForegroundColor Yellow
try {
    $result = docker exec predict-lotto-postgres pg_isready -U postgres 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Database is ready" -ForegroundColor Green
        
        # Get row count
        $count = docker exec predict-lotto-postgres psql -U postgres -d predict_lotto_nz -t -c 'SELECT COUNT(*) FROM "LottoDraws";' 2>&1
        if ($count -match '\d+') {
            Write-Host "   📊 Lotto Draws: $($count.Trim())" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ❌ Database not ready" -ForegroundColor Red
    }
} catch {
    Write-Host "   ❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Redis
Write-Host "`n5. Redis Cache (port 6379):" -ForegroundColor Yellow
try {
    $result = docker exec predict-lotto-redis redis-cli ping 2>&1
    if ($result -eq "PONG") {
        Write-Host "   ✅ Redis is responding" -ForegroundColor Green
        
        # Get Redis info
        $info = docker exec predict-lotto-redis redis-cli info server | Select-String "redis_version"
        if ($info) {
            Write-Host "   📊 $($info.ToString().Trim())" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ❌ Redis not responding" -ForegroundColor Red
    }
} catch {
    Write-Host "   ❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Database Connection String:" -ForegroundColor Yellow
Write-Host "Host=localhost;Port=5434;Database=predict_lotto_nz;Username=postgres;Password=password" -ForegroundColor Gray

Write-Host "`nService URLs:" -ForegroundColor Yellow
Write-Host "  Frontend:  http://localhost:3000" -ForegroundColor Gray
Write-Host "  Backend:   http://localhost:5001" -ForegroundColor Gray
Write-Host "  Predictor: http://localhost:8000" -ForegroundColor Gray
Write-Host "  Database:  localhost:5434" -ForegroundColor Gray
Write-Host "  Redis:     localhost:6379" -ForegroundColor Gray

Write-Host "`nFor detailed logs, use:" -ForegroundColor Cyan
Write-Host "  docker-compose logs -f [service-name]" -ForegroundColor Gray
Write-Host ""
