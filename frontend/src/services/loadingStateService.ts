import { ref, reactive, computed } from 'vue'

export interface LoadingOperation {
  id: string
  type: string
  message: string
  progress: number
  startTime: Date
  estimatedDuration?: number
  canCancel: boolean
  itemsProcessed?: number
  totalItems?: number
}

export interface LoadingState {
  isLoading: boolean
  operations: Map<string, LoadingOperation>
  globalProgress: number
  hasLongRunningOperations: boolean
}

class LoadingStateService {
  private state = reactive<LoadingState>({
    isLoading: false,
    operations: new Map(),
    globalProgress: 0,
    hasLongRunningOperations: false
  })

  private progressUpdateCallbacks = new Map<string, (progress: number, message?: string) => void>()
  private completionCallbacks = new Map<string, (success: boolean, result?: any) => void>()

  /**
   * Start a new loading operation
   */
  startOperation(
    type: string, 
    message: string, 
    options: {
      canCancel?: boolean
      estimatedDuration?: number
      totalItems?: number
    } = {}
  ): string {
    const id = this.generateOperationId()
    
    const operation: LoadingOperation = {
      id,
      type,
      message,
      progress: 0,
      startTime: new Date(),
      canCancel: options.canCancel ?? false,
      estimatedDuration: options.estimatedDuration,
      totalItems: options.totalItems,
      itemsProcessed: 0
    }

    this.state.operations.set(id, operation)
    this.updateGlobalState()

    console.log(`Started loading operation: ${type} (${id})`)
    return id
  }

  /**
   * Update progress for an operation
   */
  updateProgress(
    operationId: string, 
    progress: number, 
    message?: string,
    itemsProcessed?: number
  ): void {
    const operation = this.state.operations.get(operationId)
    if (!operation) {
      console.warn(`Operation ${operationId} not found for progress update`)
      return
    }

    operation.progress = Math.max(0, Math.min(100, progress))
    if (message) {
      operation.message = message
    }
    if (itemsProcessed !== undefined) {
      operation.itemsProcessed = itemsProcessed
    }

    // Calculate estimated time remaining
    if (operation.progress > 0) {
      const elapsed = Date.now() - operation.startTime.getTime()
      const estimatedTotal = (elapsed / operation.progress) * 100
      const remaining = estimatedTotal - elapsed
      operation.estimatedDuration = Math.max(0, remaining)
    }

    this.updateGlobalState()

    // Call progress callback if registered
    const callback = this.progressUpdateCallbacks.get(operationId)
    if (callback) {
      callback(progress, message)
    }
  }

  /**
   * Complete an operation
   */
  completeOperation(operationId: string, success: boolean = true, result?: any): void {
    const operation = this.state.operations.get(operationId)
    if (!operation) {
      console.warn(`Operation ${operationId} not found for completion`)
      return
    }

    // Call completion callback if registered
    const callback = this.completionCallbacks.get(operationId)
    if (callback) {
      callback(success, result)
    }

    // Remove operation
    this.state.operations.delete(operationId)
    this.progressUpdateCallbacks.delete(operationId)
    this.completionCallbacks.delete(operationId)

    this.updateGlobalState()

    const duration = Date.now() - operation.startTime.getTime()
    console.log(`Completed loading operation: ${operation.type} (${operationId}) in ${duration}ms`)
  }

  /**
   * Cancel an operation
   */
  cancelOperation(operationId: string): void {
    const operation = this.state.operations.get(operationId)
    if (!operation) {
      console.warn(`Operation ${operationId} not found for cancellation`)
      return
    }

    if (!operation.canCancel) {
      console.warn(`Operation ${operationId} cannot be cancelled`)
      return
    }

    this.completeOperation(operationId, false)
    console.log(`Cancelled loading operation: ${operation.type} (${operationId})`)
  }

  /**
   * Register a progress update callback
   */
  onProgress(operationId: string, callback: (progress: number, message?: string) => void): void {
    this.progressUpdateCallbacks.set(operationId, callback)
  }

  /**
   * Register a completion callback
   */
  onComplete(operationId: string, callback: (success: boolean, result?: any) => void): void {
    this.completionCallbacks.set(operationId, callback)
  }

