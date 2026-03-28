# Lottery Lookup and Navigation - API Documentation

## Overview

This document provides comprehensive API documentation for the Lottery Lookup and Navigation features in PredictLottoNZ. These APIs enable programmatic access to number lookup, frequency analysis, draw navigation, bookmarking, and export functionality.

## Table of Contents

1. [Base URLs and Authentication](#base-urls-and-authentication)
2. [Lookup Controller](#lookup-controller)
3. [Navigation Controller](#navigation-controller)
4. [Frequency Controller](#frequency-controller)
5. [Cache Controller](#cache-controller)
6. [Data Models](#data-models)
7. [Error Handling](#error-handling)
8. [Rate Limiting](#rate-limiting)
9. [Examples](#examples)

## Base URLs and Authentication

### Base URLs

| Environment | Base URL |
|-------------|----------|
| Local Development | `http://localhost:5000` |
| Production | `https://api.predict-lotto-nz.com` |

### Authentication

Currently, the Lottery Lookup and Navigation APIs do not require authentication. Future versions will implement:
- JWT token-based authentication
- API key authentication for external services
- Role-based access control for advanced features

## Lookup Controller

### Number Lookup

#### GET /api/lookup/number/{number}

Search for all occurrences of a specific lottery number.

**Parameters:**
- `number` (path, required): Integer between 1-40
- `startDate` (query, optional): Start date filter (ISO 8601 format)
- `endDate` (query, optional): End date filter (ISO 8601 format)
- `includeBonus` (query, optional): Include bonus number occurrences (default: true)
- `includePowerball` (query, optional): Include powerball occurrences (default: true)

**Response:**
```json
{
  "number": 7,
  "totalOccurrences": 156,
  "occurrences": [
    {
      "drawNumber": 1999,
      "drawDate": "2024-12-07T00:00:00Z",
      "number": 7,
      "position": 1,
      "isBonus": false,
      "isPowerball": false,
      "fullCombination": [7, 14, 21, 28, 35, 42],
      "bonusNumber": 10,
      "powerball": 5
    }
  ],
  "frequencyStats": {
    "percentage": 15.6,
    "lastAppearance": "2024-12-07T00:00:00Z",
    "longestGap": 45,
    "currentGap": 0,
    "averageGap": 6.4
  }
}
```

**Example:**
```bash
curl "http://localhost:5000/api/lookup/number/7?startDate=2024-01-01&includeBonus=true"
```

#### POST /api/lookup/numbers

Search for multiple numbers simultaneously.

**Request Body:**
```json
{
  "numbers": [7, 14, 21],
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-12-31T23:59:59Z",
  "includeBonus": true,
  "includePowerball": true,
  "combineResults": false
}
```

**Response:**
```json
{
  "searchCriteria": {
    "numbers": [7, 14, 21],
    "dateRange": {
      "start": "2024-01-01T00:00:00Z",
      "end": "2024-12-31T23:59:59Z"
    }
  },
  "results": [
    {
      "number": 7,
      "occurrences": [...],
      "frequencyStats": {...}
    },
    {
      "number": 14,
      "occurrences": [...],
      "frequencyStats": {...}
    }
  ],
  "combinedStats": {
    "totalOccurrences": 468,
    "averagePerNumber": 156,
    "dateRange": {
      "earliest": "2024-01-03T00:00:00Z",
      "latest": "2024-12-07T00:00:00Z"
    }
  }
}
```

### Combination Search

#### POST /api/lookup/combination

Search for number combinations with exact and partial matches.

**Request Body:**
```json
{
  "combination": [7, 14, 21, 28, 35, 42],
  "includePartialMatches": true,
  "minimumMatches": 2,
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-12-31T23:59:59Z",
  "maxResults": 100
}
```

**Response:**
```json
{
  "searchedCombination": [7, 14, 21, 28, 35, 42],
  "exactMatches": [
    {
      "drawNumber": 1999,
      "drawDate": "2024-12-07T00:00:00Z",
      "winningCombination": [7, 14, 21, 28, 35, 42],
      "matchedNumbers": [7, 14, 21, 28, 35, 42],
      "matchCount": 6,
      "isExactMatch": true,
      "bonusNumber": 10,
      "powerball": 5,
      "division1Winners": 2,
      "division1Prize": 500000.00
    }
  ],
  "partialMatches": [
    {
      "drawNumber": 1995,
      "drawDate": "2024-11-30T00:00:00Z",
      "winningCombination": [7, 14, 21, 28, 35, 39],
      "matchedNumbers": [7, 14, 21, 28, 35],
      "matchCount": 5,
      "isExactMatch": false,
      "proximityScore": 0.92
    }
  ],
  "statistics": {
    "totalExactMatches": 1,
    "totalPartialMatches": 23,
    "bestPartialMatch": 5,
    "averageMatchCount": 3.2,
    "probabilityAnalysis": {
      "exactMatchOdds": "1 in 38,383,800",
      "partialMatchOdds": {
        "5of6": "1 in 55,491",
        "4of6": "1 in 1,032",
        "3of6": "1 in 57"
      }
    }
  }
}
```

### Auto-completion and Suggestions

#### GET /api/lookup/suggestions

Get search suggestions and auto-completion.

**Parameters:**
- `query` (query, required): Partial search query
- `type` (query, required): Suggestion type (`number`, `combination`, `range`)
- `maxSuggestions` (query, optional): Maximum suggestions to return (default: 10)
- `includePreview` (query, optional): Include preview information (default: true)

**Response:**
```json
{
  "query": "7,14",
  "suggestions": [
    {
      "text": "7, 14, 21",
      "type": "combination",
      "relevance": 95,
      "previewInfo": "Appeared together 12 times",
      "metadata": {
        "lastAppearance": "2024-12-07T00:00:00Z",
        "frequency": 0.012,
        "isPopular": true
      }
    },
    {
      "text": "7, 14, 28",
      "type": "combination",
      "relevance": 87,
      "previewInfo": "Appeared together 8 times",
      "metadata": {
        "lastAppearance": "2024-11-15T00:00:00Z",
        "frequency": 0.008,
        "isPopular": false
      }
    }
  ],
  "presetOptions": [
    {
      "label": "Hot Numbers (Last 30 draws)",
      "numbers": [5, 12, 18, 25, 33, 40],
      "description": "Most frequently appearing numbers recently"
    },
    {
      "label": "Cold Numbers (Last 30 draws)",
      "numbers": [3, 9, 16, 23, 31, 38],
      "description": "Least frequently appearing numbers recently"
    }
  ]
}
```

## Navigation Controller

### Draw Navigation

#### GET /api/navigation/draw/{drawNumber}

Get details for a specific draw number.

**Parameters:**
- `drawNumber` (path, required): Draw number to retrieve
- `includeContext` (query, optional): Include navigation context (default: true)

**Response:**
```json
{
  "draw": {
    "drawNumber": 1999,
    "date": "2024-12-07T00:00:00Z",
    "winningNumbers": [7, 14, 21, 28, 35, 42],
    "bonusNumber": 10,
    "powerball": 5,
    "division1Winners": 2,
    "division1Prize": 500000.00,
    "totalPrizePool": 2500000.00
  },
  "navigationContext": {
    "currentPosition": 1999,
    "totalDraws": 2000,
    "hasPrevious": true,
    "hasNext": true,
    "previousDrawNumber": 1998,
    "nextDrawNumber": 2000,
    "earliestDate": "2008-02-13T00:00:00Z",
    "latestDate": "2024-12-07T00:00:00Z",
    "missingDrawNumbers": [1456, 1457]
  }
}
```

#### GET /api/navigation/draw/date/{date}

Get the draw closest to a specific date.

**Parameters:**
- `date` (path, required): Date in YYYY-MM-DD format
- `findClosest` (query, optional): Find closest draw if exact date not found (default: true)

**Response:**
```json
{
  "requestedDate": "2024-12-07",
  "exactMatch": true,
  "draw": {
    "drawNumber": 1999,
    "date": "2024-12-07T00:00:00Z",
    "winningNumbers": [7, 14, 21, 28, 35, 42],
    "bonusNumber": 10,
    "powerball": 5
  },
  "alternativeDates": []
}
```

#### GET /api/navigation/draw/{drawNumber}/previous

Navigate to the previous draw.

**Response:**
```json
{
  "currentDraw": 1999,
  "previousDraw": {
    "drawNumber": 1998,
    "date": "2024-11-30T00:00:00Z",
    "winningNumbers": [5, 12, 18, 25, 33, 40],
    "bonusNumber": 7,
    "powerball": 3
  },
  "navigationContext": {
    "hasPrevious": true,
    "hasNext": true,
    "position": 1998,
    "totalDraws": 2000
  }
}
```

#### GET /api/navigation/draw/{drawNumber}/next

Navigate to the next draw.

**Response:**
```json
{
  "currentDraw": 1999,
  "nextDraw": {
    "drawNumber": 2000,
    "date": "2024-12-14T00:00:00Z",
    "winningNumbers": [8, 15, 22, 29, 36, 39],
    "bonusNumber": 4,
    "powerball": 6
  },
  "navigationContext": {
    "hasPrevious": true,
    "hasNext": false,
    "position": 2000,
    "totalDraws": 2000
  }
}
```

### Draw Range Operations

#### GET /api/navigation/draws/range

Get multiple draws within a date range.

**Parameters:**
- `startDate` (query, required): Start date (ISO 8601 format)
- `endDate` (query, required): End date (ISO 8601 format)
- `limit` (query, optional): Maximum draws to return (default: 50)
- `offset` (query, optional): Number of draws to skip (default: 0)

**Response:**
```json
{
  "dateRange": {
    "start": "2024-12-01T00:00:00Z",
    "end": "2024-12-07T23:59:59Z"
  },
  "draws": [
    {
      "drawNumber": 1999,
      "date": "2024-12-07T00:00:00Z",
      "winningNumbers": [7, 14, 21, 28, 35, 42],
      "bonusNumber": 10,
      "powerball": 5
    }
  ],
  "pagination": {
    "totalDraws": 2,
    "limit": 50,
    "offset": 0,
    "hasMore": false
  }
}
```

### Bookmarks

#### POST /api/navigation/bookmarks

Create a new bookmark.

**Request Body:**
```json
{
  "drawNumber": 1999,
  "label": "Interesting Pattern",
  "description": "All numbers are multiples of 7",
  "category": "patterns"
}
```

**Response:**
```json
{
  "bookmarkId": 123,
  "drawNumber": 1999,
  "label": "Interesting Pattern",
  "description": "All numbers are multiples of 7",
  "category": "patterns",
  "createdAt": "2024-12-17T10:30:00Z",
  "draw": {
    "drawNumber": 1999,
    "date": "2024-12-07T00:00:00Z",
    "winningNumbers": [7, 14, 21, 28, 35, 42]
  }
}
```

#### GET /api/navigation/bookmarks

Get user bookmarks.

**Parameters:**
- `category` (query, optional): Filter by category
- `limit` (query, optional): Maximum bookmarks to return (default: 50)
- `sortBy` (query, optional): Sort field (`date`, `label`, `created`) (default: `created`)
- `sortOrder` (query, optional): Sort order (`asc`, `desc`) (default: `desc`)

**Response:**
```json
{
  "bookmarks": [
    {
      "bookmarkId": 123,
      "drawNumber": 1999,
      "label": "Interesting Pattern",
      "description": "All numbers are multiples of 7",
      "category": "patterns",
      "createdAt": "2024-12-17T10:30:00Z",
      "draw": {
        "drawNumber": 1999,
        "date": "2024-12-07T00:00:00Z",
        "winningNumbers": [7, 14, 21, 28, 35, 42]
      }
    }
  ],
  "totalBookmarks": 15,
  "categories": ["patterns", "research", "personal", "high-matches"]
}
```

#### DELETE /api/navigation/bookmarks/{bookmarkId}

Delete a bookmark.

**Response:**
```json
{
  "success": true,
  "message": "Bookmark deleted successfully",
  "deletedBookmarkId": 123
}
```

## Frequency Controller

### Number Frequency Analysis

#### GET /api/frequency/numbers

Get frequency analysis for all numbers.

**Parameters:**
- `startDate` (query, optional): Start date filter
- `endDate` (query, optional): End date filter
- `includeBonus` (query, optional): Include bonus numbers (default: false)
- `includePowerball` (query, optional): Include powerball numbers (default: false)
- `sortBy` (query, optional): Sort field (`frequency`, `number`, `lastAppearance`) (default: `frequency`)
- `sortOrder` (query, optional): Sort order (`asc`, `desc`) (default: `desc`)

**Response:**
```json
{
  "analysisDate": "2024-12-17T10:30:00Z",
  "dateRange": {
    "start": "2008-02-13T00:00:00Z",
    "end": "2024-12-07T00:00:00Z"
  },
  "totalDraws": 2000,
  "numberFrequencies": [
    {
      "number": 7,
      "totalOccurrences": 156,
      "percentage": 15.6,
      "lastAppearance": "2024-12-07T00:00:00Z",
      "firstAppearance": "2008-02-20T00:00:00Z",
      "longestGap": 45,
      "currentGap": 0,
      "averageGap": 6.4,
      "isHot": true,
      "isCold": false,
      "trend": "increasing"
    }
  ],
  "statistics": {
    "averageFrequency": 12.8,
    "mostFrequent": {
      "number": 7,
      "occurrences": 156
    },
    "leastFrequent": {
      "number": 39,
      "occurrences": 89
    },
    "hotNumbers": [7, 14, 21, 28, 35],
    "coldNumbers": [3, 9, 16, 23, 31]
  }
}
```

### Range Frequency Analysis

#### POST /api/frequency/ranges

Analyze frequency patterns across number ranges.

**Request Body:**
```json
{
  "ranges": [
    {
      "startNumber": 1,
      "endNumber": 10,
      "label": "Low Numbers"
    },
    {
      "startNumber": 11,
      "endNumber": 30,
      "label": "Mid Numbers"
    },
    {
      "startNumber": 31,
      "endNumber": 40,
      "label": "High Numbers"
    }
  ],
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-12-31T23:59:59Z",
  "includeBonus": false,
  "includePowerball": false
}
```

**Response:**
```json
{
  "analysisDate": "2024-12-17T10:30:00Z",
  "rangeFrequencies": [
    {
      "range": {
        "startNumber": 1,
        "endNumber": 10,
        "label": "Low Numbers"
      },
      "totalOccurrences": 1245,
      "percentage": 20.75,
      "averagePerDraw": 2.07,
      "individualNumbers": [
        {
          "number": 1,
          "occurrences": 125,
          "percentage": 10.04
        }
      ],
      "trend": "stable",
      "seasonalVariation": 0.12
    }
  ],
  "comparison": {
    "mostActive": "Mid Numbers",
    "leastActive": "High Numbers",
    "evenDistribution": false,
    "statisticalSignificance": 0.95
  }
}
```

### Hot and Cold Analysis

#### GET /api/frequency/hot-cold

Get hot and cold number analysis.

**Parameters:**
- `periodDays` (query, optional): Analysis period in days (default: 365)
- `threshold` (query, optional): Hot/cold threshold multiplier (default: 1.2)

**Response:**
```json
{
  "analysisPeriod": {
    "days": 365,
    "startDate": "2023-12-17T00:00:00Z",
    "endDate": "2024-12-17T00:00:00Z",
    "totalDraws": 52
  },
  "hotNumbers": [
    {
      "number": 7,
      "recentOccurrences": 12,
      "historicalAverage": 8.2,
      "hotColdScore": 1.46,
      "classification": "Hot",
      "trend": "increasing",
      "lastAppearance": "2024-12-07T00:00:00Z"
    }
  ],
  "coldNumbers": [
    {
      "number": 39,
      "recentOccurrences": 3,
      "historicalAverage": 8.2,
      "hotColdScore": 0.37,
      "classification": "Cold",
      "trend": "decreasing",
      "lastAppearance": "2024-10-15T00:00:00Z"
    }
  ],
  "normalNumbers": [
    {
      "number": 15,
      "recentOccurrences": 8,
      "historicalAverage": 8.2,
      "hotColdScore": 0.98,
      "classification": "Normal"
    }
  ],
  "statistics": {
    "hotThreshold": 9.84,
    "coldThreshold": 6.56,
    "totalHot": 8,
    "totalCold": 12,
    "totalNormal": 20
  }
}
```

## Cache Controller

### Cache Management

#### GET /api/cache/status

Get cache status and statistics.

**Response:**
```json
{
  "cacheStatus": "healthy",
  "statistics": {
    "totalKeys": 1247,
    "memoryUsage": "245MB",
    "hitRate": 0.87,
    "missRate": 0.13,
    "evictionCount": 23
  },
  "cacheTypes": {
    "numberLookup": {
      "keys": 456,
      "hitRate": 0.92,
      "averageResponseTime": "12ms"
    },
    "frequencyAnalysis": {
      "keys": 234,
      "hitRate": 0.78,
      "averageResponseTime": "45ms"
    },
    "navigationContext": {
      "keys": 557,
      "hitRate": 0.95,
      "averageResponseTime": "8ms"
    }
  },
  "performance": {
    "averageCacheResponseTime": "15ms",
    "averageDatabaseResponseTime": "125ms",
    "performanceImprovement": "8.3x"
  }
}
```

#### POST /api/cache/warm-up

Warm up cache with frequently accessed data.

**Request Body:**
```json
{
  "cacheTypes": ["numberLookup", "frequencyAnalysis", "navigationContext"],
  "priority": "high",
  "backgroundProcess": true
}
```

**Response:**
```json
{
  "warmupJobId": "warmup_20241217_103000",
  "status": "initiated",
  "estimatedDuration": "5 minutes",
  "cacheTypes": ["numberLookup", "frequencyAnalysis", "navigationContext"],
  "progress": {
    "numberLookup": "pending",
    "frequencyAnalysis": "pending",
    "navigationContext": "pending"
  }
}
```

#### DELETE /api/cache/invalidate

Invalidate cache entries.

**Request Body:**
```json
{
  "cacheTypes": ["numberLookup"],
  "keys": ["number_7", "number_14"],
  "invalidateAll": false
}
```

**Response:**
```json
{
  "success": true,
  "invalidatedKeys": 2,
  "cacheTypes": ["numberLookup"],
  "message": "Cache entries invalidated successfully"
}
```

## Data Models

### Core Models

#### NumberOccurrence
```typescript
interface NumberOccurrence {
  drawNumber: number;
  drawDate: string; // ISO 8601
  number: number;
  position: number; // 1-6 for main numbers, 7 for bonus, 8 for powerball
  isBonus: boolean;
  isPowerball: boolean;
  fullCombination: number[];
  bonusNumber?: number;
  powerball?: number;
}
```

#### LottoDrawDto
```typescript
interface LottoDrawDto {
  drawNumber: number;
  date: string; // ISO 8601
  winningNumbers: number[];
  bonusNumber: number;
  powerball: number;
  division1Winners?: number;
  division1Prize?: number;
  totalPrizePool?: number;
}
```

#### NumberFrequency
```typescript
interface NumberFrequency {
  number: number;
  totalOccurrences: number;
  percentage: number;
  lastAppearance: string; // ISO 8601
  firstAppearance: string; // ISO 8601
  longestGap: number;
  currentGap: number;
  averageGap: number;
  isHot: boolean;
  isCold: boolean;
  trend: 'increasing' | 'decreasing' | 'stable';
}
```

### Request Models

#### NumberLookupRequest
```typescript
interface NumberLookupRequest {
  numbers: number[];
  startDate?: string; // ISO 8601
  endDate?: string; // ISO 8601
  includeBonus: boolean;
  includePowerball: boolean;
  combineResults: boolean;
}
```

#### CombinationSearchRequest
```typescript
interface CombinationSearchRequest {
  combination: number[];
  includePartialMatches: boolean;
  minimumMatches: number;
  startDate?: string; // ISO 8601
  endDate?: string; // ISO 8601
  maxResults: number;
}
```

#### FrequencyAnalysisRequest
```typescript
interface FrequencyAnalysisRequest {
  ranges: NumberRange[];
  startDate?: string; // ISO 8601
  endDate?: string; // ISO 8601
  includeBonus: boolean;
  includePowerball: boolean;
}

interface NumberRange {
  startNumber: number;
  endNumber: number;
  label: string;
}
```

### Response Models

#### CombinationSearchResult
```typescript
interface CombinationSearchResult {
  searchedCombination: number[];
  exactMatches: CombinationMatch[];
  partialMatches: CombinationMatch[];
  statistics: {
    totalExactMatches: number;
    totalPartialMatches: number;
    bestPartialMatch: number;
    averageMatchCount: number;
    probabilityAnalysis: ProbabilityAnalysis;
  };
}

interface CombinationMatch {
  drawNumber: number;
  drawDate: string; // ISO 8601
  winningCombination: number[];
  matchedNumbers: number[];
  matchCount: number;
  isExactMatch: boolean;
  proximityScore?: number;
  bonusNumber?: number;
  powerball?: number;
}
```

#### NavigationContext
```typescript
interface NavigationContext {
  currentPosition: number;
  totalDraws: number;
  hasPrevious: boolean;
  hasNext: boolean;
  previousDrawNumber?: number;
  nextDrawNumber?: number;
  earliestDate: string; // ISO 8601
  latestDate: string; // ISO 8601
  missingDrawNumbers: number[];
}
```

#### Bookmark
```typescript
interface Bookmark {
  bookmarkId: number;
  drawNumber: number;
  label: string;
  description?: string;
  category?: string;
  createdAt: string; // ISO 8601
  updatedAt?: string; // ISO 8601
  draw: LottoDrawDto;
}
```

## Error Handling

### Standard Error Response Format

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid number range",
    "details": "Numbers must be between 1 and 40",
    "timestamp": "2024-12-17T10:30:00Z",
    "path": "/api/lookup/number/45",
    "requestId": "req_20241217_103000_abc123"
  }
}
```

### Error Codes

| Code | Description | HTTP Status |
|------|-------------|-------------|
| `VALIDATION_ERROR` | Input validation failed | 400 |
| `NUMBER_OUT_OF_RANGE` | Number not in valid range (1-40) | 400 |
| `INVALID_DATE_RANGE` | Invalid or malformed date range | 400 |
| `DRAW_NOT_FOUND` | Requested draw does not exist | 404 |
| `BOOKMARK_NOT_FOUND` | Bookmark does not exist | 404 |
| `CACHE_ERROR` | Cache operation failed | 500 |
| `DATABASE_ERROR` | Database operation failed | 500 |
| `RATE_LIMIT_EXCEEDED` | Too many requests | 429 |

### Validation Rules

#### Number Validation
- Numbers must be integers between 1 and 40 (inclusive)
- Combinations must contain 2-6 unique numbers
- Duplicate numbers in combinations are not allowed

#### Date Validation
- Dates must be in ISO 8601 format (YYYY-MM-DDTHH:mm:ssZ)
- Start date must be before or equal to end date
- Dates must be within available data range (2008-02-13 to present)

#### Range Validation
- Number ranges must have startNumber ≤ endNumber
- Range boundaries must be within 1-40
- Range labels must be non-empty strings

## Rate Limiting

### Current Limits

| Endpoint Category | Requests per Minute | Burst Limit |
|------------------|-------------------|-------------|
| Number Lookup | 60 | 10 |
| Combination Search | 30 | 5 |
| Frequency Analysis | 20 | 3 |
| Navigation | 120 | 20 |
| Cache Operations | 10 | 2 |

### Rate Limit Headers

```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1702814400
X-RateLimit-Retry-After: 60
```

### Rate Limit Response

```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded",
    "details": "Maximum 60 requests per minute allowed",
    "retryAfter": 60,
    "timestamp": "2024-12-17T10:30:00Z"
  }
}
```

## Examples

### Complete Workflow Example

```bash
# 1. Search for a specific number
curl "http://localhost:5000/api/lookup/number/7?startDate=2024-01-01"

# 2. Search for a combination
curl -X POST "http://localhost:5000/api/lookup/combination" \
  -H "Content-Type: application/json" \
  -d '{
    "combination": [7, 14, 21, 28, 35, 42],
    "includePartialMatches": true,
    "minimumMatches": 3
  }'

# 3. Get frequency analysis
curl "http://localhost:5000/api/frequency/numbers?sortBy=frequency&sortOrder=desc"

# 4. Navigate to a specific draw
curl "http://localhost:5000/api/navigation/draw/1999"

# 5. Create a bookmark
curl -X POST "http://localhost:5000/api/navigation/bookmarks" \
  -H "Content-Type: application/json" \
  -d '{
    "drawNumber": 1999,
    "label": "Perfect Match",
    "description": "My combination matched exactly!"
  }'

# 6. Get hot and cold numbers
curl "http://localhost:5000/api/frequency/hot-cold?periodDays=90"
```

### JavaScript/TypeScript Client Example

```typescript
class LotteryLookupClient {
  constructor(private baseUrl: string) {}

  async lookupNumber(number: number, options?: {
    startDate?: string;
    endDate?: string;
    includeBonus?: boolean;
  }): Promise<NumberLookupResult> {
    const params = new URLSearchParams();
    if (options?.startDate) params.append('startDate', options.startDate);
    if (options?.endDate) params.append('endDate', options.endDate);
    if (options?.includeBonus !== undefined) {
      params.append('includeBonus', options.includeBonus.toString());
    }

    const response = await fetch(
      `${this.baseUrl}/api/lookup/number/${number}?${params}`
    );
    return response.json();
  }

  async searchCombination(request: CombinationSearchRequest): Promise<CombinationSearchResult> {
    const response = await fetch(`${this.baseUrl}/api/lookup/combination`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    });
    return response.json();
  }

  async getFrequencyAnalysis(options?: {
    sortBy?: string;
    sortOrder?: string;
    startDate?: string;
    endDate?: string;
  }): Promise<FrequencyAnalysisResult> {
    const params = new URLSearchParams();
    if (options?.sortBy) params.append('sortBy', options.sortBy);
    if (options?.sortOrder) params.append('sortOrder', options.sortOrder);
    if (options?.startDate) params.append('startDate', options.startDate);
    if (options?.endDate) params.append('endDate', options.endDate);

    const response = await fetch(
      `${this.baseUrl}/api/frequency/numbers?${params}`
    );
    return response.json();
  }

  async navigateToDraw(drawNumber: number): Promise<DrawNavigationResult> {
    const response = await fetch(
      `${this.baseUrl}/api/navigation/draw/${drawNumber}`
    );
    return response.json();
  }

  async createBookmark(bookmark: CreateBookmarkRequest): Promise<Bookmark> {
    const response = await fetch(`${this.baseUrl}/api/navigation/bookmarks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(bookmark)
    });
    return response.json();
  }
}

// Usage example
const client = new LotteryLookupClient('http://localhost:5000');

// Look up number 7
const numberResult = await client.lookupNumber(7, {
  startDate: '2024-01-01T00:00:00Z',
  includeBonus: true
});

// Search for combination
const combinationResult = await client.searchCombination({
  combination: [7, 14, 21, 28, 35, 42],
  includePartialMatches: true,
  minimumMatches: 3,
  maxResults: 50
});

// Get frequency analysis
const frequencyResult = await client.getFrequencyAnalysis({
  sortBy: 'frequency',
  sortOrder: 'desc'
});
```

### Python Client Example

```python
import requests
from typing import List, Dict, Optional
from datetime import datetime

class LotteryLookupClient:
    def __init__(self, base_url: str):
        self.base_url = base_url

    def lookup_number(self, number: int, start_date: Optional[str] = None, 
                     end_date: Optional[str] = None, include_bonus: bool = True) -> Dict:
        params = {'includeBonus': include_bonus}
        if start_date:
            params['startDate'] = start_date
        if end_date:
            params['endDate'] = end_date
            
        response = requests.get(
            f"{self.base_url}/api/lookup/number/{number}",
            params=params
        )
        response.raise_for_status()
        return response.json()

    def search_combination(self, combination: List[int], 
                          include_partial: bool = True,
                          minimum_matches: int = 2) -> Dict:
        data = {
            'combination': combination,
            'includePartialMatches': include_partial,
            'minimumMatches': minimum_matches
        }
        
        response = requests.post(
            f"{self.base_url}/api/lookup/combination",
            json=data
        )
        response.raise_for_status()
        return response.json()

    def get_frequency_analysis(self, sort_by: str = 'frequency', 
                              sort_order: str = 'desc') -> Dict:
        params = {
            'sortBy': sort_by,
            'sortOrder': sort_order
        }
        
        response = requests.get(
            f"{self.base_url}/api/frequency/numbers",
            params=params
        )
        response.raise_for_status()
        return response.json()

    def navigate_to_draw(self, draw_number: int) -> Dict:
        response = requests.get(
            f"{self.base_url}/api/navigation/draw/{draw_number}"
        )
        response.raise_for_status()
        return response.json()

    def create_bookmark(self, draw_number: int, label: str, 
                       description: str = None, category: str = None) -> Dict:
        data = {
            'drawNumber': draw_number,
            'label': label
        }
        if description:
            data['description'] = description
        if category:
            data['category'] = category
            
        response = requests.post(
            f"{self.base_url}/api/navigation/bookmarks",
            json=data
        )
        response.raise_for_status()
        return response.json()

# Usage example
client = LotteryLookupClient('http://localhost:5000')

# Look up number 7
number_result = client.lookup_number(7, start_date='2024-01-01T00:00:00Z')
print(f"Number 7 appeared {number_result['totalOccurrences']} times")

# Search for combination
combination_result = client.search_combination([7, 14, 21, 28, 35, 42])
print(f"Found {len(combination_result['exactMatches'])} exact matches")

# Get frequency analysis
frequency_result = client.get_frequency_analysis()
hot_numbers = [nf['number'] for nf in frequency_result['numberFrequencies'][:5]]
print(f"Top 5 most frequent numbers: {hot_numbers}")
```

---

*This API documentation provides comprehensive coverage of all Lottery Lookup and Navigation endpoints. For user-facing documentation, see the User Guide. For implementation details, see the Developer Documentation.*