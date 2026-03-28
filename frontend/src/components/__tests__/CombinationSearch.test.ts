import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import CombinationSearch from '../CombinationSearch.vue'
import { LookupService } from '@/services/lookupService'

// Mock the lookup service
vi.mock('@/services/lookupService', () => ({
  LookupService: {
    parseNumberInput: vi.fn(),
    validateCombination: vi.fn(),
    searchCombination: vi.fn(),
    validateNumber: vi.fn(),
    formatDate: vi.fn()
  }
}))

// Mock the app store
vi.mock('@/stores/app', () => ({
  useAppStore: () => ({
    addNotification: vi.fn()
  })
}))

describe('CombinationSearch', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    
    // Set default mock return values
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
  })

  it('renders correctly with default props', () => {
    const wrapper = mount(CombinationSearch)
    
    expect(wrapper.find('h2').text()).toBe('Combination Search')
    expect(wrapper.find('.combination-input').exists()).toBe(true)
    expect(wrapper.find('.search-button').exists()).toBe(true)
    expect(wrapper.find('.clear-button').exists()).toBe(true)
  })

  it('validates combination input correctly', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23, 35])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })

    const wrapper = mount(CombinationSearch)
    const input = wrapper.find('.combination-input')
    
    await input.setValue('1, 15, 23, 35')
    await input.trigger('input')
    
    expect(LookupService.parseNumberInput).toHaveBeenCalledWith('1, 15, 23, 35')
    expect(LookupService.validateCombination).toHaveBeenCalledWith([1, 15, 23, 35])
    expect(wrapper.vm.validationErrors).toEqual([])
  })

  it('shows validation errors for invalid combinations', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: false,
      errors: ['Combination must contain at least 2 numbers']
    })

    const wrapper = mount(CombinationSearch)
    const input = wrapper.find('.combination-input')
    
    await input.setValue('1')
    await input.trigger('input')
    
    expect(wrapper.vm.validationErrors).toContain('Combination must contain at least 2 numbers')
    expect(wrapper.find('.error-messages').exists()).toBe(true)
  })

  it('adjusts minimum matches when combination size changes', async () => {
    const wrapper = mount(CombinationSearch)
    
    // Set minimum matches to 3 initially
    wrapper.vm.minimumMatches = 3
    
    // Mock the parsed combination to return 2 numbers
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    
    const input = wrapper.find('.combination-input')
    await input.setValue('1, 15')
    await input.trigger('input')
    
    // Wait for watchers to trigger
    await wrapper.vm.$nextTick()
    
    // Should adjust to 2 (minimum allowed) since combination length is 2
    expect(wrapper.vm.minimumMatches).toBe(2)
  })

  it('performs combination search correctly', async () => {
    const mockResult = {
      searchedCombination: [1, 15, 23, 35],
      exactMatches: [
        {
          drawNumber: 1234,
          drawDate: '2023-01-01',
          winningCombination: [1, 15, 23, 35, 40, 42],
          matchedNumbers: [1, 15, 23, 35],
          matchCount: 4,
          isExactMatch: true
        }
      ],
      partialMatches: [],
      totalExactMatches: 1,
      totalPartialMatches: 0
    }

    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23, 35])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    vi.mocked(LookupService.searchCombination).mockResolvedValue(mockResult)

    const wrapper = mount(CombinationSearch)
    const input = wrapper.find('.combination-input')
    const searchButton = wrapper.find('.search-button')
    
    await input.setValue('1, 15, 23, 35')
    await input.trigger('input')
    await searchButton.trigger('click')
    
    expect(LookupService.searchCombination).toHaveBeenCalledWith({
      combination: [1, 15, 23, 35],
      includePartialMatches: false,
      minimumMatches: 2
    })
  })

  it('displays exact matches correctly', async () => {
    const mockResult = {
      searchedCombination: [1, 15, 23, 35],
      exactMatches: [
        {
          drawNumber: 1234,
          drawDate: '2023-01-01T00:00:00Z',
          winningCombination: [1, 15, 23, 35, 40, 42],
          matchedNumbers: [1, 15, 23, 35],
          matchCount: 4,
          isExactMatch: true
        }
      ],
      partialMatches: [],
      totalExactMatches: 1,
      totalPartialMatches: 0
    }

    const wrapper = mount(CombinationSearch)
    
    // Set up the component state
    wrapper.vm.searchResult = mockResult
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedCombination = [1, 15, 23, 35]
    
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.results-section').exists()).toBe(true)
    expect(wrapper.find('.results-summary').text()).toContain('1 exact match')
    expect(wrapper.find('.matches-section').exists()).toBe(true)
  })

  it('displays partial matches when enabled', async () => {
    const mockResult = {
      searchedCombination: [1, 15, 23, 35],
      exactMatches: [],
      partialMatches: [
        {
          drawNumber: 1235,
          drawDate: '2023-01-02T00:00:00Z',
          winningCombination: [1, 15, 30, 32, 40, 42],
          matchedNumbers: [1, 15],
          matchCount: 2,
          isExactMatch: false
        }
      ],
      totalExactMatches: 0,
      totalPartialMatches: 1
    }

    const wrapper = mount(CombinationSearch)
    
    // Enable partial matches
    wrapper.vm.includePartialMatches = true
    wrapper.vm.searchResult = mockResult
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedCombination = [1, 15, 23, 35]
    
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.results-summary').text()).toContain('1 partial match')
    expect(wrapper.findAll('.matches-section')).toHaveLength(1) // Only partial matches section
  })

  it('highlights matched numbers in winning combinations', async () => {
    const mockResult = {
      searchedCombination: [1, 15, 23, 35],
      exactMatches: [
        {
          drawNumber: 1234,
          drawDate: '2023-01-01T00:00:00Z',
          winningCombination: [1, 15, 23, 35, 40, 42],
          matchedNumbers: [1, 15, 23, 35],
          matchCount: 4,
          isExactMatch: true
        }
      ],
      partialMatches: [],
      totalExactMatches: 1,
      totalPartialMatches: 0
    }

    const wrapper = mount(CombinationSearch)
    
    wrapper.vm.searchResult = mockResult
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedCombination = [1, 15, 23, 35]
    
    await wrapper.vm.$nextTick()
    
    const combinationNumbers = wrapper.findAll('.combination-number')
    const matchedNumbers = wrapper.findAll('.combination-number.matched')
    
    expect(combinationNumbers.length).toBe(6) // Full winning combination
    expect(matchedNumbers.length).toBe(4) // Matched numbers
  })

  it('shows no results message when no matches found', async () => {
    const mockResult = {
      searchedCombination: [1, 15, 23, 35],
      exactMatches: [],
      partialMatches: [],
      totalExactMatches: 0,
      totalPartialMatches: 0
    }

    const wrapper = mount(CombinationSearch)
    
    wrapper.vm.searchResult = mockResult
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedCombination = [1, 15, 23, 35]
    wrapper.vm.isLoading = false
    wrapper.vm.includePartialMatches = true // Enable partial matches to test the condition
    
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.no-results').exists()).toBe(true)
    expect(wrapper.find('.no-results h3').text()).toBe('No Matches Found')
  })

  it('clears form when clear button is clicked', async () => {
    const wrapper = mount(CombinationSearch)
    
    // Set some form data
    await wrapper.find('.combination-input').setValue('1, 15, 23, 35')
    await wrapper.find('#start-date').setValue('2023-01-01')
    await wrapper.find('#end-date').setValue('2023-12-31')
    wrapper.vm.includePartialMatches = false
    wrapper.vm.minimumMatches = 3
    
    const clearButton = wrapper.find('.clear-button')
    await clearButton.trigger('click')
    
    expect(wrapper.vm.combinationInput).toBe('')
    expect(wrapper.vm.startDate).toBe('')
    expect(wrapper.vm.endDate).toBe('')
    expect(wrapper.vm.includePartialMatches).toBe(false) // Reset to default
    expect(wrapper.vm.minimumMatches).toBe(2) // Reset to default
    expect(wrapper.vm.searchResult).toBeNull()
    expect(wrapper.vm.hasSearched).toBe(false)
  })

  it('respects maxCombinationSize prop', async () => {
    const wrapper = mount(CombinationSearch, {
      props: {
        maxCombinationSize: 4
      }
    })
    
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23, 35, 40, 42])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    
    await wrapper.find('.combination-input').setValue('1, 15, 23, 35, 40, 42')
    await wrapper.find('.combination-input').trigger('input')
    
    expect(wrapper.vm.validationErrors).toContain('Maximum 4 numbers allowed')
  })

  it('includes date filters in search request', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23, 35])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    vi.mocked(LookupService.searchCombination).mockResolvedValue({
      searchedCombination: [1, 15, 23, 35],
      exactMatches: [],
      partialMatches: [],
      totalExactMatches: 0,
      totalPartialMatches: 0
    })

    const wrapper = mount(CombinationSearch)
    
    await wrapper.find('.combination-input').setValue('1, 15, 23, 35')
    await wrapper.find('#start-date').setValue('2023-01-01')
    await wrapper.find('#end-date').setValue('2023-12-31')
    await wrapper.find('.combination-input').trigger('input')
    await wrapper.find('.search-button').trigger('click')
    
    expect(LookupService.searchCombination).toHaveBeenCalledWith({
      combination: [1, 15, 23, 35],
      includePartialMatches: false,
      minimumMatches: 2,
      startDate: '2023-01-01',
      endDate: '2023-12-31'
    })
  })

  it('handles search errors gracefully', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23, 35])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    vi.mocked(LookupService.searchCombination).mockRejectedValue(new Error('API Error'))

    const wrapper = mount(CombinationSearch)
    const input = wrapper.find('.combination-input')
    const searchButton = wrapper.find('.search-button')
    
    await input.setValue('1, 15, 23, 35')
    await input.trigger('input')
    await searchButton.trigger('click')
    
    // Wait for the async operation to complete
    await new Promise(resolve => setTimeout(resolve, 0))
    
    expect(wrapper.vm.searchResult).toBeNull()
    expect(wrapper.vm.isLoading).toBe(false)
  })

  it('paginates partial matches correctly', async () => {
    const partialMatches = Array.from({ length: 25 }, (_, i) => ({
      drawNumber: 1000 + i,
      drawDate: '2023-01-01T00:00:00Z',
      winningCombination: [1, 15, 30 + i, 32, 40, 42],
      matchedNumbers: [1, 15],
      matchCount: 2,
      isExactMatch: false
    }))

    const mockResult = {
      searchedCombination: [1, 15, 23, 35],
      exactMatches: [],
      partialMatches,
      totalExactMatches: 0,
      totalPartialMatches: 25
    }

    const wrapper = mount(CombinationSearch)
    
    wrapper.vm.includePartialMatches = true
    wrapper.vm.searchResult = mockResult
    wrapper.vm.hasSearched = true
    
    await wrapper.vm.$nextTick()
    
    // Should show pagination controls
    expect(wrapper.find('.pagination').exists()).toBe(true)
    expect(wrapper.vm.partialMatchPages).toBe(3) // 25 items / 10 per page = 3 pages
    
    // Should show only first 10 items
    expect(wrapper.vm.paginatedPartialMatches).toHaveLength(10)
  })

  it('validates date range correctly', async () => {
    const wrapper = mount(CombinationSearch)
    
    // Set invalid date range (start after end)
    wrapper.vm.startDate = '2023-12-31'
    wrapper.vm.endDate = '2023-01-01'
    wrapper.vm.combinationInput = '1, 15, 23, 35'
    
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23, 35])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    
    await wrapper.vm.validateInput()
    
    expect(wrapper.vm.validationErrors).toContain('Start date must be before end date')
  })
})