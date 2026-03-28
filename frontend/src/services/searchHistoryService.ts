import api from './api'

export interface SearchHistoryItem {
  id: number
  userId: string
  searchType: string
  searchCriteria: string
  resultCount: number
  searchedAt: string
  executionTime: string
  parsedCriteria?: any // Parsed JSON criteria for easier access
}

export interface SearchHistoryFilter {
  searchType?: string
  startDate?: string
  endDate?: string
  limit?: number
  offset?: number
}

export class SearchHistoryService {
  private readonly baseUrl = '/search-history'

  async getUserSearchHistory(filter?: SearchHistoryFilter): Promise<SearchHistoryItem[]> {
    const params = new URLSearchParams()
    
    if (filter?.searchType) params.append('searchType', filter.searchType)
    if (filter?.startDate) params.append('startDate', filter.startDate)
    if (filter?.endDate) params.append('endDate', filter.endDate)
    if (filter?.limit) params.append('limit', filter.limit.toString())
    if (filter?.offset) params.append('offset', filter.offset.toString())

    const url = params.toString() ? `${this.baseUrl}?${params}` : this.baseUrl
    const response = await api.get<SearchHistoryItem[]>(url)
    
    // Parse the JSON criteria for easier access
    return response.data.map(item => ({
      ...item,
      parsedCriteria: this.parseSearchCriteria(item.searchCriteria)
    }))
  }

  async getRecentSearches(limit: number = 10): Promise<SearchHistoryItem[]> {
    const response = await api.get<SearchHistoryItem[]>(`${this.baseUrl}/recent?limit=${limit}`)
    return response.data.map(item => ({
      ...item,
      parsedCriteria: this.parseSearchCriteria(item.searchCriteria)
    }))
  }

  async getPopularSearches(limit: number = 10): Promise<SearchHistoryItem[]> {
    const response = await api.get<SearchHistoryItem[]>(`${this.baseUrl}/popular?limit=${limit}`)
    return response.data.map(item => ({
      ...item,
      parsedCriteria: this.parseSearchCriteria(item.searchCriteria)
    }))
  }

  async deleteSearchHistoryItem(id: number): Promise<boolean> {
    try {
      await api.delete(`${this.baseUrl}/${id}`)
      return true
    } catch (error) {
      console.error('Error deleting search history item:', error)
      return false
    }
  }

  async clearSearchHistory(): Promise<boolean> {
    try {
      await api.delete(this.baseUrl)
      return true
    } catch (error) {
      console.error('Error clearing search history:', error)
      return false
    }
  }

  async saveSearch(searchType: string, criteria: any, resultCount: number): Promise<SearchHistoryItem> {
    const request = {
      searchType,
      searchCriteria: JSON.stringify(criteria),
      resultCount
    }
    
    const response = await api.post<SearchHistoryItem>(this.baseUrl, request)
    return {
      ...response.data,
      parsedCriteria: criteria
    }
  }

  // Helper method to parse search criteria JSON
  private parseSearchCriteria(criteriaJson: string): any {
    try {
      return JSON.parse(criteriaJson)
    } catch (error) {
      console.warn('Failed to parse search criteria:', error)
      return {}
    }
  }

  // Helper method to format search criteria for display
  formatSearchCriteria(item: SearchHistoryItem): string {
    const criteria = item.parsedCriteria || this.parseSearchCriteria(item.searchCriteria)
    
    switch (item.searchType.toLowerCase()) {
      case 'number':
        return `Numbers: ${criteria.numbers?.join(', ') || 'N/A'}`
      case 'combination':
        return `Combination: ${criteria.combination?.join(', ') || 'N/A'}`
      case 'range':
        return `Range: ${criteria.ranges?.map((r: any) => `${r.startNumber}-${r.endNumber}`).join(', ') || 'N/A'}`
      case 'frequency':
        return `Frequency Analysis: ${criteria.ranges?.length || 0} ranges`
      default:
        return 'Unknown search type'
    }
  }

  // Helper method to get search type display name
  getSearchTypeDisplayName(searchType: string): string {
    switch (searchType.toLowerCase()) {
      case 'number':
        return 'Number Lookup'
      case 'combination':
        return 'Combination Search'
      case 'range':
        return 'Range Analysis'
      case 'frequency':
        return 'Frequency Analysis'
      case 'advanced':
        return 'Advanced Search'
      default:
        return searchType
    }
  }

  // Helper method to create search criteria for reuse
  createSearchCriteriaForReuse(item: SearchHistoryItem): any {
    const criteria = item.parsedCriteria || this.parseSearchCriteria(item.searchCriteria)
    
    // Return criteria in a format that can be used to populate search forms
    return {
      type: item.searchType,
      ...criteria
    }
  }
}

export const searchHistoryService = new SearchHistoryService()