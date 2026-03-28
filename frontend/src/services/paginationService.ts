import api from './api'
import { loadingStateService, LoadingOperationType } from './loadingStateService'

// Pagination interfaces
export interface PaginationRequest {
  page: number
  pageSize: number
}

export interface SortedPaginationRequest extends PaginationRequest {
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  itemCount: number
  firstItemIndex: number
  lastItemIndex: number
  metadata: PaginationMetadata
}

export interface PaginationMetadata {
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  firstItemIndex: number
  lastItemIndex: number
}

export interface NumberLookupPaginationRequest extends SortedPaginationRequest {
  numbers: number[]
  startDate?: string
  endDate?: string
  includeBonus?: boolean
  includePowerball?: boolean
}

export interface CombinationSearchPaginationRequest extends SortedPaginationRequest {
  combination: number[]
  includePartialMatches?: boolean
  minimumMatches?: number
  startDate?: string
  endDate?: string
}

// Types from lookup service
export interface NumberOccurrence {
  drawNumber: number
  drawDate: string
  number: number
  position: number
  isBonus: boolean
  isPowerball: boolean
  fullCombination: number[]
}

export interface CombinationMatch {
  drawNumber: number
  drawDate: string
  winningCombination: number[]
  matchedNumbers: number[]
  matchCount: number
  isExactMatch: boolean
}

export class PaginationService {
  private cache = new Map<string, { data: any; timestamp: number; ttl: number }>()
  private readonly defaultTTL = 5 * 60 * 1000 // 5 minutes

  /**
   * Look up a single number with pagination
   */
  async lookupNumberPaginated(
    number: number,
    pagination: PaginationRequest
  ): Promise<PaginatedResponse<NumberOccurrence>> {
    const cacheKey = `number_${number}_${pagination.page}_${pagination.pageSize}`
    
    // Check cache first
    const cached = this.getFromCache<PaginatedResponse<NumberOccurrence>>(cacheKey)
    if (cached) {
      return cached
    }

    return loadingStateService.withLoading(
      LoadingOperationType.NUMBER_LOOKUP,
      `Looking up number ${number} (page ${pagination.page})`,
      async (updateProgress) => {
        updateProgress(25, 'Sending request...')
        
        const response = await api.get(`/lookup/number/${number}/paginated`, {
          params: {
            page: pagination.page,
            pageSize: pagination.pageSize
          }
        })
        
        updateProgress(75, 'Processing results...')
        
        const result = response.data as PaginatedResponse<NumberOccurrence>
        
        // Cache the result
        this.setCache(cacheKey, result)
        
        updateProgress(100, 'Complete')
        return result
      }
    )
  }

  /**
   * Look up multiple numbers with pagination and filtering
   */
  async lookupNumbersPaginated(
    request: NumberLookupPaginationRequest
  ): Promise<PaginatedResponse<NumberOccurrence>> {
    const cacheKey = this.generateCacheKey('numbers', request)
    
    // Check cache first
    const cached = this.getFromCache<PaginatedResponse<NumberOccurrence>>(cacheKey)
    if (cached) {
      return cached
    }

    return loadingStateService.withLoading(
      LoadingOperationType.NUMBER_LOOKUP,
      `Looking up ${request.numbers.length} numbers (page ${request.page})`,
      async (updateProgress) => {
        updateProgress(25, 'Sending request...')
        
        const response = await api.post('/lookup/numbers/paginated', request)
        
        updateProgress(75, 'Processing results...')
        
        const result = response.data as PaginatedResponse<NumberOccurrence>
        
        // Cache the result
        this.setCache(cacheKey, result)
        
        updateProgress(100, 'Complete')
        return result
      },
      {
        estimatedDuration: 2000, // 2 seconds
        totalItems: request.numbers.length
      }
    )
  }

  /**
   * Search combinations with pagination
   */
  async searchCombinationPaginated(
    request: CombinationSearchPaginationRequest
  ): Promise<PaginatedResponse<CombinationMatch>> {
    const cacheKey = this.generateCacheKey('combination', request)
    
    // Check cache first
    const cached = this.getFromCache<PaginatedResponse<CombinationMatch>>(cacheKey)
    if (cached) {
      return cached
    }

    return loadingStateService.withLoading(
      LoadingOperationType.COMBINATION_SEARCH,
      `Searching combination [${request.combination.join(', ')}] (page ${request.page})`,
      async (updateProgress) => {
        updateProgress(20, 'Sending request...')
        
        const response = await api.post('/lookup/combination/paginated', request)
        
        updateProgress(60, 'Analyzing matches...')
        
        const result = response.data as PaginatedResponse<CombinationMatch>
        
        updateProgress(90, 'Caching results...')
        
        // Cache the result
        this.setCache(cacheKey, result)
        
        updateProgress(100, 'Complete')
        return result
      },
      {
        estimatedDuration: 3000, // 3 seconds for combination search
        canCancel: true
      }
    )
  }

