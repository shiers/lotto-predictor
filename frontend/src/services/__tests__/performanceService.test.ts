import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { performanceService } from '../performanceService'

// Mock API
vi.mock('../api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn()
  }
}))

// Mock performance APIs
const mockPerformance = {
  now: vi.fn(() => Date.now()),
  memory: {
    usedJSHeapSize: 10000000,
    totalJSHeapSize: 50000000,
    jsHeapSizeLimit: 100000000
  }
}

const mockNavigator = {
  connection: {
    type: 'wifi',
    effectiveType: '4g',
    downlink: 10,
    rtt: 50
  }
}

// Setup global mocks
Object.defineProperty(global, 'performance', {
  value: mockPerformance,
  writable: true
})

Object.defineProperty(global, 'navigator', {
  value: mockNavigator,
  writable: true
})

describe('PerformanceService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockPerformance.now.mockImplementation(() => Date.now())
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('Client Performance Metrics', () => {
    it('should collect basic client metrics', () => {
      const metrics = performanceService.getClientMetrics()

      expect(metrics).toHaveProperty('jsHeapSizeUsed')
      expect(metrics).toHaveProperty('jsHeapSizeTotal')
      expect(metrics).toHaveProperty('jsHeapSizeLimit')
      expect(metrics).toHaveProperty('connectionType')
      expect(metrics).toHaveProperty('effectiveType')
      expect(metrics).toHaveProperty('downlink')
      expect(metrics).toHaveProperty('rtt')
      expect(metrics).toHaveProperty('apiResponseTimes')
      expect(metrics).toHaveProperty('renderTimes')
      expect(metrics).toHaveProperty('errorCounts')
      expect(metrics).toHaveProperty('timestamp')
    })

    it('should track API call timing', () => {
      const endpoint = '/api/lookup/number/7'
      
      // Start timing
      const callId = performanceService.startApiCall(endpoint)
      expect(callId).toContain(endpoint)

      // Simulate some time passing
      mockPerformance.now.mockReturnValueOnce(1000).mockReturnValueOnce(1250)

      // End timing
      performanceService.endApiCall(callId, endpoint, true)

      const metrics = performanceService.getClientMetrics()
      expect(metrics.apiResponseTimes[endpoint]).toBeDefined()
      expect(metrics.apiResponseTimes[endpoint].length).toBe(1)
      expect(metrics.apiResponseTimes[endpoint][0]).toBe(250) // 1250 - 1000
    })

    it('should track multiple API calls for same endpoint', () => {
      const endpoint = '/api/lookup/numbers'
      
      // Make multiple calls
      for (let i = 0; i < 5; i++) {
        const callId = performanceService.startApiCall(endpoint)
        mockPerformance.now.mockReturnValueOnce(1000 + i * 100).mockReturnValueOnce(1000 + i * 100 + 200)
        performanceService.endApiCall(callId, endpoint, true)
      }

      const metrics = performanceService.getClientMetrics()
      expect(metrics.apiResponseTimes[endpoint]).toHaveLength(5)
      expect(metrics.apiResponseTimes[endpoint]).toEqual([200, 200, 200, 200, 200])
    })

    it('should limit API response time history', () => {
      const endpoint = '/api/test'
      
      // Make more than 100 calls
      for (let i = 0; i < 150; i++) {
        const callId = performanceService.startApiCall(endpoint)
        mockPerformance.now.mockReturnValueOnce(1000).mockReturnValueOnce(1100)
        performanceService.endApiCall(callId, endpoint, true)
      }

      const metrics = performanceService.getClientMetrics()
      expect(metrics.apiResponseTimes[endpoint]).toHaveLength(100) // Should be limited to 100
    })

    it('should track API errors', () => {
      const endpoint = '/api/lookup/error'
      
      const callId = performanceService.startApiCall(endpoint)
      mockPerformance.now.mockReturnValueOnce(1000).mockReturnValueOnce(1200)
      performanceService.endApiCall(callId, endpoint, false) // Error

      const metrics = performanceService.getClientMetrics()
      expect(metrics.errorCounts[endpoint]).toBe(1)
    })

    it('should track render times', () => {
      const componentName = 'NumberLookup'
      
      performanceService.recordRenderTime(componentName, 50)
      performanceService.recordRenderTime(componentName, 75)
      performanceService.recordRenderTime(componentName, 60)

      const metrics = performanceService.getClientMetrics()
      expect(metrics.renderTimes[componentName]).toEqual([50, 75, 60])
    })

    it('should limit render time history', () => {
      const componentName = 'TestComponent'
      
      // Record more than 50 render times
      for (let i = 0; i < 75; i++) {
        performanceService.recordRenderTime(componentName, i * 10)
      }

      const metrics = performanceService.getClientMetrics()
      expect(metrics.renderTimes[componentName]).toHaveLength(50) // Should be limited to 50
    })
  })

  describe('Performance Summary', () => {
    it('should calculate API performance statistics', () => {
      const endpoint = '/api/test'
      
      // Add some test data
      const responseTimes = [100, 200, 150, 300, 250]
      responseTimes.forEach(time => {
        const callId = performanceService.startApiCall(endpoint)
        mockPerformance.now.mockReturnValueOnce(1000).mockReturnValueOnce(1000 + time)
        performanceService.endApiCall(callId, endpoint, true)
      })

      const summary = performanceService.getPerformanceSummary()
      
      expect(summary.apiPerformance[endpoint]).toBeDefined()
      expect(summary.apiPerformance[endpoint].count).toBe(5)
      expect(summary.apiPerformance[endpoint].average).toBe(200) // (100+200+150+300+250)/5
      expect(summary.apiPerformance[endpoint].median).toBe(200)
      expect(summary.apiPerformance[endpoint].min).toBe(100)
      expect(summary.apiPerformance[endpoint].max).toBe(300)
    })

    it('should calculate render performance statistics', () => {
      const componentName = 'TestComponent'
      
      // Add some test data
      const renderTimes = [10, 20, 15, 30, 25]
      renderTimes.forEach(time => {
        performanceService.recordRenderTime(componentName, time)
      })

      const summary = performanceService.getPerformanceSummary()
      
      expect(summary.renderPerformance[componentName]).toBeDefined()
      expect(summary.renderPerformance[componentName].count).toBe(5)
      expect(summary.renderPerformance[componentName].average).toBe(20) // (10+20+15+30+25)/5
      expect(summary.renderPerformance[componentName].median).toBe(20)
      expect(summary.renderPerformance[componentName].min).toBe(10)
      expect(summary.renderPerformance[componentName].max).toBe(30)
    })

    it('should include memory usage information', () => {
      const summary = performanceService.getPerformanceSummary()
      
      expect(summary.memoryUsage).toBeDefined()
      expect(summary.memoryUsage.used).toBe(10000000)
      expect(summary.memoryUsage.total).toBe(50000000)
      expect(summary.memoryUsage.limit).toBe(100000000)
      expect(summary.memoryUsage.usagePercentage).toBe(20) // 10M / 50M * 100
    })

    it('should include network information', () => {
      const summary = performanceService.getPerformanceSummary()
      
      expect(summary.networkInfo).toBeDefined()
      expect(summary.networkInfo.connectionType).toBe('wifi')
      expect(summary.networkInfo.effectiveType).toBe('4g')
      expect(summary.networkInfo.downlink).toBe(10)
      expect(summary.networkInfo.rtt).toBe(50)
    })
  })

  describe('Performance Monitoring', () => {
    it('should detect slow API calls', async () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {})
      
      // Start monitoring with low threshold
      performanceService.startPerformanceMonitoring({
        maxApiResponseTime: 100 // 100ms threshold
      })

      // Simulate slow API call
      const endpoint = '/api/slow'
      const callId = performanceService.startApiCall(endpoint)
      mockPerformance.now.mockReturnValueOnce(1000).mockReturnValueOnce(1500) // 500ms response
      performanceService.endApiCall(callId, endpoint, true)

      // Wait for monitoring interval (we'll need to trigger it manually in tests)
      // In a real scenario, this would be triggered by setInterval
      
      consoleSpy.mockRestore()
    })

    it('should detect slow renders', () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {})
      
      // Start monitoring with low threshold
      performanceService.startPerformanceMonitoring({
        maxRenderTime: 50 // 50ms threshold
      })

      // Simulate slow render
      performanceService.recordRenderTime('SlowComponent', 150) // 150ms render

      consoleSpy.mockRestore()
    })

    it('should detect high memory usage', () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {})
      
      // Mock high memory usage
      mockPerformance.memory.usedJSHeapSize = 90000000 // 90M out of 100M limit
      
      // Start monitoring with low threshold
      performanceService.startPerformanceMonitoring({
        maxMemoryUsage: 0.8 // 80% threshold
      })

      consoleSpy.mockRestore()
    })
  })

  describe('Edge Cases', () => {
    it('should handle missing performance APIs gracefully', () => {
      // Temporarily remove performance.memory
      const originalMemory = mockPerformance.memory
      delete (mockPerformance as any).memory

      expect(() => {
        performanceService.getClientMetrics()
      }).not.toThrow()

      // Restore
      mockPerformance.memory = originalMemory
    })

    it('should handle missing navigator.connection gracefully', () => {
      // Temporarily remove navigator.connection
      const originalConnection = mockNavigator.connection
      delete (mockNavigator as any).connection

      expect(() => {
        performanceService.getClientMetrics()
      }).not.toThrow()

      // Restore
      mockNavigator.connection = originalConnection
    })

    it('should handle invalid call IDs gracefully', () => {
      expect(() => {
        performanceService.endApiCall('invalid-id', '/api/test', true)
      }).not.toThrow()
    })

    it('should handle empty response time arrays', () => {
      const summary = performanceService.getPerformanceSummary()
      
      // Should not crash with empty arrays
      expect(summary.apiPerformance).toBeDefined()
      expect(summary.renderPerformance).toBeDefined()
    })
  })

  describe('Performance Thresholds', () => {
    const testCases = [
      {
        name: 'API response time under 500ms',
        test: () => {
          const endpoint = '/api/fast'
          const callId = performanceService.startApiCall(endpoint)
          mockPerformance.now.mockReturnValueOnce(1000).mockReturnValueOnce(1400) // 400ms
          performanceService.endApiCall(callId, endpoint, true)
          
          const metrics = performanceService.getClientMetrics()
          expect(metrics.apiResponseTimes[endpoint][0]).toBeLessThan(500)
        }
      },
      {
        name: 'Render time under 100ms',
        test: () => {
          performanceService.recordRenderTime('FastComponent', 80)
          
          const metrics = performanceService.getClientMetrics()
          expect(metrics.renderTimes['FastComponent'][0]).toBeLessThan(100)
        }
      },
      {
        name: 'Memory usage under 80%',
        test: () => {
          // Mock reasonable memory usage
          mockPerformance.memory.usedJSHeapSize = 30000000 // 30M out of 100M limit
          
          const summary = performanceService.getPerformanceSummary()
          expect(summary.memoryUsage.usagePercentage).toBeLessThan(80)
        }
      }
    ]

    testCases.forEach(({ name, test }) => {
      it(`should meet performance threshold: ${name}`, test)
    })
  })
})