# Redis Cache Configuration

Redis has been integrated into the PredictLottoNZ stack to provide distributed caching and improve application performance.

## Overview

Redis is used for:
- **Distributed Caching**: Caching frequently accessed data across services
- **Session Storage**: Managing user sessions and temporary data
- **Performance Optimization**: Reducing database load for repeated queries
- **Frequency Analysis Caching**: Storing computed frequency analysis results
- **Prediction Result Caching**: Caching prediction results to avoid recomputation

## Configuration

### Docker Compose Setup

Redis is automatically configured in the Docker Compose stack:

```yaml
redis:
  image: redis:7-alpine
  container_name: predict-lotto-redis
  ports:
    - "${REDIS_PORT:-6379}:6379"
  volumes:
    - redis_data:/data
  networks:
    - predict-lotto-network
  healthcheck:
    test: ["CMD", "redis-cli", "ping"]
    interval: 10s
    timeout: 5s
    retries: 5
  restart: unless-stopped
  command: redis-server --appendonly yes
```

### Environment Variables

Add these to your `.env` file:

```bash
# Redis Configuration
REDIS_PORT=6379
REDIS_CONNECTION_STRING=redis:6379
```

### Backend Integration

The .NET backend automatically connects to Redis using the connection string:

```csharp
// Configured in Program.cs
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "PredictLottoNZ";
});
```

## Usage

### Starting with Redis

```bash
# Start all services including Redis
docker-compose up -d

# Or in development mode
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### Testing Redis

Use the provided test script:

```powershell
# Test Redis connectivity and basic operations
.\scripts\test-redis.ps1
```

### Verifying Redis Status

```bash
# Check if Redis is running
docker ps | grep redis

# Test Redis connection
docker exec predict-lotto-redis redis-cli ping

# View Redis info
docker exec predict-lotto-redis redis-cli info server
```

## Monitoring

### Health Checks

Redis health is monitored through:
- Docker health checks (ping command)
- Application-level connection monitoring
- Included in the main service verification script

### Logs

View Redis logs:

```bash
# View Redis container logs
docker-compose logs redis

# Follow Redis logs in real-time
docker-compose logs -f redis
```

## Data Persistence

Redis is configured with:
- **AOF (Append Only File)**: Ensures data persistence across restarts
- **Named Volume**: `redis_data` volume for persistent storage
- **Automatic Restart**: Container restarts automatically on failure

## Performance Benefits

With Redis caching, you can expect:
- **Faster API Responses**: Cached data served directly from memory
- **Reduced Database Load**: Fewer queries to PostgreSQL
- **Improved Scalability**: Better handling of concurrent requests
- **Enhanced User Experience**: Faster page loads and data retrieval

## Troubleshooting

### Common Issues

1. **Connection Refused**
   ```bash
   # Check if Redis container is running
   docker ps | grep redis
   
   # Restart Redis if needed
   docker-compose restart redis
   ```

2. **Memory Issues**
   ```bash
   # Check Redis memory usage
   docker exec predict-lotto-redis redis-cli info memory
   ```

3. **Data Loss**
   ```bash
   # Verify AOF is enabled
   docker exec predict-lotto-redis redis-cli config get appendonly
   ```

### Reset Redis Data

```bash
# Stop services and remove Redis volume
docker-compose down
docker volume rm predict-lotto-redis-data

# Restart services (Redis will start fresh)
docker-compose up -d
```

## Development Notes

- Redis runs on port 6379 (configurable via REDIS_PORT)
- Connection string uses service name `redis` in Docker network
- For local development outside Docker, use `localhost:6379`
- Cache keys are prefixed with "PredictLottoNZ" instance name
- TTL (Time To Live) can be configured per cache entry