  /**
   * Get current loading state
   */
  getState(): LoadingState {
    return this.state
  }

  /**
   * Get specific operation
   */
  getOperation(operationId: string): LoadingOperation | undefined {
    return this.state.operations.get(operationId)
  }

  /**
   * Get operations by type
   */
  getOperationsByType(type: string): LoadingOperation[] {
    return Array.from(this.state.operations.values())
      .filter(op => op.type === type)
  }

  /**
   * Check if any operations are running
   */
  get isLoading(): boolean {
    return this.state.isLoading
  }

  /**
   * Get global progress (average of all operations)
   */
  get globalProgress(): number {
    return this.state.globalProgress
  }

  /**
   * Check if there are long-running operations (>5 seconds)
   */
  get hasLongRunningOperations(): boolean {
    return this.state.hasLongRunningOperations
  }

  /**
   * Get all active operations
   */
  get activeOperations(): LoadingOperation[] {
    return Array.from(this.state.operations.values())
  }

  /**
   * Clear all operations (use with caution)
   */
  clearAll(): void {
    this.state.operations.clear()
    this.progressUpdateCallbacks.clear()
    this.completionCallbacks.clear()
    this.updateGlobalState()
    console.log('Cleared all loading operations')
  }

  /**
   * Create a loading wrapper for async operations
   */
  async withLoading<T>(
    type: string,
    message: string,
    operation: (updateProgress: (progress: number, message?: string) => void) => Promise<T>,
    options: {
      canCancel?: boolean
      estimatedDuration?: number
      totalItems?: number
    } = {}
  ): Promise<T> {
    const operationId = this.startOperation(type, message, options)
    
    try {
      const updateProgress = (progress: number, message?: string) => {
        this.updateProgress(operationId, progress, message)
      }

      const result = await operation(updateProgress)
      this.completeOperation(operationId, true, result)
      return result
    } catch (error) {
      this.completeOperation(operationId, false)
      throw error
    }
  }

  private updateGlobalState(): void {
    const operations = Array.from(this.state.operations.values())
    
    this.state.isLoading = operations.length > 0
    
    if (operations.length === 0) {
      this.state.globalProgress = 0
    } else {
      this.state.globalProgress = operations.reduce((sum, op) => sum + op.progress, 0) / operations.length
    }

    // Check for long-running operations (>5 seconds)
    const now = Date.now()
    this.state.hasLongRunningOperations = operations.some(op => 
      now - op.startTime.getTime() > 5000
    )
  }

  private generateOperationId(): string {
    return `op_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
  }
}

// Create singleton instance
export const loadingStateService = new LoadingStateService()

// Vue composable for reactive loading state
export function useLoadingState() {
  const state = loadingStateService.getState()
  
  return {
    isLoading: computed(() => state.isLoading),
    globalProgress: computed(() => state.globalProgress),
    hasLongRunningOperations: computed(() => state.hasLongRunningOperations),
    activeOperations: computed(() => Array.from(state.operations.values())),
    
    startOperation: loadingStateService.startOperation.bind(loadingStateService),
    updateProgress: loadingStateService.updateProgress.bind(loadingStateService),
    completeOperation: loadingStateService.completeOperation.bind(loadingStateService),
    cancelOperation: loadingStateService.cancelOperation.bind(loadingStateService),
    withLoading: loadingStateService.withLoading.bind(loadingStateService),
    
    onProgress: loadingStateService.onProgress.bind(loadingStateService),
    onComplete: loadingStateService.onComplete.bind(loadingStateService)
  }
}

// Common loading operation types
export const LoadingOperationType = {
  NUMBER_LOOKUP: 'number_lookup',
  COMBINATION_SEARCH: 'combination_search',
  FREQUENCY_ANALYSIS: 'frequency_analysis',
  EXPORT_GENERATION: 'export_generation',
  DATA_IMPORT: 'data_import',
  CACHE_WARMUP: 'cache_warmup'
} as const

export type LoadingOperationType = typeof LoadingOperationType[keyof typeof LoadingOperationType]