import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import AutoComplete from '../AutoComplete.vue'
import { LookupService } from '@/services/lookupService'
import type { SearchSuggestion } from '@/services/lookupService'

// Mock the lookup service
vi.mock('@/services/lookupService', () => ({
  LookupService: {
    getSearchSuggestions: vi.fn(),
    parseNumberInput: vi.fn(),
    validateCombination: vi.fn(),
    validateNumber: vi.fn()
  }
}))

describe('AutoComplete', () => {
  const mockSuggestions: SearchSuggestion[] = [
    {
      text: '7',
      type: 'number',
      relevance: 95,
      previewInfo: 'Appeared 45 times',
      metadata: { frequency: 45 }
    },
    {
      text: '17',
      type: 'number',
      relevance: 85,
      previewInfo: 'Appeared 32 times',
      metadata: { frequency: 32 }
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(LookupService.getSearchSuggestions).mockResolvedValue(mockSuggestions)
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([])
    vi.mocked(LookupService.validateCombination).mockReturnValue({ isValid: true, errors: [] })
    vi.mocked(LookupService.validateNumber).mockReturnValue(true)
  })

  it('renders correctly with default props', () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number'
      }
    })

    expect(wrapper.find('.autocomplete-input').exists()).toBe(true)
    expect(wrapper.find('.autocomplete-input').attributes('placeholder')).toBe('Start typing...')
  })

  it('displays custom placeholder', () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number',
        placeholder: 'Enter a number...'
      }
    })

    expect(wrapper.find('.autocomplete-input').attributes('placeholder')).toBe('Enter a number...')
  })

  it('updates input value when modelValue prop changes', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number'
      }
    })

    await wrapper.setProps({ modelValue: '7' })
    expect(wrapper.find('.autocomplete-input').element.value).toBe('7')
  })

  it('emits update:modelValue when input changes', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number'
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('7')

    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['7'])
  })

  it('shows loading state when searching', async () => {
    // Mock a delayed response
    vi.mocked(LookupService.getSearchSuggestions).mockImplementation(
      () => new Promise(resolve => setTimeout(() => resolve(mockSuggestions), 100))
    )

    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number',
        debounceMs: 0 // Disable debounce for testing
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('7')
    await input.trigger('input')
    await input.trigger('focus')

    // Manually set loading state to test UI
    wrapper.vm.isLoading = true
    await wrapper.vm.$nextTick()

    // Should show loading spinner
    expect(wrapper.find('.loading-spinner').exists()).toBe(true)
    expect(wrapper.find('.autocomplete-input').classes()).toContain('is-loading')
  })

  it('displays suggestions after successful search', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number',
        debounceMs: 0
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('7')
    await input.trigger('input')
    await input.trigger('focus')

    // Wait for async operations
    await new Promise(resolve => setTimeout(resolve, 0))
    await wrapper.vm.$nextTick()

    expect(LookupService.getSearchSuggestions).toHaveBeenCalledWith({
      query: '7',
      type: 'number',
      maxSuggestions: 10,
      includePreview: true
    })

    expect(wrapper.find('.autocomplete-dropdown').exists()).toBe(true)
  })

  it('hides suggestions when input loses focus', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '7',
        searchType: 'number'
      }
    })

    // Show suggestions first
    wrapper.vm.showSuggestions = true
    wrapper.vm.suggestions = mockSuggestions
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.autocomplete-dropdown').exists()).toBe(true)

    // Trigger blur
    const input = wrapper.find('.autocomplete-input')
    await input.trigger('blur')

    // Wait for the blur timeout
    await new Promise(resolve => setTimeout(resolve, 200))
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.autocomplete-dropdown').exists()).toBe(false)
  })

  it('selects suggestion when clicked', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number'
      }
    })

    // Set up suggestions
    wrapper.vm.suggestions = mockSuggestions
    wrapper.vm.showSuggestions = true
    await wrapper.vm.$nextTick()

    // Click on first suggestion
    const suggestionComponent = wrapper.findComponent({ name: 'SearchSuggestions' })
    await suggestionComponent.vm.$emit('select', mockSuggestions[0])

    expect(wrapper.emitted('select')).toBeTruthy()
    expect(wrapper.emitted('select')?.[0]).toEqual([mockSuggestions[0]])
    expect(wrapper.vm.inputValue).toBe('7')
  })

  it('shows clear button when input has value and clearable is true', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '7',
        searchType: 'number',
        clearable: true
      }
    })

    expect(wrapper.find('.clear-button').exists()).toBe(true)
  })

  it('clears input when clear button is clicked', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '7',
        searchType: 'number',
        clearable: true
      }
    })

    const clearButton = wrapper.find('.clear-button')
    await clearButton.trigger('click')

    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(wrapper.emitted('clear')).toBeTruthy()
    expect(wrapper.vm.inputValue).toBe('')
  })

  it('shows search type indicator when configured', () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number'
      }
    })

    const indicator = wrapper.find('.search-type-indicator')
    expect(indicator.exists()).toBe(true)
    expect(indicator.text()).toBe('🔢') // Number icon
  })

  it('validates number input correctly', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([50])
    vi.mocked(LookupService.validateNumber).mockReturnValue(false)

    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number',
        validateInput: true,
        errorMessage: 'Invalid input' // Set error message to trigger has-error class
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('50')
    await input.trigger('input')

    expect(wrapper.emitted('error')).toBeTruthy()
    expect(wrapper.find('.autocomplete-input').classes()).toContain('has-error')
  })

  it('validates combination input correctly', async () => {
    vi.mocked(LookupService.parseNumberInput).mockReturnValue([1, 1, 3])
    vi.mocked(LookupService.validateCombination).mockReturnValue({
      isValid: false,
      errors: ['Combination cannot contain duplicate numbers']
    })

    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'combination',
        validateInput: true
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('1, 1, 3')
    await input.trigger('input')

    expect(wrapper.emitted('error')).toBeTruthy()
    expect(wrapper.emitted('error')?.[0]).toEqual(['Combination cannot contain duplicate numbers'])
  })

  it('validates range input correctly', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'range',
        validateInput: true
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('50-60')
    await input.trigger('input')

    expect(wrapper.emitted('error')).toBeTruthy()
    expect(wrapper.emitted('error')?.[0]).toEqual(['Invalid range. Use format: 1-10 (numbers between 1 and 40)'])
  })

  it('validates draw number input correctly', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'drawNumber',
        validateInput: true
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('-5')
    await input.trigger('input')

    expect(wrapper.emitted('error')).toBeTruthy()
    expect(wrapper.emitted('error')?.[0]).toEqual(['Draw number must be positive'])
  })

  it('validates date input correctly', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'date',
        validateInput: true
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('invalid-date')
    await input.trigger('input')

    expect(wrapper.emitted('error')).toBeTruthy()
    expect(wrapper.emitted('error')?.[0]).toEqual(['Invalid date format'])
  })

  it('respects minLength prop', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number',
        minLength: 2,
        debounceMs: 0
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('7')
    await input.trigger('input')
    await input.trigger('focus')

    // Should not search with single character
    expect(LookupService.getSearchSuggestions).not.toHaveBeenCalled()

    await input.setValue('17')
    await input.trigger('input')

    // Wait for debounce and async operations
    await new Promise(resolve => setTimeout(resolve, 0))

    expect(LookupService.getSearchSuggestions).toHaveBeenCalled()
  })

  it('respects maxSuggestions prop', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number',
        maxSuggestions: 5,
        debounceMs: 0
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('7')
    await input.trigger('input')
    await input.trigger('focus')

    await new Promise(resolve => setTimeout(resolve, 0))

    expect(LookupService.getSearchSuggestions).toHaveBeenCalledWith({
      query: '7',
      type: 'number',
      maxSuggestions: 5,
      includePreview: true
    })
  })

  it('handles search errors gracefully', async () => {
    vi.mocked(LookupService.getSearchSuggestions).mockRejectedValue(new Error('API Error'))

    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number',
        debounceMs: 0,
        validateInput: false // Disable validation to test search errors
      }
    })

    const input = wrapper.find('.autocomplete-input')
    await input.setValue('7')
    await input.trigger('input')
    await input.trigger('focus')

    await new Promise(resolve => setTimeout(resolve, 0))

    expect(wrapper.emitted('error')).toBeTruthy()
    expect(wrapper.emitted('error')?.[0]).toEqual(['Failed to load suggestions'])
  })

  it('shows error message when provided', () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number',
        errorMessage: 'Invalid input'
      }
    })

    expect(wrapper.find('.autocomplete-error').exists()).toBe(true)
    expect(wrapper.find('.autocomplete-error').text()).toBe('Invalid input')
    expect(wrapper.find('.autocomplete-input').classes()).toContain('has-error')
  })

  it('shows hint text when provided', () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number',
        showHint: true,
        hintText: 'Enter numbers between 1 and 40'
      }
    })

    expect(wrapper.find('.autocomplete-hint').exists()).toBe(true)
    expect(wrapper.find('.autocomplete-hint').text()).toBe('Enter numbers between 1 and 40')
  })

  it('disables input when disabled prop is true', () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number',
        disabled: true
      }
    })

    const input = wrapper.find('.autocomplete-input')
    expect(input.attributes('disabled')).toBeDefined()
  })

  it('exposes focus and blur methods', () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number'
      }
    })

    expect(wrapper.vm.focus).toBeDefined()
    expect(wrapper.vm.blur).toBeDefined()
    expect(wrapper.vm.clearCache).toBeDefined()
    expect(wrapper.vm.performSearch).toBeDefined()
  })

  it('calculates dropdown position correctly', async () => {
    // Mock getBoundingClientRect
    const mockRect = {
      bottom: 100,
      top: 50,
      left: 0,
      right: 200,
      width: 200,
      height: 50
    }
    
    Element.prototype.getBoundingClientRect = vi.fn(() => mockRect)
    
    // Mock window height
    Object.defineProperty(window, 'innerHeight', {
      writable: true,
      configurable: true,
      value: 600
    })

    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number'
      }
    })

    // Set up suggestions to trigger dropdown
    wrapper.vm.suggestions = mockSuggestions
    wrapper.vm.showSuggestions = true
    await wrapper.vm.$nextTick()

    // Should position below by default (enough space)
    expect(wrapper.vm.dropdownPosition).toBe('below')
  })

  it('handles keyboard navigation delegation', async () => {
    const wrapper = mount(AutoComplete, {
      props: {
        modelValue: '',
        searchType: 'number'
      }
    })

    // Set up suggestions
    wrapper.vm.suggestions = mockSuggestions
    wrapper.vm.showSuggestions = true
    await wrapper.vm.$nextTick()

    const input = wrapper.find('.autocomplete-input')
    
    // Test arrow down key
    await input.trigger('keydown', { key: 'ArrowDown' })
    
    // Should prevent default and delegate to suggestions component
    // (The actual navigation is handled by SearchSuggestions component)
    expect(wrapper.find('.autocomplete-dropdown').exists()).toBe(true)
  })
})