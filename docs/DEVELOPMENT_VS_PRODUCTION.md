# Development vs Production Mode - Quick Reference

## TL;DR

**Currently Running**: Production Mode (requires rebuild for code changes)  
**Recommended for Development**: Development Mode (automatic hot-reload)

## Mode Comparison

| Feature | Production Mode | Development Mode |
|---------|----------------|------------------|
| **Command** | `docker-compose up -d` | `docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d` |
| **Code Changes** | ❌ Requires rebuild | ✅ Auto-reload |
| **Rebuild Time** | 2-5 minutes | Not needed |
| **Volume Mounts** | ❌ No code mounting | ✅ Code mounted |
| **Image Size** | Smaller (optimized) | Larger (includes dev tools) |
| **Performance** | Faster (compiled) | Slightly slower (dev mode) |
| **Use Case** | Production, Testing | Active Development |

## How Code Updates Work

### Production Mode (Current Setup)

```
Your Code → Docker Build → Image → Container
     ↓           ↓            ↓         ↓
  Edit File   Copy Code   Baked In   Running
     ↓
  ❌ Container doesn't see changes
     ↓
  Must rebuild image to see changes
```

**Workflow:**
1. Edit code in your IDE
2. Run `docker-compose build [service]`
3. Run `docker-compose up -d [service]`
4. Wait 2-5 minutes
5. Test changes

### Development Mode (Recommended)

```
Your Code ←→ Container (Volume Mount)
     ↓            ↓
  Edit File   Auto-Reload
     ↓            ↓
  Save File   See Changes
     ↓
  ✅ Changes appear in seconds
```

**Workflow:**
1. Edit code in your IDE
2. Save file (Ctrl+S)
3. Changes appear automatically
4. Test immediately

## Quick Start Commands

### Switch to Development Mode
```powershell
# Stop production containers
docker-compose down

# Start development mode
.\scripts\dev-start.ps1

# Or manually
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### View Logs (Development)
```powershell
# All services
.\scripts\dev-logs.ps1

# Specific service
.\scripts\dev-logs.ps1 backend
.\scripts\dev-logs.ps1 frontend
.\scripts\dev-logs.ps1 predictor
```

### Restart Service (Development)
```powershell
.\scripts\dev-restart.ps1 backend
.\scripts\dev-restart.ps1 frontend
.\scripts\dev-restart.ps1 predictor
```

### Rebuild After Adding Dependencies
```powershell
# When you add new npm/NuGet/pip packages
.\scripts\dev-rebuild.ps1 backend
.\scripts\dev-rebuild.ps1 frontend
.\scripts\dev-rebuild.ps1 predictor
```

## Service-Specific Details

### Backend (.NET)

**Production Mode:**
- Code compiled to DLL during build
- Changes require full rebuild (~3-5 min)
- Optimized for performance

**Development Mode:**
- Uses `dotnet watch run`
- Detects .cs file changes
- Auto-recompiles and restarts (~5-10 sec)
- Volume mount: `./backend:/app`

### Frontend (Vue.js)

**Production Mode:**
- Code built to static files during build
- Changes require full rebuild (~2-3 min)
- Served by nginx

**Development Mode:**
- Uses Vite dev server
- Hot Module Replacement (HMR)
- Changes appear instantly (~1-2 sec)
- Volume mount: `./frontend:/app`

### Predictor (Python)

**Production Mode:**
- Code copied during build
- Changes require rebuild (~1-2 min)
- Optimized for production

**Development Mode:**
- Uses `uvicorn --reload`
- Detects .py file changes
- Auto-restarts (~2-3 sec)
- Volume mount: `./predictor:/app`

## When to Rebuild in Development Mode

You only need to rebuild when:

### Backend
- ✅ Adding new NuGet packages
- ✅ Changing .csproj file
- ❌ Editing .cs files (auto-reload)

### Frontend
- ✅ Adding new npm packages
- ✅ Changing package.json
- ❌ Editing .vue, .ts, .js files (auto-reload)

### Predictor
- ✅ Adding new pip packages
- ✅ Changing requirements.txt
- ❌ Editing .py files (auto-reload)

## Troubleshooting

### "Changes not appearing in development mode"

**Backend:**
```powershell
# Check if dotnet watch is running
docker-compose logs backend | Select-String "watch"

# Restart if needed
.\scripts\dev-restart.ps1 backend
```

**Frontend:**
```powershell
# Check if Vite is running
docker-compose logs frontend | Select-String "vite"

# Restart if needed
.\scripts\dev-restart.ps1 frontend
```

**Predictor:**
```powershell
# Check if uvicorn reload is enabled
docker-compose logs predictor | Select-String "reload"

# Restart if needed
.\scripts\dev-restart.ps1 predictor
```

### "Volume mount not working"

```powershell
# Verify volumes are mounted
docker inspect predict-lotto-backend | Select-String "Mounts" -Context 0,10

# Recreate containers
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

## Recommendations

### For Daily Development Work
```powershell
# Use development mode
.\scripts\dev-start.ps1
```

**Benefits:**
- ✅ Instant feedback on code changes
- ✅ No waiting for rebuilds
- ✅ Faster iteration cycle
- ✅ Better developer experience

### Before Deploying
```powershell
# Test production build
docker-compose down
docker-compose build
docker-compose up -d
```

**Benefits:**
- ✅ Test optimized builds
- ✅ Catch production-specific issues
- ✅ Verify performance

## Summary

**Your Current Situation:**
- Running in **production mode**
- Code changes require **manual rebuild**
- Takes **2-5 minutes** per change

**Recommended Solution:**
- Switch to **development mode**
- Code changes **auto-reload**
- See changes in **seconds**

**How to Switch:**
```powershell
.\scripts\dev-start.ps1
```

That's it! Now you can edit code and see changes immediately without rebuilding. 🚀
