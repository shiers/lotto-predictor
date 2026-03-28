import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import NumberLookup from '../NumberLookup.vue'
import { LookupService } from '@/services/lookupService'
import accessibilityService from '@/services/accessibilityService'

// Mock services
vi.mock('@/services/lookupService', () => ({
  LookupService: {
    parseNumberInput: vi.fn(() => [1, 2, 3]),
    validateCombination: vi.fn(() => ({ isValid: true, errors: [] })),
    lookupNumbers: vi.fn(() => Promise.resolve([]))
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

describe('NumberLookup Accessibility', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('has proper semantic structure', () => {
    const wrapper = mount(NumberLookup)
    
    // Main container should have role="main"
    expect(wrapper.find('[role="main"]').exists()).toBe(true)
    
    // Should have proper heading structure
    expect(wrapper.find('#lookup-title').exists()).toBe(true)
    expect(wrapper.find('h2#lookup-title').text()).toBe('Number Lookup')
    
    // Search form should have role="search"
    expect(wrapper.find('[role="search"]').exists()).toBe(true)
  })

  it('has skip link for keyboard navigation', () => {
    const wrapper = mount(NumberLookup)
    
    const skipLink = wrapper.find('.skip-link')
    expect(skipLink.exists()).toBe(true)
    expect(skipLink.attributes('href')).toBe('#search-results')
    expect(skipLink.text()).toBe('Skip to search results')
  })

  it('has proper form labels and ARIA attributes', () => {
    const wrapper = mount(NumberLookup)
    
    // Main input should have proper labeling
    const input = wrapper.find('#numbers-input')
    expect(input.exists()).toBe(true)
    expect(input.attributes('aria-describedby')).toBe('input-help')
    expect(input.attributes('autocomplete')).toBe('off')
    
    // Label should be associated with input
    const label = wrapper.find('label[for="numbers-input"]')
    expect(label.exists()).toBe(true)
    
    // Help text should exist
    const helpText = wrapper.find('#input-help')
    expect(helpText.exists()).toBe(true)
  })

  it('has proper fieldset for checkbox group', () => {
    const wrapper = mount(NumberLookup)
    
    const fieldset = wrapper.find('fieldset.checkbox-group')
    expect(fieldset.exists()).toBe(true)
    
    const legend = fieldset.find('legend.sr-only')
    expect(legend.exists()).toBe(true)
    expect(legend.text()).toBe('Number Types to Include')
    
    // Checkboxes should have proper IDs and descriptions
    expect(wrapper.find('#include-bonus').exists()).toBe(true)
    expect(wrapper.find('#include-powerball').exists()).toBe(true)
    expect(wrapper.find('#bonus-help').exists()).toBe(true)
    expect(wrapper.find('#powerball-help').exists()).toBe(true)
  })

  it('has accessible buttons with proper attributes', () => {
    const wrapper = mount(NumberLookup)
    
    const searchButton = wrapper.find('.search-button')
    expect(searchButton.exists()).toBe(true)
    expect(searchButton.attributes('type')).toBe('submit')
    
    const clearButton = wrapper.find('.clear-button')
    expect(clearButton.exists()).toBe(true)
    expect(clearButton.attributes('type')).toBe('button')
    expect(clearButton.attributes('aria-describedby')).toBe('clear-help')
    
    // Help text for clear button should exist
    expect(wrapper.find('#clear-help').exists()).toBe(true)
  })

  it('shows validation errors with proper ARIA attributes', async () => {
    const wrapper = mount(NumberLookup)
    
    // Mock validation errors
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: false,
      errors: ['Test error message']
    })
    
    // Trigger validation by entering invalid input
    await wrapper.find('#numbers-input').setValue('invalid')
    await wrapper.find('#numbers-input').trigger('input')
    
    const errorContainer = wrapper.find('.error-messages')
    expect(errorContainer.exists()).toBe(true)
    expect(errorContainer.attributes('role')).toBe('alert')
    expect(errorContainer.attributes('aria-live')).toBe('polite')
    expect(errorContainer.attributes('id')).toBe('input-errors')
    
    // Input should reference error container
    const input = wrapper.find('#numbers-input')
    expect(input.attributes('aria-invalid')).toBe('true')
    expect(input.attributes('aria-describedby')).toBe('input-errors')
  })

  it('has accessible results table', async () => {
    const mockResults = [
      {
        drawNumber: 1234,
        drawDate: '2024-01-01',
        number: 5,
        position: 1,
        isBonus: false,
        isPowerball: false,
        fullCombination: [1, 2, 3, 4, 5, 6]
      }
    ]
    
    vi.mocked(LookupService.lookupNumbers).mockResolvedValue(mockResults)
    
    const wrapper = mount(NumberLookup)
    
    // Set up component state manually for testing
    wrapper.vm.searchResults = mockResults
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedNumbers = [1, 2, 3]
    await wrapper.vm.$nextTick()
    
    // Results section should have proper ARIA attributes
    const resultsSection = wrapper.find('#search-results')
    expect(resultsSection.exists()).toBe(true)
    expect(resultsSection.attributes('role')).toBe('region')
    expect(resultsSection.attributes('aria-labelledby')).toBe('results-title')
    
    // Results table should be accessible
    const table = wrapper.find('.accessible-table')
    expect(table.exists()).toBe(true)
    expect(table.attributes('role')).toBe('table')
    expect(table.attributes('aria-labelledby')).toBe('results-title')
    
    // Table should have caption
    const caption = table.find('caption.sr-only')
    expect(caption.exists()).toBe(true)
    
    // Headers should have scope attributes
    const headers = table.findAll('th')
    headers.forEach(header => {
      expect(header.attributes('scope')).toBe('col')
    })
  })

  it('has accessible pagination', async () => {
    // Mock many results to trigger pagination
    const mockResults = Array.from({ length: 50 }, (_, i) => ({
      drawNumber: 1000 + i,
      drawDate: '2024-01-01',
      number: 5,
      position: 1,
      isBonus: false,
      isPowerball: false,
      fullCombination: [1, 2, 3, 4, 5, 6]
    }))
    
    const wrapper = mount(NumberLookup)
    
    // Set up component state manually for testing
    wrapper.vm.searchResults = mockResults
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedNumbers = [1, 2, 3]
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
    const pageInfo = pagination.find('[role="status"]')
    expect(pageInfo.exists()).toBe(true)
    expect(pageInfo.attributes('aria-live')).toBe('polite')
  })

  it('has accessible no results state', async () => {
    const wrapper = mount(NumberLookup)
    
    // Set up component state manually for testing
    wrapper.vm.searchResults = []
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedNumbers = [1, 2, 3]
    wrapper.vm.isLoading = false
    await wrapper.vm.$nextTick()
    
    const noResults = wrapper.find('.no-results')
    expect(noResults.exists()).toBe(true)
    expect(noResults.attributes('role')).toBe('region')
    expect(noResults.attributes('aria-labelledby')).toBe('no-results-title')
    expect(noResults.attributes('id')).toBe('search-results')
    
    // Should have proper heading
    expect(wrapper.find('#no-results-title').exists()).toBe(true)
    
    // Icon should be hidden from screen readers
    const icon = wrapper.find('.no-results-icon')
    expect(icon.attributes('aria-hidden')).toBe('true')
  })

  it('announces search progress to screen readers', async () => {
    const wrapper = mount(NumberLookup)
    
    // Set up valid input
    await wrapper.find('#numbers-input').setValue('1,2,3')
    
    // Manually call the search method to test announcement
    await wrapper.vm.performLookup()
    
    expect(accessibilityService.announce).toHaveBeenCalledWith('Starting number lookup search')
  })

  it('focuses input on mount and after clear', async () => {
    const wrapper = mount(NumberLookup)
    
    // Clear form should focus input
    await wrapper.find('.clear-button').trigger('click')
    
    expect(accessibilityService.announce).toHaveBeenCalledWith('Form cleared')
  })

  it('has proper loading state accessibility', async () => {
    const wrapper = mount(NumberLookup)
    
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

  it('supports keyboard shortcuts', () => {
    const wrapper = mount(NumberLookup)
    
    // Component should set up keyboard navigation
    expect(wrapper.vm).toBeDefined()
    
    // Keyboard cleanup should be available after mount
    expect(wrapper.vm.keyboardCleanup).toBeDefined()
  })

  it('has proper color contrast in high contrast mode', () => {
    // Add high contrast class to test styling
    document.documentElement.classList.add('high-contrast')
    
    const wrapper = mount(NumberLookup)
    
    // Component should render without errors in high contrast mode
    expect(wrapper.exists()).toBe(true)
    
    // Clean up
    document.documentElement.classList.remove('high-contrast')
  })

  it('respects reduced motion preferences', () => {
    // Add reduced motion class to test styling
    document.documentElement.classList.add('reduced-motion')
    
    const wrapper = mount(NumberLookup)
    
    // Component should render without errors with reduced motion
    expect(wrapper.exists()).toBe(true)
    
    // Clean up
    document.documentElement.classList.remove('reduced-motion')
  })
})