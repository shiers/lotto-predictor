# Lottery Lookup and Navigation - Developer Guide

## Overview

This guide provides comprehensive documentation for developers working with the Lottery Lookup and Navigation features. It covers component architecture, customization options, extension points, and best practices for integration and development.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Backend Services](#backend-services)
3. [Frontend Components](#frontend-components)
4. [Database Schema](#database-schema)
5. [Caching Strategy](#caching-strategy)
6. [Testing Framework](#testing-framework)
7. [Performance Optimization](#performance-optimization)
8. [Customization Guide](#customization-guide)
9. [Extension Points](#extension-points)
10. [Deployment Considerations](#deployment-considerations)

## Architecture Overview

The Lottery Lookup and Navigation system follows a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend Layer (Vue.js)                  │
├─────────────────────────────────────────────────────────────┤
│                    API Layer (.NET Core)                    │
├─────────────────────────────────────────────────────────────┤
│                   Service Layer (Business Logic)            │
├─────────────────────────────────────────────────────────────┤
│                   Data Access Layer (EF Core)               │
├─────────────────────────────────────────────────────────────┤
│                   Database Layer (PostgreSQL)               │
└─────────────────────────────────────────────────────────────┘
```

### Key Design Principles

- **Separation of Concerns**: Clear boundaries between presentation, business logic, and data access
- **Dependency Injection**: Loose coupling through DI container
- **Async/Await**: Non-blocking operations for better performance
- **Caching Strategy**: Multi-level caching for optimal performance
- **Error Handling**: Comprehensive error handling and logging
- **Testability**: Property-based and unit testing throughout
## Backend Services

### Service Architecture

The backend services are organized into distinct layers with specific responsibilities:

#### Core Services

**INumberLookupService**
```csharp
public interface INumberLookupService
{
    Task<IEnumerable<NumberOccurrence>> LookupNumberAsync(int number);
    Task<IEnumerable<NumberOccurrence>> LookupNumbersAsync(int[] numbers);
    Task<CombinationSearchResult> SearchCombinationAsync(int[] combination, bool includePartialMatches = true);
    Task<IEnumerable<SearchSuggestion>> GetSearchSuggestionsAsync(string query, SearchType type);
}
```

**Implementation Example:**
```csharp
public class NumberLookupService : INumberLookupService
{
    private readonly LottoDbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<NumberLookupService> _logger;

    public NumberLookupService(
        LottoDbContext context,
        ICacheService cache,
        ILogger<NumberLookupService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<NumberOccurrence>> LookupNumberAsync(int number)
    {
        var cacheKey = $"number_lookup_{number}";
        
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            return await _context.LottoDraws
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
                    Position = GetNumberPosition(d, number),
                    FullCombination = new[] { d.WinningNumber1, d.WinningNumber2, 
                                            d.WinningNumber3, d.WinningNumber4, 
                                            d.WinningNumber5, d.WinningNumber6 }
                })
                .OrderByDescending(o => o.DrawDate)
                .ToListAsync();
        }, TimeSpan.FromMinutes(30));
    }
}
```
**IFrequencyAnalysisService**
```csharp
public interface IFrequencyAnalysisService
{
    Task<IEnumerable<NumberFrequency>> GetNumberFrequenciesAsync();
    Task<IEnumerable<RangeFrequency>> GetRangeFrequenciesAsync(IEnumerable<NumberRange> ranges);
    Task<FrequencyComparison> CompareFrequenciesAsync(FrequencyComparisonRequest request);
    Task<IEnumerable<HotColdNumber>> GetHotColdAnalysisAsync(int periodDays = 365);
}
```

**IDrawNavigationService**
```csharp
public interface IDrawNavigationService
{
    Task<LottoDraw> GetDrawByNumberAsync(int drawNumber);
    Task<LottoDraw> GetDrawByDateAsync(DateTime date);
    Task<LottoDraw> GetPreviousDrawAsync(int currentDrawNumber);
    Task<LottoDraw> GetNextDrawAsync(int currentDrawNumber);
    Task<NavigationContext> GetNavigationContextAsync(int drawNumber);
    Task<IEnumerable<LottoDraw>> GetDrawsInRangeAsync(DateTime startDate, DateTime endDate);
}
```

### Service Registration

Configure services in `Program.cs`:

```csharp
// Register core services
builder.Services.AddScoped<INumberLookupService, NumberLookupService>();
builder.Services.AddScoped<IFrequencyAnalysisService, FrequencyAnalysisService>();
builder.Services.AddScoped<IDrawNavigationService, DrawNavigationService>();
builder.Services.AddScoped<IBookmarkService, BookmarkService>();
builder.Services.AddScoped<IExportService, ExportService>();

// Register caching services
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
builder.Services.AddScoped<ICacheWarmupService, CacheWarmupService>();

// Register performance monitoring
builder.Services.AddScoped<IPerformanceMonitoringService, PerformanceMonitoringService>();
```
## Frontend Components

### Component Architecture

The frontend uses Vue 3 with TypeScript and follows the Composition API pattern for better type safety and reusability.

#### Core Components

**NumberLookup.vue**
```vue
<template>
  <div class="number-lookup">
    <div class="search-section">
      <input
        v-model="searchNumber"
        type="number"
        min="1"
        max="40"
        placeholder="Enter number (1-40)"
        @keyup.enter="performSearch"
        :disabled="isLoading"
      />
      <button @click="performSearch" :disabled="isLoading || !isValidNumber">
        {{ isLoading ? 'Searching...' : 'Search' }}
      </button>
    </div>
    
    <div v-if="searchResults" class="results-section">
      <h3>Results for Number {{ searchResults.number }}</h3>
      <div class="stats">
        <span>Total Occurrences: {{ searchResults.totalOccurrences }}</span>
        <span>Frequency: {{ searchResults.frequencyStats.percentage }}%</span>
      </div>
      
      <div class="occurrences-list">
        <div
          v-for="occurrence in searchResults.occurrences"
          :key="`${occurrence.drawNumber}-${occurrence.position}`"
          class="occurrence-item"
        >
          <span class="draw-number">Draw {{ occurrence.drawNumber }}</span>
          <span class="draw-date">{{ formatDate(occurrence.drawDate) }}</span>
          <span class="position">Position {{ occurrence.position }}</span>
          <div class="combination">
            {{ occurrence.fullCombination.join(', ') }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { lookupService } from '@/services/lookupService'
import type { NumberLookupResult } from '@/types/lottery'

const searchNumber = ref<number | null>(null)
const searchResults = ref<NumberLookupResult | null>(null)
const isLoading = ref(false)

const isValidNumber = computed(() => {
  return searchNumber.value !== null && 
         searchNumber.value >= 1 && 
         searchNumber.value <= 40
})

const performSearch = async () => {
  if (!isValidNumber.value) return
  
  isLoading.value = true
  try {
    searchResults.value = await lookupService.lookupNumber(searchNumber.value!)
  } catch (error) {
    console.error('Search failed:', error)
    // Handle error (show toast, etc.)
  } finally {
    isLoading.value = false
  }
}

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleDateString()
}
</script>
```
**CombinationSearch.vue**
```vue
<template>
  <div class="combination-search">
    <div class="input-section">
      <div class="number-inputs">
        <input
          v-for="(number, index) in combination"
          :key="index"
          v-model.number="combination[index]"
          type="number"
          min="1"
          max="40"
          :placeholder="`Number ${index + 1}`"
          class="number-input"
        />
      </div>
      
      <div class="controls">
        <button @click="addNumberInput" :disabled="combination.length >= 6">
          Add Number
        </button>
        <button @click="removeNumberInput" :disabled="combination.length <= 2">
          Remove Number
        </button>
        <button @click="searchCombination" :disabled="!isValidCombination">
          Search Combination
        </button>
      </div>
      
      <div class="options">
        <label>
          <input v-model="includePartialMatches" type="checkbox" />
          Include partial matches
        </label>
        <div v-if="includePartialMatches" class="min-matches">
          <label>
            Minimum matches:
            <select v-model="minimumMatches">
              <option v-for="n in maxMinMatches" :key="n" :value="n">{{ n }}</option>
            </select>
          </label>
        </div>
      </div>
    </div>
    
    <div v-if="searchResults" class="results-section">
      <div class="exact-matches" v-if="searchResults.exactMatches.length > 0">
        <h3>Exact Matches ({{ searchResults.exactMatches.length }})</h3>
        <div
          v-for="match in searchResults.exactMatches"
          :key="match.drawNumber"
          class="match-item exact"
        >
          <div class="match-header">
            <span class="draw-info">Draw {{ match.drawNumber }} - {{ formatDate(match.drawDate) }}</span>
            <span class="match-count">{{ match.matchCount }}/{{ combination.length }}</span>
          </div>
          <div class="combination">
            <span
              v-for="number in match.winningCombination"
              :key="number"
              :class="{ 'matched': match.matchedNumbers.includes(number) }"
              class="number"
            >
              {{ number }}
            </span>
          </div>
        </div>
      </div>
      
      <div class="partial-matches" v-if="searchResults.partialMatches.length > 0">
        <h3>Partial Matches ({{ searchResults.partialMatches.length }})</h3>
        <div
          v-for="match in searchResults.partialMatches"
          :key="match.drawNumber"
          class="match-item partial"
        >
          <div class="match-header">
            <span class="draw-info">Draw {{ match.drawNumber }} - {{ formatDate(match.drawDate) }}</span>
            <span class="match-count">{{ match.matchCount }}/{{ combination.length }}</span>
          </div>
          <div class="combination">
            <span
              v-for="number in match.winningCombination"
              :key="number"
              :class="{ 'matched': match.matchedNumbers.includes(number) }"
              class="number"
            >
              {{ number }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { lookupService } from '@/services/lookupService'
import type { CombinationSearchResult } from '@/types/lottery'

const combination = ref<(number | null)[]>([null, null, null, null, null, null])
const includePartialMatches = ref(true)
const minimumMatches = ref(2)
const searchResults = ref<CombinationSearchResult | null>(null)

const isValidCombination = computed(() => {
  const validNumbers = combination.value.filter(n => n !== null && n >= 1 && n <= 40)
  const uniqueNumbers = new Set(validNumbers)
  return validNumbers.length >= 2 && validNumbers.length === uniqueNumbers.size
})

const maxMinMatches = computed(() => {
  const validCount = combination.value.filter(n => n !== null).length
  return Math.max(2, validCount - 1)
})

const addNumberInput = () => {
  if (combination.value.length < 6) {
    combination.value.push(null)
  }
}

const removeNumberInput = () => {
  if (combination.value.length > 2) {
    combination.value.pop()
  }
}

const searchCombination = async () => {
  if (!isValidCombination.value) return
  
  const validNumbers = combination.value.filter(n => n !== null) as number[]
  
  try {
    searchResults.value = await lookupService.searchCombination({
      combination: validNumbers,
      includePartialMatches: includePartialMatches.value,
      minimumMatches: minimumMatches.value
    })
  } catch (error) {
    console.error('Combination search failed:', error)
  }
}

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleDateString()
}
</script>
```
### Service Layer (Frontend)

**lookupService.ts**
```typescript
import axios from 'axios'
import type { 
  NumberLookupResult, 
  CombinationSearchRequest, 
  CombinationSearchResult,
  SearchSuggestion 
} from '@/types/lottery'

class LookupService {
  private baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

  async lookupNumber(
    number: number, 
    options?: {
      startDate?: string
      endDate?: string
      includeBonus?: boolean
    }
  ): Promise<NumberLookupResult> {
    const params = new URLSearchParams()
    if (options?.startDate) params.append('startDate', options.startDate)
    if (options?.endDate) params.append('endDate', options.endDate)
    if (options?.includeBonus !== undefined) {
      params.append('includeBonus', options.includeBonus.toString())
    }

    const response = await axios.get(
      `${this.baseUrl}/api/lookup/number/${number}?${params}`
    )
    return response.data
  }

  async lookupNumbers(numbers: number[]): Promise<NumberLookupResult[]> {
    const response = await axios.post(`${this.baseUrl}/api/lookup/numbers`, {
      numbers,
      combineResults: false
    })
    return response.data.results
  }

  async searchCombination(request: CombinationSearchRequest): Promise<CombinationSearchResult> {
    const response = await axios.post(`${this.baseUrl}/api/lookup/combination`, request)
    return response.data
  }

  async getSearchSuggestions(
    query: string, 
    type: 'number' | 'combination' | 'range'
  ): Promise<SearchSuggestion[]> {
    const response = await axios.get(`${this.baseUrl}/api/lookup/suggestions`, {
      params: { query, type, maxSuggestions: 10, includePreview: true }
    })
    return response.data.suggestions
  }
}

export const lookupService = new LookupService()
```

**navigationService.ts**
```typescript
import axios from 'axios'
import type { LottoDrawDto, NavigationContext, Bookmark } from '@/types/lottery'

class NavigationService {
  private baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

  async getDrawByNumber(drawNumber: number): Promise<{
    draw: LottoDrawDto
    navigationContext: NavigationContext
  }> {
    const response = await axios.get(`${this.baseUrl}/api/navigation/draw/${drawNumber}`)
    return response.data
  }

  async getDrawByDate(date: string): Promise<{
    draw: LottoDrawDto
    exactMatch: boolean
    alternativeDates: string[]
  }> {
    const response = await axios.get(`${this.baseUrl}/api/navigation/draw/date/${date}`)
    return response.data
  }

  async getPreviousDraw(currentDrawNumber: number): Promise<{
    previousDraw: LottoDrawDto
    navigationContext: NavigationContext
  }> {
    const response = await axios.get(
      `${this.baseUrl}/api/navigation/draw/${currentDrawNumber}/previous`
    )
    return response.data
  }

  async getNextDraw(currentDrawNumber: number): Promise<{
    nextDraw: LottoDrawDto
    navigationContext: NavigationContext
  }> {
    const response = await axios.get(
      `${this.baseUrl}/api/navigation/draw/${currentDrawNumber}/next`
    )
    return response.data
  }

  async createBookmark(bookmark: {
    drawNumber: number
    label: string
    description?: string
    category?: string
  }): Promise<Bookmark> {
    const response = await axios.post(`${this.baseUrl}/api/navigation/bookmarks`, bookmark)
    return response.data
  }

  async getBookmarks(options?: {
    category?: string
    limit?: number
    sortBy?: string
    sortOrder?: string
  }): Promise<{ bookmarks: Bookmark[]; totalBookmarks: number }> {
    const params = new URLSearchParams()
    if (options?.category) params.append('category', options.category)
    if (options?.limit) params.append('limit', options.limit.toString())
    if (options?.sortBy) params.append('sortBy', options.sortBy)
    if (options?.sortOrder) params.append('sortOrder', options.sortOrder)

    const response = await axios.get(`${this.baseUrl}/api/navigation/bookmarks?${params}`)
    return response.data
  }

  async deleteBookmark(bookmarkId: number): Promise<void> {
    await axios.delete(`${this.baseUrl}/api/navigation/bookmarks/${bookmarkId}`)
  }
}

export const navigationService = new NavigationService()
```
## Database Schema

### Core Tables

The lottery lookup and navigation features extend the existing database schema with specialized tables for efficient querying and analysis.

**NumberOccurrences Table**
```sql
CREATE TABLE "NumberOccurrences" (
    "Id" SERIAL PRIMARY KEY,
    "DrawNumber" INTEGER NOT NULL,
    "Number" INTEGER NOT NULL,
    "Position" INTEGER NOT NULL, -- 1-6 for main numbers, 7 for bonus, 8 for powerball
    "DrawDate" TIMESTAMP NOT NULL,
    "IsBonus" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsPowerball" BOOLEAN NOT NULL DEFAULT FALSE,
    
    CONSTRAINT "FK_NumberOccurrences_LottoDraws" 
        FOREIGN KEY ("DrawNumber") REFERENCES "LottoDraws"("Draw")
);

-- Indexes for performance
CREATE INDEX "IX_NumberOccurrences_Number_Date" ON "NumberOccurrences" ("Number", "DrawDate" DESC);
CREATE INDEX "IX_NumberOccurrences_Draw_Position" ON "NumberOccurrences" ("DrawNumber", "Position");
CREATE INDEX "IX_NumberOccurrences_Date" ON "NumberOccurrences" ("DrawDate" DESC);
```

**NumberFrequencies Table**
```sql
CREATE TABLE "NumberFrequencies" (
    "Id" SERIAL PRIMARY KEY,
    "Number" INTEGER NOT NULL UNIQUE,
    "TotalOccurrences" INTEGER NOT NULL DEFAULT 0,
    "LastAppearance" TIMESTAMP,
    "FirstAppearance" TIMESTAMP,
    "LongestGap" INTEGER NOT NULL DEFAULT 0,
    "AverageFrequency" DECIMAL(10,4) NOT NULL DEFAULT 0,
    "LastCalculated" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "IX_NumberFrequencies_Number" ON "NumberFrequencies" ("Number");
CREATE INDEX "IX_NumberFrequencies_LastCalculated" ON "NumberFrequencies" ("LastCalculated");
```

**Bookmarks Table**
```sql
CREATE TABLE "Bookmarks" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" VARCHAR(450), -- For future user authentication
    "DrawNumber" INTEGER NOT NULL,
    "Label" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "Category" VARCHAR(50),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    
    CONSTRAINT "FK_Bookmarks_LottoDraws" 
        FOREIGN KEY ("DrawNumber") REFERENCES "LottoDraws"("Draw")
);

CREATE INDEX "IX_Bookmarks_UserId" ON "Bookmarks" ("UserId");
CREATE INDEX "IX_Bookmarks_DrawNumber" ON "Bookmarks" ("DrawNumber");
CREATE INDEX "IX_Bookmarks_Category" ON "Bookmarks" ("Category");
```

### Entity Framework Models

**NumberOccurrence.cs**
```csharp
[Table("NumberOccurrences")]
public class NumberOccurrence
{
    public int Id { get; set; }
    public int DrawNumber { get; set; }
    public int Number { get; set; }
    public int Position { get; set; }
    public DateTime DrawDate { get; set; }
    public bool IsBonus { get; set; }
    public bool IsPowerball { get; set; }
    
    // Navigation property
    public virtual LottoDraw Draw { get; set; }
}
```

**NumberFrequency.cs**
```csharp
[Table("NumberFrequencies")]
public class NumberFrequency
{
    public int Id { get; set; }
    public int Number { get; set; }
    public int TotalOccurrences { get; set; }
    public DateTime? LastAppearance { get; set; }
    public DateTime? FirstAppearance { get; set; }
    public int LongestGap { get; set; }
    public decimal AverageFrequency { get; set; }
    public DateTime LastCalculated { get; set; }
}
```

### Database Context Configuration

**LottoDbContext.cs Extensions**
```csharp
public partial class LottoDbContext : DbContext
{
    // Existing DbSets...
    public DbSet<NumberOccurrence> NumberOccurrences { get; set; }
    public DbSet<NumberFrequency> NumberFrequencies { get; set; }
    public DbSet<Bookmark> Bookmarks { get; set; }
    public DbSet<SearchHistory> SearchHistory { get; set; }
    public DbSet<ExportJob> ExportJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // NumberOccurrence configuration
        modelBuilder.Entity<NumberOccurrence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Number).IsRequired();
            entity.Property(e => e.Position).IsRequired();
            entity.Property(e => e.DrawDate).IsRequired();
            
            entity.HasOne(e => e.Draw)
                  .WithMany()
                  .HasForeignKey(e => e.DrawNumber)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => new { e.Number, e.DrawDate })
                  .HasDatabaseName("IX_NumberOccurrences_Number_Date");
        });
        
        // NumberFrequency configuration
        modelBuilder.Entity<NumberFrequency>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Number).IsUnique();
            entity.Property(e => e.AverageFrequency).HasPrecision(10, 4);
        });
        
        // Bookmark configuration
        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50);
            
            entity.HasOne(e => e.Draw)
                  .WithMany()
                  .HasForeignKey(e => e.DrawNumber)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```
## Caching Strategy

### Multi-Level Caching Architecture

The system implements a sophisticated multi-level caching strategy for optimal performance:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   L1: Memory    │───▶│ L2: Distributed │───▶│ L3: Database    │
│   Cache (Fast)  │    │ Cache (Redis)   │    │ (Persistent)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Cache Service Implementation

**ICacheService.cs**
```csharp
public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);
}
```

**CacheService.cs**
```csharp
public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        // L1: Check memory cache first
        if (_memoryCache.TryGetValue(key, out T cachedValue))
        {
            _logger.LogDebug("Cache hit (L1): {Key}", key);
            return cachedValue;
        }

        // L2: Check distributed cache
        var distributedValue = await _distributedCache.GetStringAsync(key);
        if (!string.IsNullOrEmpty(distributedValue))
        {
            var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, _jsonOptions);
            
            // Store in L1 cache for faster access
            _memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
            
            _logger.LogDebug("Cache hit (L2): {Key}", key);
            return deserializedValue;
        }

        // L3: Execute factory and cache result
        _logger.LogDebug("Cache miss: {Key}", key);
        var freshValue = await factory();

        // Store in both caches
        await SetAsync(key, freshValue, expiration);
        _memoryCache.Set(key, freshValue, TimeSpan.FromMinutes(5));

        return freshValue;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        
        await _distributedCache.SetStringAsync(key, serializedValue, options);
        _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
        
        _logger.LogDebug("Cache set: {Key}", key);
    }
}
```

### Cache Key Strategies

**Consistent Cache Key Generation**
```csharp
public static class CacheKeys
{
    public static string NumberLookup(int number, DateTime? startDate = null, DateTime? endDate = null)
    {
        var key = $"number_lookup_{number}";
        if (startDate.HasValue) key += $"_from_{startDate:yyyyMMdd}";
        if (endDate.HasValue) key += $"_to_{endDate:yyyyMMdd}";
        return key;
    }

