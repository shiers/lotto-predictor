import api from './api'
import { suggestionCache } from './suggestionCacheService'

// Types for lookup functionality
export interface NumberLookupRequest {
  numbers: number[]
  startDate?: string
  endDate?: string
  includeBonus?: boolean
  includePowerball?: boolean
}

export interface NumberOccurrence {
  drawNumber: number
  drawDate: string
  number: number
  position: number
  isBonus: boolean
  isPowerball: boolean
  fullCombination: number[]
}

export interface CombinationSearchRequest {
  combination: number[]
  includePartialMatches?: boolean
  minimumMatches?: number
  startDate?: string
  endDate?: string
}

export interface CombinationMatch {
  drawNumber: number
  drawDate: string
  winningCombination: number[]
  matchedNumbers: number[]
  matchCount: number
  isExactMatch: boolean
}

export interface CombinationSearchResult {
  searchedCombination: number[]
  exactMatches: CombinationMatch[]
  partialMatches: CombinationMatch[]
  totalExactMatches: number
  totalPartialMatches: number
}

export interface SearchSuggestion {
  text: string
  type: 'number' | 'combination' | 'range' | 'date' | 'drawNumber'
  relevance: number
  previewInfo: string
  metadata?: any
}

export interface AutoCompleteRequest {
  query: string
  type: 'number' | 'combination' | 'range' | 'date' | 'drawNumber'
  maxSuggestions?: number
  includePreview?: boolean
}

// Lookup Service
export class LookupService {
  // Single number lookup
  static async lookupNumber(number: number): Promise<NumberOccurrence[]> {
    const response = await api.get(`/lookup/number/${number}`)
    return response.data
  }

  // Multiple numbers lookup
  static async lookupNumbers(request: NumberLookupRequest): Promise<NumberOccurrence[]> {
    const response = await api.post('/lookup/numbers', request)
    return response.data
  }

  // Combination search
  static async searchCombination(request: CombinationSearchRequest): Promise<CombinationSearchResult> {
    const response = await api.post('/lookup/combination', request)
    return response.data
  }

  // Get search suggestions with caching
  static async getSearchSuggestions(request: AutoCompleteRequest): Promise<SearchSuggestion[]> {
    // Check cache first
    const cached = suggestionCache.get(request.query, request.type)
    if (cached) {
      return cached
    }
    
    try {
      const response = await api.post('/lookup/suggestions', request)
      const suggestions = response.data
      
      // Cache the results
      suggestionCache.set(request.query, request.type, suggestions)
      
      return suggestions
    } catch (error) {
      // Try partial match from cache as fallback
      const partialMatch = suggestionCache.getPartialMatch(request.query, request.type)
      if (partialMatch) {
        return partialMatch
      }
      throw error
    }
  }

  // Validate lottery number
  static validateNumber(number: number): boolean {
    return number >= 1 && number <= 40 && Number.isInteger(number)
  }

  // Validate combination
  static validateCombination(numbers: number[]): { isValid: boolean; errors: string[] } {
    const errors: string[] = []
    
    if (numbers.length < 2) {
      errors.push('Combination must contain at least 2 numbers')
    }
    
    if (numbers.length > 6) {
      errors.push('Combination cannot contain more than 6 numbers')
    }
    
    const uniqueNumbers = new Set(numbers)
    if (uniqueNumbers.size !== numbers.length) {
      errors.push('Combination cannot contain duplicate numbers')
    }
    
    const invalidNumbers = numbers.filter(n => !this.validateNumber(n))
    if (invalidNumbers.length > 0) {
      errors.push(`Invalid numbers: ${invalidNumbers.join(', ')}. Numbers must be between 1 and 40`)
    }
    
    return {
      isValid: errors.length === 0,
      errors
    }
  }

  // Format date for API
  static formatDate(date: Date): string {
    return date.toISOString().split('T')[0]
  }

  // Parse number input string
  static parseNumberInput(input: string): number[] {
    return input
      .split(/[,\s]+/)
      .map(s => s.trim())
      .filter(s => s.length > 0)
      .map(s => parseInt(s, 10))
      .filter(n => !isNaN(n))
  }
}