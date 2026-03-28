import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { performanceService } from '../performanceService'
import { paginationService } from '../paginationService'
import { lookupService } from '../lookupService'

// Mock APIs
const mockApi = {
  get: vi.fn(),
  post: vi.fn(),
  delete: vi.fn()
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

describe('Performance Integration Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    paginationService.clearCache()
    
    // Setup performance monitoring
    performanceService.startPerformanceMonitoring({
      maxApiResponseTime: 2000,
      maxRenderTime: 100,
      maxMemoryUsage: 0.9
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('Search Response Times with Large Datasets', () => {
    it('should complete number lookup within 500ms for large datasets', async () => {
      // Mock large dataset response
      const largeDatasetResponse = {
        items: Array.from({ length: 1000 }, (_, i) => ({
          drawNumber: i + 1,
          drawDate: `2023-${String(Math.floor(i / 30) + 1).padStart(2, '0')}-${String((i % 30) + 1).padStart(2, '0')}`,
          number: 7,
          position: (i % 6) + 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        })),
        page: 1,
        pageSize: 50,
        totalItems: 1000,
        totalPages: 20,
        hasPreviousPage: false,
        hasNextPage: true,
        itemCount: 50,
        firstItemIndex: 1,
        lastItemIndex: 50,
        metadata: {
          page: 1,
          pageSize: 50,
          totalItems: 1000,
          totalPages: 20,
          hasPreviousPage: false,
          hasNextPage: true,
          firstItemIndex: 1,
          lastItemIndex: 50
        }
      }

      // Simulate network delay
      mockApi.get.mockImplementation(() => 
        new Promise(resolve => 
          setTimeout(() => resolve({ data: largeDatasetResponse }), 100)
        )
      )

      const startTime = performance.now()
      const result = await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })
      const endTime = performance.now()

      const duration = endTime - startTime
      expect(duration).toBeLessThan(500) // 500ms target
      expect(result.totalItems).toBe(1000)
      expect(result.items).toHaveLength(50)
    })

    it('should handle combination search within 2000ms for complex queries', async () => {
      const combinationResponse = {
        items: Array.from({ length: 25 }, (_, i) => ({
          drawNumber: i + 1,
          drawDate: `2023-12-${String(i + 1).padStart(2, '0')}`,
          winningCombination: [7, 14, 21, 28, 35, 42],
          matchedNumbers: [7, 14, 21],
          matchCount: 3,
          isExactMatch: false
        })),
        page: 1,
        pageSize: 25,
        totalItems: 100,
        totalPages: 4,
        hasPreviousPage: false,
        hasNextPage: true,
        itemCount: 25,
        firstItemIndex: 1,
        lastItemIndex: 25,
        metadata: {
          page: 1,
          pageSize: 25,
          totalItems: 100,
          totalPages: 4,
          hasPreviousPage: false,
          hasNextPage: true,
          firstItemIndex: 1,
          lastItemIndex: 25
        }
      }

      // Simulate longer processing time for combination search
      mockApi.post.mockImplementation(() => 
        new Promise(resolve => 
          setTimeout(() => resolve({ data: combinationResponse }), 300)
        )
      )

      const startTime = performance.now()
      const result = await paginationService.searchCombinationPaginated({
        combination: [7, 14, 21, 28, 35, 42],
        page: 1,
        pageSize: 25,
        includePartialMatches: true,
        minimumMatches: 3
      })
      const endTime = performance.now()

      const duration = endTime - startTime
      expect(duration).toBeLessThan(2000) // 2000ms target for complex searches
      expect(result.totalItems).toBe(100)
    })

    it('should maintain performance across different page sizes', async () => {
      const pageSizes = [10, 25, 50, 100]
      const performanceResults: Array<{ pageSize: number; duration: number }> = []

      for (const pageSize of pageSizes) {
        const response = {
          items: Array.from({ length: pageSize }, (_, i) => ({
            drawNumber: i + 1,
            drawDate: '2023-12-01',
            number: 7,
            position: 1,
            isBonus: false,
            isPowerball: false,
            fullCombination: [7, 14, 21, 28, 35, 42]
          })),
          page: 1,
          pageSize,
          totalItems: 500,
          totalPages: Math.ceil(500 / pageSize),
          hasPreviousPage: false,
          hasNextPage: true,
          itemCount: pageSize,
          firstItemIndex: 1,
          lastItemIndex: pageSize,
          metadata: {
            page: 1,
            pageSize,
            totalItems: 500,
            totalPages: Math.ceil(500 / pageSize),
            hasPreviousPage: false,
            hasNextPage: true,
            firstItemIndex: 1,
            lastItemIndex: pageSize
          }
        }

        mockApi.get.mockResolvedValueOnce({ data: response })

        const startTime = performance.now()
        await paginationService.lookupNumberPaginated(7, { page: 1, pageSize })
        const endTime = performance.now()

        const duration = endTime - startTime
        performanceResults.push({ pageSize, duration })

        // Each page size should complete within reasonable time
        expect(duration).toBeLessThan(300)
      }

      // Performance should scale reasonably with page size
      const smallPageTime = performanceResults.find(r => r.pageSize === 10)?.duration || 0
      const largePageTime = performanceResults.find(r => r.pageSize === 100)?.duration || 0
      
      // Large page shouldn't be more than 3x slower than small page
      expect(largePageTime).toBeLessThan(smallPageTime * 3)
    })
  })

  describe('Pagination and Lazy Loading Functionality', () => {
    it('should verify pagination reduces response time compared to full queries', async () => {
      const fullResponse = {
        items: Array.from({ length: 500 }, (_, i) => ({
          drawNumber: i + 1,
          drawDate: '2023-12-01',
          number: 7,
          position: 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        }))
      }

      const paginatedResponse = {
        items: Array.from({ length: 50 }, (_, i) => ({
          drawNumber: i + 1,
          drawDate: '2023-12-01',
          number: 7,
          position: 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        })),
        page: 1,
        pageSize: 50,
        totalItems: 500,
        totalPages: 10,
        hasPreviousPage: false,
        hasNextPage: true,
        itemCount: 50,
        firstItemIndex: 1,
        lastItemIndex: 50,
        metadata: {
          page: 1,
          pageSize: 50,
          totalItems: 500,
          totalPages: 10,
          hasPreviousPage: false,
          hasNextPage: true,
          firstItemIndex: 1,
          lastItemIndex: 50
        }
      }

      // Mock full query (slower)
      mockApi.get.mockImplementationOnce(() => 
        new Promise(resolve => 
          setTimeout(() => resolve({ data: fullResponse }), 200)
        )
      )

      // Mock paginated query (faster)
      mockApi.get.mockImplementationOnce(() => 
        new Promise(resolve => 
          setTimeout(() => resolve({ data: paginatedResponse }), 50)
        )
      )

      // Test full query
      const fullStartTime = performance.now()
      const fullResult = await lookupService.lookupNumber(7)
      const fullEndTime = performance.now()
      const fullDuration = fullEndTime - fullStartTime

      // Test paginated query
      const paginatedStartTime = performance.now()
      const paginatedResult = await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })
      const paginatedEndTime = performance.now()
      const paginatedDuration = paginatedEndTime - paginatedStartTime

      // Paginated should be faster
      expect(paginatedDuration).toBeLessThan(fullDuration)
      expect(paginatedResult.items).toHaveLength(50)
      expect(fullResult).toHaveLength(500)
    })

    it('should demonstrate lazy loading benefits with progressive data loading', async () => {
      const responses = [
        // First page
        {
          items: Array.from({ length: 20 }, (_, i) => ({
            drawNumber: i + 1,
            drawDate: '2023-12-01',
            number: 7,
            position: 1,
            isBonus: false,
            isPowerball: false,
            fullCombination: [7, 14, 21, 28, 35, 42]
          })),
          page: 1,
          pageSize: 20,
          totalItems: 100,
          totalPages: 5,
          hasPreviousPage: false,
          hasNextPage: true,
          itemCount: 20,
          firstItemIndex: 1,
          lastItemIndex: 20
        },
        // Second page
        {
          items: Array.from({ length: 20 }, (_, i) => ({
            drawNumber: i + 21,
            drawDate: '2023-11-01',
            number: 7,
            position: 2,
            isBonus: false,
            isPowerball: false,
            fullCombination: [7, 14, 21, 28, 35, 42]
          })),
          page: 2,
          pageSize: 20,
          totalItems: 100,
          totalPages: 5,
          hasPreviousPage: true,
          hasNextPage: true,
          itemCount: 20,
          firstItemIndex: 21,
          lastItemIndex: 40
        }
      ]

      // Mock progressive loading
      mockApi.get
        .mockResolvedValueOnce({ data: responses[0] })
        .mockResolvedValueOnce({ data: responses[1] })

      // Load first page
      const firstPageStart = performance.now()
      const firstPage = await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 20 })
      const firstPageEnd = performance.now()
      const firstPageDuration = firstPageEnd - firstPageStart

      // Load second page
      const secondPageStart = performance.now()
      const secondPage = await paginationService.lookupNumberPaginated(7, { page: 2, pageSize: 20 })
      const secondPageEnd = performance.now()
      const secondPageDuration = secondPageEnd - secondPageStart

      // Both pages should load quickly
      expect(firstPageDuration).toBeLessThan(100)
      expect(secondPageDuration).toBeLessThan(100)
      
      // Should have loaded different data
      expect(firstPage.items[0].drawNumber).toBe(1)
      expect(secondPage.items[0].drawNumber).toBe(21)
      
      // Total time for both pages should be less than loading all at once
      const totalLazyTime = firstPageDuration + secondPageDuration
      expect(totalLazyTime).toBeLessThan(150) // Should be very fast for lazy loading
    })

    it('should verify pagination navigation performance', async () => {
      const createPageResponse = (page: number) => ({
        items: Array.from({ length: 25 }, (_, i) => ({
          drawNumber: (page - 1) * 25 + i + 1,
          drawDate: '2023-12-01',
          number: 7,
          position: 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        })),
        page,
        pageSize: 25,
        totalItems: 200,
        totalPages: 8,
        hasPreviousPage: page > 1,
        hasNextPage: page < 8,
        itemCount: 25,
        firstItemIndex: (page - 1) * 25 + 1,
        lastItemIndex: page * 25,
        metadata: {
          page,
          pageSize: 25,
          totalItems: 200,
          totalPages: 8,
          hasPreviousPage: page > 1,
          hasNextPage: page < 8,
          firstItemIndex: (page - 1) * 25 + 1,
          lastItemIndex: page * 25
        }
      })

      // Test navigation through multiple pages
      const navigationTimes: number[] = []
      
      for (let page = 1; page <= 5; page++) {
        mockApi.get.mockResolvedValueOnce({ data: createPageResponse(page) })
        
        const startTime = performance.now()
        const result = await paginationService.lookupNumberPaginated(7, { page, pageSize: 25 })
        const endTime = performance.now()
        
        const duration = endTime - startTime
        navigationTimes.push(duration)
        
        expect(duration).toBeLessThan(200) // Each navigation should be fast
        expect(result.page).toBe(page)
        expect(result.items[0].drawNumber).toBe((page - 1) * 25 + 1)
      }

      // Navigation times should be consistent
      const avgTime = navigationTimes.reduce((sum, time) => sum + time, 0) / navigationTimes.length
      const maxDeviation = Math.max(...navigationTimes.map(time => Math.abs(time - avgTime)))
      
      expect(maxDeviation).toBeLessThan(avgTime * 0.5) // No more than 50% deviation from average
    })
  })

  describe('Cache Performance and Hit Rates', () => {
    it('should demonstrate significant cache performance improvement', async () => {
      const response = {
        items: Array.from({ length: 50 }, (_, i) => ({
          drawNumber: i + 1,
          drawDate: '2023-12-01',
          number: 7,
          position: 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        })),
        page: 1,
        pageSize: 50,
        totalItems: 200,
        totalPages: 4,
        hasPreviousPage: false,
        hasNextPage: true,
        itemCount: 50,
        firstItemIndex: 1,
        lastItemIndex: 50,
        metadata: {
          page: 1,
          pageSize: 50,
          totalItems: 200,
          totalPages: 4,
          hasPreviousPage: false,
          hasNextPage: true,
          firstItemIndex: 1,
          lastItemIndex: 50
        }
      }

      // Clear cache first
      paginationService.clearCache()

      // First call (cache miss) - simulate network delay
      mockApi.get.mockImplementationOnce(() => 
        new Promise(resolve => 
          setTimeout(() => resolve({ data: response }), 150)
        )
      )

      const firstCallStart = performance.now()
      await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })
      const firstCallEnd = performance.now()
      const firstCallDuration = firstCallEnd - firstCallStart

      // Second call (cache hit) - no network delay
      const secondCallStart = performance.now()
      await paginationService.lookupNumberPaginated(7, { page: 1, pageSize: 50 })
      const secondCallEnd = performance.now()
      const secondCallDuration = secondCallEnd - secondCallStart

      // Cache hit should be significantly faster
      const improvementRatio = firstCallDuration / secondCallDuration
      expect(improvementRatio).toBeGreaterThan(5) // At least 5x improvement
      expect(secondCallDuration).toBeLessThan(20) // Cache hit should be very fast
      expect(mockApi.get).toHaveBeenCalledTimes(1) // Only one API call
    })

    it('should maintain high cache hit rates with repeated access patterns', async () => {
      const numbers = [7, 14, 21, 28, 35]
      const responses = numbers.map(number => ({
        items: [{
          drawNumber: 1,
          drawDate: '2023-12-01',
          number,
          position: 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        }],
        page: 1,
        pageSize: 50,
        totalItems: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
        itemCount: 1,
        firstItemIndex: 1,
        lastItemIndex: 1,
        metadata: {
          page: 1,
          pageSize: 50,
          totalItems: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
          firstItemIndex: 1,
          lastItemIndex: 1
        }
      }))

      // Clear cache
      paginationService.clearCache()

      // First round - populate cache
      for (let i = 0; i < numbers.length; i++) {
        mockApi.get.mockResolvedValueOnce({ data: responses[i] })
        await paginationService.lookupNumberPaginated(numbers[i], { page: 1, pageSize: 50 })
      }

      expect(mockApi.get).toHaveBeenCalledTimes(numbers.length)

      // Second round - should use cache
      const secondRoundStart = performance.now()
      for (const number of numbers) {
        await paginationService.lookupNumberPaginated(number, { page: 1, pageSize: 50 })
      }
      const secondRoundEnd = performance.now()
      const secondRoundDuration = secondRoundEnd - secondRoundStart

      // Should still only have the initial API calls (cache hits)
      expect(mockApi.get).toHaveBeenCalledTimes(numbers.length)
      
      // Second round should be very fast due to caching
      expect(secondRoundDuration).toBeLessThan(50) // Very fast for cached data
      
      // Verify cache statistics
      const cacheStats = paginationService.getCacheStats()
      expect(cacheStats.size).toBe(numbers.length)
    })

    it('should handle cache eviction gracefully under memory pressure', async () => {
      const response = {
        items: [{
          drawNumber: 1,
          drawDate: '2023-12-01',
          number: 7,
          position: 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        }],
        page: 1,
        pageSize: 50,
        totalItems: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
        itemCount: 1,
        firstItemIndex: 1,
        lastItemIndex: 1,
        metadata: {
          page: 1,
          pageSize: 50,
          totalItems: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
          firstItemIndex: 1,
          lastItemIndex: 1
        }
      }

      // Clear cache
      paginationService.clearCache()

      // Fill cache beyond typical limits to test eviction
      for (let i = 0; i < 150; i++) {
        mockApi.get.mockResolvedValueOnce({ data: response })
        await paginationService.lookupNumberPaginated(i + 1, { page: 1, pageSize: 50 })
      }

      const cacheStats = paginationService.getCacheStats()
      
      // Cache should implement LRU eviction and stay within reasonable bounds
      expect(cacheStats.size).toBeLessThanOrEqual(100) // Should not exceed reasonable limit
      
      // Performance should still be good even with eviction
      const testStart = performance.now()
      await paginationService.lookupNumberPaginated(1, { page: 1, pageSize: 50 })
      const testEnd = performance.now()
      const testDuration = testEnd - testStart
      
      expect(testDuration).toBeLessThan(100) // Should still be reasonably fast
    })
  })

  describe('Concurrent Load Testing', () => {
    it('should handle high concurrent load efficiently', async () => {
      const response = {
        items: [{
          drawNumber: 1,
          drawDate: '2023-12-01',
          number: 7,
          position: 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        }],
        page: 1,
        pageSize: 50,
        totalItems: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
        itemCount: 1,
        firstItemIndex: 1,
        lastItemIndex: 1,
        metadata: {
          page: 1,
          pageSize: 50,
          totalItems: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
          firstItemIndex: 1,
          lastItemIndex: 1
        }
      }

      // Mock responses for concurrent requests
      for (let i = 0; i < 50; i++) {
        mockApi.get.mockResolvedValueOnce({ data: response })
      }

      // Create 50 concurrent requests
      const concurrentRequests = Array.from({ length: 50 }, (_, i) =>
        paginationService.lookupNumberPaginated((i % 40) + 1, { page: 1, pageSize: 50 })
      )

      const startTime = performance.now()
      const results = await Promise.all(concurrentRequests)
      const endTime = performance.now()

      const totalDuration = endTime - startTime
      const averagePerRequest = totalDuration / concurrentRequests.length

      // All requests should complete
      expect(results).toHaveLength(50)
      expect(results.every(result => result !== null)).toBe(true)

      // Average time per request should be reasonable
      expect(averagePerRequest).toBeLessThan(200) // 200ms average per request
      
      // Total time should be much less than sequential execution
      expect(totalDuration).toBeLessThan(5000) // 5 seconds for 50 concurrent requests
    })

    it('should maintain performance under mixed workload', async () => {
      const numberResponse = {
        items: [{
          drawNumber: 1,
          drawDate: '2023-12-01',
          number: 7,
          position: 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        }],
        page: 1,
        pageSize: 50,
        totalItems: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
        itemCount: 1,
        firstItemIndex: 1,
        lastItemIndex: 1,
        metadata: {
          page: 1,
          pageSize: 50,
          totalItems: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
          firstItemIndex: 1,
          lastItemIndex: 1
        }
      }

      const combinationResponse = {
        items: [{
          drawNumber: 1,
          drawDate: '2023-12-01',
          winningCombination: [7, 14, 21, 28, 35, 42],
          matchedNumbers: [7, 14, 21],
          matchCount: 3,
          isExactMatch: false
        }],
        page: 1,
        pageSize: 25,
        totalItems: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
        itemCount: 1,
        firstItemIndex: 1,
        lastItemIndex: 1,
        metadata: {
          page: 1,
          pageSize: 25,
          totalItems: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
          firstItemIndex: 1,
          lastItemIndex: 1
        }
      }

      // Mock mixed responses
      for (let i = 0; i < 20; i++) {
        mockApi.get.mockResolvedValueOnce({ data: numberResponse })
      }
      for (let i = 0; i < 10; i++) {
        mockApi.post.mockResolvedValueOnce({ data: combinationResponse })
      }

      // Create mixed workload
      const mixedRequests = [
        // Number lookups
        ...Array.from({ length: 20 }, (_, i) =>
          paginationService.lookupNumberPaginated((i % 40) + 1, { page: 1, pageSize: 50 })
        ),
        // Combination searches
        ...Array.from({ length: 10 }, (_, i) =>
          paginationService.searchCombinationPaginated({
            combination: [i + 1, i + 8, i + 15],
            page: 1,
            pageSize: 25
          })
        )
      ]

      const startTime = performance.now()
      const results = await Promise.all(mixedRequests)
      const endTime = performance.now()

      const totalDuration = endTime - startTime
      const averagePerRequest = totalDuration / mixedRequests.length

      // All requests should complete successfully
      expect(results).toHaveLength(30)
      expect(results.every(result => result !== null)).toBe(true)

      // Performance should be reasonable for mixed workload
      expect(averagePerRequest).toBeLessThan(300) // 300ms average per request
      expect(totalDuration).toBeLessThan(6000) // 6 seconds total for mixed workload
    })
  })

  describe('Memory Usage and Resource Management', () => {
    it('should maintain reasonable memory usage under load', async () => {
      const response = {
        items: Array.from({ length: 100 }, (_, i) => ({
          drawNumber: i + 1,
          drawDate: '2023-12-01',
          number: 7,
          position: 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        })),
        page: 1,
        pageSize: 100,
        totalItems: 1000,
        totalPages: 10,
        hasPreviousPage: false,
        hasNextPage: true,
        itemCount: 100,
        firstItemIndex: 1,
        lastItemIndex: 100,
        metadata: {
          page: 1,
          pageSize: 100,
          totalItems: 1000,
          totalPages: 10,
          hasPreviousPage: false,
          hasNextPage: true,
          firstItemIndex: 1,
          lastItemIndex: 100
        }
      }

      // Get initial memory usage
      const initialMemory = (performance as any).memory?.usedJSHeapSize || 0

      // Perform multiple operations with large datasets
      for (let i = 0; i < 20; i++) {
        mockApi.get.mockResolvedValueOnce({ data: response })
        await paginationService.lookupNumberPaginated((i % 40) + 1, { page: 1, pageSize: 100 })
      }

      // Get final memory usage
      const finalMemory = (performance as any).memory?.usedJSHeapSize || 0
      const memoryIncrease = finalMemory - initialMemory

      // Memory increase should be reasonable (less than 50MB for this test)
      expect(memoryIncrease).toBeLessThan(50 * 1024 * 1024)

      // Cache should not grow indefinitely
      const cacheStats = paginationService.getCacheStats()
      expect(cacheStats.size).toBeLessThanOrEqual(100) // Reasonable cache size limit
    })

    it('should handle garbage collection efficiently', async () => {
      const response = {
        items: Array.from({ length: 50 }, (_, i) => ({
          drawNumber: i + 1,
          drawDate: '2023-12-01',
          number: 7,
          position: 1,
          isBonus: false,
          isPowerball: false,
          fullCombination: [7, 14, 21, 28, 35, 42]
        })),
        page: 1,
        pageSize: 50,
        totalItems: 200,
        totalPages: 4,
        hasPreviousPage: false,
        hasNextPage: true,
        itemCount: 50,
        firstItemIndex: 1,
        lastItemIndex: 50,
        metadata: {
          page: 1,
          pageSize: 50,
          totalItems: 200,
          totalPages: 4,
          hasPreviousPage: false,
          hasNextPage: true,
          firstItemIndex: 1,
          lastItemIndex: 50
        }
      }

      // Create and release many objects to test GC behavior
      const iterations = 100
      const timings: number[] = []

      for (let i = 0; i < iterations; i++) {
        mockApi.get.mockResolvedValueOnce({ data: response })
        
        const startTime = performance.now()
        await paginationService.lookupNumberPaginated((i % 40) + 1, { page: 1, pageSize: 50 })
        const endTime = performance.now()
        
        timings.push(endTime - startTime)
      }

      // Performance should remain consistent (no significant degradation due to GC pressure)
      const firstHalfAvg = timings.slice(0, 50).reduce((sum, time) => sum + time, 0) / 50
      const secondHalfAvg = timings.slice(50).reduce((sum, time) => sum + time, 0) / 50
      
      // Second half shouldn't be significantly slower than first half
      expect(secondHalfAvg).toBeLessThan(firstHalfAvg * 1.5) // No more than 50% degradation
    })
  })
})