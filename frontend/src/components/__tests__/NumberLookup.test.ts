import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import NumberLookup from '../NumberLookup.vue'
import { LookupService } from '@/services/lookupService'

// Mock the lookup service
vi.mock('@/services/lookupService', () => ({
  LookupService: {
    parseNumberInput: vi.fn(),
    validateCombination: vi.fn(),
    lookupNumbers: vi.fn(),
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

describe('NumberLookup', () => {
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
    const wrapper = mount(NumberLookup)
    
    expect(wrapper.find('h2').text()).toBe('Number Lookup')
    expect(wrapper.find('.numbers-input').exists()).toBe(true)
    expect(wrapper.find('.search-button').exists()).toBe(true)
    expect(wrapper.find('.clear-button').exists()).toBe(true)
  })

  it('renders with initial numbers when provided', () => {
    const initialNumbers = [1, 15, 23]
    const wrapper = mount(NumberLookup, {
      props: {
        initialNumbers
      }
    })
    
    expect(wrapper.vm.numbersInput).toBe('1, 15, 23')
  })

  it('validates input correctly', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })

    const wrapper = mount(NumberLookup)
    const input = wrapper.find('.numbers-input')
    
    await input.setValue('1, 15, 23')
    await input.trigger('input')
    
    expect(LookupService.parseNumberInput).toHaveBeenCalledWith('1, 15, 23')
    expect(LookupService.validateCombination).toHaveBeenCalledWith([1, 15, 23])
    expect(wrapper.vm.validationErrors).toEqual([])
  })

  it('shows validation errors for invalid input', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 50, 23])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: false,
      errors: ['Invalid numbers: 50. Numbers must be between 1 and 40']
    })

    const wrapper = mount(NumberLookup)
    const input = wrapper.find('.numbers-input')
    
    await input.setValue('1, 50, 23')
    await input.trigger('input')
    
    expect(wrapper.vm.validationErrors).toContain('Invalid numbers: 50. Numbers must be between 1 and 40')
    expect(wrapper.find('.error-messages').exists()).toBe(true)
  })

  it('disables search button when input is invalid', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([])
    
    const wrapper = mount(NumberLookup)
    const input = wrapper.find('.numbers-input')
    const searchButton = wrapper.find('.search-button')
    
    await input.setValue('')
    await input.trigger('input')
    
    expect(searchButton.attributes('disabled')).toBeDefined()
  })

  it('enables search button when input is valid', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })

    const wrapper = mount(NumberLookup)
    const input = wrapper.find('.numbers-input')
    const searchButton = wrapper.find('.search-button')
    
    await input.setValue('1, 15, 23')
    await input.trigger('input')
    
    expect(searchButton.attributes('disabled')).toBeUndefined()
  })

  it('performs search when search button is clicked', async () => {
    const mockResults = [
      {
        drawNumber: 1234,
        drawDate: '2023-01-01',
        number: 1,
        position: 1,
        isBonus: false,
        isPowerball: false,
        fullCombination: [1, 15, 23, 35, 40, 42]
      }
    ]

    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    vi.mocked(LookupService.lookupNumbers).mockResolvedValue(mockResults)

    const wrapper = mount(NumberLookup)
    const input = wrapper.find('.numbers-input')
    const searchButton = wrapper.find('.search-button')
    
    await input.setValue('1, 15, 23')
    await input.trigger('input')
    await searchButton.trigger('click')
    
    expect(LookupService.lookupNumbers).toHaveBeenCalledWith({
      numbers: [1, 15, 23],
      includeBonus: true,
      includePowerball: true
    })
  })

  it('displays search results correctly', async () => {
    const mockResults = [
      {
        drawNumber: 1234,
        drawDate: '2023-01-01T00:00:00Z',
        number: 1,
        position: 1,
        isBonus: false,
        isPowerball: false,
        fullCombination: [1, 15, 23, 35, 40, 42]
      }
    ]

    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    vi.mocked(LookupService.lookupNumbers).mockResolvedValue(mockResults)

    const wrapper = mount(NumberLookup)
    
    // Set up the component state
    wrapper.vm.searchResults = mockResults
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedNumbers = [1]
    
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.results-section').exists()).toBe(true)
    expect(wrapper.find('.results-summary').text()).toContain('Found 1 occurrence for 1')
  })

  it('shows no results message when no matches found', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([99])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    vi.mocked(LookupService.lookupNumbers).mockResolvedValue([])

    const wrapper = mount(NumberLookup)
    
    // Set up the component state for no results
    wrapper.vm.searchResults = []
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedNumbers = [99]
    wrapper.vm.isLoading = false
    
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.no-results').exists()).toBe(true)
    expect(wrapper.find('.no-results h3').text()).toBe('No Results Found')
  })

  it('clears form when clear button is clicked', async () => {
    const wrapper = mount(NumberLookup)
    
    // Set some form data
    await wrapper.find('.numbers-input').setValue('1, 15, 23')
    await wrapper.find('#start-date').setValue('2023-01-01')
    await wrapper.find('#end-date').setValue('2023-12-31')
    
    const clearButton = wrapper.find('.clear-button')
    await clearButton.trigger('click')
    
    expect(wrapper.vm.numbersInput).toBe('')
    expect(wrapper.vm.startDate).toBe('')
    expect(wrapper.vm.endDate).toBe('')
    expect(wrapper.vm.searchResults).toEqual([])
    expect(wrapper.vm.hasSearched).toBe(false)
  })

  it('validates date range correctly', async () => {
    const wrapper = mount(NumberLookup)
    
    // Set invalid date range (start after end)
    wrapper.vm.startDate = '2023-12-31'
    wrapper.vm.endDate = '2023-01-01'
    wrapper.vm.numbersInput = '1, 15, 23'
    
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    
    await wrapper.vm.validateInput()
    
    expect(wrapper.vm.validationErrors).toContain('Start date must be before end date')
  })

  it('respects maxNumbers prop', async () => {
    const wrapper = mount(NumberLookup, {
      props: {
        maxNumbers: 3
      }
    })
    
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23, 35, 40])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    
    await wrapper.find('.numbers-input').setValue('1, 15, 23, 35, 40')
    await wrapper.find('.numbers-input').trigger('input')
    
    expect(wrapper.vm.validationErrors).toContain('Maximum 3 numbers allowed')
  })

  it('handles search errors gracefully', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 15, 23])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: true,
      errors: []
    })
    vi.mocked(LookupService.lookupNumbers).mockRejectedValue(new Error('API Error'))

    const wrapper = mount(NumberLookup)
    const input = wrapper.find('.numbers-input')
    const searchButton = wrapper.find('.search-button')
    
    await input.setValue('1, 15, 23')
    await input.trigger('input')
    await searchButton.trigger('click')
    
    // Wait for the async operation to complete
    await new Promise(resolve => setTimeout(resolve, 0))
    
    expect(wrapper.vm.searchResults).toEqual([])
    expect(wrapper.vm.isLoading).toBe(false)
  })

  it('formats dates correctly in results', () => {
    const wrapper = mount(NumberLookup)
    
    // Test the formatDate method
    const formattedDate = wrapper.vm.formatDate('2023-01-15T00:00:00Z')
    
    // The exact format may vary based on locale, but it should be a readable date
    expect(formattedDate).toMatch(/\d{1,2}\s\w{3}\s\d{4}/)
  })

  it('highlights searched numbers in combination display', async () => {
    const mockResults = [
      {
        drawNumber: 1234,
        drawDate: '2023-01-01T00:00:00Z',
        number: 1,
        position: 1,
        isBonus: false,
        isPowerball: false,
        fullCombination: [1, 15, 23, 35, 40, 42]
      }
    ]

    const wrapper = mount(NumberLookup)
    
    // Set up the component state
    wrapper.vm.searchResults = mockResults
    wrapper.vm.hasSearched = true
    wrapper.vm.searchedNumbers = [1, 15]
    
    await wrapper.vm.$nextTick()
    
    const combinationNumbers = wrapper.findAll('.combination-number')
    const highlightedNumbers = wrapper.findAll('.combination-number.highlighted')
    
    expect(combinationNumbers.length).toBe(6) // Full combination
    expect(highlightedNumbers.length).toBe(2) // Only searched numbers highlighted
  })
})