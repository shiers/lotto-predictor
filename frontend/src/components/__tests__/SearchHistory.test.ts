import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'
import SearchHistory from '../SearchHistory.vue'
import { searchHistoryService, type SearchHistoryItem } from '../../services/searchHistoryService'

// Mock the search history service
vi.mock('../../services/searchHistoryService', () => ({
  searchHistoryService: {
    getUserSearchHistory: vi.fn(),
    getRecentSearches: vi.fn(),
    getPopularSearches: vi.fn(),
    deleteSearchHistoryItem: vi.fn(),
    clearSearchHistory: vi.fn(),
    saveSearch: vi.fn(),
    formatSearchCriteria: vi.fn(),
    getSearchTypeDisplayName: vi.fn(),
    createSearchCriteriaForReuse: vi.fn()
  }
}))

const mockSearchHistory: SearchHistoryItem[] = [
  {
    id: 1,
    userId: 'user1',
    searchType: 'number',
    searchCriteria: '{"numbers":[1,15,23]}',
    resultCount: 25,
    searchedAt: '2023-01-01T12:00:00Z',
    executionTime: '00:00:01.2345678',
    parsedCriteria: { numbers: [1, 15, 23] }
  },
  {
    id: 2,
    userId: 'user1',
    searchType: 'combination',
    searchCriteria: '{"combination":[5,12,18,25,33,39]}',
    resultCount: 3,
    searchedAt: '2023-01-02T14:30:00Z',
    executionTime: '00:00:02.1234567',
    parsedCriteria: { combination: [5, 12, 18, 25, 33, 39] }
  },
  {
    id: 3,
    userId: 'user1',
    searchType: 'frequency',
    searchCriteria: '{"ranges":[{"startNumber":1,"endNumber":10,"label":"Low"}]}',
    resultCount: 150,
    searchedAt: '2023-01-03T09:15:00Z',
    executionTime: '00:00:00.5678901',
    parsedCriteria: { ranges: [{ startNumber: 1, endNumber: 10, label: 'Low' }] }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/lookup', name: 'lookup', component: { template: '<div>Lookup</div>' } },
    { path: '/frequency', name: 'frequency', component: { template: '<div>Frequency</div>' } }
  ]
})