    public static string CombinationSearch(int[] combination, bool includePartial, int minMatches)
    {
        var sortedNumbers = string.Join(",", combination.OrderBy(n => n));
        return $"combination_search_{sortedNumbers}_partial_{includePartial}_min_{minMatches}";
    }

    public static string FrequencyAnalysis(DateTime? startDate = null, DateTime? endDate = null)
    {
        var key = "frequency_analysis";
        if (startDate.HasValue) key += $"_from_{startDate:yyyyMMdd}";
        if (endDate.HasValue) key += $"_to_{endDate:yyyyMMdd}";
        return key;
    }

    public static string NavigationContext(int drawNumber) => $"navigation_context_{drawNumber}";
    
    public static string DrawByNumber(int drawNumber) => $"draw_{drawNumber}";
    
    public static string DrawByDate(DateTime date) => $"draw_date_{date:yyyyMMdd}";
}
```

### Cache Invalidation Strategy

**ICacheInvalidationService.cs**
```csharp
public interface ICacheInvalidationService
{
    Task InvalidateNumberLookupCacheAsync(int number);
    Task InvalidateFrequencyCacheAsync();
    Task InvalidateNavigationCacheAsync(int drawNumber);
    Task InvalidateAllCacheAsync();
    Task InvalidateByPatternAsync(string pattern);
}
```

**Implementation with Event-Driven Invalidation**
```csharp
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public async Task InvalidateNumberLookupCacheAsync(int number)
    {
        var pattern = $"number_lookup_{number}*";
        await _cache.RemoveByPatternAsync(pattern);
        _logger.LogInformation("Invalidated number lookup cache for number {Number}", number);
    }

    public async Task InvalidateFrequencyCacheAsync()
    {
        await _cache.RemoveByPatternAsync("frequency_*");
        await _cache.RemoveByPatternAsync("hot_cold_*");
        _logger.LogInformation("Invalidated frequency analysis cache");
    }

    // Trigger invalidation when new draws are imported
    public async Task OnNewDrawImported(LottoDraw draw)
    {
        // Invalidate all frequency-related caches
        await InvalidateFrequencyCacheAsync();
        
        // Invalidate navigation caches
        await InvalidateNavigationCacheAsync(draw.Draw);
        
        // Invalidate number lookup caches for all numbers in the draw
        var allNumbers = new[] { 
            draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3,
            draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6,
            draw.BonusNumber, draw.Powerball 
        };
        
        foreach (var number in allNumbers.Where(n => n > 0))
        {
            await InvalidateNumberLookupCacheAsync(number);
        }
    }
}
```