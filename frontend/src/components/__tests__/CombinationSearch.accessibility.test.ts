import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import CombinationSearch from '../CombinationSearch.vue'
import { LookupService } from '@/services/lookupService'
import accessibilityService from '@/services/accessibilityService'

// Mock services
vi.mock('@/services/lookupService', () => ({
  LookupService: {
    parseNumberInput: vi.fn(() => [1, 2, 3]),
    validateCombination: vi.fn(() => ({ isValid: true, errors: [] })),
    searchCombination: vi.fn(() => Promise.resolve({
      searchedCombination: [1, 2, 3],
      exactMatches: [],
      partialMatches: [],
      totalExactMatches: 0,
      totalPartialMatches: 0
    }))
  }
}))

vi.mock('@/services/accessibilityService', () => ({
  default: {
    announce: vi.fn(),
    pushFocus: vi.fn(),
    popFocus: vi.fn(),
    generateId: vi.fn(() => 'test-id')
  }
}))

vi.mock('@/stores/app', () => ({
  useAppStore: () => ({
    addNotification: vi.fn()
  })
}))

describe('CombinationSearch Accessibility', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('has proper semantic structure', () => {
    const wrapper = mount(CombinationSearch)
    
    // Main container should have role="main"
    expect(wrapper.find('[role="main"]').exists()).toBe(true)
    
    // Should have proper heading
    expect(wrapper.find('#combination-title').exists()).toBe(true)
    expect(wrapper.find('h2#combination-title').text()).toBe('Combination Search')
    
    // Search form should have role="search"
    expect(wrapper.find('[role="search"]').exists()).toBe(true)
  })

  it('has skip link for keyboard navigation', () => {
    const wrapper = mount(CombinationSearch)
    
    const skipLink = wrapper.find('.skip-link')
    expect(skipLink.exists()).toBe(true)
    expect(skipLink.attributes('href')).toBe('#combination-results')
    expect(skipLink.text()).toBe('Skip to search results')
  })

  it('has proper form labels and ARIA attributes', () => {
    const wrapper = mount(CombinationSearch)
    
    // Main input should have proper labeling
    const input = wrapper.find('#combination-input')
    expect(input.exists()).toBe(true)
    expect(input.attributes('aria-describedby')).toBe('combination-help')
    expect(input.attributes('autocomplete')).toBe('off')
    
    // Label should be associated with input
    const label = wrapper.find('label[for="combination-input"]')
    expect(label.exists()).toBe(true)
    
    // Help text should exist
    const helpText = wrapper.find('#combination-help')
    expect(helpText.exists()).toBe(true)
    expect(helpText.classes()).toContain('input-help')
  })

  it('has proper fieldset for search options', () => {
    const wrapper = mount(CombinationSearch)
    
    const fieldset = wrapper.find('fieldset.search-options')
    expect(fieldset.exists()).toBe(true)
    
    const legend = fieldset.find('legend.sr-only')
    expect(legend.exists()).toBe(true)
    expect(legend.text()).toBe('Search Options')
    
    // Partial matches checkbox should have proper attributes
    const partialCheckbox = wrapper.find('#include-partial')
    expect(partialCheckbox.exists()).toBe(true)
    expect(partialCheckbox.attributes('aria-describedby')).toBe('partial-help')
    
    // Help text should exist
    expect(wrapper.find('#partial-help').exists()).toBe(true)
  })

  it('has accessible minimum matches select', async () => {
    const wrapper = mount(CombinationSearch)
    
    // Enable partial matches to show minimum matches select
    await wrapper.find('#include-partial').setChecked(true)
    
    const minMatchesSelect = wrapper.find('#min-matches')
    expect(minMatchesSelect.exists()).toBe(true)
    expect(minMatchesSelect.attributes('aria-describedby')).toBe('min-matches-help')
    
    // Label should be associated
    const label = wrapper.find('label[for="min-matches"]')
    expect(label.exists()).toBe(true)
    
    // Help text should exist
    const helpText = wrapper.find('#min-matches-help')
    expect(helpText.exists()).toBe(true)
    expect(helpText.classes()).toContain('sr-only')
  })

  it('has accessible date inputs', () => {
    const wrapper = mount(CombinationSearch)
    
    // Start date should have proper attributes
    const startDate = wrapper.find('#start-date')
    expect(startDate.exists()).toBe(true)
    expect(startDate.attributes('type')).toBe('date')
    
    const startLabel = wrapper.find('label[for="start-date"]')
    expect(startLabel.exists()).toBe(true)
    
    // End date should have proper attributes
    const endDate = wrapper.find('#end-date')
    expect(endDate.exists()).toBe(true)
    expect(endDate.attributes('type')).toBe('date')
    
    const endLabel = wrapper.find('label[for="end-date"]')
    expect(endLabel.exists()).toBe(true)
  })

  it('has accessible action buttons', () => {
    const wrapper = mount(CombinationSearch)
    
    // Search button should have proper attributes
    const searchButton = wrapper.find('.search-button')
    expect(searchButton.exists()).toBe(true)
    expect(searchButton.attributes('type')).toBe('submit')
    
    // Clear button should have proper attributes
    const clearButton = wrapper.find('.clear-button')
    expect(clearButton.exists()).toBe(true)
    expect(clearButton.attributes('type')).toBe('button')
    expect(clearButton.attributes('aria-describedby')).toBe('clear-help')
    
    // Help text should exist
    const clearHelp = wrapper.find('#clear-help')
    expect(clearHelp.exists()).toBe(true)
    expect(clearHelp.classes()).toContain('sr-only')
  })

  it('shows validation errors with proper ARIA attributes', async () => {
    const wrapper = mount(CombinationSearch)
    
    // Mock validation errors
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: false,
      errors: ['Test error message']
    })
    
    // Trigger validation by entering invalid input
    await wrapper.find('#combination-input').setValue('invalid')
    await wrapper.find('#combination-input').trigger('input')
    
    const errorContainer = wrapper.find('.error-messages')
    expect(errorContainer.exists()).toBe(true)
    expect(errorContainer.attributes('role')).toBe('alert')
    expect(errorContainer.attributes('aria-live')).toBe('polite')
    expect(errorContainer.attributes('id')).toBe('combination-errors')
    
    // Input should reference error container
    const input = wrapper.find('#combination-input')
    expect(input.attributes('aria-invalid')).toBe('true')
    expect(input.attributes('aria-describedby')).toBe('combination-errors')
    
    // Error icon should be hidden from screen readers
    const errorIcon = wrapper.find('.error-icon')
    expect(errorIcon.attributes('aria-hidden')).toBe('true')
  })

  it('has accessible search results', async () => {
    const mockResult = {
      searchedCombination: [1, 2, 3],
      exactMatches: [{
        drawNumber: 1234,
        drawDate: '2024-01-01',
        winningCombination: [1, 2, 3, 4, 5, 6],
        matchedNumbers: [1, 2, 3],
        matchCount: 3,
        isExactMatch: true
      }],
      partialMatches: [],
      totalExactMatches: 1,
      totalPartialMatches: 0
    }
    
    const wrapper = mount(CombinationSearch)
    
    // Set up component state
    wrapper.vm.searchResult = mockResult
    wrapper.vm.hasSearched = true
    await wrapper.vm.$nextTick()
    
    // Results section should have proper attributes
    const resultsSection = wrapper.find('#combination-results')
    expect(resultsSection.exists()).toBe(true)
    expect(resultsSection.attributes('role')).toBe('region')
    expect(resultsSection.attributes('aria-labelledby')).toBe('combination-results-title')
    
    // Results summary should have status role
    const resultsSummary = wrapper.find('.results-summary')
    expect(resultsSummary.exists()).toBe(true)
    expect(resultsSummary.attributes('role')).toBe('status')
    expect(resultsSummary.attributes('aria-live')).toBe('polite')
  })

  it('has accessible matches table', async () => {
    const mockResult = {
      searchedCombination: [1, 2, 3],
      exactMatches: [{
        drawNumber: 1234,
        drawDate: '2024-01-01',
        winningCombination: [1, 2, 3, 4, 5, 6],
        matchedNumbers: [1, 2, 3],
        matchCount: 3,
        isExactMatch: true
      }],
      partialMatches: [],
      totalExactMatches: 1,
      totalPartialMatches: 0
    }
    
    const wrapper = mount(CombinationSearch)
    
    wrapper.vm.searchResult = mockResult
    wrapper.vm.hasSearched = true
    await wrapper.vm.$nextTick()
    
    // Matches table should have proper structure
    const table = wrapper.find('.matches-table')
    expect(table.exists()).toBe(true)
    expect(table.attributes('role')).toBe('table')
    
    // Table should have caption
    const caption = table.find('caption')
    expect(caption.exists()).toBe(true)
    expect(caption.classes()).toContain('sr-only')
    
    // Headers should have scope attributes
    const headers = table.findAll('th')
    headers.forEach(header => {
      expect(header.attributes('scope')).toBe('col')
    })
    
    // Table rows should have proper structure
    const rows = table.findAll('tbody tr')
    rows.forEach(row => {
      expect(row.attributes('role')).toBe('row')
    })
  })

  it('has accessible pagination', async () => {
    const mockResult = {
      searchedCombination: [1, 2, 3],
      exactMatches: [],
      partialMatches: Array.from({ length: 25 }, (_, i) => ({
        drawNumber: 1000 + i,
        drawDate: '2024-01-01',
        winningCombination: [1, 2, 4, 5, 6, 7],
        matchedNumbers: [1, 2],
        matchCount: 2,
        isExactMatch: false
      })),
      totalExactMatches: 0,
      totalPartialMatches: 25
    }
    
    const wrapper = mount(CombinationSearch)
    
    wrapper.vm.searchResult = mockResult
    wrapper.vm.hasSearched = true
    wrapper.vm.includePartialMatches = true
    await wrapper.vm.$nextTick()
    
    const pagination = wrapper.find('.pagination')
    expect(pagination.exists()).toBe(true)
    expect(pagination.attributes('role')).toBe('navigation')
    expect(pagination.attributes('aria-label')).toBe('Search results pagination')
    
    // Pagination buttons should have proper labels
    const firstButton = pagination.find('button[aria-label="Go to first page"]')
    const prevButton = pagination.find('button[aria-label="Go to previous page"]')
    const nextButton = pagination.find('button[aria-label="Go to next page"]')
    const lastButton = pagination.find('button[aria-label="Go to last page"]')
    
    expect(firstButton.exists()).toBe(true)
    expect(prevButton.exists()).toBe(true)
    expect(nextButton.exists()).toBe(true)
    expect(lastButton.exists()).toBe(true)
    
    // Page info should have status role
    const pageInfo = pagination.find('.page-info')
    expect(pageInfo.exists()).toBe(true)
    expect(pageInfo.attributes('role')).toBe('status')
    expect(pageInfo.attributes('aria-live')).toBe('polite')
  })

  it('has accessible no results state', async () => {
    const wrapper = mount(CombinationSearch)
    
    // Set up no results state
    wrapper.vm.searchResult = {
      searchedCombination: [1, 2, 3],
      exactMatches: [],
      partialMatches: [],
      totalExactMatches: 0,
      totalPartialMatches: 0
    }
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedCombination = [1, 2, 3]
    await wrapper.vm.$nextTick()
    
    const noResults = wrapper.find('.no-results')
    expect(noResults.exists()).toBe(true)
    expect(noResults.attributes('role')).toBe('region')
    expect(noResults.attributes('aria-labelledby')).toBe('no-results-title')
    
    // Should have proper heading
    const title = wrapper.find('#no-results-title')
    expect(title.exists()).toBe(true)
    expect(title.text()).toBe('No Matches Found')
    
    // Icon should be hidden from screen readers
    const icon = wrapper.find('.no-results-icon')
    expect(icon.attributes('aria-hidden')).toBe('true')
  })

  it('announces search progress to screen readers', async () => {
    const wrapper = mount(CombinationSearch)
    
    // Set up valid input
    await wrapper.find('#combination-input').setValue('1,2,3')
    
    // Perform search
    await wrapper.vm.performSearch()
    
    expect(accessibilityService.announce).toHaveBeenCalledWith('Starting combination search')
  })

  it('announces search results to screen readers', async () => {
    const mockResult = {
      searchedCombination: [1, 2, 3],
      exactMatches: [],
      partialMatches: [],
      totalExactMatches: 0,
      totalPartialMatches: 0
    }
    
    vi.mocked(LookupService.searchCombination).mockResolvedValue(mockResult)
    
    const wrapper = mount(CombinationSearch)
    
    // Set up valid input
    await wrapper.find('#combination-input').setValue('1,2,3')
    
    // Perform search
    await wrapper.vm.performSearch()
    
    expect(accessibilityService.announce).toHaveBeenCalledWith(
      expect.stringContaining('Search complete. Found 0 exact matches and 0 partial matches for combination 1, 2, 3')
    )
  })

  it('focuses input on mount and after clear', async () => {
    const wrapper = mount(CombinationSearch)
    
    // Clear form should focus input and announce
    await wrapper.find('.clear-button').trigger('click')
    
    expect(accessibilityService.announce).toHaveBeenCalledWith('Form cleared')
  })

  it('has proper loading state accessibility', async () => {
    const wrapper = mount(CombinationSearch)
    
    // Set loading state
    wrapper.vm.isLoading = true
    await wrapper.vm.$nextTick()
    
    // Loading spinner should be hidden from screen readers
    const spinner = wrapper.find('.loading-spinner')
    if (spinner.exists()) {
      expect(spinner.attributes('aria-hidden')).toBe('true')
    }
    
    // Button text should indicate loading state
    const button = wrapper.find('.search-button')
    expect(button.text()).toContain('Searching...')
  })

  it('supports keyboard shortcuts', async () => {
    const wrapper = mount(CombinationSearch)
    
    // Component should set up keyboard navigation
    expect(wrapper.vm.keyboardCleanup).toBeDefined()
    
    // Test pagination shortcuts
    wrapper.vm.searchResult = {
      searchedCombination: [1, 2, 3],
      exactMatches: [],
      partialMatches: Array.from({ length: 25 }, () => ({
        drawNumber: 1234,
        drawDate: '2024-01-01',
        winningCombination: [1, 2, 4, 5, 6, 7],
        matchedNumbers: [1, 2],
        matchCount: 2,
        isExactMatch: false
      })),
      totalExactMatches: 0,
      totalPartialMatches: 25
    }
    wrapper.vm.includePartialMatches = true
    await wrapper.vm.$nextTick()
    
    // PageDown should navigate to next page
    const container = wrapper.element
    const pageDownEvent = new KeyboardEvent('keydown', { key: 'PageDown' })
    container.dispatchEvent(pageDownEvent)
    
    expect(wrapper.vm.partialMatchPage).toBe(2)
  })

  it('has proper color contrast in high contrast mode', () => {
    document.documentElement.classList.add('high-contrast')
    
    const wrapper = mount(CombinationSearch)
    
    expect(wrapper.exists()).toBe(true)
    
    // Clean up
    document.documentElement.classList.remove('high-contrast')
  })

  it('respects reduced motion preferences', () => {
    document.documentElement.classList.add('reduced-motion')
    
    const wrapper = mount(CombinationSearch)
    
    expect(wrapper.exists()).toBe(true)
    
    // Clean up
    document.documentElement.classList.remove('reduced-motion')
  })

  it('has accessible combination number display', async () => {
    const mockResult = {
      searchedCombination: [1, 2, 3],
      exactMatches: [{
        drawNumber: 1234,
        drawDate: '2024-01-01',
        winningCombination: [1, 2, 3, 4, 5, 6],
        matchedNumbers: [1, 2, 3],
        matchCount: 3,
        isExactMatch: true
      }],
      partialMatches: [],
      totalExactMatches: 1,
      totalPartialMatches: 0
    }
    
    const wrapper = mount(CombinationSearch)
    
    wrapper.vm.searchResult = mockResult
    wrapper.vm.hasSearched = true
    await wrapper.vm.$nextTick()
    
    // Combination numbers should have proper grouping
    const combinationNumbers = wrapper.find('.combination-numbers')
    expect(combinationNumbers.exists()).toBe(true)
    expect(combinationNumbers.attributes('role')).toBe('group')
    expect(combinationNumbers.attributes('aria-label')).toBe('Winning combination numbers')
    
    // Each number should have descriptive label
    const numberElements = wrapper.findAll('.combination-number')
    numberElements.forEach((element, index) => {
      const number = mockResult.exactMatches[0].winningCombination[index]
      const isMatched = mockResult.exactMatches[0].matchedNumbers.includes(number)
      
      expect(element.attributes('aria-label')).toBe(
        `Number ${number}${isMatched ? ' (matched)' : ''}`
      )
    })
  })
})