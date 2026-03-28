# GroqCloud Integration Troubleshooting Guide

## "An unexpected error occurred, please retry" - Troubleshooting Steps

### Step 1: Check API Key Configuration

1. **Verify API Key is Set**
   ```bash
   # Check if the API key is in your .env file
   grep GROQCLOUD_API_KEY .env
   ```
   
2. **Test API Key Directly**
   ```bash
   curl -X GET "https://api.groq.com/openai/v1/models" \
     -H "Authorization: Bearer your_groq_api_key_here"
   ```

### Step 2: Test GroqCloud Provider Directly

1. **Use the Test Endpoint**
   - Start your backend application
   - Navigate to: `http://localhost:5001/api/groqcloudtest/test`
   - This will test the GroqCloud provider in isolation

2. **Check Provider Registration**
   - Navigate to: `http://localhost:5001/api/groqcloudtest/providers`
   - Verify GroqCloud provider is listed with priority 20

### Step 3: Check Application Logs

Look for these specific error patterns in your application logs:

#### Configuration Errors
```
GroqCloud API key is not configured
Configuration error in GroqCloud provider
```

#### API Communication Errors
```
GroqCloud API communication error
HTTP error calling GroqCloud API
```

#### Response Parsing Errors
```
GroqCloud response parsing error
JSON parsing error from GroqCloud response
Invalid GroqCloud API response structure
```

### Step 4: Common Issues and Solutions

#### Issue 1: API Key Not Found
**Error**: "GroqCloud API key is not configured"

**Solution**:
1. Ensure `.env` file contains: `GROQCLOUD_API_KEY=your_groq_api_key_here`
2. Restart the application after updating `.env`
3. Check if environment variables are loaded correctly

#### Issue 2: Model Decommissioned
**Error**: "The model `llama-3.1-70b-versatile` has been decommissioned"

**Solution**:
1. Update `.env` file: `GROQCLOUD_MODEL=llama-3.3-70b-versatile`
2. Restart the application

#### Issue 3: Rate Limiting
**Error**: "Rate limit exceeded" or HTTP 429 errors

**Solution**:
1. Wait a few minutes before retrying
2. The provider has built-in retry logic with exponential backoff
3. Check your GroqCloud account usage limits

#### Issue 4: Network/Connectivity Issues
**Error**: "GroqCloud API communication error"

**Solution**:
1. Check internet connectivity
2. Verify firewall settings allow HTTPS to api.groq.com
3. Check if proxy settings are interfering

#### Issue 5: Database Context Issues
**Error**: Database-related errors in logs

**Solution**:
1. The provider gracefully handles missing database context
2. Ensure database is running and accessible
3. Check connection string configuration

### Step 5: Enable Debug Logging

Add this to your `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "PredictLottoNZ.Services.GroqCloudPredictionProvider": "Debug",
      "PredictLottoNZ.Services.PredictionService": "Debug"
    }
  }
}
```

### Step 6: Manual Testing

If the provider test endpoint works but the main application fails, the issue might be in the prediction service chain. Check:

1. **Prediction Service Priority**: GroqCloud should be priority 20
2. **Circuit Breaker State**: Previous failures might have opened the circuit breaker
3. **Provider Chain**: Ensure all providers in the chain are properly configured

### Step 7: Fallback Testing

Test if other prediction providers work:

1. **Frequency Provider**: Should always work as it doesn't require external APIs
2. **FastAPI Provider**: Check if the Python predictor service is running
3. **AWS Provider**: Check AWS credentials if configured

### Step 8: Response Format Issues

If you see JSON parsing errors, the GroqCloud API might be returning unexpected formats:

1. Check the debug logs for the actual API response
2. Verify the model is returning JSON as requested
3. The provider enforces `response_format: { type: "json_object" }`

### Emergency Workaround

If GroqCloud continues to fail, you can temporarily disable it:

1. **Comment out the registration in Program.cs**:
   ```csharp
   // builder.Services.AddScoped<IPredictionProvider, GroqCloudPredictionProvider>();
   ```

2. **Or set a higher priority** to make it lower priority:
   ```csharp
   public int Priority => 100; // Lower priority than other providers
   ```

### Getting Help

If the issue persists:

1. **Check the test endpoint response** for detailed error information
2. **Review application logs** with debug logging enabled
3. **Verify GroqCloud service status** at their status page
4. **Test the API key directly** using curl or Postman

### Monitoring

For ongoing monitoring, watch these metrics:

- **Success Rate**: Should be > 95%
- **Response Time**: Should be < 2 seconds
- **Error Patterns**: Look for recurring error types
- **Circuit Breaker State**: Should remain closed under normal operation

The GroqCloud provider includes comprehensive error handling and should provide clear error messages to help identify the root cause of any issues.