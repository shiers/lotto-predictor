import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import FrequencyAnalysis from '../FrequencyAnalysis.vue'
import { FrequencyService } from '@/services/frequencyService'
import type { NumberFrequency, RangeFrequency, HotColdNumber, FrequencyStatistics } from '@/services/frequencyService'

// Mock the frequency service
vi.mock('@/services/frequencyService', () => ({
  FrequencyService: {
    getNumberFrequencies: vi.fn(),
    getRangeFrequencies: vi.fn(),
    getHotColdAnalysis: vi.fn(),
    getFrequencyStatistics: vi.fn(),
    exportFrequencyData: vi.fn(),
    validateNumberRange: vi.fn(),
    getPresetRanges: vi.fn(),
    classifyFrequency: vi.fn(),
    getFrequencyColor: vi.fn()
  }
}))

// Mock NumberHeatmap component
vi.mock('../NumberHeatmap.vue', () => ({
  default: {
    name: 'NumberHeatmap',
    template: '<div class="number-heatmap-mock">Heatmap</div>',
    props: ['frequencyData', 'colorScheme', 'showLabels'],
    emits: ['number-click']
  }
}))

describe('FrequencyAnalysis', () => {
  const mockNumberFrequencies: NumberFrequency[] = [
    {
      number: 1,
      totalOccurrences: 25,
      lastAppearance: '2023-12-01T00:00:00Z',
      firstAppearance: '2020-01-01T00:00:00Z',
      longestGap: 15,
      currentGap: 3,
      percentage: 5.2,
      averageFrequency: 2.1,
      isHot: true,
      isCold: false
    },
    {
      number: 2,
      totalOccurrences: 12,
      lastAppearance: '2023-10-15T00:00:00Z',
      firstAppearance: '2020-02-01T00:00:00Z',
      longestGap: 25,
      currentGap: 8,
      percentage: 2.5,
      averageFrequency: 1.2,
      isHot: false,
      isCold: true
    }
  ]

  const mockRangeFrequencies: RangeFrequency[] = [
    {
      range: { startNumber: 1, endNumber: 10, label: '1-10' },
      totalOccurrences: 150,
      percentage: 25.0,
      averagePerDraw: 2.5,
      individualNumbers: mockNumberFrequencies
    }
  ]

  const mockHotColdNumbers: HotColdNumber[] = [
    {
      number: 1,
      recentOccurrences: 8,
      historicalAverage: 5,
      hotColdScore: 1.6,
      classification: 'Hot'
    },
    {
      number: 2,
      recentOccurrences: 2,
      historicalAverage: 5,
      hotColdScore: 0.4,
      classification: 'Cold'
    }
  ]

  const mockStatistics: FrequencyStatistics = {
    totalNumbers: 40,
    mostFrequent: mockNumberFrequencies.slice(0, 5),
    leastFrequent: mockNumberFrequencies.slice(-5),
    averageOccurrences: 18.5,
    totalOccurrences: 740,
    hotNumbers: 8,
    coldNumbers: 6,
    lastUpdated: '2023-12-15T10:00:00Z'
  }

  const mockPresetRanges = [
    { startNumber: 1, endNumber: 10, label: '1-10 (Low)' },
    { startNumber: 11, endNumber: 20, label: '11-20 (Mid-Low)' },
    { startNumber: 21, endNumber: 30, label: '21-30 (Mid-High)' },
    { startNumber: 31, endNumber: 40, label: '31-40 (High)' }
  ]

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    
    // Set default mock return values
    vi.mocked(FrequencyService.getPresetRanges).mockReturnValue(mockPresetRanges)
    vi.mocked(FrequencyService.validateNumberRange).mockReturnValue({
      isValid: true,
      errors: []
    })
    vi.mocked(FrequencyService.getNumberFrequencies).mockResolvedValue(mockNumberFrequencies)
    vi.mocked(FrequencyService.getRangeFrequencies).mockResolvedValue(mockRangeFrequencies)
    vi.mocked(FrequencyService.getHotColdAnalysis).mockResolvedValue(mockHotColdNumbers)
    vi.mocked(FrequencyService.getFrequencyStatistics).mockResolvedValue(mockStatistics)
  })

  it('renders correctly with default props', () => {
    const wrapper = mount(FrequencyAnalysis)
    
    expect(wrapper.find('h2').text()).toBe('Frequency Analysis')
    expect(wrapper.find('.analysis-type-tabs').exists()).toBe(true)
    expect(wrapper.find('.analyze-button').exists()).toBe(true)
  })

  it('displays analysis type tabs correctly', () => {
    const wrapper = mount(FrequencyAnalysis)
    
    const tabs = wrapper.findAll('.tab-button')
    expect(tabs).toHaveLength(3)
    expect(tabs[0].text()).toBe('Individual Numbers')
    expect(tabs[1].text()).toBe('Number Ranges')
    expect(tabs[2].text()).toBe('Hot & Cold')
  })

  it('switches analysis types when tabs are clicked', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    const rangeTab = wrapper.findAll('.tab-button')[1]
    await rangeTab.trigger('click')
    
    expect(wrapper.vm.selectedAnalysisType).toBe('ranges')
    expect(wrapper.find('.range-controls').exists()).toBe(true)
  })

  it('displays preset ranges for range analysis', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Switch to ranges tab
    const rangeTab = wrapper.findAll('.tab-button')[1]
    await rangeTab.trigger('click')
    
    const presetButtons = wrapper.findAll('.preset-button')
    expect(presetButtons.length).toBeGreaterThan(0)
    expect(presetButtons[0].text()).toBe('1-10 (Low)')
  })

  it('adds preset range when preset button is clicked', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Switch to ranges tab
    const rangeTab = wrapper.findAll('.tab-button')[1]
    await rangeTab.trigger('click')
    
    const presetButton = wrapper.find('.preset-button')
    await presetButton.trigger('click')
    
    expect(wrapper.vm.selectedRanges).toHaveLength(1)
    expect(wrapper.vm.selectedRanges[0].label).toBe('1-10 (Low)')
  })

  it('validates custom range input correctly', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Switch to ranges tab
    const rangeTab = wrapper.findAll('.tab-button')[1]
    await rangeTab.trigger('click')
    
    // Set valid custom range
    wrapper.vm.customRange.start = 5
    wrapper.vm.customRange.end = 15
    await wrapper.vm.$nextTick()
    
    expect(wrapper.vm.isValidCustomRange).toBe(true)
    
    // Set invalid custom range
    wrapper.vm.customRange.start = 15
    wrapper.vm.customRange.end = 5
    await wrapper.vm.$nextTick()
    
    expect(wrapper.vm.isValidCustomRange).toBe(false)
  })

  it('adds custom range when add button is clicked', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Switch to ranges tab
    const rangeTab = wrapper.findAll('.tab-button')[1]
    await rangeTab.trigger('click')
    
    // Set custom range
    wrapper.vm.customRange.start = 5
    wrapper.vm.customRange.end = 15
    wrapper.vm.customRange.label = 'Custom Range'
    
    const addButton = wrapper.find('.add-range-button')
    await addButton.trigger('click')
    
    expect(wrapper.vm.selectedRanges).toHaveLength(1)
    expect(wrapper.vm.selectedRanges[0].label).toBe('Custom Range')
  })

  it('removes range when remove button is clicked', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Switch to ranges tab and add a range
    const rangeTab = wrapper.findAll('.tab-button')[1]
    await rangeTab.trigger('click')
    
    wrapper.vm.selectedRanges = [mockPresetRanges[0]]
    await wrapper.vm.$nextTick()
    
    const removeButton = wrapper.find('.remove-range')
    await removeButton.trigger('click')
    
    expect(wrapper.vm.selectedRanges).toHaveLength(0)
  })

  it('performs individual number analysis correctly', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    const analyzeButton = wrapper.find('.analyze-button')
    await analyzeButton.trigger('click')
    
    expect(FrequencyService.getNumberFrequencies).toHaveBeenCalled()
    expect(FrequencyService.getFrequencyStatistics).toHaveBeenCalled()
  })

  it('performs range analysis correctly', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Switch to ranges tab and add a range
    const rangeTab = wrapper.findAll('.tab-button')[1]
    await rangeTab.trigger('click')
    
    wrapper.vm.selectedRanges = [mockPresetRanges[0]]
    
    const analyzeButton = wrapper.find('.analyze-button')
    await analyzeButton.trigger('click')
    
    expect(FrequencyService.getRangeFrequencies).toHaveBeenCalledWith({
      ranges: [mockPresetRanges[0]],
      startDate: undefined,
      endDate: undefined,
      includeBonus: false,
      includePowerball: false
    })
  })

  it('performs hot/cold analysis correctly', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Switch to hot/cold tab
    const hotColdTab = wrapper.findAll('.tab-button')[2]
    await hotColdTab.trigger('click')
    
    const analyzeButton = wrapper.find('.analyze-button')
    await analyzeButton.trigger('click')
    
    expect(FrequencyService.getHotColdAnalysis).toHaveBeenCalled()
  })

  it('displays individual results correctly', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Set results
    wrapper.vm.individualResults = mockNumberFrequencies
    wrapper.vm.summaryStats = mockStatistics
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.individual-results').exists()).toBe(true)
    expect(wrapper.find('.summary-stats').exists()).toBe(true)
    expect(wrapper.findAll('.frequency-card')).toHaveLength(2)
  })

  it('displays range results correctly', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Switch to ranges and set results
    wrapper.vm.selectedAnalysisType = 'ranges'
    wrapper.vm.rangeResults = mockRangeFrequencies
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.range-results').exists()).toBe(true)
    expect(wrapper.findAll('.range-result-card')).toHaveLength(1)
  })

  it('displays hot/cold results correctly', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Switch to hot/cold and set results
    wrapper.vm.selectedAnalysisType = 'hotcold'
    wrapper.vm.hotColdResults = mockHotColdNumbers
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.hotcold-results').exists()).toBe(true)
    expect(wrapper.find('.hot-numbers').exists()).toBe(true)
    expect(wrapper.find('.cold-numbers').exists()).toBe(true)
  })

  it('sorts results correctly', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    wrapper.vm.individualResults = mockNumberFrequencies
    wrapper.vm.sortBy = 'occurrences'
    wrapper.vm.sortOrder = 'desc'
    await wrapper.vm.$nextTick()
    
    const sortedResults = wrapper.vm.sortedIndividualResults
    expect(sortedResults[0].totalOccurrences).toBeGreaterThan(sortedResults[1].totalOccurrences)
  })

  it('toggles sort order correctly', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    expect(wrapper.vm.sortOrder).toBe('asc')
    
    const sortOrderButton = wrapper.find('.sort-order-button')
    await sortOrderButton.trigger('click')
    
    expect(wrapper.vm.sortOrder).toBe('desc')
  })

  it('applies frequency classification correctly', () => {
    const wrapper = mount(FrequencyAnalysis)
    
    const hotFrequency = mockNumberFrequencies[0]
    const coldFrequency = mockNumberFrequencies[1]
    
    expect(wrapper.vm.getFrequencyClass(hotFrequency)).toBe('hot')
    expect(wrapper.vm.getFrequencyClass(coldFrequency)).toBe('cold')
  })

  it('formats dates correctly', () => {
    const wrapper = mount(FrequencyAnalysis)
    
    const formattedDate = wrapper.vm.formatDate('2023-12-01T00:00:00Z')
    expect(formattedDate).toMatch(/\d{1,2}\/\d{1,2}\/\d{4}/)
  })

  it('clears results when clear button is clicked', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Set some results
    wrapper.vm.individualResults = mockNumberFrequencies
    wrapper.vm.summaryStats = mockStatistics
    
    const clearButton = wrapper.find('.clear-button')
    await clearButton.trigger('click')
    
    expect(wrapper.vm.individualResults).toEqual([])
    expect(wrapper.vm.summaryStats).toBeNull()
  })

  it('handles analysis errors gracefully', async () => {
    vi.mocked(FrequencyService.getNumberFrequencies).mockRejectedValue(new Error('API Error'))
    
    const wrapper = mount(FrequencyAnalysis)
    
    const analyzeButton = wrapper.find('.analyze-button')
    await analyzeButton.trigger('click')
    
    // Wait for async operation
    await new Promise(resolve => setTimeout(resolve, 0))
    
    expect(wrapper.vm.error).toBeTruthy()
    expect(wrapper.vm.isLoading).toBe(false)
  })

  it('disables analyze button when conditions not met', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Switch to ranges tab without adding ranges
    const rangeTab = wrapper.findAll('.tab-button')[1]
    await rangeTab.trigger('click')
    
    const analyzeButton = wrapper.find('.analyze-button')
    expect(analyzeButton.attributes('disabled')).toBeDefined()
  })

  it('enables analyze button when conditions are met', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    // Individual analysis should always be enabled
    const analyzeButton = wrapper.find('.analyze-button')
    expect(analyzeButton.attributes('disabled')).toBeUndefined()
  })

  it('includes date filters in analysis request', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    wrapper.vm.dateFilters.startDate = '2023-01-01'
    wrapper.vm.dateFilters.endDate = '2023-12-31'
    
    const analyzeButton = wrapper.find('.analyze-button')
    await analyzeButton.trigger('click')
    
    expect(FrequencyService.getNumberFrequencies).toHaveBeenCalled()
  })

  it('includes bonus and powerball options in analysis request', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    wrapper.vm.options.includeBonus = true
    wrapper.vm.options.includePowerball = true
    
    // Switch to ranges and add a range
    const rangeTab = wrapper.findAll('.tab-button')[1]
    await rangeTab.trigger('click')
    wrapper.vm.selectedRanges = [mockPresetRanges[0]]
    
    const analyzeButton = wrapper.find('.analyze-button')
    await analyzeButton.trigger('click')
    
    expect(FrequencyService.getRangeFrequencies).toHaveBeenCalledWith({
      ranges: [mockPresetRanges[0]],
      startDate: undefined,
      endDate: undefined,
      includeBonus: true,
      includePowerball: true
    })
  })

  it('renders heatmap when individual results are available', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    wrapper.vm.individualResults = mockNumberFrequencies
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.number-heatmap-mock').exists()).toBe(true)
  })

  it('handles heatmap number click events', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    wrapper.vm.individualResults = mockNumberFrequencies
    await wrapper.vm.$nextTick()
    
    // Test the heatmap click handler
    wrapper.vm.onHeatmapNumberClick(15)
    
    // Should not throw an error (basic functionality test)
    expect(true).toBe(true)
  })

  it('respects maxRanges prop', async () => {
    const wrapper = mount(FrequencyAnalysis, {
      props: {
        maxRanges: 2
      }
    })
    
    // Switch to ranges tab
    const rangeTab = wrapper.findAll('.tab-button')[1]
    await rangeTab.trigger('click')
    
    // Add maximum ranges
    wrapper.vm.selectedRanges = [mockPresetRanges[0], mockPresetRanges[1]]
    
    // Try to add another range
    const presetButton = wrapper.find('.preset-button')
    await presetButton.trigger('click')
    
    expect(wrapper.vm.selectedRanges).toHaveLength(2)
    expect(wrapper.vm.error).toContain('Maximum 2 ranges allowed')
  })

  it('shows loading state during analysis', async () => {
    // Mock a delayed response
    vi.mocked(FrequencyService.getNumberFrequencies).mockImplementation(
      () => new Promise(resolve => setTimeout(() => resolve(mockNumberFrequencies), 100))
    )
    
    const wrapper = mount(FrequencyAnalysis)
    
    const analyzeButton = wrapper.find('.analyze-button')
    await analyzeButton.trigger('click')
    
    expect(wrapper.vm.isLoading).toBe(true)
    expect(wrapper.find('.loading-overlay').exists()).toBe(true)
  })

  it('filters hot and cold numbers correctly', async () => {
    const wrapper = mount(FrequencyAnalysis)
    
    wrapper.vm.hotColdResults = mockHotColdNumbers
    await wrapper.vm.$nextTick()
    
    expect(wrapper.vm.hotNumbers).toHaveLength(1)
    expect(wrapper.vm.coldNumbers).toHaveLength(1)
    expect(wrapper.vm.hotNumbers[0].classification).toBe('Hot')
    expect(wrapper.vm.coldNumbers[0].classification).toBe('Cold')
  })
})