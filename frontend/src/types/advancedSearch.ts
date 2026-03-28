export interface SearchCondition {
  id: string
  type: 'number' | 'combination' | 'range' | 'frequency' | 'date' | 'gap'
  value: any
}

export interface NumberCondition {
  number: number
  position?: string
}

export interface CombinationCondition {
  numbersInput: string
  numbers: number[]
  matchType: 'exact' | 'partial'
  minimumMatches?: number
}

export interface RangeCondition {
  startNumber: number
  endNumber: number
  includeInRange: boolean
}

export interface FrequencyCondition {
  operator: 'greater' | 'less' | 'equal' | 'between'
  value: number
  secondValue?: number
  timeframe?: 'all' | 'last30' | 'last90' | 'last365'
}

export interface DateCondition {
  startDate: Date | null
  endDate: Date | null
}

export interface GapCondition {
  operator: 'greater' | 'less' | 'equal'
  value: number
  gapType: 'current' | 'longest' | 'average'
}

export interface SearchCriteria {
  conditions: SearchCondition[]
  logic: 'AND' | 'OR'
  dateRange: {
    startDate: Date | null
    endDate: Date | null
  }
  frequencyFilters: {
    minOccurrences: number | null
    maxOccurrences: number | null
  }
  includeBonus: boolean
  includePowerball: boolean
}

export interface AdvancedSearchResult {
  totalResults: number
  numberOccurrences?: import('./lottery').NumberOccurrence[]
  combinationMatches?: import('./lottery').CombinationMatch[]
  frequencyData?: import('./lottery').NumberFrequency[]
  executionTime: number
  searchCriteria: SearchCriteria
}

export interface SearchConfiguration {
  id?: string
  name: string
  description?: string
  criteria: SearchCriteria
  createdAt: string
  updatedAt?: string
  userId?: string
}

export interface ExportRequest {
  data: AdvancedSearchResult
  format: 'csv' | 'json' | 'pdf' | 'excel'
  fileName?: string
  includeMetadata: boolean
  searchCriteria: SearchCriteria
}

export interface SavedSearchSummary {
  id: string
  name: string
  description?: string
  createdAt: string
  lastUsed?: string
  resultCount?: number
}