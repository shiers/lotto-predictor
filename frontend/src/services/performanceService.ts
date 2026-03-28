import api from './api'

export interface PerformanceMetrics {
  activeOperations: number
  operationsPerSecond: number
  averageResponseTime: number
  cacheHitRate: number
  memoryUsage: number
  operationCounts: Record<string, number>
  timestamp: string
}

export interface PerformanceStatistics {
  operationType: string
  totalOperations: number
  successfulOperations: number
  failedOperations: number
  successRate: number
  averageResponseTime: number
  medianResponseTime: number
  p95ResponseTime: number
  p99ResponseTime: number
  minResponseTime: number
  maxResponseTime: number
  averageResultCount: number
  maxResultCount: number
  minResultCount: number
  periodStart: string
  periodEnd: string
  slowQueries: SlowQueryInfo[]
}

export interface SlowQueryInfo {
  operationId: string
  operationType: string
  duration: number
  timestamp: string
  parameters?: any
  context: Record<string, any>
  checkpoints: CheckpointInfo[]
}

export interface CheckpointInfo {
  name: string
  elapsedTime: number
  timestamp: string
}

export interface CacheStatistics {
  memoryCacheHits: number
  memoryCacheMisses: number
  distributedCacheHits: number
  distributedCacheMisses: number
  memoryCacheEntries: number
  distributedCacheEntries: number
}

export interface ClientPerformanceMetrics {
  // Page performance
  pageLoadTime: number
  domContentLoadedTime: number
  firstContentfulPaint: number
  largestContentfulPaint: number
  
  // JavaScript performance
  jsHeapSizeUsed: number
  jsHeapSizeTotal: number
  jsHeapSizeLimit: number
  
  // Network performance
  connectionType: string
  effectiveType: string
  downlink: number
  rtt: number
  
  // Custom metrics
  apiResponseTimes: Record<string, number[]>
  renderTimes: Record<string, number[]>
  errorCounts: Record<string, number>
  
  timestamp: Date
}

class PerformanceService {
  private metrics: ClientPerformanceMetrics
  private performanceObserver?: PerformanceObserver
  private apiCallTimes = new Map<string, number>()

  constructor() {
    this.metrics = this.initializeMetrics()
    this.setupPerformanceObserver()
    this.startMetricsCollection()
  }

  /**
   * Get current server performance metrics
   */
  async getServerMetrics(): Promise<PerformanceMetrics> {
    const response = await api.get('/performance/metrics')
    return response.data
  }

  /**
   * Get server performance statistics for a time period
   */
  async getServerStatistics(
    startTime?: Date,
    endTime?: Date
  ): Promise<PerformanceStatistics> {
    const params: any = {}
    if (startTime) params.startTime = startTime.toISOString()
    if (endTime) params.endTime = endTime.toISOString()

    const response = await api.get('/performance/statistics', { params })
    return response.data
  }

  /**
   * Get cache statistics
   */
  async getCacheStatistics(): Promise<CacheStatistics> {
    const response = await api.get('/performance/cache-statistics')
    return response.data
  }

  /**
   * Get slow query analysis
   */
  async getSlowQueries(hours: number = 24, limit: number = 50): Promise<any> {
    const response = await api.get('/performance/slow-queries', {
      params: { hours, limit }
    })
    return response.data
  }

  /**
   * Warm up server cache
   */
  async warmUpCache(): Promise<void> {
    await api.post('/performance/cache/warmup')
  }

  /**
   * Clear server cache
   */
  async clearCache(level: 'memory' | 'distributed' | 'both' = 'both'): Promise<void> {
    await api.delete('/performance/cache', {
      params: { level }
    })
  }

  /**
   * Get client-side performance metrics
   */
  getClientMetrics(): ClientPerformanceMetrics {
    this.updateClientMetrics()
    return { ...this.metrics }
  }

  /**
   * Start timing an API call
   */
  startApiCall(endpoint: string): string {
    const callId = `${endpoint}_${Date.now()}_${Math.random()}`
    this.apiCallTimes.set(callId, performance.now())
    return callId
  }

