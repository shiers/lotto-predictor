# Swagger API Documentation

## Overview

The PredictLottoNZ API includes comprehensive Swagger/OpenAPI documentation for testing and exploring all available endpoints. This documentation provides an interactive interface for developers to understand and test the API functionality.

## Accessing Swagger UI

### Local Development
- **URL**: `http://localhost:5001/swagger` (or your configured port)
- **Environment**: Available only in development environment

### Production/Staging
- **Swagger UI**: Not available in production for security reasons
- **API Documentation**: This documentation serves as the reference for production environments
- **Health Check**: Use `/health` endpoint to verify API status

## Features

### Interactive API Testing
- **Try It Out**: Test endpoints directly from the browser
- **Request/Response Examples**: View sample requests and responses
- **Parameter Documentation**: Detailed parameter descriptions and validation rules
- **Authentication Testing**: Test secured endpoints with authentication tokens

### API Documentation Structure

#### Core Endpoints

1. **Health Controller** (`/api/health`)
   - System health checks
   - Service status monitoring
   - Database connectivity verification

2. **Lotto Controller** (`/api/lotto`)
   - Historical draw data retrieval
   - Latest draw information
   - Draw statistics and analysis

3. **Predictions Controller** (`/api/predictions`)
   - Number prediction generation
   - Multiple prediction provider support
   - Confidence scoring and comparison

4. **Lookup Controller** (`/api/lookup`)
   - Number combination searches
   - Historical occurrence lookup
   - Pattern matching and analysis

5. **Navigation Controller** (`/api/navigation`)
   - Draw navigation and browsing
   - Date range filtering
   - Pagination support

6. **Frequency Controller** (`/api/frequency`)
   - Number frequency analysis
   - Statistical calculations
   - Trend analysis and visualization

7. **Performance Controller** (`/api/performance`)
   - System performance metrics
   - Cache performance monitoring
   - Response time analytics

8. **Cache Controller** (`/api/cache`)
   - Cache management operations
   - Cache warming and invalidation
   - Performance optimization

## API Features

### Request/Response Format
- **Content Type**: `application/json`
- **Naming Convention**: camelCase for JSON properties
- **Null Handling**: Null values are omitted from responses
- **Indentation**: Pretty-printed JSON in development mode

### Error Handling
- **Standard HTTP Status Codes**: 200, 400, 404, 500, etc.
- **Consistent Error Format**: Structured error responses with details
- **Request Tracing**: Unique request IDs for debugging

### Performance Features
- **Caching**: Redis-based distributed caching
- **Pagination**: Efficient data retrieval for large datasets
- **Compression**: Response compression for better performance
- **Request Logging**: Comprehensive request/response logging

## Authentication

### Security Scheme
- **Type**: Bearer Token (JWT)
- **Header**: `Authorization: Bearer <token>`
- **Location**: HTTP Header

### Testing Authentication
1. Obtain a valid JWT token from your authentication provider
2. Click the "Authorize" button in Swagger UI
3. Enter `Bearer <your-token>` in the authorization field
4. Test secured endpoints with authentication

## Common Use Cases

### 1. Testing Prediction Endpoints
```http
POST /api/predictions/generate
Content-Type: application/json

{
  "drawType": "lotto",
  "numberOfPredictions": 5,
  "useAdvancedAnalysis": true
}
```

### 2. Searching Historical Data
```http
GET /api/lookup/combinations?numbers=1,2,3,4,5,6&startDate=2023-01-01
```

### 3. Frequency Analysis
```http
GET /api/frequency/analysis?startDate=2023-01-01&endDate=2023-12-31&includeBonus=true
```

### 4. Navigation and Pagination
```http
GET /api/navigation/draws?page=1&pageSize=20&sortBy=drawDate&sortOrder=desc
```

## Response Examples

### Success Response
```json
{
  "success": true,
  "data": {
    "predictions": [
      {
        "numbers": [1, 15, 23, 31, 38, 42],
        "bonusNumber": 7,
        "confidence": 0.85,
        "provider": "GroqCloud"
      }
    ]
  },
  "metadata": {
    "requestId": "abc123",
    "timestamp": "2024-01-15T10:30:00Z",
    "processingTime": "245ms"
  }
}
```

### Error Response
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid number range provided",
    "details": {
      "field": "numbers",
      "value": [1, 2, 3, 50],
      "constraint": "Numbers must be between 1 and 40"
    }
  },
  "requestId": "def456",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Development Tips

### Testing Workflow
1. **Start with Health Check**: Verify API connectivity with `/api/health`
2. **Explore Data**: Use lookup endpoints to understand available data
3. **Test Core Features**: Try prediction and analysis endpoints
4. **Monitor Performance**: Check performance metrics during testing

### Best Practices
- **Use Pagination**: Always use pagination for large data sets
- **Handle Errors**: Implement proper error handling for all responses
- **Cache Responses**: Leverage caching for frequently accessed data
- **Monitor Performance**: Use performance endpoints to optimize usage

## Troubleshooting

### Common Issues

#### 1. Swagger UI Not Loading
- **Check URL**: Ensure you're accessing `/swagger` (not `/swagger/index.html`)
- **Clear Cache**: Clear browser cache and cookies
- **Check Console**: Look for JavaScript errors in browser console

#### 2. Authentication Failures
- **Token Format**: Ensure token is prefixed with "Bearer "
- **Token Expiry**: Check if your JWT token has expired
- **Permissions**: Verify token has required permissions for endpoint

#### 3. API Errors
- **Request Format**: Verify JSON format and required fields
- **Parameter Validation**: Check parameter types and constraints
- **Rate Limiting**: Ensure you're not exceeding rate limits

### Getting Help
- **API Documentation**: Refer to endpoint descriptions in Swagger UI
- **Error Messages**: Check detailed error messages in responses
- **Logs**: Review application logs for detailed error information
- **Support**: Contact the development team for complex issues

## Customization

### Swagger UI Customization
The Swagger UI includes custom styling for better user experience:
- **Brand Colors**: Custom color scheme matching application branding
- **Enhanced Layout**: Improved readability and navigation
- **Custom Headers**: Branded headers and footers
- **Response Highlighting**: Better code syntax highlighting

### Configuration Options
- **Environment Variables**: Configure API behavior through environment variables
- **Feature Flags**: Enable/disable specific features for testing
- **Cache Settings**: Adjust caching behavior for different environments
- **Logging Levels**: Configure logging verbosity for debugging

## Security Considerations

### Production Deployment
- **Swagger Disabled**: Swagger UI is disabled in production environments for security
- **HTTPS Only**: Always use HTTPS in production environments
- **Authentication**: Implement proper authentication for sensitive endpoints
- **Rate Limiting**: Configure rate limiting to prevent abuse
- **CORS Policy**: Set appropriate CORS policies for frontend integration

### Data Protection
- **Input Validation**: All inputs are validated and sanitized
- **SQL Injection Protection**: Entity Framework provides SQL injection protection
- **XSS Prevention**: JSON responses prevent XSS attacks
- **Error Information**: Production errors don't expose sensitive information

## Version Information

- **API Version**: v1
- **Swagger Version**: 3.0
- **Last Updated**: December 2024
- **Compatibility**: .NET 8.0, ASP.NET Core 8.0

For the most up-to-date API documentation, always refer to the live Swagger UI at your deployment URL.