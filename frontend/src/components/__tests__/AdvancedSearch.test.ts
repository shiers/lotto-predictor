import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import AdvancedSearch from '../AdvancedSearch.vue'
import { AdvancedSearchService } from '@/services/advancedSearchService'
import type { SearchCriteria, AdvancedSearchResult } from '@/types/advancedSearch'

// Mock the advanced search service
vi.mock('@/services/advancedSearchService', () => ({
  AdvancedSearchService: {
    executeSearch: vi.fn(),
    exportResults: vi.fn(),
    saveSearchConfiguration: vi.fn(),
    getSavedSearches: vi.fn(),
    loadSearchConfiguration: vi.fn(),
    deleteSearchConfiguration: vi.fn(),
    validateSearchCriteria: vi.fn()
  }
}))

// Mock child components
vi.mock('../SearchCriteriaBuilder.vue', () => ({
  default: {
    name: 'SearchCriteriaBuilder',
    template: '<div data-testid="search-criteria-builder">Mocked SearchCriteriaBuilder</div>',
    props: ['criteria'],
    emits: ['update:criteria', 'validate'],
    setup(props: any, { emit }: any) {
      return {
        handleValidation: (errors: string[]) => emit('validate', errors),
        updateCriteria: (criteria: any) => emit('update:criteria', criteria)
      }
    }
  }
}))

vi.mock('../ExportDialog.vue', () => ({
  default: {
    name: 'ExportDialog',
    template: '<div data-testid="export-dialog"></div>',
    emits: ['close', 'export']
  }
}))

vi.mock('../SavedSearches.vue', () => ({
  default: {
    name: 'SavedSearches',
    template: '<div data-testid="saved-searches"></div>',
    emits: ['close', 'load', 'delete']
  }
}))

