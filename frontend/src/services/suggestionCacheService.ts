import type { SearchSuggestion } from './lookupService'

interface CacheEntry {
  suggestions: SearchSuggestion[]
  timestamp: number
  searchType: string
  expiresAt: number
}

interface CacheStats {
  hits: number
  misses: number
  totalRequests: number
  cacheSize: number
  hitRate: number
}

export class SuggestionCacheService {
  private cache = new Map<string, CacheEntry>()
  private readonly defaultTTL = 5 * 60 * 1000 // 5 minutes
  private readonly maxCacheSize = 1000
  private stats: CacheStats = {
    hits: 0,
    misses: 0,
    totalRequests: 0,
    cacheSize: 0,
    hitRate: 0
  }

  /**
   * Get cached suggestions for a query
   */
  get(query: string, searchType: string): SearchSuggestion[] | null {
    this.stats.totalRequests++
    
    const cacheKey = this.generateCacheKey(query, searchType)
    const entry = this.cache.get(cacheKey)
    
    if (!entry) {
      this.stats.misses++
      this.updateHitRate()
      return null
    }
    
    // Check if entry has expired
    if (Date.now() > entry.expiresAt) {
      this.cache.delete(cacheKey)
      this.stats.misses++
      this.updateHitRate()
      return null
    }
    
    this.stats.hits++
    this.updateHitRate()
    return entry.suggestions
  }

  /**
   * Store suggestions in cache
   */
  set(
    query: string, 
    searchType: string, 
    suggestions: SearchSuggestion[], 
    ttl?: number
  ): void {
    // Don't cache empty results or very short queries
    if (suggestions.length === 0 || query.length < 2) {
      return
    }
    
    const cacheKey = this.generateCacheKey(query, searchType)
    const expirationTime = ttl || this.defaultTTL
    
    const entry: CacheEntry = {
      suggestions: [...suggestions], // Clone to avoid mutations
      timestamp: Date.now(),
      searchType,
      expiresAt: Date.now() + expirationTime
    }
    
    // Implement LRU eviction if cache is full
    if (this.cache.size >= this.maxCacheSize) {
      this.evictOldestEntries()
    }
    
    this.cache.set(cacheKey, entry)
    this.stats.cacheSize = this.cache.size
  }

  /**
   * Check if suggestions are cached for a query
   */
  has(query: string, searchType: string): boolean {
    const cacheKey = this.generateCacheKey(query, searchType)
    const entry = this.cache.get(cacheKey)
    
    if (!entry) {
      return false
    }
    
    // Check expiration
    if (Date.now() > entry.expiresAt) {
      this.cache.delete(cacheKey)
      return false
    }
    
    return true
  }

  /**
   * Clear all cached suggestions
   */
  clear(): void {
    this.cache.clear()
    this.stats.cacheSize = 0
  }

  /**
   * Clear expired entries
   */
  clearExpired(): void {
    const now = Date.now()
    const expiredKeys: string[] = []
    
    for (const [key, entry] of this.cache.entries()) {
      if (now > entry.expiresAt) {
        expiredKeys.push(key)
      }
    }
    
    expiredKeys.forEach(key => this.cache.delete(key))
    this.stats.cacheSize = this.cache.size
  }

  /**
   * Clear suggestions for a specific search type
   */
  clearByType(searchType: string): void {
    const keysToDelete: string[] = []
    
    for (const [key, entry] of this.cache.entries()) {
      if (entry.searchType === searchType) {
        keysToDelete.push(key)
      }
    }
    
    keysToDelete.forEach(key => this.cache.delete(key))
    this.stats.cacheSize = this.cache.size
  }

  /**
   * Get cache statistics
   */
  getStats(): CacheStats {
    return { ...this.stats }
  }

  /**
   * Reset cache statistics
   */
  resetStats(): void {
    this.stats = {
      hits: 0,
      misses: 0,
      totalRequests: 0,
      cacheSize: this.cache.size,
      hitRate: 0
    }
  }