  /**
   * End timing an API call
   */
  endApiCall(callId: string, endpoint: string, success: boolean = true): void {
    const startTime = this.apiCallTimes.get(callId)
    if (!startTime) return

    const duration = performance.now() - startTime
    this.apiCallTimes.delete(callId)

    // Record API response time
    if (!this.metrics.apiResponseTimes[endpoint]) {
      this.metrics.apiResponseTimes[endpoint] = []
    }
    this.metrics.apiResponseTimes[endpoint].push(duration)

    // Keep only last 100 measurements per endpoint
    if (this.metrics.apiResponseTimes[endpoint].length > 100) {
      this.metrics.apiResponseTimes[endpoint] = this.metrics.apiResponseTimes[endpoint].slice(-100)
    }

    // Record errors
    if (!success) {
      this.metrics.errorCounts[endpoint] = (this.metrics.errorCounts[endpoint] || 0) + 1
    }
  }

  /**
   * Record component render time
   */
  recordRenderTime(componentName: string, duration: number): void {
    if (!this.metrics.renderTimes[componentName]) {
      this.metrics.renderTimes[componentName] = []
    }
    this.metrics.renderTimes[componentName].push(duration)

    // Keep only last 50 measurements per component
    if (this.metrics.renderTimes[componentName].length > 50) {
      this.metrics.renderTimes[componentName] = this.metrics.renderTimes[componentName].slice(-50)
    }
  }

  /**
   * Get performance summary
   */
  getPerformanceSummary(): {
    pagePerformance: any
    apiPerformance: any
    renderPerformance: any
    memoryUsage: any
    networkInfo: any
  } {
    const metrics = this.getClientMetrics()

    return {
      pagePerformance: {
        loadTime: metrics.pageLoadTime,
        domContentLoaded: metrics.domContentLoadedTime,
        firstContentfulPaint: metrics.firstContentfulPaint,
        largestContentfulPaint: metrics.largestContentfulPaint
      },
      apiPerformance: this.calculateApiPerformanceStats(),
      renderPerformance: this.calculateRenderPerformanceStats(),
      memoryUsage: {
        used: metrics.jsHeapSizeUsed,
        total: metrics.jsHeapSizeTotal,
        limit: metrics.jsHeapSizeLimit,
        usagePercentage: (metrics.jsHeapSizeUsed / metrics.jsHeapSizeTotal) * 100
      },
      networkInfo: {
        connectionType: metrics.connectionType,
        effectiveType: metrics.effectiveType,
        downlink: metrics.downlink,
        rtt: metrics.rtt
      }
    }
  }

  /**
   * Monitor performance and alert on issues
   */
  startPerformanceMonitoring(
    thresholds: {
      maxApiResponseTime?: number
      maxRenderTime?: number
      maxMemoryUsage?: number
    } = {}
  ): void {
    const defaultThresholds = {
      maxApiResponseTime: 5000, // 5 seconds
      maxRenderTime: 100, // 100ms
      maxMemoryUsage: 0.8 // 80% of heap limit
    }

    const finalThresholds = { ...defaultThresholds, ...thresholds }

    setInterval(() => {
      const metrics = this.getClientMetrics()

      // Check API response times
      Object.entries(metrics.apiResponseTimes).forEach(([endpoint, times]) => {
        const avgTime = times.reduce((sum, time) => sum + time, 0) / times.length
        if (avgTime > finalThresholds.maxApiResponseTime) {
          console.warn(`Slow API detected: ${endpoint} averaging ${avgTime.toFixed(2)}ms`)
        }
      })

      // Check render times
      Object.entries(metrics.renderTimes).forEach(([component, times]) => {
        const avgTime = times.reduce((sum, time) => sum + time, 0) / times.length
        if (avgTime > finalThresholds.maxRenderTime) {
          console.warn(`Slow render detected: ${component} averaging ${avgTime.toFixed(2)}ms`)
        }
      })

      // Check memory usage
      const memoryUsage = metrics.jsHeapSizeUsed / metrics.jsHeapSizeLimit
      if (memoryUsage > finalThresholds.maxMemoryUsage) {
        console.warn(`High memory usage detected: ${(memoryUsage * 100).toFixed(1)}%`)
      }
    }, 30000) // Check every 30 seconds
  }

  private initializeMetrics(): ClientPerformanceMetrics {
    return {
      pageLoadTime: 0,
      domContentLoadedTime: 0,
      firstContentfulPaint: 0,
      largestContentfulPaint: 0,
      jsHeapSizeUsed: 0,
      jsHeapSizeTotal: 0,
      jsHeapSizeLimit: 0,
      connectionType: 'unknown',
      effectiveType: 'unknown',
      downlink: 0,
      rtt: 0,
      apiResponseTimes: {},
      renderTimes: {},
      errorCounts: {},
      timestamp: new Date()
    }
  }