  /**
   * Create pagination navigation info
   */
  createPaginationInfo(response: PaginatedResponse<any>): {
    currentPage: number
    totalPages: number
    hasNext: boolean
    hasPrevious: boolean
    pageNumbers: number[]
    showingText: string
  } {
    const { page, totalPages, hasPreviousPage, hasNextPage, firstItemIndex, lastItemIndex, totalItems } = response

    // Generate page numbers for pagination controls (show 5 pages around current)
    const pageNumbers: number[] = []
    const startPage = Math.max(1, page - 2)
    const endPage = Math.min(totalPages, page + 2)
    
    for (let i = startPage; i <= endPage; i++) {
      pageNumbers.push(i)
    }

    // Add first and last pages if not already included
    if (startPage > 1) {
      pageNumbers.unshift(1)
      if (startPage > 2) {
        pageNumbers.splice(1, 0, -1) // -1 represents ellipsis
      }
    }
    
    if (endPage < totalPages) {
      if (endPage < totalPages - 1) {
        pageNumbers.push(-1) // -1 represents ellipsis
      }
      pageNumbers.push(totalPages)
    }

    const showingText = totalItems > 0 
      ? `Showing ${firstItemIndex}-${lastItemIndex} of ${totalItems} results`
      : 'No results found'

    return {
      currentPage: page,
      totalPages,
      hasNext: hasNextPage,
      hasPrevious: hasPreviousPage,
      pageNumbers,
      showingText
    }
  }

  /**
   * Calculate optimal page size based on viewport and content type
   */
  calculateOptimalPageSize(contentType: 'compact' | 'detailed' | 'cards' = 'detailed'): number {
    const viewportHeight = window.innerHeight
    const headerHeight = 120 // Approximate header height
    const footerHeight = 80 // Approximate footer height
    const availableHeight = viewportHeight - headerHeight - footerHeight

    const itemHeights = {
      compact: 40,   // Simple list items
      detailed: 120, // Detailed cards with multiple lines
      cards: 200    // Large cards with images/charts
    }

    const itemHeight = itemHeights[contentType]
    const optimalCount = Math.floor(availableHeight / itemHeight)
    
    // Ensure reasonable bounds
    return Math.max(10, Math.min(100, optimalCount))
  }

  /**
   * Prefetch next page for better UX
   */
  async prefetchNextPage<T>(
    currentRequest: any,
    fetchFunction: (request: any) => Promise<PaginatedResponse<T>>
  ): Promise<void> {
    if (!currentRequest.hasNextPage) {
      return
    }

    try {
      const nextPageRequest = { ...currentRequest, page: currentRequest.page + 1 }
      const cacheKey = this.generateCacheKey('prefetch', nextPageRequest)
      
      // Only prefetch if not already cached
      if (!this.getFromCache(cacheKey)) {
        const result = await fetchFunction(nextPageRequest)
        this.setCache(cacheKey, result, this.defaultTTL / 2) // Shorter TTL for prefetched data
      }
    } catch (error) {
      // Silently fail prefetch - it's not critical
      console.debug('Prefetch failed:', error)
    }
  }

  /**
   * Clear pagination cache
   */
  clearCache(pattern?: string): void {
    if (pattern) {
      for (const key of this.cache.keys()) {
        if (key.includes(pattern)) {
          this.cache.delete(key)
        }
      }
    } else {
      this.cache.clear()
    }
  }

  /**
   * Get cache statistics
   */
  getCacheStats(): { size: number; hitRate: number; entries: string[] } {
    const entries = Array.from(this.cache.keys())
    return {
      size: this.cache.size,
      hitRate: 0, // Would need to track hits/misses for accurate calculation
      entries
    }
  }

  private getFromCache<T>(key: string): T | null {
    const entry = this.cache.get(key)
    if (!entry) {
      return null
    }

    // Check if expired
    if (Date.now() > entry.timestamp + entry.ttl) {
      this.cache.delete(key)
      return null
    }

    return entry.data as T
  }

  private setCache<T>(key: string, data: T, ttl: number = this.defaultTTL): void {
    // Implement LRU eviction if cache gets too large
    if (this.cache.size >= 100) {
      const oldestKey = this.cache.keys().next().value
      this.cache.delete(oldestKey)
    }

    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl
    })
  }

  private generateCacheKey(type: string, request: any): string {
    // Create a stable cache key from the request object
    const keyParts = [type]
    
    if (request.numbers) {
      keyParts.push(`nums_${request.numbers.sort().join('_')}`)
    }
    
    if (request.combination) {
      keyParts.push(`combo_${request.combination.sort().join('_')}`)
    }
    
    keyParts.push(`page_${request.page}`)
    keyParts.push(`size_${request.pageSize}`)
    
    if (request.sortBy) {
      keyParts.push(`sort_${request.sortBy}_${request.sortDirection || 'asc'}`)
    }
    
    if (request.startDate) {
      keyParts.push(`start_${request.startDate}`)
    }
    
    if (request.endDate) {
      keyParts.push(`end_${request.endDate}`)
    }
    
    return keyParts.join('_')
  }
}

// Create singleton instance
export const paginationService = new PaginationService()

// Vue composable for pagination
export function usePagination() {
  return {
    lookupNumberPaginated: paginationService.lookupNumberPaginated.bind(paginationService),
    lookupNumbersPaginated: paginationService.lookupNumbersPaginated.bind(paginationService),
    searchCombinationPaginated: paginationService.searchCombinationPaginated.bind(paginationService),
    createPaginationInfo: paginationService.createPaginationInfo.bind(paginationService),
    calculateOptimalPageSize: paginationService.calculateOptimalPageSize.bind(paginationService),
    prefetchNextPage: paginationService.prefetchNextPage.bind(paginationService),
    clearCache: paginationService.clearCache.bind(paginationService),
    getCacheStats: paginationService.getCacheStats.bind(paginationService)
  }
}