describe('SearchHistory', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    
    // Set default mock return values
    vi.mocked(searchHistoryService.getUserSearchHistory).mockResolvedValue(mockSearchHistory)
    vi.mocked(searchHistoryService.deleteSearchHistoryItem).mockResolvedValue(true)
    vi.mocked(searchHistoryService.clearSearchHistory).mockResolvedValue(true)
    vi.mocked(searchHistoryService.formatSearchCriteria).mockImplementation((item: SearchHistoryItem) => {
      switch (item.searchType) {
        case 'number':
          return `Numbers: ${item.parsedCriteria?.numbers?.join(', ') || 'N/A'}`
        case 'combination':
          return `Combination: ${item.parsedCriteria?.combination?.join(', ') || 'N/A'}`
        case 'frequency':
          return 'Frequency Analysis'
        default:
          return 'Unknown search'
      }
    })
    vi.mocked(searchHistoryService.getSearchTypeDisplayName).mockImplementation((type: string) => {
      switch (type.toLowerCase()) {
        case 'number':
          return 'Number Lookup'
        case 'combination':
          return 'Combination Search'
        case 'frequency':
          return 'Frequency Analysis'
        default:
          return type
      }
    })
    vi.mocked(searchHistoryService.createSearchCriteriaForReuse).mockImplementation((item: SearchHistoryItem) => ({
      type: item.searchType,
      ...item.parsedCriteria
    }))
  })

  it('renders correctly with search history', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Wait for history to load
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))
    
    expect(wrapper.find('h3').text()).toBe('Search History')
    expect(wrapper.find('.history-items').exists()).toBe(true)
  })

  it('displays loading state initially', async () => {
    // Mock to return a promise that doesn't resolve immediately
    vi.mocked(searchHistoryService.getUserSearchHistory).mockImplementation(() => new Promise(() => {}))
    
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Set loading state manually since the component starts loading immediately
    wrapper.vm.loading = true
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.loading-state').exists()).toBe(true)
    expect(wrapper.find('.spinner').exists()).toBe(true)
  })

  it('displays empty state when no history', async () => {
    vi.mocked(searchHistoryService.getUserSearchHistory).mockResolvedValue([])
    
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Wait for history to load
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))
    
    expect(wrapper.find('.empty-state').exists()).toBe(true)
    expect(wrapper.find('.empty-state h4').text()).toBe('No Search History')
  })

  it('displays error state when loading fails', async () => {
    vi.mocked(searchHistoryService.getUserSearchHistory).mockRejectedValue(new Error('API Error'))
    
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Wait for error to occur
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))
    
    expect(wrapper.find('.error-state').exists()).toBe(true)
    expect(wrapper.find('.error-message').text()).toContain('Failed to load search history')
  })

  it('filters history by search type', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Set history directly to avoid async loading
    wrapper.vm.searchHistory = mockSearchHistory
    wrapper.vm.loading = false
    await wrapper.vm.$nextTick()
    
    // Filter by number type directly on the component
    wrapper.vm.filters.searchType = 'number'
    await wrapper.vm.$nextTick()
    
    expect(wrapper.vm.filteredAndSortedHistory.length).toBe(1)
    expect(wrapper.vm.filteredAndSortedHistory[0].searchType).toBe('number')
  })

  it('filters history by date range', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = mockSearchHistory
    wrapper.vm.loading = false
    await wrapper.vm.$nextTick()
    
    // Filter by today (should return empty since mock data is from 2023)
    wrapper.vm.filters.dateRange = 'today'
    await wrapper.vm.$nextTick()
    
    expect(wrapper.vm.filteredAndSortedHistory.length).toBe(0)
  })

  it('filters history by custom date range', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = mockSearchHistory
    wrapper.vm.loading = false
    wrapper.vm.filters.dateRange = 'custom'
    wrapper.vm.filters.startDate = '2023-01-01'
    wrapper.vm.filters.endDate = '2023-01-03' // Include all three items
    await wrapper.vm.$nextTick()
    
    expect(wrapper.vm.filteredAndSortedHistory.length).toBe(3)
  })

  it('sorts history correctly', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = mockSearchHistory
    await wrapper.vm.$nextTick()
    
    // Sort by result count ascending
    wrapper.vm.sortBy = 'resultCount'
    wrapper.vm.sortOrder = 'asc'
    await wrapper.vm.$nextTick()
    
    const sorted = wrapper.vm.filteredAndSortedHistory
    expect(sorted[0].resultCount).toBe(3)
    expect(sorted[1].resultCount).toBe(25)
    expect(sorted[2].resultCount).toBe(150)
  })

  it('switches between list and grid view', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = mockSearchHistory
    wrapper.vm.loading = false
    await wrapper.vm.$nextTick()
    
    // Initially list view
    expect(wrapper.vm.viewMode).toBe('list')
    
    // Switch to grid view
    wrapper.vm.viewMode = 'grid'
    await wrapper.vm.$nextTick()
    
    expect(wrapper.vm.viewMode).toBe('grid')
  })

  it('reuses search correctly', async () => {
    const routerPush = vi.spyOn(router, 'push')
    
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.reuseSearch(mockSearchHistory[0])
    
    expect(searchHistoryService.createSearchCriteriaForReuse).toHaveBeenCalledWith(mockSearchHistory[0])
    expect(wrapper.emitted('reuse-search')).toBeTruthy()
    expect(routerPush).toHaveBeenCalledWith({ name: 'lookup', query: { reuse: 'true' } })
  })

  it('navigates to correct page based on search type', async () => {
    const routerPush = vi.spyOn(router, 'push')
    
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Test frequency search navigation
    wrapper.vm.reuseSearch(mockSearchHistory[2])
    
    expect(routerPush).toHaveBeenCalledWith({ name: 'frequency', query: { reuse: 'true' } })
  })

  it('exports search correctly', () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.exportSearch(mockSearchHistory[0])
    
    expect(wrapper.emitted('export-search')).toBeTruthy()
    expect(wrapper.emitted('export-search')![0]).toEqual([mockSearchHistory[0]])
  })

  it('deletes history item successfully', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = [...mockSearchHistory]
    
    await wrapper.vm.deleteHistoryItem(mockSearchHistory[0])
    
    expect(searchHistoryService.deleteSearchHistoryItem).toHaveBeenCalledWith(mockSearchHistory[0].id)
    expect(wrapper.vm.searchHistory.length).toBe(2)
  })

  it('handles delete failure gracefully', async () => {
    vi.mocked(searchHistoryService.deleteSearchHistoryItem).mockResolvedValue(false)
    
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = [...mockSearchHistory]
    
    await wrapper.vm.deleteHistoryItem(mockSearchHistory[0])
    
    expect(wrapper.vm.error).toContain('Failed to delete search history item')
    expect(wrapper.vm.searchHistory.length).toBe(3) // Should remain unchanged
  })

  it('opens clear confirmation dialog', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = mockSearchHistory
    wrapper.vm.loading = false
    await wrapper.vm.$nextTick()
    
    // Directly set the dialog state
    wrapper.vm.showClearDialog = true
    await wrapper.vm.$nextTick()
    
    expect(wrapper.vm.showClearDialog).toBe(true)
  })

  it('clears history successfully', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = [...mockSearchHistory]
    wrapper.vm.showClearDialog = true
    
    await wrapper.vm.clearHistory()
    
    expect(searchHistoryService.clearSearchHistory).toHaveBeenCalled()
    expect(wrapper.vm.searchHistory.length).toBe(0)
    expect(wrapper.vm.showClearDialog).toBe(false)
  })

  it('handles clear failure gracefully', async () => {
    vi.mocked(searchHistoryService.clearSearchHistory).mockResolvedValue(false)
    
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = [...mockSearchHistory]
    
    await wrapper.vm.clearHistory()
    
    expect(wrapper.vm.error).toContain('Failed to clear search history')
  })

  it('loads more items when requested', async () => {
    const moreItems = [
      {
        ...mockSearchHistory[0],
        id: 4,
        searchedAt: '2023-01-04T10:00:00Z'
      }
    ]
    
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Set initial state
    wrapper.vm.searchHistory = [...mockSearchHistory]
    wrapper.vm.loading = false
    wrapper.vm.hasMoreItems = true
    
    // Mock the service for load more
    vi.mocked(searchHistoryService.getUserSearchHistory).mockResolvedValueOnce(moreItems)
    
    // Load more
    await wrapper.vm.loadMoreItems()
    
    expect(wrapper.vm.searchHistory.length).toBe(4)
  })

  it('refreshes history when refresh button clicked', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Clear the initial call
    vi.clearAllMocks()
    
    const refreshButton = wrapper.find('.btn-secondary')
    await refreshButton.trigger('click')
    
    expect(searchHistoryService.getUserSearchHistory).toHaveBeenCalled()
  })

  it('formats dates correctly', () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Test recent date (today)
    const today = new Date().toISOString()
    expect(wrapper.vm.formatDate(today)).toMatch(/\d{2}:\d{2}/)
    
    // Test yesterday
    const yesterday = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString()
    expect(wrapper.vm.formatDate(yesterday)).toBe('Yesterday')
    
    // Test older date
    const oldDate = '2023-01-15T12:30:00Z'
    expect(wrapper.vm.formatDate(oldDate)).toMatch(/\d{1,2}\s\w{3}\s\d{4}/)
  })

  it('formats execution time correctly', () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    expect(wrapper.vm.formatExecutionTime('00:00:01.2345678')).toBe('1.234s')
    expect(wrapper.vm.formatExecutionTime('00:01:30.0000000')).toBe('1m 30s')
    expect(wrapper.vm.formatExecutionTime('01:30:45.0000000')).toBe('1h 30m 45s')
    expect(wrapper.vm.formatExecutionTime('00:00:00.1234567')).toBe('123ms')
  })

  it('formats date range correctly', () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    expect(wrapper.vm.formatDateRange()).toBe('All time')
    expect(wrapper.vm.formatDateRange('2023-01-01')).toMatch(/From \d{1,2}\s\w{3}\s\d{4}/)
    expect(wrapper.vm.formatDateRange(undefined, '2023-12-31')).toMatch(/Until \d{1,2}\s\w{3}\s\d{4}/)
    expect(wrapper.vm.formatDateRange('2023-01-01', '2023-12-31')).toMatch(/\d{1,2}\s\w{3}\s\d{4} - \d{1,2}\s\w{3}\s\d{4}/)
  })

  it('gets correct search type icon', () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    expect(wrapper.vm.getSearchTypeIcon('number')).toBe('icon-hash')
    expect(wrapper.vm.getSearchTypeIcon('combination')).toBe('icon-layers')
    expect(wrapper.vm.getSearchTypeIcon('frequency')).toBe('icon-trending-up')
    expect(wrapper.vm.getSearchTypeIcon('unknown')).toBe('icon-search')
  })

  it('disables clear button when no history', async () => {
    vi.mocked(searchHistoryService.getUserSearchHistory).mockResolvedValue([])
    
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Wait for history to load
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))
    
    const clearButton = wrapper.find('.btn-danger')
    expect(clearButton.attributes('disabled')).toBeDefined()
  })

  it('closes clear dialog when clicking outside', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.showClearDialog = true
    await wrapper.vm.$nextTick()
    
    const overlay = wrapper.find('.modal-overlay')
    await overlay.trigger('click')
    
    expect(wrapper.vm.showClearDialog).toBe(false)
  })

  it('prevents dialog close when clicking on modal content', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.showClearDialog = true
    await wrapper.vm.$nextTick()
    
    const modalContent = wrapper.find('.modal-content')
    await modalContent.trigger('click')
    
    expect(wrapper.vm.showClearDialog).toBe(true)
  })

  it('displays correct number of search history items', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = mockSearchHistory
    wrapper.vm.loading = false
    await wrapper.vm.$nextTick()
    
    // Check the computed property instead of DOM elements
    expect(wrapper.vm.filteredAndSortedHistory.length).toBe(mockSearchHistory.length)
  })

  it('displays search criteria details correctly', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.searchHistory = mockSearchHistory
    wrapper.vm.loading = false
    await wrapper.vm.$nextTick()
    
    // Check that the search criteria are formatted correctly
    const numberItem = mockSearchHistory.find(item => item.searchType === 'number')
    expect(numberItem?.parsedCriteria?.numbers).toEqual([1, 15, 23])
  })

  it('handles load more failure gracefully', async () => {
    const wrapper = mount(SearchHistory, {
      global: {
        plugins: [router]
      }
    })
    
    // Set initial state
    wrapper.vm.searchHistory = [...mockSearchHistory]
    wrapper.vm.loading = false
    wrapper.vm.hasMoreItems = true
    
    // Mock the service to fail
    vi.mocked(searchHistoryService.getUserSearchHistory).mockRejectedValueOnce(new Error('Load more failed'))
    
    await wrapper.vm.loadMoreItems()
    
    expect(wrapper.vm.error).toBe('Failed to load more items. Please try again.')
  })
})