  private setupPerformanceObserver(): void {
    if ('PerformanceObserver' in window) {
      this.performanceObserver = new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) {
          if (entry.entryType === 'navigation') {
            const navEntry = entry as PerformanceNavigationTiming
            this.metrics.pageLoadTime = navEntry.loadEventEnd - navEntry.loadEventStart
            this.metrics.domContentLoadedTime = navEntry.domContentLoadedEventEnd - navEntry.domContentLoadedEventStart
          } else if (entry.entryType === 'paint') {
            if (entry.name === 'first-contentful-paint') {
              this.metrics.firstContentfulPaint = entry.startTime
            }
          } else if (entry.entryType === 'largest-contentful-paint') {
            this.metrics.largestContentfulPaint = entry.startTime
          }
        }
      })

      try {
        this.performanceObserver.observe({ entryTypes: ['navigation', 'paint', 'largest-contentful-paint'] })
      } catch (error) {
        console.warn('Performance observer setup failed:', error)
      }
    }
  }

  private startMetricsCollection(): void {
    // Update metrics every 5 seconds
    setInterval(() => {
      this.updateClientMetrics()
    }, 5000)
  }

  private updateClientMetrics(): void {
    // Update memory metrics
    if ('memory' in performance) {
      const memory = (performance as any).memory
      this.metrics.jsHeapSizeUsed = memory.usedJSHeapSize
      this.metrics.jsHeapSizeTotal = memory.totalJSHeapSize
      this.metrics.jsHeapSizeLimit = memory.jsHeapSizeLimit
    }

    // Update network information
    if ('connection' in navigator) {
      const connection = (navigator as any).connection
      this.metrics.connectionType = connection.type || 'unknown'
      this.metrics.effectiveType = connection.effectiveType || 'unknown'
      this.metrics.downlink = connection.downlink || 0
      this.metrics.rtt = connection.rtt || 0
    }

    this.metrics.timestamp = new Date()
  }

  private calculateApiPerformanceStats(): any {
    const stats: any = {}

    Object.entries(this.metrics.apiResponseTimes).forEach(([endpoint, times]) => {
      if (times.length === 0) return

      const sorted = [...times].sort((a, b) => a - b)
      stats[endpoint] = {
        count: times.length,
        average: times.reduce((sum, time) => sum + time, 0) / times.length,
        median: sorted[Math.floor(sorted.length / 2)],
        p95: sorted[Math.floor(sorted.length * 0.95)],
        min: sorted[0],
        max: sorted[sorted.length - 1]
      }
    })

    return stats
  }

  private calculateRenderPerformanceStats(): any {
    const stats: any = {}

    Object.entries(this.metrics.renderTimes).forEach(([component, times]) => {
      if (times.length === 0) return

      const sorted = [...times].sort((a, b) => a - b)
      stats[component] = {
        count: times.length,
        average: times.reduce((sum, time) => sum + time, 0) / times.length,
        median: sorted[Math.floor(sorted.length / 2)],
        p95: sorted[Math.floor(sorted.length * 0.95)],
        min: sorted[0],
        max: sorted[sorted.length - 1]
      }
    })

    return stats
  }
}

// Create singleton instance
export const performanceService = new PerformanceService()

// Vue composable for performance monitoring
export function usePerformance() {
  return {
    getServerMetrics: performanceService.getServerMetrics.bind(performanceService),
    getServerStatistics: performanceService.getServerStatistics.bind(performanceService),
    getCacheStatistics: performanceService.getCacheStatistics.bind(performanceService),
    getSlowQueries: performanceService.getSlowQueries.bind(performanceService),
    warmUpCache: performanceService.warmUpCache.bind(performanceService),
    clearCache: performanceService.clearCache.bind(performanceService),
    getClientMetrics: performanceService.getClientMetrics.bind(performanceService),
    startApiCall: performanceService.startApiCall.bind(performanceService),
    endApiCall: performanceService.endApiCall.bind(performanceService),
    recordRenderTime: performanceService.recordRenderTime.bind(performanceService),
    getPerformanceSummary: performanceService.getPerformanceSummary.bind(performanceService),
    startPerformanceMonitoring: performanceService.startPerformanceMonitoring.bind(performanceService)
  }
}