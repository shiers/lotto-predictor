# Development Workflow - Code Updates in Docker

## Overview

Your Docker setup has **two modes** for handling code changes:

1. **Production Mode** (`docker-compose.yml`) - Code is baked into the image
2. **Development Mode** (`docker-compose.dev.yml`) - Code is mounted with hot-reload

## Current Situation (Production Mode)

When you run `docker-compose up`, you're using **production mode**:

### How It Works
- Code is **copied into the Docker image** during build
- Changes to your local files **do NOT** automatically update the container
- You must **rebuild the image** to see changes

### Backend (Production)
```dockerfile
# Code is copied during build
COPY . ./
RUN dotnet publish -c Release -o /app/publish
```

**To see code changes:**
```powershell
# Rebuild and restart backend
docker-compose build backend
docker-compose up -d backend
```

### Frontend (Production)
```dockerfile
# Code is built into static files during build
COPY . .
RUN npm run build-only
```

**To see code changes:**
```powershell
# Rebuild and restart frontend
docker-compose build frontend
docker-compose up -d frontend
```

### Predictor (Production)
```dockerfile
# Code is copied during build
COPY . .
```

**To see code changes:**
```powershell
# Rebuild and restart predictor
docker-compose build predictor
docker-compose up -d predictor
```

## Development Mode (Recommended for Active Development)

Use `docker-compose.dev.yml` for **automatic hot-reload** when you modify code.

### How It Works
- Your local code directory is **mounted as a volume** into the container
- Changes to local files **automatically update** the running container
- No rebuild needed for most changes

### Backend (Development)
```yaml
volumes:
  - ./backend:/app      # Your code is mounted
  - /app/bin           # Exclude build artifacts
  - /app/obj           # Exclude build artifacts
```

**Features:**
- Uses `dotnet watch run` for automatic recompilation
- Detects C# file changes and restarts automatically
- No rebuild needed for code changes

### Frontend (Development)
```yaml
volumes:
  - ./frontend:/app     # Your code is mounted
  - /app/node_modules  # Exclude node_modules
```

**Features:**
- Uses Vite dev server with HMR (Hot Module Replacement)
- Changes appear instantly in browser
- No rebuild needed for code changes

### Predictor (Development)
```yaml
volumes:
  - ./predictor:/app   # Your code is mounted
```

**Features:**
- Uses `uvicorn` with `--reload` flag
- Detects Python file changes and restarts automatically
- No rebuild needed for code changes

## Switching to Development Mode

### Start Development Environment
```powershell
# Stop production containers
docker-compose down

# Start development containers with hot-reload
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Or use the shorthand
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

### Verify Development Mode
```powershell
# Check that volumes are mounted
docker inspect predict-lotto-backend | Select-String "Mounts" -Context 0,20
docker inspect predict-lotto-frontend | Select-String "Mounts" -Context 0,20
```

## Quick Reference

### Production Mode (Current)
```powershell
# Start services
docker-compose up -d

# After code changes - MUST REBUILD
docker-compose build [service-name]
docker-compose up -d [service-name]

# Or rebuild all
docker-compose build
docker-compose up -d
```

### Development Mode (Hot-Reload)
```powershell
# Start services with hot-reload
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# After code changes - NO REBUILD NEEDED
# Just save your file and changes appear automatically!

# View logs to see auto-reload in action
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f predictor
```

## When to Use Each Mode

### Use Production Mode When:
- Testing production builds
- Deploying to staging/production
- Performance testing
- Building for release

### Use Development Mode When:
- Actively writing code
- Testing features quickly
- Debugging issues
- Iterating on UI/UX

## Development Mode Ports

Note: Development mode uses different ports for some services:

| Service | Production Port | Development Port |
|---------|----------------|------------------|
| Backend | 5001 | 5000 (HTTP), 5001 (HTTPS) |
| Frontend | 3000 | 3000 (but Vite dev server) |
| Predictor | 8000 | 8000 |
| Database | 5434 | 5434 |

## Troubleshooting Development Mode

### Backend Not Reloading
```powershell
# Check if dotnet watch is running
docker-compose logs backend | Select-String "watch"

# Restart backend
docker-compose restart backend
```

### Frontend Not Reloading
```powershell
# Check if Vite dev server is running
docker-compose logs frontend | Select-String "vite"

# Restart frontend
docker-compose restart frontend
```

### Volume Mount Issues
```powershell
# Verify volumes are mounted
docker-compose -f docker-compose.yml -f docker-compose.dev.yml config

# Recreate containers with fresh mounts
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

## Best Practices

### 1. Use Development Mode for Daily Work
```powershell
# Add to your daily startup script
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### 2. Test Production Builds Before Deploying
```powershell
# Switch to production mode to test
docker-compose down
docker-compose build
docker-compose up -d
```

### 3. Keep Dependencies in Sync
When you add new packages:

**Backend (.NET):**
```powershell
# Rebuild to install new NuGet packages
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build backend
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d backend
```

**Frontend (npm):**
```powershell
# Rebuild to install new npm packages
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build frontend
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d frontend
```

**Predictor (pip):**
```powershell
# Rebuild to install new Python packages
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build predictor
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d predictor
```

## Helper Scripts

Create these scripts for easier workflow:

### scripts/dev-start.ps1
```powershell
# Start development environment
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
Write-Host "Development environment started with hot-reload!" -ForegroundColor Green
```

### scripts/dev-logs.ps1
```powershell
# View all development logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f
```

### scripts/dev-restart.ps1
```powershell
# Restart a specific service in development mode
param([string]$service)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart $service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f $service
```

## Summary

**Current Setup (Production Mode):**
- ❌ Code changes require rebuild
- ✅ Optimized for production
- ✅ Smaller image sizes

**Development Mode (Recommended):**
- ✅ Code changes auto-reload
- ✅ Faster development cycle
- ✅ No rebuild needed for code changes
- ❌ Larger containers (includes dev tools)

**Switch to development mode for active coding:**
```powershell
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```
