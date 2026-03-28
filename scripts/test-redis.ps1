#!/usr/bin/env pwsh

# Test Redis connectivity in the Docker Compose stack

Write-Host "Testing Redis connectivity..." -ForegroundColor Green

# Check if Redis container is running
$redisContainer = docker ps --filter "name=predict-lotto-redis" --format "table {{.Names}}\t{{.Status}}"
if ($redisContainer -match "predict-lotto-redis") {
    Write-Host "✓ Redis container is running" -ForegroundColor Green
    Write-Host $redisContainer
} else {
    Write-Host "✗ Redis container is not running" -ForegroundColor Red
    exit 1
}

# Test Redis connection
Write-Host "`nTesting Redis connection..." -ForegroundColor Yellow
$pingResult = docker exec predict-lotto-redis redis-cli ping
if ($pingResult -eq "PONG") {
    Write-Host "✓ Redis is responding to ping" -ForegroundColor Green
} else {
    Write-Host "✗ Redis is not responding" -ForegroundColor Red
    exit 1
}

# Test basic Redis operations
Write-Host "`nTesting basic Redis operations..." -ForegroundColor Yellow
docker exec predict-lotto-redis redis-cli set test-key "Hello Redis"
$getValue = docker exec predict-lotto-redis redis-cli get test-key
if ($getValue -eq "Hello Redis") {
    Write-Host "✓ Redis SET/GET operations working" -ForegroundColor Green
} else {
    Write-Host "✗ Redis SET/GET operations failed" -ForegroundColor Red
    exit 1
}

# Clean up test key
docker exec predict-lotto-redis redis-cli del test-key

# Show Redis info
Write-Host "`nRedis Information:" -ForegroundColor Yellow
docker exec predict-lotto-redis redis-cli info server | Select-String "redis_version", "uptime_in_seconds", "connected_clients"

Write-Host "`n✓ All Redis tests passed!" -ForegroundColor Green