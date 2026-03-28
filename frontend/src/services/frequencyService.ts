import api from './api'

// Types for frequency analysis
export interface NumberRange {
  startNumber: number
  endNumber: number
  label?: string
}

export interface FrequencyAnalysisRequest {
  ranges?: NumberRange[]
  startDate?: string
  endDate?: string
  includeBonus?: boolean
  includePowerball?: boolean
}

export interface NumberFrequency {
  number: number
  totalOccurrences: number
  lastAppearance: string
  firstAppearance: string
  longestGap: number
  currentGap: number
  percentage: number
  averageFrequency: number
  isHot: boolean
  isCold: boolean
}

export interface RangeFrequency {
  range: NumberRange
  totalOccurrences: number
  percentage: number
  averagePerDraw: number
  individualNumbers: NumberFrequency[]
}

export interface HotColdNumber {
  number: number
  recentOccurrences: number
  historicalAverage: number
  hotColdScore: number
  classification: 'Hot' | 'Cold' | 'Normal'
}

export interface FrequencyExportRequest {
  searchCriteria: FrequencyAnalysisRequest
  format: 'CSV' | 'JSON' | 'PDF' | 'Excel'
  includeCharts?: boolean
  fileName?: string
}

export interface ExportResult {
  exportId: string
  downloadUrl: string
  expiresAt: string
  fileSizeBytes: number
}

export interface ExportStatus {
  exportId: string
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed'
  progress?: number
  errorMessage?: string
  downloadUrl?: string
}

export interface FrequencyStatistics {
  totalNumbers: number
  mostFrequent: NumberFrequency[]
  leastFrequent: NumberFrequency[]
  averageOccurrences: number
  totalOccurrences: number
  hotNumbers: number
  coldNumbers: number
  lastUpdated: string
}

// Frequency Service
export class FrequencyService {
  // Get frequency analysis for all numbers
  static async getNumberFrequencies(): Promise<NumberFrequency[]> {
    const response = await api.get('/frequency/numbers')
    return response.data
  }

  // Get frequency analysis for specific ranges
  static async getRangeFrequencies(request: FrequencyAnalysisRequest): Promise<RangeFrequency[]> {
    const response = await api.post('/frequency/ranges', request)
    return response.data
  }

  // Compare frequencies across different criteria
  static async compareFrequencies(request: FrequencyAnalysisRequest): Promise<NumberFrequency[]> {
    const response = await api.post('/frequency/compare', request)
    return response.data
  }

  // Get hot and cold number analysis
  static async getHotColdAnalysis(periodDays: number = 365): Promise<HotColdNumber[]> {
    const response = await api.get(`/frequency/hot-cold?periodDays=${periodDays}`)
    return response.data
  }

  // Get frequency statistics for a specific number
  static async getNumberFrequency(number: number): Promise<NumberFrequency> {
    const response = await api.get(`/frequency/number/${number}`)
    return response.data
  }

  // Get frequency trends over time periods
  static async getFrequencyTrends(
    numbers?: number[],
    startDate?: string,
    endDate?: string,
    intervalDays: number = 30
  ): Promise<any> {
    const params = new URLSearchParams()
    if (numbers && numbers.length > 0) {
      numbers.forEach(n => params.append('numbers', n.toString()))
    }
    if (startDate) params.append('startDate', startDate)
    if (endDate) params.append('endDate', endDate)
    params.append('intervalDays', intervalDays.toString())

    const response = await api.get(`/frequency/trends?${params.toString()}`)
    return response.data
  }

  // Refresh frequency data cache
  static async refreshFrequencyData(): Promise<void> {
    await api.post('/frequency/refresh')
  }

  // Export frequency analysis results
  static async exportFrequencyData(request: FrequencyExportRequest): Promise<ExportResult> {
    const response = await api.post('/frequency/export', request)
    return response.data
  }

  // Get export status
  static async getExportStatus(exportId: string): Promise<ExportStatus> {
    const response = await api.get(`/frequency/export/${exportId}/status`)
    return response.data
  }

  // Get frequency statistics summary
  static async getFrequencyStatistics(): Promise<FrequencyStatistics> {
    const response = await api.get('/frequency/statistics')
    return response.data
  }

  // Get frequency distribution by position
  static async getPositionDistribution(): Promise<any> {
    const response = await api.get('/frequency/position-distribution')
    return response.data
  }

  // Utility methods
  static validateNumberRange(range: NumberRange): { isValid: boolean; errors: string[] } {
    const errors: string[] = []
    
    if (range.startNumber < 1 || range.startNumber > 40) {
      errors.push('Start number must be between 1 and 40')
    }
    
    if (range.endNumber < 1 || range.endNumber > 40) {
      errors.push('End number must be between 1 and 40')
    }
    
    if (range.startNumber > range.endNumber) {
      errors.push('Start number must be less than or equal to end number')
    }
    
    return {
      isValid: errors.length === 0,
      errors
    }
  }

  static formatDate(date: Date): string {
    return date.toISOString().split('T')[0]
  }

  static getPresetRanges(): NumberRange[] {
    return [
      { startNumber: 1, endNumber: 10, label: '1-10 (Low)' },
      { startNumber: 11, endNumber: 20, label: '11-20 (Mid-Low)' },
      { startNumber: 21, endNumber: 30, label: '21-30 (Mid-High)' },
      { startNumber: 31, endNumber: 40, label: '31-40 (High)' },
      { startNumber: 1, endNumber: 20, label: '1-20 (Lower Half)' },
      { startNumber: 21, endNumber: 40, label: '21-40 (Upper Half)' },
      { startNumber: 1, endNumber: 5, label: '1-5 (Very Low)' },
      { startNumber: 36, endNumber: 40, label: '36-40 (Very High)' }
    ]
  }

  static classifyFrequency(frequency: NumberFrequency, allFrequencies: NumberFrequency[]): string {
    const average = allFrequencies.reduce((sum, f) => sum + f.totalOccurrences, 0) / allFrequencies.length
    const stdDev = Math.sqrt(
      allFrequencies.reduce((sum, f) => sum + Math.pow(f.totalOccurrences - average, 2), 0) / allFrequencies.length
    )
    
    if (frequency.totalOccurrences > average + stdDev) {
      return 'High'
    } else if (frequency.totalOccurrences < average - stdDev) {
      return 'Low'
    } else {
      return 'Normal'
    }
  }

  static getFrequencyColor(frequency: NumberFrequency, allFrequencies: NumberFrequency[]): string {
    const classification = this.classifyFrequency(frequency, allFrequencies)
    switch (classification) {
      case 'High': return '#ff4444'
      case 'Low': return '#4444ff'
      default: return '#44ff44'
    }
  }
}