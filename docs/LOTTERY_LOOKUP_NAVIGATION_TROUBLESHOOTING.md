# Lottery Lookup and Navigation - Troubleshooting Guide

## Overview

This guide provides comprehensive troubleshooting information for common issues encountered with the Lottery Lookup and Navigation features. It includes diagnostic steps, performance optimization techniques, and solutions for frequently reported problems.

## Table of Contents

1. [Common Issues](#common-issues)
2. [Performance Problems](#performance-problems)
3. [Database Issues](#database-issues)
4. [Caching Problems](#caching-problems)
5. [Frontend Issues](#frontend-issues)
6. [API Errors](#api-errors)
7. [Search and Navigation Problems](#search-and-navigation-problems)
8. [Export and Import Issues](#export-and-import-issues)
9. [Diagnostic Tools](#diagnostic-tools)
10. [Performance Tuning](#performance-tuning)

## Common Issues

### Search Returns No Results

**Symptoms:**
- Number lookup returns empty results for valid numbers
- Combination search finds no matches for known combinations
- Frequency analysis shows zero occurrences

**Diagnostic Steps:**
```bash
# Check if data exists in database
curl "http://localhost:5000/api/lotto/latest"

# Verify number range
curl "http://localhost:5000/api/lookup/number/7"

# Check database connection
docker-compose logs backend | grep -i database
```

**Common Causes and Solutions:**

1. **Empty Database**
   ```bash
   # Check total draws in database
   curl "http://localhost:5000/api/navigation/draws/range?startDate=2008-01-01&endDate=2024-12-31&limit=1"
   
   # If empty, import lottery data
   curl -X POST -F "file=@lottery-data.csv" "http://localhost:5000/api/lotto/upload"
   ```

2. **Invalid Number Range**
   - Ensure numbers are between 1-40
   - Check for typos in API requests
   - Validate input parameters

3. **Date Range Issues**
   ```bash
   # Check available date range
   curl "http://localhost:5000/api/navigation/draw/1" | jq '.navigationContext'
   
   # Adjust search dates to match available data
   curl "http://localhost:5000/api/lookup/number/7?startDate=2008-02-13&endDate=2024-12-31"
   ```

### Slow Search Performance

**Symptoms:**
- Search requests take longer than 5 seconds
- Timeout errors during complex searches
- High CPU usage during searches

**Diagnostic Steps:**
```bash
# Check API response times
curl -w "@curl-format.txt" "http://localhost:5000/api/lookup/number/7"

# Monitor database performance
docker-compose exec postgres psql -U postgres -d predict_lotto_nz -c "
SELECT query, mean_exec_time, calls 
FROM pg_stat_statements 
ORDER BY mean_exec_time DESC 
LIMIT 10;"

# Check cache hit rates
curl "http://localhost:5000/api/cache/status" | jq '.statistics.hitRate'
```

**Performance Optimization:**

1. **Enable Database Indexes**
   ```sql
   -- Verify indexes exist
   SELECT indexname, tablename FROM pg_indexes 
   WHERE tablename IN ('LottoDraws', 'NumberOccurrences', 'NumberFrequencies');
   
   -- Create missing indexes if needed
   CREATE INDEX CONCURRENTLY IF NOT EXISTS IX_LottoDraws_Date ON "LottoDraws" ("Date" DESC);
   CREATE INDEX CONCURRENTLY IF NOT EXISTS IX_NumberOccurrences_Number_Date 
   ON "NumberOccurrences" ("Number", "DrawDate" DESC);
   ```

2. **Optimize Cache Configuration**
   ```bash
   # Warm up cache for frequently accessed data
   curl -X POST "http://localhost:5000/api/cache/warm-up" \
     -H "Content-Type: application/json" \
     -d '{"cacheTypes": ["numberLookup", "frequencyAnalysis"], "priority": "high"}'
   ```

3. **Limit Search Scope**
   ```bash
   # Use date ranges to limit search scope
   curl "http://localhost:5000/api/lookup/number/7?startDate=2024-01-01&endDate=2024-12-31"
   
   # Limit result count for large datasets
   curl "http://localhost:5000/api/frequency/numbers?limit=20"
   ```
### Navigation Controls Not Working

**Symptoms:**
- Previous/Next buttons don't respond
- Jump to draw functionality fails
- Navigation context shows incorrect information

**Diagnostic Steps:**
```bash
# Test navigation endpoints directly
curl "http://localhost:5000/api/navigation/draw/1999"
curl "http://localhost:5000/api/navigation/draw/1999/previous"
curl "http://localhost:5000/api/navigation/draw/1999/next"

# Check for JavaScript errors in browser console
# Look for network errors in browser developer tools
```

**Common Solutions:**

1. **Invalid Draw Numbers**
   ```bash
   # Check if draw exists
   curl "http://localhost:5000/api/lotto/exists/1999"
   
   # Get valid draw range
   curl "http://localhost:5000/api/navigation/draw/1" | jq '.navigationContext'
   ```

2. **Frontend State Issues**
   ```javascript
   // Clear component state in browser console
   localStorage.clear();
   sessionStorage.clear();
   location.reload();
   ```

3. **API Connectivity**
   ```bash
   # Test API connectivity
   curl "http://localhost:5000/api/health"
   
   # Check CORS configuration if accessing from different domain
   curl -H "Origin: http://localhost:3000" \
        -H "Access-Control-Request-Method: GET" \
        -H "Access-Control-Request-Headers: X-Requested-With" \
        -X OPTIONS "http://localhost:5000/api/navigation/draw/1999"
   ```

## Performance Problems

### High Memory Usage

**Symptoms:**
- Application consuming excessive RAM
- Out of memory errors
- Slow garbage collection

**Diagnostic Steps:**
```bash
# Check memory usage
docker stats predict-lotto-backend

# Monitor .NET memory usage
curl "http://localhost:5000/api/health" | jq '.memoryUsage'

# Check cache memory consumption
curl "http://localhost:5000/api/cache/status" | jq '.statistics.memoryUsage'
```

**Solutions:**

1. **Optimize Cache Size**
   ```csharp
   // In appsettings.json
   {
     "Caching": {
       "MemoryCacheSize": "100MB",
       "DistributedCacheSize": "500MB",
       "DefaultExpiration": "00:30:00"
     }
   }
   ```

2. **Implement Pagination**
   ```bash
   # Use pagination for large result sets
   curl "http://localhost:5000/api/lookup/number/7?limit=50&offset=0"
   curl "http://localhost:5000/api/frequency/numbers?limit=20&offset=0"
   ```

3. **Clear Unused Cache**
   ```bash
   # Clear old cache entries
   curl -X DELETE "http://localhost:5000/api/cache/invalidate" \
     -H "Content-Type: application/json" \
     -d '{"invalidateAll": true}'
   ```

### Database Connection Issues

**Symptoms:**
- Connection timeout errors
- "Cannot connect to database" messages
- Intermittent database failures

**Diagnostic Steps:**
```bash
# Check database container status
docker-compose ps postgres

# Test database connectivity
docker-compose exec postgres psql -U postgres -d predict_lotto_nz -c "SELECT 1;"

# Check connection pool status
curl "http://localhost:5000/api/health" | jq '.database'

# Monitor active connections
docker-compose exec postgres psql -U postgres -c "
SELECT count(*) as active_connections, 
       state, 
       application_name 
FROM pg_stat_activity 
WHERE datname = 'predict_lotto_nz' 
GROUP BY state, application_name;"
```

**Solutions:**

1. **Restart Database Container**
   ```bash
   docker-compose restart postgres
   
   # Wait for database to be ready
   docker-compose exec postgres pg_isready -U postgres
   ```

2. **Optimize Connection Pool**
   ```json
   // In appsettings.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=postgres;Database=predict_lotto_nz;Username=postgres;Password=yourpassword;Pooling=true;MinPoolSize=5;MaxPoolSize=20;ConnectionIdleLifetime=300"
     }
   }
   ```

3. **Check Database Locks**
   ```sql
   -- Check for blocking queries
   SELECT blocked_locks.pid AS blocked_pid,
          blocked_activity.usename AS blocked_user,
          blocking_locks.pid AS blocking_pid,
          blocking_activity.usename AS blocking_user,
          blocked_activity.query AS blocked_statement,
          blocking_activity.query AS current_statement_in_blocking_process
   FROM pg_catalog.pg_locks blocked_locks
   JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
   JOIN pg_catalog.pg_locks blocking_locks ON blocking_locks.locktype = blocked_locks.locktype
   JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
   WHERE NOT blocked_locks.granted;
   ```

## Caching Problems

### Cache Miss Rate Too High

**Symptoms:**
- Cache hit rate below 70%
- Slow response times despite caching
- High database load

**Diagnostic Steps:**
```bash
# Check cache statistics
curl "http://localhost:5000/api/cache/status" | jq '.statistics'

# Monitor cache performance by type
curl "http://localhost:5000/api/cache/status" | jq '.cacheTypes'

# Check cache key distribution
curl "http://localhost:5000/api/cache/status" | jq '.statistics.totalKeys'
```

**Solutions:**

1. **Optimize Cache Keys**
   ```csharp
   // Ensure consistent key generation
   public static string NumberLookup(int number, DateTime? startDate = null)
   {
       var key = $"number_lookup_{number}";
       if (startDate.HasValue) 
           key += $"_from_{startDate.Value:yyyyMMdd}";
       return key;
   }
   ```

2. **Increase Cache TTL**
   ```csharp
   // Extend cache expiration for stable data
   await _cache.SetAsync(key, result, TimeSpan.FromHours(2)); // Instead of 30 minutes
   ```

3. **Implement Cache Warming**
   ```bash
   # Warm up frequently accessed data
   curl -X POST "http://localhost:5000/api/cache/warm-up" \
     -H "Content-Type: application/json" \
     -d '{
       "cacheTypes": ["numberLookup", "frequencyAnalysis", "navigationContext"],
       "priority": "high",
       "backgroundProcess": true
     }'
   ```

### Redis Connection Issues

**Symptoms:**
- "Redis server is not available" errors
- Fallback to database for all requests
- Distributed cache failures

**Diagnostic Steps:**
```bash
# Check Redis container status
docker-compose ps redis

# Test Redis connectivity
docker-compose exec redis redis-cli ping

# Check Redis memory usage
docker-compose exec redis redis-cli info memory

# Monitor Redis logs
docker-compose logs redis
```

**Solutions:**

1. **Restart Redis Container**
   ```bash
   docker-compose restart redis
   
   # Verify Redis is responding
   docker-compose exec redis redis-cli ping
   ```

2. **Configure Redis Persistence**
   ```bash
   # Check Redis configuration
   docker-compose exec redis redis-cli config get save
   
   # Enable persistence if needed
   docker-compose exec redis redis-cli config set save "900 1 300 10 60 10000"
   ```

3. **Monitor Redis Performance**
   ```bash
   # Check slow queries
   docker-compose exec redis redis-cli slowlog get 10
   
   # Monitor Redis stats
   docker-compose exec redis redis-cli --stat
   ```

## Frontend Issues

### Components Not Loading

**Symptoms:**
- Blank pages or components
- JavaScript errors in console
- Components showing loading state indefinitely

**Diagnostic Steps:**
```bash
# Check frontend container status
docker-compose ps frontend

# Check frontend logs
docker-compose logs frontend

# Test API connectivity from frontend
curl "http://localhost:3000/health"

# Check browser console for errors
# Open browser developer tools and look for:
# - Network errors (failed API calls)
# - JavaScript errors
# - CORS issues
```

**Solutions:**

1. **Restart Frontend Container**
   ```bash
   docker-compose restart frontend
   
   # Clear browser cache
   # Hard refresh: Ctrl+Shift+R (Windows/Linux) or Cmd+Shift+R (Mac)
   ```

2. **Check API Base URL Configuration**
   ```javascript
   // In .env or environment configuration
   VITE_API_BASE_URL=http://localhost:5000
   
   // Verify in browser console
   console.log(import.meta.env.VITE_API_BASE_URL);
   ```

3. **Fix CORS Issues**
   ```csharp
   // In Program.cs
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowFrontend", policy =>
       {
           policy.WithOrigins("http://localhost:3000")
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials();
       });
   });
   ```

### Search Suggestions Not Working

**Symptoms:**
- Auto-completion dropdown doesn't appear
- Suggestions are empty or irrelevant
- Slow suggestion response

**Diagnostic Steps:**
```bash
# Test suggestions API directly
curl "http://localhost:5000/api/lookup/suggestions?query=7&type=number"

# Check suggestion cache
curl "http://localhost:5000/api/cache/status" | jq '.cacheTypes.suggestions'

# Monitor suggestion performance
curl -w "@curl-format.txt" "http://localhost:5000/api/lookup/suggestions?query=7,14&type=combination"
```

**Solutions:**

1. **Verify Suggestion Data**
   ```bash
   # Check if historical data exists for suggestions
   curl "http://localhost:5000/api/frequency/numbers?limit=5"
   
   # Verify combination patterns exist
   curl -X POST "http://localhost:5000/api/lookup/combination" \
     -H "Content-Type: application/json" \
     -d '{"combination": [7, 14], "includePartialMatches": true, "minimumMatches": 2}'
   ```

2. **Optimize Suggestion Algorithm**
   ```csharp
   // Improve suggestion relevance scoring
   public async Task<IEnumerable<SearchSuggestion>> GetSearchSuggestionsAsync(string query, SearchType type)
   {
       var suggestions = await _cache.GetOrSetAsync($"suggestions_{query}_{type}", async () =>
       {
           // Implement smarter suggestion logic
           return await GenerateRelevantSuggestions(query, type);
       }, TimeSpan.FromMinutes(15));
       
       return suggestions.OrderByDescending(s => s.Relevance).Take(10);
   }
   ```

3. **Implement Debouncing**
   ```javascript
   // In Vue component
   import { debounce } from 'lodash-es'
   
   const debouncedSearch = debounce(async (query) => {
     if (query.length >= 2) {
       suggestions.value = await lookupService.getSearchSuggestions(query, 'number')
     }
   }, 300)
   
   watch(searchQuery, debouncedSearch)
   ```
## API Errors

### 400 Bad Request Errors

**Common Causes:**
- Invalid number ranges (outside 1-40)
- Malformed date formats
- Invalid combination sizes
- Missing required parameters

**Diagnostic Steps:**
```bash
# Test with valid parameters
curl "http://localhost:5000/api/lookup/number/7"

# Check parameter validation
curl "http://localhost:5000/api/lookup/number/45" # Should return 400

# Verify date format
curl "http://localhost:5000/api/lookup/number/7?startDate=2024-01-01T00:00:00Z"
```

**Solutions:**

1. **Validate Input Parameters**
   ```javascript
   // Frontend validation
   const isValidNumber = (num) => num >= 1 && num <= 40
   const isValidDate = (date) => !isNaN(Date.parse(date))
   const isValidCombination = (combo) => 
     combo.length >= 2 && combo.length <= 6 && 
     combo.every(n => isValidNumber(n)) &&
     new Set(combo).size === combo.length // No duplicates
   ```

2. **Use Proper Date Formats**
   ```bash
   # Correct ISO 8601 format
   curl "http://localhost:5000/api/lookup/number/7?startDate=2024-01-01T00:00:00Z"
   
   # Date-only format (also acceptable)
   curl "http://localhost:5000/api/lookup/number/7?startDate=2024-01-01"
   ```

### 404 Not Found Errors

**Common Causes:**
- Requesting non-existent draw numbers
- Invalid API endpoints
- Missing data for specific dates

**Solutions:**

1. **Check Draw Existence**
   ```bash
   # Verify draw exists before navigation
   curl "http://localhost:5000/api/lotto/exists/1999"
   
   # Get valid draw range
   curl "http://localhost:5000/api/navigation/draw/1" | jq '.navigationContext.totalDraws'
   ```

2. **Handle Missing Data Gracefully**
   ```javascript
   // Frontend error handling
   try {
     const draw = await navigationService.getDrawByNumber(drawNumber)
     return draw
   } catch (error) {
     if (error.response?.status === 404) {
       // Suggest nearest available draw
       const context = await navigationService.getNavigationContext()
       const nearestDraw = findNearestDraw(drawNumber, context)
       return nearestDraw
     }
     throw error
   }
   ```

### 500 Internal Server Errors

**Diagnostic Steps:**
```bash
# Check backend logs
docker-compose logs backend | tail -50

# Check database connectivity
curl "http://localhost:5000/api/health"

# Monitor system resources
docker stats
```

**Solutions:**

1. **Check Database Connection**
   ```bash
   # Restart database if needed
   docker-compose restart postgres
   
   # Verify connection string
   docker-compose exec backend env | grep ConnectionStrings
   ```

2. **Review Application Logs**
   ```bash
   # Look for specific error patterns
   docker-compose logs backend | grep -i "error\|exception\|failed"
   
   # Check for memory issues
   docker-compose logs backend | grep -i "memory\|gc\|heap"
   ```

## Search and Navigation Problems

### Inconsistent Search Results

**Symptoms:**
- Same search returns different results
- Results don't match expected data
- Missing recent draws in results

**Diagnostic Steps:**
```bash
# Check data consistency
curl "http://localhost:5000/api/lotto/latest"

# Verify cache invalidation
curl "http://localhost:5000/api/cache/status"

# Test with cache bypass
curl "http://localhost:5000/api/lookup/number/7?bypassCache=true"
```

**Solutions:**

1. **Force Cache Refresh**
   ```bash
   # Invalidate specific cache entries
   curl -X DELETE "http://localhost:5000/api/cache/invalidate" \
     -H "Content-Type: application/json" \
     -d '{"cacheTypes": ["numberLookup"], "keys": ["number_7"]}'
   
   # Or invalidate all caches
   curl -X DELETE "http://localhost:5000/api/cache/invalidate" \
     -H "Content-Type: application/json" \
     -d '{"invalidateAll": true}'
   ```

2. **Verify Data Integrity**
   ```sql
   -- Check for duplicate draws
   SELECT "Draw", COUNT(*) 
   FROM "LottoDraws" 
   GROUP BY "Draw" 
   HAVING COUNT(*) > 1;
   
   -- Verify number occurrences match draws
   SELECT COUNT(*) as total_occurrences 
   FROM "NumberOccurrences" 
   WHERE "Number" = 7;
   ```

### Bookmark Functionality Issues

**Symptoms:**
- Bookmarks not saving
- Bookmarks disappearing
- Cannot delete bookmarks

**Diagnostic Steps:**
```bash
# Test bookmark creation
curl -X POST "http://localhost:5000/api/navigation/bookmarks" \
  -H "Content-Type: application/json" \
  -d '{"drawNumber": 1999, "label": "Test Bookmark"}'

# Check existing bookmarks
curl "http://localhost:5000/api/navigation/bookmarks"

# Verify database table
docker-compose exec postgres psql -U postgres -d predict_lotto_nz -c "SELECT * FROM \"Bookmarks\" LIMIT 5;"
```

**Solutions:**

1. **Check Database Permissions**
   ```sql
   -- Verify table exists and has correct structure
   \d "Bookmarks"
   
   -- Check for foreign key constraints
   SELECT conname, conrelid::regclass, confrelid::regclass 
   FROM pg_constraint 
   WHERE contype = 'f' AND conrelid = '"Bookmarks"'::regclass;
   ```

2. **Validate Draw References**
   ```bash
   # Ensure draw exists before bookmarking
   curl "http://localhost:5000/api/lotto/exists/1999"
   
   # Check for orphaned bookmarks
   docker-compose exec postgres psql -U postgres -d predict_lotto_nz -c "
   SELECT b.\"Id\", b.\"DrawNumber\" 
   FROM \"Bookmarks\" b 
   LEFT JOIN \"LottoDraws\" d ON b.\"DrawNumber\" = d.\"Draw\" 
   WHERE d.\"Draw\" IS NULL;"
   ```

## Export and Import Issues

### Export Timeouts

**Symptoms:**
- Large exports fail with timeout errors
- Export progress stalls
- Incomplete export files

**Solutions:**

1. **Implement Chunked Exports**
   ```csharp
   public async Task<byte[]> ExportLargeDatasetAsync(ExportRequest request)
   {
       const int chunkSize = 1000;
       var allData = new List<object>();
       
       for (int offset = 0; ; offset += chunkSize)
       {
           var chunk = await GetDataChunkAsync(request, offset, chunkSize);
           if (!chunk.Any()) break;
           
           allData.AddRange(chunk);
           
           // Report progress
           await _progressReporter.ReportProgressAsync(offset + chunk.Count());
       }
       
       return SerializeToFormat(allData, request.Format);
   }
   ```

2. **Use Background Jobs**
   ```bash
   # Start background export
   curl -X POST "http://localhost:5000/api/export/background" \
     -H "Content-Type: application/json" \
     -d '{"type": "frequency", "format": "csv", "dateRange": {"start": "2008-01-01", "end": "2024-12-31"}}'
   
   # Check export status
   curl "http://localhost:5000/api/export/status/export_20241217_103000"
   ```

### Import Validation Errors

**Symptoms:**
- CSV import fails with validation errors
- Duplicate data warnings
- Incorrect data parsing

**Solutions:**

1. **Validate CSV Format**
   ```bash
   # Check CSV headers
   head -1 lottery-data.csv
   
   # Expected format: Draw,Date,WinningNumber1,WinningNumber2,WinningNumber3,WinningNumber4,WinningNumber5,WinningNumber6,BonusNumber,Powerball
   
   # Validate data types
   awk -F',' 'NR>1 {print NF}' lottery-data.csv | sort | uniq -c
   ```

2. **Handle Duplicate Data**
   ```csharp
   public async Task<ImportResult> ImportCsvAsync(Stream csvStream)
   {
       var result = new ImportResult();
       var existingDraws = await _context.LottoDraws.Select(d => d.Draw).ToHashSetAsync();
       
       await foreach (var record in ReadCsvRecordsAsync(csvStream))
       {
           if (existingDraws.Contains(record.Draw))
           {
               result.RecordsSkipped++;
               continue;
           }
           
           // Validate and import new record
           if (ValidateRecord(record))
           {
               await _context.LottoDraws.AddAsync(record);
               result.RecordsAdded++;
           }
           else
           {
               result.Errors.Add($"Invalid record: Draw {record.Draw}");
           }
       }
       
       await _context.SaveChangesAsync();
       return result;
   }
   ```

## Diagnostic Tools

### Health Check Endpoints

```bash
# Overall system health
curl "http://localhost:5000/api/health" | jq

# Database connectivity
curl "http://localhost:5000/api/health/database" | jq

# Cache status
curl "http://localhost:5000/api/cache/status" | jq

# Performance metrics
curl "http://localhost:5000/api/performance/metrics" | jq
```

### Logging Configuration

**Enable Detailed Logging:**
```json
// In appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information",
      "PredictLottoNZ.Services": "Debug",
      "System.Net.Http.HttpClient": "Information"
    }
  }
}
```

**Monitor Logs in Real-time:**
```bash
# Backend logs
docker-compose logs -f backend

# Database logs
docker-compose logs -f postgres

# Frontend logs
docker-compose logs -f frontend

# All services
docker-compose logs -f
```

### Performance Monitoring

**Database Query Analysis:**
```sql
-- Enable query statistics (run once)
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- View slow queries
SELECT query, calls, total_exec_time, mean_exec_time, rows
FROM pg_stat_statements
WHERE query LIKE '%LottoDraws%' OR query LIKE '%NumberOccurrences%'
ORDER BY mean_exec_time DESC
LIMIT 10;

-- Reset statistics
SELECT pg_stat_statements_reset();
```

**API Performance Testing:**
```bash
# Create curl timing format file
cat > curl-format.txt << 'EOF'
     time_namelookup:  %{time_namelookup}\n
        time_connect:  %{time_connect}\n
     time_appconnect:  %{time_appconnect}\n
    time_pretransfer:  %{time_pretransfer}\n
       time_redirect:  %{time_redirect}\n
  time_starttransfer:  %{time_starttransfer}\n
                     ----------\n
          time_total:  %{time_total}\n
EOF

# Test API endpoint performance
curl -w "@curl-format.txt" -o /dev/null -s "http://localhost:5000/api/lookup/number/7"
```

## Performance Tuning

### Database Optimization

1. **Index Optimization**
   ```sql
   -- Analyze table statistics
   ANALYZE "LottoDraws";
   ANALYZE "NumberOccurrences";
   ANALYZE "NumberFrequencies";
   
   -- Check index usage
   SELECT schemaname, tablename, attname, n_distinct, correlation
   FROM pg_stats
   WHERE tablename IN ('LottoDraws', 'NumberOccurrences')
   ORDER BY tablename, attname;
   
   -- Create composite indexes for common queries
   CREATE INDEX CONCURRENTLY IF NOT EXISTS IX_LottoDraws_Date_Numbers
   ON "LottoDraws" ("Date" DESC, "WinningNumber1", "WinningNumber2", "WinningNumber3");
   ```

2. **Query Optimization**
   ```csharp
   // Use compiled queries for frequently executed queries
   private static readonly Func<LottoDbContext, int, IAsyncEnumerable<NumberOccurrence>> 
       GetNumberOccurrencesQuery = EF.CompileAsyncQuery(
           (LottoDbContext context, int number) =>
               context.LottoDraws
                   .Where(d => d.WinningNumber1 == number || 
                              d.WinningNumber2 == number ||
                              d.WinningNumber3 == number ||
                              d.WinningNumber4 == number ||
                              d.WinningNumber5 == number ||
                              d.WinningNumber6 == number)
                   .Select(d => new NumberOccurrence
                   {
                       DrawNumber = d.Draw,
                       DrawDate = d.Date,
                       Number = number,
                       Position = GetNumberPosition(d, number)
                   })
                   .OrderByDescending(o => o.DrawDate));
   ```

### Application Performance

1. **Memory Management**
   ```csharp
   // Configure garbage collection
   // In Program.cs
   if (Environment.GetEnvironmentVariable("DOTNET_gcServer") != "1")
   {
       Environment.SetEnvironmentVariable("DOTNET_gcServer", "1");
   }
   
   // Configure memory limits
   builder.Services.Configure<GCSettings>(options =>
   {
       options.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
   });
   ```

2. **Connection Pool Tuning**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=postgres;Database=predict_lotto_nz;Username=postgres;Password=yourpassword;Pooling=true;MinPoolSize=10;MaxPoolSize=50;ConnectionIdleLifetime=600;CommandTimeout=30"
     }
   }
   ```

3. **Cache Configuration**
   ```json
   {
     "Caching": {
       "MemoryCache": {
         "SizeLimit": 104857600,
         "CompactionPercentage": 0.25,
         "ExpirationScanFrequency": "00:05:00"
       },
       "DistributedCache": {
         "DefaultExpiration": "01:00:00",
         "SlidingExpiration": "00:30:00"
       }
     }
   }
   ```

### Frontend Performance

1. **Component Optimization**
   ```vue
   <!-- Use v-memo for expensive computations -->
   <template>
     <div v-memo="[searchResults?.totalOccurrences]">
       <expensive-chart :data="chartData" />
     </div>
   </template>
   
   <script setup>
   // Use computed properties for derived state
   const chartData = computed(() => {
     if (!searchResults.value) return []
     return processChartData(searchResults.value.occurrences)
   })
   
   // Implement virtual scrolling for large lists
   import { VirtualList } from '@tanstack/vue-virtual'
   </script>
   ```

2. **Bundle Optimization**
   ```typescript
   // In vite.config.ts
   export default defineConfig({
     build: {
       rollupOptions: {
         output: {
           manualChunks: {
             'vendor': ['vue', 'vue-router', 'pinia'],
             'charts': ['chart.js', 'vue-chartjs'],
             'utils': ['lodash-es', 'date-fns']
           }
         }
       }
     }
   })
   ```

---

*This troubleshooting guide covers the most common issues and their solutions. For additional support, check the system logs and health endpoints, or consult the API documentation for specific error codes and their meanings.*