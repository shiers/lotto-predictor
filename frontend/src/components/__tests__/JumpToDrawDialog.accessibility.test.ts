import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import JumpToDrawDialog from '../JumpToDrawDialog.vue'
import navigationService from '@/services/navigationService'
import accessibilityService from '@/services/accessibilityService'

// Mock services
vi.mock('@/services/navigationService', () => ({
  default: {
    getDrawsInRange: vi.fn(() => Promise.resolve([
      { draw: 1234, date: '2024-01-01' },
      { draw: 1235, date: '2024-01-08' }
    ]))
  }
}))

vi.mock('@/services/accessibilityService', () => ({
  default: {
    announce: vi.fn(),
    trapFocus: vi.fn(() => vi.fn()),
    pushFocus: vi.fn(),
    popFocus: vi.fn(),
    generateId: vi.fn(() => 'test-id')
  }
}))

describe('JumpToDrawDialog Accessibility', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('has proper modal dialog structure', () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Dialog overlay should have proper modal attributes
    const overlay = wrapper.find('.jump-dialog-overlay')
    expect(overlay.exists()).toBe(true)
    expect(overlay.attributes('role')).toBe('dialog')
    expect(overlay.attributes('aria-modal')).toBe('true')
    expect(overlay.attributes('aria-labelledby')).toBe('jump-dialog-title')
    
    // Dialog should have proper title
    const title = wrapper.find('#jump-dialog-title')
    expect(title.exists()).toBe(true)
    expect(title.text()).toBe('Jump to Draw')
  })

  it('has accessible close button', () => {
    const wrapper = mount(JumpToDrawDialog)
    
    const closeButton = wrapper.find('.close-button')
    expect(closeButton.exists()).toBe(true)
    expect(closeButton.attributes('aria-label')).toBe('Close dialog')
    expect(closeButton.attributes('type')).toBe('button')
    
    // Close icon should be hidden from screen readers
    const icon = closeButton.find('.icon')
    expect(icon.attributes('aria-hidden')).toBe('true')
  })

  it('has proper radio button group structure', () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Jump options should have fieldset structure
    const jumpOptions = wrapper.find('.jump-options')
    expect(jumpOptions.exists()).toBe(true)
    expect(jumpOptions.attributes('role')).toBe('radiogroup')
    expect(jumpOptions.attributes('aria-labelledby')).toBe('jump-options-title')
    
    // Should have screen reader title
    const title = wrapper.find('#jump-options-title')
    expect(title.exists()).toBe(true)
    expect(title.classes()).toContain('sr-only')
    expect(title.text()).toBe('Jump to draw options')
    
    // Radio buttons should have proper attributes
    const radioButtons = wrapper.findAll('input[type="radio"]')
    radioButtons.forEach((radio, index) => {
      expect(radio.attributes('name')).toBe('jumpType')
      expect(radio.attributes('id')).toBeDefined()
    })
  })

  it('has accessible draw number input', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Select draw number option
    await wrapper.find('input[value="drawNumber"]').setChecked(true)
    
    const drawNumberInput = wrapper.find('input[type="number"]')
    expect(drawNumberInput.exists()).toBe(true)
    expect(drawNumberInput.attributes('id')).toBe('draw-number-input')
    expect(drawNumberInput.attributes('aria-describedby')).toBe('draw-number-help')
    expect(drawNumberInput.attributes('min')).toBe('1')
    expect(drawNumberInput.attributes('max')).toBe('9999')
    
    // Label should be associated
    const label = wrapper.find('label[for="draw-number-input"]')
    expect(label.exists()).toBe(true)
    
    // Help text should exist
    const helpText = wrapper.find('#draw-number-help')
    expect(helpText.exists()).toBe(true)
    expect(helpText.classes()).toContain('sr-only')
    expect(helpText.text()).toContain('Enter a draw number between 1 and 9999')
  })

  it('has accessible date input', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Select date option
    await wrapper.find('input[value="date"]').setChecked(true)
    
    const dateInput = wrapper.find('input[type="date"]')
    expect(dateInput.exists()).toBe(true)
    expect(dateInput.attributes('id')).toBe('date-input')
    expect(dateInput.attributes('aria-describedby')).toBe('date-help')
    expect(dateInput.attributes('min')).toBe('1987-08-01')
    expect(dateInput.attributes('max')).toBeDefined()
    
    // Label should be associated
    const label = wrapper.find('label[for="date-input"]')
    expect(label.exists()).toBe(true)
    
    // Help text should exist and be visible
    const helpText = wrapper.find('#date-help')
    expect(helpText.exists()).toBe(true)
    expect(helpText.text()).toContain('System will find the closest available draw')
  })

  it('shows validation errors with proper ARIA attributes', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Set invalid draw number
    wrapper.vm.drawNumber = -1
    wrapper.vm.drawNumberError = 'Invalid draw number'
    await wrapper.vm.$nextTick()
    
    const errorMessage = wrapper.find('.input-error')
    expect(errorMessage.exists()).toBe(true)
    expect(errorMessage.attributes('role')).toBe('alert')
    expect(errorMessage.attributes('aria-live')).toBe('polite')
    expect(errorMessage.text()).toBe('Invalid draw number')
    
    // Input should be marked as invalid
    const input = wrapper.find('input[type="number"]')
    expect(input.attributes('aria-invalid')).toBe('true')
    expect(input.attributes('aria-describedby')).toContain('draw-number-error')
  })

  it('has accessible recent draws section', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Mock recent draws
    wrapper.vm.recentDraws = [
      { draw: 1234, date: '2024-01-01' },
      { draw: 1235, date: '2024-01-08' }
    ]
    await wrapper.vm.$nextTick()
    
    // Recent draws section should have proper structure
    const recentSection = wrapper.find('.recent-draws')
    expect(recentSection.exists()).toBe(true)
    expect(recentSection.attributes('role')).toBe('region')
    expect(recentSection.attributes('aria-labelledby')).toBe('recent-draws-title')
    
    // Section title should exist
    const title = wrapper.find('#recent-draws-title')
    expect(title.exists()).toBe(true)
    expect(title.text()).toBe('Recent Draws')
    
    // Recent draw buttons should have proper attributes
    const recentButtons = wrapper.findAll('.recent-draw-button')
    recentButtons.forEach((button, index) => {
      const draw = wrapper.vm.recentDraws[index]
      expect(button.attributes('type')).toBe('button')
      expect(button.attributes('aria-label')).toBe(
        `Jump to draw ${draw.draw} from ${wrapper.vm.formatDate(draw.date)}`
      )
    })
  })

  it('has accessible suggestions section', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Suggestions should be available
    await wrapper.vm.$nextTick()
    
    const suggestionsSection = wrapper.find('.suggestions')
    expect(suggestionsSection.exists()).toBe(true)
    expect(suggestionsSection.attributes('role')).toBe('region')
    expect(suggestionsSection.attributes('aria-labelledby')).toBe('suggestions-title')
    
    // Section title should exist
    const title = wrapper.find('#suggestions-title')
    expect(title.exists()).toBe(true)
    expect(title.text()).toBe('Suggestions')
    
    // Suggestion buttons should have proper attributes
    const suggestionButtons = wrapper.findAll('.suggestion-button')
    suggestionButtons.forEach(button => {
      expect(button.attributes('type')).toBe('button')
      expect(button.attributes('aria-describedby')).toBeDefined()
    })
  })

  it('has accessible action buttons', () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Cancel button should have proper attributes
    const cancelButton = wrapper.find('.cancel-button')
    expect(cancelButton.exists()).toBe(true)
    expect(cancelButton.attributes('type')).toBe('button')
    expect(cancelButton.attributes('aria-label')).toBe('Cancel and close dialog')
    
    // Jump button should have proper attributes
    const jumpButton = wrapper.find('.jump-button')
    expect(jumpButton.exists()).toBe(true)
    expect(jumpButton.attributes('type')).toBe('button')
    expect(jumpButton.attributes('aria-describedby')).toBe('jump-button-help')
    
    // Help text should exist
    const helpText = wrapper.find('#jump-button-help')
    expect(helpText.exists()).toBe(true)
    expect(helpText.classes()).toContain('sr-only')
  })

  it('has accessible loading state', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Set loading state
    wrapper.vm.isLoading = true
    await wrapper.vm.$nextTick()
    
    // Loading spinner should be hidden from screen readers
    const spinner = wrapper.find('.loading-spinner')
    if (spinner.exists()) {
      expect(spinner.attributes('aria-hidden')).toBe('true')
    }
    
    // Button should indicate loading state
    const jumpButton = wrapper.find('.jump-button')
    expect(jumpButton.text()).toContain('Jumping...')
    expect(jumpButton.attributes('aria-busy')).toBe('true')
  })

  it('has accessible error state', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Set error state
    wrapper.vm.error = 'Test error message'
    await wrapper.vm.$nextTick()
    
    const errorContainer = wrapper.find('.dialog-error')
    expect(errorContainer.exists()).toBe(true)
    expect(errorContainer.attributes('role')).toBe('alert')
    expect(errorContainer.attributes('aria-live')).toBe('assertive')
    
    // Error icon should be hidden from screen readers
    const errorIcon = wrapper.find('.error-icon')
    expect(errorIcon.attributes('aria-hidden')).toBe('true')
  })

  it('manages focus properly', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Should trap focus on mount
    expect(accessibilityService.trapFocus).toHaveBeenCalled()
    
    // Should focus appropriate input based on jump type
    await wrapper.vm.$nextTick()
    
    // Default should focus draw number input
    const drawNumberInput = wrapper.find('input[type="number"]')
    expect(document.activeElement).toBe(drawNumberInput.element)
  })

  it('supports keyboard navigation', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Escape key should close dialog
    const overlay = wrapper.find('.jump-dialog-overlay')
    await overlay.trigger('keydown', { key: 'Escape' })
    
    expect(wrapper.emitted('close')).toBeTruthy()
    
    // Enter key should submit when valid
    wrapper.vm.jumpType = 'drawNumber'
    wrapper.vm.drawNumber = 1234
    await wrapper.vm.$nextTick()
    
    const input = wrapper.find('input[type="number"]')
    await input.trigger('keyup', { key: 'Enter' })
    
    // Should attempt to jump
    expect(wrapper.emitted('jump')).toBeTruthy()
  })

  it('handles overlay click accessibility', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Clicking overlay should close dialog
    const overlay = wrapper.find('.jump-dialog-overlay')
    await overlay.trigger('click')
    
    expect(wrapper.emitted('close')).toBeTruthy()
    
    // Clicking dialog content should not close
    const dialog = wrapper.find('.jump-dialog')
    await dialog.trigger('click')
    
    // Should not emit additional close events
    expect(wrapper.emitted('close')).toHaveLength(1)
  })

  it('announces dialog actions to screen readers', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Opening dialog should announce
    expect(accessibilityService.announce).toHaveBeenCalledWith(
      'Jump to draw dialog opened. Use Tab to navigate options.'
    )
    
    // Applying suggestion should announce
    const suggestion = { label: 'Today', value: '2024-01-01', type: 'date' }
    await wrapper.vm.applySuggestion(suggestion)
    
    expect(accessibilityService.announce).toHaveBeenCalledWith(
      'Applied suggestion: Today'
    )
  })

  it('has proper form validation announcements', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Invalid input should be announced
    wrapper.vm.drawNumber = -1
    await wrapper.vm.handleJump()
    
    expect(accessibilityService.announce).toHaveBeenCalledWith(
      'Validation error: Please enter a valid draw number (1-9999)',
      'assertive'
    )
  })

  it('respects accessibility preferences', () => {
    // Test with high contrast mode
    document.documentElement.classList.add('high-contrast')
    
    const wrapper = mount(JumpToDrawDialog)
    
    expect(wrapper.exists()).toBe(true)
    
    // Test with reduced motion
    document.documentElement.classList.remove('high-contrast')
    document.documentElement.classList.add('reduced-motion')
    
    const wrapper2 = mount(JumpToDrawDialog)
    
    expect(wrapper2.exists()).toBe(true)
    
    // Clean up
    document.documentElement.classList.remove('reduced-motion')
  })

  it('has accessible date constraints', async () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Select date option
    await wrapper.find('input[value="date"]').setChecked(true)
    
    const dateInput = wrapper.find('input[type="date"]')
    
    // Should have proper min/max constraints
    expect(dateInput.attributes('min')).toBe('1987-08-01')
    
    const maxDate = dateInput.attributes('max')
    expect(maxDate).toBeDefined()
    
    // Max date should be today or earlier
    const today = new Date().toISOString().split('T')[0]
    expect(maxDate <= today).toBe(true)
  })

  it('provides clear instructions for screen readers', () => {
    const wrapper = mount(JumpToDrawDialog)
    
    // Should have instructions for screen readers
    const instructions = wrapper.find('#dialog-instructions')
    expect(instructions.exists()).toBe(true)
    expect(instructions.classes()).toContain('sr-only')
    expect(instructions.text()).toContain('Choose how to jump to a specific draw')
    expect(instructions.text()).toContain('Use Tab to navigate between options')
    expect(instructions.text()).toContain('Press Escape to close this dialog')
  })
})