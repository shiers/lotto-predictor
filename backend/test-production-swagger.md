# Testing Swagger in Production Environment

## Current Behavior (Development)
- Swagger UI: ✅ Available at `/swagger`
- Root endpoint: ✅ Redirects to `/swagger`
- Environment: Development

## Production Behavior
When `ASPNETCORE_ENVIRONMENT=Production`:
- Swagger UI: ❌ Not available (404 error)
- Root endpoint: ✅ Returns API info JSON instead of redirect
- Environment: Production

## Security Benefits
1. **No API Documentation Exposure**: Swagger UI is completely disabled in production
2. **Reduced Attack Surface**: No interactive API testing interface available to external users
3. **Information Disclosure Prevention**: API structure and endpoints are not publicly documented
4. **Performance**: No overhead from Swagger middleware in production

## Testing Production Mode Locally
To test production behavior locally:

```bash
# Set environment to Production
$env:ASPNETCORE_ENVIRONMENT="Production"

# Run the application
dotnet run

# Test endpoints
curl http://localhost:5001/swagger      # Should return 404
curl http://localhost:5001/             # Should return API info JSON
```

## Root Endpoint Response in Production
```json
{
  "name": "PredictLottoNZ API",
  "version": "v1", 
  "status": "running",
  "environment": "Production",
  "timestamp": "2024-12-18T22:10:00.000Z"
}
```

This provides basic API information without exposing the full documentation interface.