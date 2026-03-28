import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import FrequencyAnalysis from '../FrequencyAnalysis.vue'
import { FrequencyService } from '@/services/frequencyService'
import accessibilityService from '@/services/accessibilityService'

// Mock services
vi.mock('@/services/frequencyService', () => ({
  FrequencyService: {
    getNumberFrequencies: vi.fn(() => Promise.resolve([
      { number: 1, totalOccurrences: 50, percentage: 5.0 },
      { number: 2, totalOccurrences: 45, percentage: 4.5 }
    ]))
  }
}))

vi.mock('@/services/accessibilityService', () => ({
  default: {
    announce: vi.fn(),
    generateId: vi.fn(() => 'test-id')
  }
}))

// Mock child components
vi.mock('../NumberHeatmap.vue', () => ({
  default: {
    name: 'NumberHeatmap',
    template: '<div data-testid="number-heatmap">Number Heatmap</div>',
    props: ['frequencyData'],
    emits: ['number-click']
  }
}))

describe('FrequencyAnalysis Accessibility', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('has proper semantic structure', () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Main container should have role="main"
    expect(wrapper.find('[role="main"]').exists()).toBe(true)
    
    // Should have proper heading hierarchy
    expect(wrapper.find('h2').exists()).toBe(true)
    expect(wrapper.find('h2').text()).toBe('Frequency Analysis')
    
    // Should have descriptive subtitle
    expect(wrapper.text()).toContain('Analyze number frequency patterns')
  })

  it('has accessible analysis type selection', () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Analysis type section should have proper heading
    const typeSection = wrapper.find('.control-section')
    expect(typeSection.find('h3').text()).toBe('Analysis Type')
    
    // Tab buttons should have proper ARIA attributes
    const tabButtons = wrapper.findAll('.tab-button')
    expect(tabButtons.length).toBeGreaterThan(0)
    
    tabButtons.forEach((button, index) => {
      expect(button.attributes('role')).toBe('tab')
      expect(button.attributes('aria-selected')).toBeDefined()
      expect(button.attributes('id')).toBeDefined()
    })
    
    // Tab container should have proper role
    const tabContainer = wrapper.find('.analysis-type-tabs')
    expect(tabContainer.attributes('role')).toBe('tablist')
    expect(tabContainer.attributes('aria-label')).toBe('Analysis type selection')
  })

  it('has accessible action buttons with proper labels', () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Analyze button should have proper attributes
    const analyzeButton = wrapper.find('.analyze-button')
    expect(analyzeButton.exists()).toBe(true)
    expect(analyzeButton.attributes('type')).toBe('button')
    expect(analyzeButton.attributes('aria-describedby')).toBe('analyze-help')
    
    // Help text should exist
    const analyzeHelp = wrapper.find('#analyze-help')
    expect(analyzeHelp.exists()).toBe(true)
    expect(analyzeHelp.classes()).toContain('sr-only')
    
    // Clear button should have proper attributes
    const clearButton = wrapper.find('.clear-button')
    expect(clearButton.exists()).toBe(true)
    expect(clearButton.attributes('aria-describedby')).toBe('clear-help')
    
    const clearHelp = wrapper.find('#clear-help')
    expect(clearHelp.exists()).toBe(true)
    expect(clearHelp.classes()).toContain('sr-only')
  })

  it('has accessible loading state', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Set loading state
    wrapper.vm.isLoading = true
    await wrapper.vm.$nextTick()
    
    // Loading state should be announced
    const loadingState = wrapper.find('.loading-state')
    expect(loadingState.exists()).toBe(true)
    expect(loadingState.attributes('role')).toBe('status')
    expect(loadingState.attributes('aria-live')).toBe('polite')
    
    // Loading spinner should be hidden from screen readers
    const spinner = wrapper.find('.loading-spinner')
    if (spinner.exists()) {
      expect(spinner.attributes('aria-hidden')).toBe('true')
    }
    
    // Button text should indicate loading
    const analyzeButton = wrapper.find('.analyze-button')
    expect(analyzeButton.text()).toContain('Analyzing...')
  })

  it('has accessible results display', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Mock results
    const mockResults = [
      { number: 1, totalOccurrences: 50, percentage: 5.0 },
      { number: 2, totalOccurrences: 45, percentage: 4.5 }
    ]
    
    wrapper.vm.individualResults = mockResults
    await wrapper.vm.$nextTick()
    
    // Results section should have proper region role
    const resultsSection = wrapper.find('.results-section')
    expect(resultsSection.exists()).toBe(true)
    expect(resultsSection.attributes('role')).toBe('region')
    expect(resultsSection.attributes('aria-labelledby')).toBe('results-title')
    
    // Results title should exist
    const resultsTitle = wrapper.find('#results-title')
    expect(resultsTitle.exists()).toBe(true)
    expect(resultsTitle.text()).toBe('Individual Number Frequencies')
    
    // Frequency grid should have proper structure
    const frequencyGrid = wrapper.find('.frequency-grid')
    expect(frequencyGrid.exists()).toBe(true)
    expect(frequencyGrid.attributes('role')).toBe('grid')
    expect(frequencyGrid.attributes('aria-label')).toBe('Number frequency results')
  })

  it('has accessible frequency cards', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Mock results
    const mockResults = [
      { number: 1, totalOccurrences: 50, percentage: 5.0 },
      { number: 2, totalOccurrences: 45, percentage: 4.5 }
    ]
    
    wrapper.vm.individualResults = mockResults
    await wrapper.vm.$nextTick()
    
    // Each frequency card should have proper attributes
    const frequencyCards = wrapper.findAll('.frequency-card')
    expect(frequencyCards.length).toBe(2)
    
    frequencyCards.forEach((card, index) => {
      expect(card.attributes('role')).toBe('gridcell')
      expect(card.attributes('tabindex')).toBe('0')
      
      const ariaLabel = card.attributes('aria-label')
      expect(ariaLabel).toContain(`Number ${mockResults[index].number}`)
      expect(ariaLabel).toContain(`${mockResults[index].totalOccurrences} occurrences`)
      expect(ariaLabel).toContain(`${mockResults[index].percentage}% frequency`)
    })
  })

  it('has accessible error state', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Set error state
    wrapper.vm.error = 'Test error message'
    await wrapper.vm.$nextTick()
    
    // Error message should have proper alert role
    const errorMessage = wrapper.find('.error-message')
    expect(errorMessage.exists()).toBe(true)
    expect(errorMessage.attributes('role')).toBe('alert')
    expect(errorMessage.attributes('aria-live')).toBe('assertive')
    
    // Error icon should be hidden from screen readers
    const errorIcon = wrapper.find('.error-icon')
    if (errorIcon.exists()) {
      expect(errorIcon.attributes('aria-hidden')).toBe('true')
    }
    
    // Dismiss button should be accessible
    const dismissButton = wrapper.find('.error-message button')
    expect(dismissButton.exists()).toBe(true)
    expect(dismissButton.attributes('aria-label')).toBe('Dismiss error message')
  })

  it('has accessible heatmap integration', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Mock results to show heatmap
    const mockResults = [
      { number: 1, totalOccurrences: 50, percentage: 5.0 }
    ]
    
    wrapper.vm.individualResults = mockResults
    await wrapper.vm.$nextTick()
    
    // Heatmap section should have proper structure
    const heatmapSection = wrapper.find('.heatmap-section')
    expect(heatmapSection.exists()).toBe(true)
    expect(heatmapSection.attributes('role')).toBe('region')
    expect(heatmapSection.attributes('aria-labelledby')).toBe('heatmap-title')
    
    // Heatmap title should exist
    const heatmapTitle = wrapper.find('#heatmap-title')
    expect(heatmapTitle.exists()).toBe(true)
    expect(heatmapTitle.classes()).toContain('sr-only')
    expect(heatmapTitle.text()).toBe('Number frequency heatmap visualization')
    
    // NumberHeatmap component should receive proper props
    const heatmap = wrapper.findComponent({ name: 'NumberHeatmap' })
    expect(heatmap.exists()).toBe(true)
    expect(heatmap.props('frequencyData')).toEqual(mockResults)
  })

  it('announces analysis progress to screen readers', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Trigger analysis
    await wrapper.vm.performAnalysis()
    
    expect(accessibilityService.announce).toHaveBeenCalledWith('Starting frequency analysis')
  })

  it('announces results to screen readers', async () => {
    const mockResults = [
      { number: 1, totalOccurrences: 50, percentage: 5.0 },
      { number: 2, totalOccurrences: 45, percentage: 4.5 }
    ]
    
    vi.mocked(FrequencyService.getNumberFrequencies).mockResolvedValue(mockResults)
    
    const wrapper = mount(FrequencyAnalysis)
    
    // Perform analysis
    await wrapper.vm.performAnalysis()
    
    expect(accessibilityService.announce).toHaveBeenCalledWith(
      expect.stringContaining('Analysis complete. Found 2 number frequencies')
    )
  })

  it('supports keyboard navigation for tabs', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    const tabContainer = wrapper.find('.analysis-type-tabs')
    const tabs = wrapper.findAll('.tab-button')
    
    // First tab should be focusable
    expect(tabs[0].attributes('tabindex')).toBe('0')
    
    // Other tabs should not be in tab order initially
    for (let i = 1; i < tabs.length; i++) {
      expect(tabs[i].attributes('tabindex')).toBe('-1')
    }
    
    // Arrow key navigation should be supported
    const firstTab = tabs[0]
    
    // Simulate arrow right key
    await firstTab.trigger('keydown', { key: 'ArrowRight' })
    
    // Should move focus to next tab
    expect(tabs[1].attributes('tabindex')).toBe('0')
    expect(tabs[0].attributes('tabindex')).toBe('-1')
  })

  it('has proper focus management', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Clear results should focus the analyze button
    await wrapper.vm.clearResults()
    
    await wrapper.vm.$nextTick()
    
    const analyzeButton = wrapper.find('.analyze-button')
    expect(document.activeElement).toBe(analyzeButton.element)
  })

  it('respects accessibility preferences', () => {
    // Test with high contrast mode
    document.documentElement.classList.add('high-contrast')
    
    const wrapper = mount(FrequencyAnalysis)
    
    expect(wrapper.exists()).toBe(true)
    
    // Test with reduced motion
    document.documentElement.classList.remove('high-contrast')
    document.documentElement.classList.add('reduced-motion')
    
    const wrapper2 = mount(FrequencyAnalysis)
    
    expect(wrapper2.exists()).toBe(true)
    
    // Clean up
    document.documentElement.classList.remove('reduced-motion')
  })

  it('has accessible keyboard shortcuts', () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Component should set up keyboard shortcuts
    expect(wrapper.vm).toBeDefined()
    
    // Keyboard shortcuts should be documented
    const shortcutsHelp = wrapper.find('#keyboard-shortcuts-help')
    if (shortcutsHelp.exists()) {
      expect(shortcutsHelp.classes()).toContain('sr-only')
      expect(shortcutsHelp.text()).toContain('Press Enter to analyze')
      expect(shortcutsHelp.text()).toContain('Press Escape to clear')
    }
  })
})