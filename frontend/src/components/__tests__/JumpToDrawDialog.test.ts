import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import JumpToDrawDialog from '../JumpToDrawDialog.vue'
import navigationService from '@/services/navigationService'

// Mock the navigation service
vi.mock('@/services/navigationService', () => ({
  default: {
    getDrawsInRange: vi.fn()
  }
}))

describe('JumpToDrawDialog', () => {
  const mockRecentDraws = [
    {
      draw: 1234,
      date: '2023-01-15T00:00:00Z',
      winningNumbers: [1, 15, 23, 35, 40, 42],
      winningNumber1: 1,
      winningNumber2: 15,
      winningNumber3: 23,
      winningNumber4: 35,
      winningNumber5: 40,
      winningNumber6: 42,
      bonusNumber: 7,
      powerball: 8
    },
    {
      draw: 1233,
      date: '2023-01-08T00:00:00Z',
      winningNumbers: [2, 16, 24, 36, 41, 43],
      winningNumber1: 2,
      winningNumber2: 16,
      winningNumber3: 24,
      winningNumber4: 36,
      winningNumber5: 41,
      winningNumber6: 43,
      bonusNumber: 8,
      powerball: 9
    }
  ]

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    
    // Set default mock return values
    vi.mocked(navigationService.getDrawsInRange).mockResolvedValue(mockRecentDraws)
  })

  it('renders correctly', () => {
    const wrapper = mount(JumpToDrawDialog)
    
    expect(wrapper.find('.jump-dialog').exists()).toBe(true)
    expect(wrapper.find('.dialog-header h4').text()).toBe('Jump to Draw')
    expect(wrapper.find('.close-button').exists()).toBe(true)
    expect(wrapper.find('.jump-options').exists()).toBe(true)
    expect(wrapper.find('.dialog-actions').exists()).toBe(true)
  })

  it('defaults to draw number option', () => {
    const wrapper = mount(JumpToDrawDialog)
    
    expect(wrapper.vm.jumpType).toBe('drawNumber')
    expect(wrapper.find('input[value="drawNumber"]').element.checked).toBe(true)
  })

  it('shows draw number input when draw number option is selected', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const drawNumberRadio = wrapper.find('input[value="drawNumber"]')
    await drawNumberRadio.setChecked(true)
    
    expect(wrapper.find('.number-input').exists()).toBe(true)
    expect(wrapper.find('.date-input').exists()).toBe(false)
  })

  it('shows date input when date option is selected', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const dateRadio = wrapper.find('input[value="date"]')
    await dateRadio.setChecked(true)
    
    expect(wrapper.find('.date-input').exists()).toBe(true)
    expect(wrapper.find('.number-input').exists()).toBe(false)
  })

  it('validates draw number input correctly', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const numberInput = wrapper.find('.number-input')
    
    // Test valid input
    await numberInput.setValue('1234')
    expect(wrapper.vm.isValidInput).toBe(true)
    
    // Test invalid input (zero) - 0 is falsy so isValidInput returns 0
    wrapper.vm.drawNumber = 0
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.isValidInput).toBeFalsy()
    
    // Test invalid input (too large)
    wrapper.vm.drawNumber = 10000
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.isValidInput).toBeFalsy()
    
    // Test empty input
    wrapper.vm.drawNumber = null
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.isValidInput).toBeFalsy()
  })

  it('validates date input correctly', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Switch to date mode
    const dateRadio = wrapper.find('input[value="date"]')
    await dateRadio.setChecked(true)
    
    const dateInput = wrapper.find('.date-input')
    
    // Test valid date
    await dateInput.setValue('2023-01-15')
    expect(wrapper.vm.isValidInput).toBe(true)
    
    // Test date too early
    await dateInput.setValue('1980-01-01')
    expect(wrapper.vm.isValidInput).toBe(false)
    
    // Test future date
    const futureDate = new Date()
    futureDate.setFullYear(futureDate.getFullYear() + 1)
    await dateInput.setValue(futureDate.toISOString().split('T')[0])
    expect(wrapper.vm.isValidInput).toBe(false)
  })

  it('disables jump button when input is invalid', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const jumpButton = wrapper.find('.jump-button')
    
    // Initially should be disabled (no input)
    expect(jumpButton.attributes('disabled')).toBeDefined()
    
    // Should be enabled with valid input
    const numberInput = wrapper.find('.number-input')
    await numberInput.setValue('1234')
    expect(jumpButton.attributes('disabled')).toBeUndefined()
  })

  it('emits jump event with correct data for draw number', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const numberInput = wrapper.find('.number-input')
    await numberInput.setValue('1234')
    
    const jumpButton = wrapper.find('.jump-button')
    await jumpButton.trigger('click')
    
    expect(wrapper.emitted('jump')).toBeTruthy()
    expect(wrapper.emitted('jump')[0]).toEqual([{ drawNumber: 1234 }])
  })

  it('emits jump event with correct data for date', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Switch to date mode
    const dateRadio = wrapper.find('input[value="date"]')
    await dateRadio.setChecked(true)
    
    const dateInput = wrapper.find('.date-input')
    await dateInput.setValue('2023-01-15')
    
    const jumpButton = wrapper.find('.jump-button')
    await jumpButton.trigger('click')
    
    expect(wrapper.emitted('jump')).toBeTruthy()
    expect(wrapper.emitted('jump')[0]).toEqual([{ date: '2023-01-15' }])
  })

  it('emits close event when close button is clicked', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const closeButton = wrapper.find('.close-button')
    await closeButton.trigger('click')
    
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('emits close event when cancel button is clicked', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const cancelButton = wrapper.find('.cancel-button')
    await cancelButton.trigger('click')
    
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('emits close event when overlay is clicked', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const overlay = wrapper.find('.jump-dialog-overlay')
    await overlay.trigger('click')
    
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('does not emit close event when dialog content is clicked', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const dialog = wrapper.find('.jump-dialog')
    await dialog.trigger('click')
    
    expect(wrapper.emitted('close')).toBeFalsy()
  })

  it('shows validation errors for invalid draw number', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Set invalid draw number directly
    wrapper.vm.drawNumber = 0
    await wrapper.vm.$nextTick()
    
    // Call handleJump directly to test validation
    await wrapper.vm.handleJump()
    
    expect(wrapper.vm.drawNumberError).toBe('Please enter a valid draw number (1-9999)')
  })

  it('shows validation errors for invalid date', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Switch to date mode
    wrapper.vm.jumpType = 'date'
    wrapper.vm.selectedDate = '' // Empty date
    await wrapper.vm.$nextTick()
    
    // Call handleJump directly to test validation
    await wrapper.vm.handleJump()
    
    expect(wrapper.vm.dateError).toBe('Please select a valid date')
  })

  it('clears errors when input changes', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Set an error
    wrapper.vm.drawNumberError = 'Test error'
    
    const numberInput = wrapper.find('.number-input')
    await numberInput.trigger('input')
    
    expect(wrapper.vm.drawNumberError).toBe('')
  })

  it('loads recent draws on mount', async () => {
    mount(JumpToDrawDialog)
    
    // Wait for the async operation
    await new Promise(resolve => setTimeout(resolve, 0))
    
    expect(navigationService.getDrawsInRange).toHaveBeenCalled()
  })

  it('displays recent draws when available', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Set recent draws
    wrapper.vm.recentDraws = mockRecentDraws
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.recent-draws').exists()).toBe(true)
    expect(wrapper.findAll('.recent-draw-button')).toHaveLength(2)
  })

  it('jumps to recent draw when clicked', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    wrapper.vm.recentDraws = mockRecentDraws
    await wrapper.vm.$nextTick()
    
    const firstRecentDraw = wrapper.find('.recent-draw-button')
    await firstRecentDraw.trigger('click')
    
    expect(wrapper.emitted('jump')).toBeTruthy()
    expect(wrapper.emitted('jump')[0]).toEqual([{ drawNumber: 1234 }])
  })

  it('shows suggestions', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Recent draws should trigger suggestions
    wrapper.vm.recentDraws = mockRecentDraws
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.suggestions').exists()).toBe(true)
    expect(wrapper.vm.suggestions.length).toBeGreaterThan(0)
  })

  it('applies suggestion when clicked', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const suggestion = {
      label: 'Today',
      value: '2023-01-15',
      type: 'date' as const
    }
    
    await wrapper.vm.applySuggestion(suggestion)
    
    expect(wrapper.vm.jumpType).toBe('date')
    expect(wrapper.vm.selectedDate).toBe('2023-01-15')
  })

  it('handles Enter key in draw number input', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const numberInput = wrapper.find('.number-input')
    await numberInput.setValue('1234')
    await numberInput.trigger('keyup.enter')
    
    expect(wrapper.emitted('jump')).toBeTruthy()
  })

  it('handles Enter key in date input', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Switch to date mode
    const dateRadio = wrapper.find('input[value="date"]')
    await dateRadio.setChecked(true)
    
    const dateInput = wrapper.find('.date-input')
    await dateInput.setValue('2023-01-15')
    await dateInput.trigger('keyup.enter')
    
    expect(wrapper.emitted('jump')).toBeTruthy()
  })

  it('shows loading state during jump operation', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    wrapper.vm.isLoading = true
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.loading-spinner').exists()).toBe(true)
    expect(wrapper.find('.jump-button').text()).toContain('Jumping...')
  })

  it('shows error state when error occurs', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    wrapper.vm.error = 'Test error message'
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.dialog-error').exists()).toBe(true)
    expect(wrapper.find('.dialog-error p').text()).toBe('Test error message')
  })

  it('formats date correctly for display', () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const formattedDate = wrapper.vm.formatDate('2023-01-15T00:00:00Z')
    
    // The exact format may vary based on locale, but it should be a readable date
    expect(formattedDate).toMatch(/\w{3}.*\d{1,2}.*\d{4}/)
  })

  it('focuses appropriate input on mount', async () => {
    const wrapper = mount(JumpToDrawDialog, {
      attachTo: document.body
    })
    
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0)) // Wait for focus
    
    // Should focus draw number input by default
    expect(document.activeElement).toBe(wrapper.find('.number-input').element)
    
    wrapper.unmount()
  })

  it('switches focus when changing jump type', async () => {
    const wrapper = mount(JumpToDrawDialog, {
      attachTo: document.body
    })
    
    // Switch to date mode
    const dateRadio = wrapper.find('input[value="date"]')
    await dateRadio.setChecked(true)
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0)) // Wait for focus
    
    // Should focus date input
    expect(document.activeElement).toBe(wrapper.find('.date-input').element)
    
    wrapper.unmount()
  })
})