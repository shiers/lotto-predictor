import type { 
  SearchCriteria, 
  AdvancedSearchResult, 
  SearchConfiguration,
  ExportRequest,
  SavedSearchSummary 
} from '../types/advancedSearch'

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'

export class AdvancedSearchService {
  static async executeSearch(criteria: SearchCriteria): Promise<AdvancedSearchResult> {
    const backendRequest = this.convertToBackendFormat(criteria)
    
    const response = await fetch(`${API_BASE}/lookup/advanced-search`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(backendRequest)
    })

    if (!response.ok) {
      throw new Error(`Search failed: ${response.statusText}`)
    }

    const backendResult = await response.json()
    
    // Convert backend result to frontend format
    return {
      totalResults: backendResult.totalResults,
      numberOccurrences: backendResult.occurrences,
      combinationMatches: backendResult.combinationMatches,
      frequencyData: [], // Would need to be populated from a separate frequency analysis
      executionTime: backendResult.executionTime?.totalMilliseconds || 0,
      searchCriteria: criteria
    }
  }

  static async exportResults(exportRequest: ExportRequest): Promise<Blob> {
    const response = await fetch(`${API_BASE}/lookup/export`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(exportRequest)
    })

    if (!response.ok) {
      throw new Error(`Export failed: ${response.statusText}`)
    }

    return await response.blob()
  }

  static async saveSearchConfiguration(config: SearchConfiguration): Promise<SearchConfiguration> {
    const saveRequest = {
      name: config.name,
      description: config.description || '',
      searchRequest: this.convertToBackendFormat(config.criteria),
      isPublic: false
    }

    const response = await fetch(`${API_BASE}/lookup/search-configurations`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(saveRequest)
    })

    if (!response.ok) {
      throw new Error(`Save failed: ${response.statusText}`)
    }

    return await response.json()
  }

  static async getSavedSearches(): Promise<SavedSearchSummary[]> {
    const response = await fetch(`${API_BASE}/lookup/search-configurations`)

    if (!response.ok) {
      throw new Error(`Failed to load saved searches: ${response.statusText}`)
    }

    const configs = await response.json()
    return configs.map((config: any) => ({
      id: config.id.toString(),
      name: config.name,
      description: config.description,
      createdAt: config.createdAt,
      lastUsed: config.lastUsed
    }))
  }

  static async loadSearchConfiguration(id: string): Promise<SearchConfiguration> {
    const response = await fetch(`${API_BASE}/lookup/search-configurations/${id}`)

    if (!response.ok) {
      throw new Error(`Failed to load search configuration: ${response.statusText}`)
    }

    const config = await response.json()
    return {
      id: config.id.toString(),
      name: config.name,
      description: config.description,
      criteria: this.convertFromBackendFormat(config.searchRequest),
      createdAt: config.createdAt,
      updatedAt: config.lastUsed
    }
  }

  static async deleteSearchConfiguration(id: string): Promise<void> {
    const response = await fetch(`${API_BASE}/lookup/search-configurations/${id}`, {
      method: 'DELETE'
    })

    if (!response.ok) {
      throw new Error(`Delete failed: ${response.statusText}`)
    }
  }

  static async updateSearchConfiguration(id: string, config: Partial<SearchConfiguration>): Promise<SearchConfiguration> {
    const updateRequest = {
      name: config.name,
      description: config.description || '',
      searchRequest: config.criteria ? this.convertToBackendFormat(config.criteria) : undefined,
      isPublic: false
    }

    const response = await fetch(`${API_BASE}/lookup/search-configurations/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(updateRequest)
    })

    if (!response.ok) {
      throw new Error(`Update failed: ${response.statusText}`)
    }

    return await response.json()
  }

  private static convertToBackendFormat(criteria: SearchCriteria): any {
    // Convert frontend SearchCriteria to backend AdvancedSearchRequest format
    const backendCriteria = criteria.conditions.map(condition => ({
      type: this.mapConditionType(condition.type),
      value: condition.value,
      operator: 'Equals' // Default operator
    }))

    return {
      criteria: backendCriteria,
      logicalOperator: criteria.logic === 'AND' ? 'And' : 'Or',
      startDate: criteria.dateRange.startDate,
      endDate: criteria.dateRange.endDate,
      frequencyFilter: {
        minOccurrences: criteria.frequencyFilters.minOccurrences,
        maxOccurrences: criteria.frequencyFilters.maxOccurrences
      },
      includeBonus: criteria.includeBonus,
      includePowerball: criteria.includePowerball,
      maxResults: 100
    }
  }

  private static convertFromBackendFormat(backendRequest: any): SearchCriteria {
    // Convert backend AdvancedSearchRequest to frontend SearchCriteria format
    const conditions = backendRequest.criteria?.map((criterion: any) => ({
      id: `condition_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      type: this.mapBackendConditionType(criterion.type),
      value: criterion.value
    })) || []

    return {
      conditions,
      logic: backendRequest.logicalOperator === 'And' ? 'AND' : 'OR',
      dateRange: {
        startDate: backendRequest.startDate ? new Date(backendRequest.startDate) : null,
        endDate: backendRequest.endDate ? new Date(backendRequest.endDate) : null
      },
      frequencyFilters: {
        minOccurrences: backendRequest.frequencyFilter?.minOccurrences || null,
        maxOccurrences: backendRequest.frequencyFilter?.maxOccurrences || null
      },
      includeBonus: backendRequest.includeBonus ?? true,
      includePowerball: backendRequest.includePowerball ?? true
    }
  }

  private static mapConditionType(frontendType: string): string {
    const mapping: Record<string, string> = {
      'number': 'Number',
      'combination': 'Combination',
      'range': 'NumberRange',
      'frequency': 'Frequency',
      'date': 'Date',
      'gap': 'Frequency' // Map gap to frequency for backend
    }
    return mapping[frontendType] || 'Number'
  }

  private static mapBackendConditionType(backendType: string): string {
    const mapping: Record<string, string> = {
      'Number': 'number',
      'Combination': 'combination',
      'NumberRange': 'range',
      'Frequency': 'frequency',
      'Date': 'date',
      'Position': 'number'
    }
    return mapping[backendType] || 'number'
  }

  static validateSearchCriteria(criteria: SearchCriteria): string[] {
    const errors: string[] = []

    if (criteria.conditions.length === 0) {
      errors.push('At least one search condition is required')
    }

    for (const condition of criteria.conditions) {
      switch (condition.type) {
        case 'number':
          if (!condition.value.number || condition.value.number < 1 || condition.value.number > 40) {
            errors.push('Number must be between 1 and 40')
          }
          break

        case 'combination':
          if (!condition.value.numbers || condition.value.numbers.length < 2 || condition.value.numbers.length > 6) {
            errors.push('Combination must contain 2-6 numbers')
          }
          if (condition.value.numbers && new Set(condition.value.numbers).size !== condition.value.numbers.length) {
            errors.push('Combination numbers must be unique')
          }
          if (condition.value.numbers && condition.value.numbers.some((n: number) => n < 1 || n > 40)) {
            errors.push('All combination numbers must be between 1 and 40')
          }
          break

        case 'range':
          if (!condition.value.startNumber || !condition.value.endNumber) {
            errors.push('Range must have both start and end numbers')
          }
          if (condition.value.startNumber >= condition.value.endNumber) {
            errors.push('Range start must be less than end')
          }
          if (condition.value.startNumber < 1 || condition.value.endNumber > 40) {
            errors.push('Range numbers must be between 1 and 40')
          }
          break

        case 'frequency':
          if (!condition.value.value || condition.value.value < 0) {
            errors.push('Frequency value must be positive')
          }
          if (condition.value.operator === 'between' && (!condition.value.secondValue || condition.value.secondValue <= condition.value.value)) {
            errors.push('Second frequency value must be greater than first')
          }
          break

        case 'date':
          if (condition.value.startDate && condition.value.endDate && condition.value.startDate >= condition.value.endDate) {
            errors.push('Start date must be before end date')
          }
          break

        case 'gap':
          if (!condition.value.value || condition.value.value < 0) {
            errors.push('Gap value must be positive')
          }
          break
      }
    }

    if (criteria.dateRange.startDate && criteria.dateRange.endDate && criteria.dateRange.startDate >= criteria.dateRange.endDate) {
      errors.push('Global start date must be before end date')
    }

    if (criteria.frequencyFilters.minOccurrences !== null && criteria.frequencyFilters.maxOccurrences !== null) {
      if (criteria.frequencyFilters.minOccurrences >= criteria.frequencyFilters.maxOccurrences) {
        errors.push('Minimum occurrences must be less than maximum')
      }
    }

    return errors
  }
}