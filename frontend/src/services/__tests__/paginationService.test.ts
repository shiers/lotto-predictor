import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { paginationService } from '../paginationService'
import type { PaginatedResponse, NumberOccurrence, CombinationMatch } from '../paginationService'

// Mock API
const mockApi = {
  get: vi.fn(),
  post: vi.fn()
}

vi.mock('../api', () => ({
  default: mockApi
}))

// Mock loading state service
vi.mock('../loadingStateService', () => ({
  loadingStateService: {
    withLoading: vi.fn((type, message, operation) => operation(() => {}))
  },
  LoadingOperationType: {
    NUMBER_LOOKUP: 'number_lookup',
    COMBINATION_SEARCH: 'combination_search'
  }
}))

describe('PaginationService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    paginationService.clearCache()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('Number Lookup Pagination', () => {
    const mockNumberOccurrences: NumberOccurrence[] = [
      {
        drawNumber: 100,
        drawDate: '2023-12-01',
        number: 7,
        position: 1,
        isBonus: false,
        isPowerball: false,
        fullCombination: [7, 14, 21, 28, 35, 42]
      },
      {
        drawNumber: 99,
        drawDate: '2023-11-24',
        number: 7,
        position: 3,
        isBonus: false,
        isPowerball: false,
        fullCombination: [3, 7, 15, 22, 29, 36]
      }
    ]

    const mockPaginatedResponse: PaginatedResponse<NumberOccurrence> = {
      items: mockNumberOccurrences,
      page: 1,
      pageSize: 50,
      totalItems: 25,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
      itemCount: 2,
      firstItemIndex: 1,
      lastItemIndex: 2,
      metadata: {
        page: 1,
        pageSize: 50,
        totalItems: 25,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
        firstItemIndex: 1,
        lastItemIndex: 2
      }
    }

    it('should lookup single number with pagination', async () => {
      mockApi.get.mockResolvedValue({ data: mockPaginatedResponse })

      const result = await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })

      expect(mockApi.get).toHaveBeenCalledWith('/lookup/number/7/paginated', {
        params: { page: 1, pageSize: 50 }
      })
      expect(result).toEqual(mockPaginatedResponse)
      expect(result.items).toHaveLength(2)
      expect(result.items[0].number).toBe(7)
    })

    it('should cache paginated lookup results', async () => {
      mockApi.get.mockResolvedValue({ data: mockPaginatedResponse })

      // First call
      await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })
      
      // Second call (should use cache)
      const result = await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })

      expect(mockApi.get).toHaveBeenCalledTimes(1) // Only called once due to caching
      expect(result).toEqual(mockPaginatedResponse)
    })

    it('should lookup multiple numbers with pagination and filters', async () => {
      const request = {
        numbers: [7, 14, 21],
        page: 1,
        pageSize: 25,
        startDate: '2023-01-01',
        endDate: '2023-12-31',
        includeBonus: true,
        includePowerball: false,
        sortBy: 'date',
        sortDirection: 'desc' as const
      }

      mockApi.post.mockResolvedValue({ data: mockPaginatedResponse })

      const result = await paginationService.lookupNumbersPaginated(request)

      expect(mockApi.post).toHaveBeenCalledWith('/lookup/numbers/paginated', request)
      expect(result).toEqual(mockPaginatedResponse)
    })

    it('should handle API errors gracefully', async () => {
      mockApi.get.mockRejectedValue(new Error('API Error'))

      await expect(
        paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })
      ).rejects.toThrow('API Error')
    })
  })

  describe('Combination Search Pagination', () => {
    const mockCombinationMatches: CombinationMatch[] = [
      {
        drawNumber: 100,
        drawDate: '2023-12-01',
        winningCombination: [7, 14, 21, 28, 35, 42],
        matchedNumbers: [7, 14, 21],
        matchCount: 3,
        isExactMatch: false
      }
    ]

    const mockCombinationResponse: PaginatedResponse<CombinationMatch> = {
      items: mockCombinationMatches,
      page: 1,
      pageSize: 50,
      totalItems: 5,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
      itemCount: 1,
      firstItemIndex: 1,
      lastItemIndex: 1,
      metadata: {
        page: 1,
        pageSize: 50,
        totalItems: 5,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
        firstItemIndex: 1,
        lastItemIndex: 1
      }
    }

    it('should search combinations with pagination', async () => {
      const request = {
        combination: [7, 14, 21],
        page: 1,
        pageSize: 50,
        includePartialMatches: true,
        minimumMatches: 2
      }

      mockApi.post.mockResolvedValue({ data: mockCombinationResponse })

      const result = await paginationService.searchCombinationPaginated(request)

      expect(mockApi.post).toHaveBeenCalledWith('/lookup/combination/paginated', request)
      expect(result).toEqual(mockCombinationResponse)
      expect(result.items[0].matchCount).toBe(3)
    })

    it('should cache combination search results', async () => {
      const request = {
        combination: [7, 14, 21],
        page: 1,
        pageSize: 50,
        includePartialMatches: true,
        minimumMatches: 2
      }

      mockApi.post.mockResolvedValue({ data: mockCombinationResponse })

      // First call
      await paginationService.searchCombinationPaginated(request)
      
      // Second call (should use cache)
      const result = await paginationService.searchCombinationPaginated(request)

      expect(mockApi.post).toHaveBeenCalledTimes(1) // Only called once due to caching
      expect(result).toEqual(mockCombinationResponse)
    })
  })

  describe('Pagination Navigation', () => {
    const mockResponse: PaginatedResponse<any> = {
      items: [],
      page: 3,
      pageSize: 25,
      totalItems: 150,
      totalPages: 6,
      hasPreviousPage: true,
      hasNextPage: true,
      itemCount: 25,
      firstItemIndex: 51,
      lastItemIndex: 75,
      metadata: {
        page: 3,
        pageSize: 25,
        totalItems: 150,
        totalPages: 6,
        hasPreviousPage: true,
        hasNextPage: true,
        firstItemIndex: 51,
        lastItemIndex: 75
      }
    }

    it('should create pagination navigation info', () => {
      const info = paginationService.createPaginationInfo(mockResponse)

      expect(info.currentPage).toBe(3)
      expect(info.totalPages).toBe(6)
      expect(info.hasNext).toBe(true)
      expect(info.hasPrevious).toBe(true)
      expect(info.showingText).toBe('Showing 51-75 of 150 results')
      expect(info.pageNumbers).toContain(1) // Should include first page
      expect(info.pageNumbers).toContain(6) // Should include last page
      expect(info.pageNumbers).toContain(3) // Should include current page
    })

    it('should handle first page navigation', () => {
      const firstPageResponse = { ...mockResponse, page: 1, hasPreviousPage: false, firstItemIndex: 1, lastItemIndex: 25 }
      const info = paginationService.createPaginationInfo(firstPageResponse)

      expect(info.currentPage).toBe(1)
      expect(info.hasPrevious).toBe(false)
      expect(info.hasNext).toBe(true)
      expect(info.showingText).toBe('Showing 1-25 of 150 results')
    })

    it('should handle last page navigation', () => {
      const lastPageResponse = { 
        ...mockResponse, 
        page: 6, 
        hasNextPage: false, 
        itemCount: 0, 
        firstItemIndex: 0, 
        lastItemIndex: 0,
        totalItems: 0
      }
      const info = paginationService.createPaginationInfo(lastPageResponse)

      expect(info.currentPage).toBe(6)
      expect(info.hasPrevious).toBe(true)
      expect(info.hasNext).toBe(false)
      expect(info.showingText).toBe('No results found')
    })

    it('should include ellipsis in page numbers for large page counts', () => {
      const largePageResponse = { ...mockResponse, totalPages: 20, totalItems: 500 }
      const info = paginationService.createPaginationInfo(largePageResponse)

      expect(info.pageNumbers).toContain(-1) // -1 represents ellipsis
      expect(info.pageNumbers).toContain(1) // First page
      expect(info.pageNumbers).toContain(20) // Last page
    })
  })

  describe('Performance Optimization', () => {
    it('should calculate optimal page size based on viewport', () => {
      // Mock window dimensions
      Object.defineProperty(window, 'innerHeight', {
        writable: true,
        configurable: true,
        value: 800
      })

      const compactSize = paginationService.calculateOptimalPageSize('compact')
      const detailedSize = paginationService.calculateOptimalPageSize('detailed')
      const cardsSize = paginationService.calculateOptimalPageSize('cards')

      expect(compactSize).toBeGreaterThan(detailedSize)
      expect(detailedSize).toBeGreaterThan(cardsSize)
      expect(compactSize).toBeGreaterThanOrEqual(10)
      expect(compactSize).toBeLessThanOrEqual(100)
    })

    it('should prefetch next page when available', async () => {
      const mockRequest = { page: 1, pageSize: 50, hasNextPage: true }
      const mockFetchFunction = vi.fn().mockResolvedValue(mockResponse)

      await paginationService.prefetchNextPage(mockRequest, mockFetchFunction)

      expect(mockFetchFunction).toHaveBeenCalledWith({ ...mockRequest, page: 2 })
    })

    it('should not prefetch when no next page available', async () => {
      const mockRequest = { page: 6, pageSize: 50, hasNextPage: false }
      const mockFetchFunction = vi.fn()

      await paginationService.prefetchNextPage(mockRequest, mockFetchFunction)

      expect(mockFetchFunction).not.toHaveBeenCalled()
    })

    it('should handle prefetch errors gracefully', async () => {
      const mockRequest = { page: 1, pageSize: 50, hasNextPage: true }
      const mockFetchFunction = vi.fn().mockRejectedValue(new Error('Prefetch failed'))

      // Should not throw
      await expect(
        paginationService.prefetchNextPage(mockRequest, mockFetchFunction)
      ).resolves.toBeUndefined()
    })
  })

  describe('Cache Management', () => {
    it('should clear cache by pattern', () => {
      // Add some test data to cache
      paginationService['setCache']('number_7_1_50', { test: 'data1' })
      paginationService['setCache']('combination_1_7_14_1_50', { test: 'data2' })
      paginationService['setCache']('other_data', { test: 'data3' })

      // Clear cache by pattern
      paginationService.clearCache('number')

      const stats = paginationService.getCacheStats()
      expect(stats.entries).not.toContain('number_7_1_50')
      expect(stats.entries).toContain('combination_1_7_14_1_50')
      expect(stats.entries).toContain('other_data')
    })

    it('should clear all cache when no pattern provided', () => {
      // Add some test data to cache
      paginationService['setCache']('test1', { test: 'data1' })
      paginationService['setCache']('test2', { test: 'data2' })

      paginationService.clearCache()

      const stats = paginationService.getCacheStats()
      expect(stats.size).toBe(0)
      expect(stats.entries).toHaveLength(0)
    })

    it('should provide cache statistics', () => {
      // Add some test data
      paginationService['setCache']('test1', { test: 'data1' })
      paginationService['setCache']('test2', { test: 'data2' })

      const stats = paginationService.getCacheStats()
      expect(stats.size).toBe(2)
      expect(stats.entries).toContain('test1')
      expect(stats.entries).toContain('test2')
      expect(typeof stats.hitRate).toBe('number')
    })

    it('should implement LRU eviction when cache is full', () => {
      // Fill cache beyond limit (assuming limit is 100)
      for (let i = 0; i < 105; i++) {
        paginationService['setCache'](`test_${i}`, { test: `data${i}` })
      }

      const stats = paginationService.getCacheStats()
      expect(stats.size).toBeLessThanOrEqual(100) // Should not exceed limit
    })

    it('should handle cache expiration', () => {
      // Set item with very short TTL
      paginationService['setCache']('short_ttl', { test: 'data' }, 1) // 1ms TTL

      // Wait for expiration
      return new Promise(resolve => {
        setTimeout(() => {
          const cached = paginationService['getFromCache']('short_ttl')
          expect(cached).toBeNull()
          resolve(undefined)
        }, 10)
      })
    })
  })

  describe('Edge Cases', () => {
    it('should handle empty responses', async () => {
      const emptyResponse: PaginatedResponse<NumberOccurrence> = {
        items: [],
        page: 1,
        pageSize: 50,
        totalItems: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
        itemCount: 0,
        firstItemIndex: 0,
        lastItemIndex: 0,
        metadata: {
          page: 1,
          pageSize: 50,
          totalItems: 0,
          totalPages: 0,
          hasPreviousPage: false,
          hasNextPage: false,
          firstItemIndex: 0,
          lastItemIndex: 0
        }
      }

      mockApi.get.mockResolvedValue({ data: emptyResponse })

      const result = await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })

      expect(result.items).toHaveLength(0)
      expect(result.totalItems).toBe(0)
    })

    it('should handle invalid page numbers', async () => {
      mockApi.get.mockResolvedValue({ data: mockNumberOccurrences })

      // Should handle negative page numbers
      await expect(
        paginationService.lookupNumberPaginated(7, { page: -1, pageSize: 50 })
      ).resolves.toBeDefined()

      // Should handle zero page numbers
      await expect(
        paginationService.lookupNumberPaginated(7, { page: 0, pageSize: 50 })
      ).resolves.toBeDefined()
    })

    it('should handle invalid page sizes', async () => {
      mockApi.get.mockResolvedValue({ data: mockNumberOccurrences })

      // Should handle negative page sizes
      await expect(
        paginationService.lookupNumberPaginated(7, { page: 1, pageSize: -10 })
      ).resolves.toBeDefined()

      // Should handle zero page sizes
      await expect(
        paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 0 })
      ).resolves.toBeDefined()
    })
  })

  describe('Performance Benchmarks', () => {
    it('should complete single number lookup within time limit', async () => {
      mockApi.get.mockResolvedValue({ data: mockPaginatedResponse })

      const startTime = performance.now()
      await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })
      const endTime = performance.now()

      const duration = endTime - startTime
      expect(duration).toBeLessThan(100) // Should complete within 100ms (excluding network)
    })

    it('should handle concurrent requests efficiently', async () => {
      mockApi.get.mockResolvedValue({ data: mockPaginatedResponse })

      const requests = Array.from({ length: 10 }, (_, i) =>
        paginationService.lookupNumberPaginated(i + 1, { page: 1, pageSize: 50 })
      )

      const startTime = performance.now()
      await Promise.all(requests)
      const endTime = performance.now()

      const duration = endTime - startTime
      expect(duration).toBeLessThan(500) // Should handle 10 concurrent requests within 500ms
    })

    it('should maintain performance with large page sizes', async () => {
      const largeResponse = {
        ...mockPaginatedResponse,
        items: Array.from({ length: 100 }, (_, i) => ({
          ...mockNumberOccurrences[0],
          drawNumber: i + 1
        })),
        pageSize: 100,
        itemCount: 100
      }

      mockApi.get.mockResolvedValue({ data: largeResponse })

      const startTime = performance.now()
      await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 100 })
      const endTime = performance.now()

      const duration = endTime - startTime
      expect(duration).toBeLessThan(200) // Should handle large pages within 200ms
    })

    it('should show performance improvement with caching', async () => {
      mockApi.get.mockResolvedValue({ data: mockPaginatedResponse })

      // First call (cache miss)
      const firstStart = performance.now()
      await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })
      const firstEnd = performance.now()
      const firstDuration = firstEnd - firstStart

      // Second call (cache hit)
      const secondStart = performance.now()
      await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })
      const secondEnd = performance.now()
      const secondDuration = secondEnd - secondStart

      // Cache hit should be significantly faster
      expect(secondDuration).toBeLessThan(firstDuration)
      expect(mockApi.get).toHaveBeenCalledTimes(1) // Only one API call due to caching
    })

    it('should handle pagination navigation efficiently', async () => {
      const multiPageResponse = {
        ...mockPaginatedResponse,
        totalPages: 10,
        hasNextPage: true
      }

      mockApi.get.mockResolvedValue({ data: multiPageResponse })

      // Test multiple page navigations
      const pageRequests = Array.from({ length: 5 }, (_, i) =>
        paginationService.lookupNumberPaginated(7, { page: i + 1, pageSize: 25 })
      )

      const startTime = performance.now()
      await Promise.all(pageRequests)
      const endTime = performance.now()

      const duration = endTime - startTime
      const averagePerPage = duration / 5

      expect(averagePerPage).toBeLessThan(100) // Each page should load within 100ms
    })

    it('should optimize memory usage with large datasets', async () => {
      // Create a large mock response
      const largeDataset = Array.from({ length: 1000 }, (_, i) => ({
        ...mockNumberOccurrences[0],
        drawNumber: i + 1,
        drawDate: `2023-${String(Math.floor(i / 30) + 1).padStart(2, '0')}-${String((i % 30) + 1).padStart(2, '0')}`
      }))

      const largeResponse = {
        ...mockPaginatedResponse,
        items: largeDataset,
        totalItems: 1000,
        itemCount: 1000
      }

      mockApi.get.mockResolvedValue({ data: largeResponse })

      // Measure memory before
      const initialMemory = (performance as any).memory?.usedJSHeapSize || 0

      await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 1000 })

      // Measure memory after
      const finalMemory = (performance as any).memory?.usedJSHeapSize || 0
      const memoryIncrease = finalMemory - initialMemory

      // Memory increase should be reasonable (less than 10MB for this test)
      expect(memoryIncrease).toBeLessThan(10 * 1024 * 1024)
    })

    it('should maintain performance under concurrent load', async () => {
      mockApi.get.mockResolvedValue({ data: mockPaginatedResponse })
      mockApi.post.mockResolvedValue({ data: mockCombinationResponse })

      // Mix of different request types
      const mixedRequests = [
        ...Array.from({ length: 5 }, (_, i) =>
          paginationService.lookupNumberPaginated(i + 1, { page: 1, pageSize: 50 })
        ),
        ...Array.from({ length: 3 }, (_, i) =>
          paginationService.searchCombinationPaginated({
            combination: [i + 1, i + 8, i + 15],
            page: 1,
            pageSize: 25
          })
        )
      ]

      const startTime = performance.now()
      await Promise.all(mixedRequests)
      const endTime = performance.now()

      const duration = endTime - startTime
      const averagePerRequest = duration / mixedRequests.length

      expect(averagePerRequest).toBeLessThan(150) // Average should be under 150ms per request
    })

    it('should demonstrate lazy loading benefits', async () => {
      const smallPageResponse = {
        ...mockPaginatedResponse,
        pageSize: 10,
        itemCount: 10,
        items: mockNumberOccurrences.slice(0, 10)
      }

      const largePageResponse = {
        ...mockPaginatedResponse,
        pageSize: 100,
        itemCount: 100,
        items: Array.from({ length: 100 }, (_, i) => ({
          ...mockNumberOccurrences[0],
          drawNumber: i + 1
        }))
      }

      // Small page (lazy loading)
      mockApi.get.mockResolvedValueOnce({ data: smallPageResponse })
      const lazyStart = performance.now()
      await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 10 })
      const lazyEnd = performance.now()
      const lazyDuration = lazyEnd - lazyStart

      // Large page (eager loading)
      mockApi.get.mockResolvedValueOnce({ data: largePageResponse })
      const eagerStart = performance.now()
      await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 100 })
      const eagerEnd = performance.now()
      const eagerDuration = eagerEnd - eagerStart

      // Lazy loading should be faster or equal
      expect(lazyDuration).toBeLessThanOrEqual(eagerDuration)
      
      // Performance per item should be reasonable
      const lazyPerItem = lazyDuration / 10
      const eagerPerItem = eagerDuration / 100
      expect(Math.abs(lazyPerItem - eagerPerItem)).toBeLessThan(5) // Should be similar per-item performance
    })
  })
})