describe('AdvancedSearch', () => {
  let wrapper: any
  let pinia: any

  const mockSearchCriteria: SearchCriteria = {
    conditions: [{
      id: 'test-1',
      type: 'number',
      value: { number: 15, position: '' }
    }],
    logic: 'AND',
    dateRange: {
      startDate: null,
      endDate: null
    },
    frequencyFilters: {
      minOccurrences: null,
      maxOccurrences: null
    },
    includeBonus: true,
    includePowerball: true
  }

  const mockSearchResult: AdvancedSearchResult = {
    totalResults: 5,
    numberOccurrences: [
      {
        drawNumber: 100,
        drawDate: '2023-01-01',
        number: 15,
        position: 1,
        isBonus: false,
        isPowerball: false,
        fullCombination: [15, 20, 25, 30, 35, 40]
      }
    ],
    combinationMatches: [],
    frequencyData: [],
    executionTime: 150,
    searchCriteria: mockSearchCriteria
  }

  beforeEach(() => {
    pinia = createPinia()
    setActivePinia(pinia)
    vi.clearAllMocks()
  })

  it('renders correctly with initial state', () => {
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    expect(wrapper.find('h2').text()).toBe('Advanced Search')
    expect(wrapper.find('[data-testid="search-criteria-builder"]').exists()).toBe(true)
    expect(wrapper.find('.btn-primary').text()).toContain('Execute Search')
  })

  it('validates search criteria and enables/disables execute button', async () => {
    vi.mocked(AdvancedSearchService.validateSearchCriteria).mockReturnValue([])
    
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    // Initially should be disabled (no criteria)
    expect(wrapper.find('.btn-primary').attributes('disabled')).toBeDefined()

    // Simulate validation with no errors
    await wrapper.vm.handleValidation([])
    await wrapper.vm.$nextTick()

    expect(wrapper.vm.isValidCriteria).toBe(true)
  })

  it('displays validation errors when criteria are invalid', async () => {
    const errors = ['Number must be between 1 and 40', 'At least one condition is required']
    vi.mocked(AdvancedSearchService.validateSearchCriteria).mockReturnValue(errors)
    
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.handleValidation(errors)
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.validation-errors').exists()).toBe(true)
    expect(wrapper.find('.validation-errors').text()).toContain('Number must be between 1 and 40')
    expect(wrapper.find('.validation-errors').text()).toContain('At least one condition is required')
  })

  it('executes search and displays results', async () => {
    vi.mocked(AdvancedSearchService.validateSearchCriteria).mockReturnValue([])
    vi.mocked(AdvancedSearchService.executeSearch).mockResolvedValue(mockSearchResult)
    
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    // Set valid criteria
    wrapper.vm.searchCriteria = mockSearchCriteria
    await wrapper.vm.handleValidation([])
    await wrapper.vm.$nextTick()

    // Execute search
    await wrapper.find('.btn-primary').trigger('click')
    await wrapper.vm.$nextTick()

    expect(AdvancedSearchService.executeSearch).toHaveBeenCalledWith(mockSearchCriteria)
    expect(wrapper.vm.searchResults).toEqual(mockSearchResult)
    expect(wrapper.find('.search-results').exists()).toBe(true)
    expect(wrapper.find('.result-count').text()).toContain('5 results found')
  })

  it('shows loading state during search execution', async () => {
    vi.mocked(AdvancedSearchService.validateSearchCriteria).mockReturnValue([])
    vi.mocked(AdvancedSearchService.executeSearch).mockImplementation(() => 
      new Promise(resolve => setTimeout(() => resolve(mockSearchResult), 100))
    )
    
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    wrapper.vm.searchCriteria = mockSearchCriteria
    await wrapper.vm.handleValidation([])
    await wrapper.vm.$nextTick()

    // Start search
    const executeButton = wrapper.find('.btn-primary')
    await executeButton.trigger('click')

    expect(executeButton.text()).toContain('Searching...')
    expect(executeButton.attributes('disabled')).toBeDefined()
  })

  it('enables export button when results are available', async () => {
    vi.mocked(AdvancedSearchService.validateSearchCriteria).mockReturnValue([])
    
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    // Initially export should be disabled
    const exportButton = wrapper.find('.btn-secondary')
    expect(exportButton.attributes('disabled')).toBeDefined()

    // Set search results
    wrapper.vm.searchResults = mockSearchResult
    await wrapper.vm.$nextTick()

    expect(exportButton.attributes('disabled')).toBeUndefined()
  })

  it('opens and closes export dialog', async () => {
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    wrapper.vm.searchResults = mockSearchResult
    await wrapper.vm.$nextTick()

    // Open export dialog
    await wrapper.find('.btn-secondary').trigger('click')
    expect(wrapper.vm.showExportDialog).toBe(true)

    // Close export dialog
    wrapper.vm.showExportDialog = false
    await wrapper.vm.$nextTick()
    expect(wrapper.find('[data-testid="export-dialog"]').exists()).toBe(false)
  })

  it('opens and closes save search dialog', async () => {
    vi.mocked(AdvancedSearchService.validateSearchCriteria).mockReturnValue([])
    
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    wrapper.vm.searchCriteria = mockSearchCriteria
    await wrapper.vm.handleValidation([])
    await wrapper.vm.$nextTick()

    // Open save dialog
    const saveButton = wrapper.findAll('.btn-outline')[0]
    await saveButton.trigger('click')
    expect(wrapper.vm.showSaveDialog).toBe(true)
    expect(wrapper.find('.modal-overlay').exists()).toBe(true)

    // Close save dialog
    await wrapper.find('.modal-overlay').trigger('click')
    expect(wrapper.vm.showSaveDialog).toBe(false)
  })

  it('saves search configuration', async () => {
    const savedConfig = { id: '1', name: 'Test Search', ...mockSearchCriteria }
    vi.mocked(AdvancedSearchService.saveSearchConfiguration).mockResolvedValue(savedConfig)
    
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    wrapper.vm.searchCriteria = mockSearchCriteria
    wrapper.vm.showSaveDialog = true
    wrapper.vm.saveSearchForm = { name: 'Test Search', description: 'Test description' }
    await wrapper.vm.$nextTick()

    // Submit save form
    await wrapper.vm.saveSearchConfiguration()

    expect(AdvancedSearchService.saveSearchConfiguration).toHaveBeenCalledWith({
      name: 'Test Search',
      description: 'Test description',
      criteria: mockSearchCriteria,
      createdAt: expect.any(String)
    })
    expect(wrapper.vm.showSaveDialog).toBe(false)
  })

  it('opens and closes saved searches dialog', async () => {
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    // Open saved searches dialog
    const savedSearchesButton = wrapper.findAll('.btn-outline')[1]
    await savedSearchesButton.trigger('click')
    expect(wrapper.vm.showSavedSearches).toBe(true)

    // Close saved searches dialog
    wrapper.vm.showSavedSearches = false
    await wrapper.vm.$nextTick()
    expect(wrapper.find('[data-testid="saved-searches"]').exists()).toBe(false)
  })

  it('loads search configuration from saved searches', async () => {
    const loadedConfig = { 
      id: '1', 
      name: 'Loaded Search', 
      criteria: mockSearchCriteria,
      createdAt: '2023-01-01',
      description: 'Loaded description'
    }
    
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.loadSearchConfiguration(loadedConfig)

    expect(wrapper.vm.searchCriteria).toEqual(mockSearchCriteria)
    expect(wrapper.vm.showSavedSearches).toBe(false)
  })

  it('formats dates correctly', () => {
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    const dateString = '2023-01-15T10:30:00Z'
    const formatted = wrapper.vm.formatDate(dateString)
    expect(formatted).toMatch(/\d{1,2}\/\d{1,2}\/\d{4}/)

    const dateObject = new Date('2023-01-15')
    const formattedObject = wrapper.vm.formatDate(dateObject)
    expect(formattedObject).toMatch(/\d{1,2}\/\d{1,2}\/\d{4}/)
  })

  it('handles search errors gracefully', async () => {
    vi.mocked(AdvancedSearchService.validateSearchCriteria).mockReturnValue([])
    vi.mocked(AdvancedSearchService.executeSearch).mockRejectedValue(new Error('Search failed'))
    
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
    
    wrapper = mount(AdvancedSearch, {
      global: {
        plugins: [pinia]
      }
    })

    wrapper.vm.searchCriteria = mockSearchCriteria
    await wrapper.vm.handleValidation([])
    await wrapper.vm.$nextTick()

    await wrapper.vm.executeSearch()

    expect(consoleSpy).toHaveBeenCalledWith('Search failed:', expect.any(Error))
    expect(wrapper.vm.isSearching).toBe(false)
    
    consoleSpy.mockRestore()
  })
})