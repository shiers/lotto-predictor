# Docker Build Troubleshooting Guide

## Issue: Docker Build Hanging on Package Downloads

### Symptoms
- Build hangs at "Downloading langsmith-0.4.56-py3-none-any.whl.metadata"
- No progress for several minutes
- Cannot access database or other services

### Root Causes
1. Network timeout issues with PyPI
2. Large package downloads without proper timeout settings
3. Unpinned package versions causing resolution conflicts
4. Docker build cache corruption

## Solutions Applied

### 1. Pinned Package Versions
Updated `predictor/requirements.txt` with specific versions to avoid resolution conflicts and ensure reproducible builds.

### 2. Added Timeout and Retry Settings
Modified Dockerfiles to include:
- `--timeout=300`: 5-minute timeout per package
- `--retries=5`: Retry failed downloads up to 5 times

### 3. Quick Access Scripts

#### Get Database Running Immediately
```powershell
.\scripts\start-db-only.ps1
```
This starts only PostgreSQL, giving you immediate database access.

#### Full Rebuild with Clean Cache
```powershell
.\scripts\docker-rebuild.ps1
```
This performs a complete rebuild with cache clearing.

## Manual Troubleshooting Steps

### Option 1: Rebuild Predictor Service Only
```powershell
docker-compose down
docker-compose build --no-cache predictor
docker-compose up -d
```

### Option 2: Use Minimal Requirements (No LangSmith)
If you need to get running quickly without observability:

1. Temporarily swap requirements:
```powershell
Copy-Item predictor\requirements.txt predictor\requirements.full.txt
Copy-Item predictor\requirements.minimal.txt predictor\requirements.txt
```

2. Rebuild:
```powershell
docker-compose build --no-cache predictor
docker-compose up -d
```

3. Restore full requirements later:
```powershell
Copy-Item predictor\requirements.full.txt predictor\requirements.txt
```

### Option 3: Build with Different Network Settings
```powershell
# Use host network for build
docker-compose build --no-cache --network=host predictor
```

### Option 4: Manual Package Installation
```powershell
# Enter the container and install manually
docker-compose run --rm predictor /bin/bash
pip install --timeout=300 --retries=5 langsmith==0.1.147
```

## Database Access While Troubleshooting

### Connect via Docker
```powershell
docker exec -it predict-lotto-postgres psql -U postgres -d predict_lotto_nz
```

### Connection String
```
Host=localhost;Port=5434;Database=predict_lotto_nz;Username=postgres;Password=password
```

### Using pgAdmin or DBeaver
- Host: localhost
- Port: 5434
- Database: predict_lotto_nz
- Username: postgres
- Password: password

## Verification Steps

After rebuild, verify services are running:

```powershell
# Check all services
docker-compose ps

# Check predictor logs
docker-compose logs -f predictor

# Test predictor health
curl http://localhost:8000/health

# Test database connection
docker exec -it predict-lotto-postgres pg_isready -U postgres
```

## Prevention

1. Always pin package versions in requirements.txt
2. Use timeout and retry settings in Dockerfiles
3. Regularly prune Docker build cache: `docker builder prune`
4. Keep Docker Desktop updated
5. Monitor network connectivity during builds

## Additional Resources

- [Docker Build Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [pip Timeout Settings](https://pip.pypa.io/en/stable/cli/pip_install/)
- [LangSmith Documentation](https://docs.smith.langchain.com/)