  /**
   * Get all cached queries for debugging
   */
  getCachedQueries(): Array<{ query: string; type: string; timestamp: number; expiresAt: number }> {
    const queries: Array<{ query: string; type: string; timestamp: number; expiresAt: number }> = []
    
    for (const [key, entry] of this.cache.entries()) {
      const [query, type] = this.parseCacheKey(key)
      queries.push({
        query,
        type,
        timestamp: entry.timestamp,
        expiresAt: entry.expiresAt
      })
    }
    
    return queries.sort((a, b) => b.timestamp - a.timestamp)
  }

  /**
   * Prefetch suggestions for common queries
   */
  async prefetch(
    commonQueries: Array<{ query: string; type: string }>,
    fetchFunction: (query: string, type: string) => Promise<SearchSuggestion[]>
  ): Promise<void> {
    const prefetchPromises = commonQueries.map(async ({ query, type }) => {
      if (!this.has(query, type)) {
        try {
          const suggestions = await fetchFunction(query, type)
          this.set(query, type, suggestions)
        } catch (error) {
          console.warn(`Failed to prefetch suggestions for "${query}" (${type}):`, error)
        }
      }
    })
    
    await Promise.allSettled(prefetchPromises)
  }

  /**
   * Get suggestions with partial matching for better cache utilization
   */
  getPartialMatch(query: string, searchType: string): SearchSuggestion[] | null {
    // Try exact match first
    const exactMatch = this.get(query, searchType)
    if (exactMatch) {
      return exactMatch
    }
    
    // For number and combination types, try to find cached results for shorter queries
    if ((searchType === 'number' || searchType === 'combination') && query.length > 2) {
      for (let i = query.length - 1; i >= 2; i--) {
        const partialQuery = query.substring(0, i)
        const partialMatch = this.get(partialQuery, searchType)
        
        if (partialMatch) {
          // Filter results to match the longer query
          const filteredSuggestions = partialMatch.filter(suggestion =>
            suggestion.text.toLowerCase().includes(query.toLowerCase())
          )
          
          if (filteredSuggestions.length > 0) {
            return filteredSuggestions
          }
        }
      }
    }
    
    return null
  }

  /**
   * Warm up cache with popular searches
   */
  warmUp(
    popularSearches: Array<{ query: string; type: string; suggestions: SearchSuggestion[] }>
  ): void {
    popularSearches.forEach(({ query, type, suggestions }) => {
      this.set(query, type, suggestions, this.defaultTTL * 2) // Longer TTL for popular searches
    })
  }

  private generateCacheKey(query: string, searchType: string): string {
    return `${searchType}:${query.toLowerCase().trim()}`
  }

  private parseCacheKey(cacheKey: string): [string, string] {
    const [type, query] = cacheKey.split(':')
    return [query, type]
  }

  private updateHitRate(): void {
    this.stats.hitRate = this.stats.totalRequests > 0 
      ? (this.stats.hits / this.stats.totalRequests) * 100 
      : 0
  }

  private evictOldestEntries(): void {
    // Remove 10% of oldest entries to make room
    const entriesToRemove = Math.floor(this.maxCacheSize * 0.1)
    const sortedEntries = Array.from(this.cache.entries())
      .sort(([, a], [, b]) => a.timestamp - b.timestamp)
    
    for (let i = 0; i < entriesToRemove && i < sortedEntries.length; i++) {
      this.cache.delete(sortedEntries[i][0])
    }
  }
}

// Create singleton instance
export const suggestionCache = new SuggestionCacheService()

// Auto-cleanup expired entries every 5 minutes
setInterval(() => {
  suggestionCache.clearExpired()
}, 5 * 60 * 1000)

// Popular searches for warm-up (these would typically come from analytics)
export const popularSearches = [
  { query: '1', type: 'number' },
  { query: '7', type: 'number' },
  { query: '13', type: 'number' },
  { query: '21', type: 'number' },
  { query: '1-10', type: 'range' },
  { query: '11-20', type: 'range' },
  { query: '21-30', type: 'range' },
  { query: '31-40', type: 'range' },
  { query: '1, 7, 13', type: 'combination' },
  { query: '2023', type: 'date' }
]