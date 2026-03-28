import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import FrequencyChart from '../FrequencyChart.vue'

// Mock Chart.js
const mockChart = {
  destroy: vi.fn(),
  update: vi.fn(),
  toBase64Image: vi.fn(() => 'data:image/png;base64,mock-image-data'),
  config: { type: 'bar' },
  data: {},
  options: {}
}

vi.mock('chart.js', () => ({
  Chart: vi.fn().mockImplementation(() => mockChart),
  CategoryScale: vi.fn(),
  LinearScale: vi.fn(),
  BarElement: vi.fn(),
  LineElement: vi.fn(),
  PointElement: vi.fn(),
  ArcElement: vi.fn(),
  Title: vi.fn(),
  Tooltip: vi.fn(),
  Legend: vi.fn(),
  register: vi.fn()
}))

// Mock DOM methods
Object.defineProperty(global, 'HTMLCanvasElement', {
  value: class HTMLCanvasElement {
    getContext() {
      return {}
    }
  }
})

describe('FrequencyChart', () => {
  const mockData = [
    {
      number: 1,
      totalOccurrences: 25,
      percentage: 5.2,
      isHot: true,
      isCold: false,
      lastAppearance: '2023-12-01'
    },
    {
      number: 2,
      totalOccurrences: 15,
      percentage: 3.1,
      isHot: false,
      isCold: true,
      lastAppearance: '2023-11-15'
    },
    {
      number: 3,
      totalOccurrences: 20,
      percentage: 4.2,
      isHot: false,
      isCold: false,
      lastAppearance: '2023-12-10'
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders with default props', () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData
      }
    })

    expect(wrapper.find('.frequency-chart').exists()).toBe(true)
    expect(wrapper.find('h3').text()).toBe('Frequency Analysis')
    expect(wrapper.find('canvas').exists()).toBe(true)
  })

  it('renders with custom title', () => {
    const customTitle = 'Custom Frequency Chart'
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData,
        title: customTitle
      }
    })

    expect(wrapper.find('h3').text()).toBe(customTitle)
  })

  it('displays chart type selector', () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData
      }
    })

    const selector = wrapper.find('.chart-type-selector')
    expect(selector.exists()).toBe(true)
    
    const options = selector.findAll('option')
    expect(options).toHaveLength(3)
    expect(options[0].text()).toBe('Bar Chart')
    expect(options[1].text()).toBe('Line Chart')
    expect(options[2].text()).toBe('Doughnut Chart')
  })

  it('displays export button when enabled', () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData,
        exportEnabled: true
      }
    })

    const exportBtn = wrapper.find('.export-btn')
    expect(exportBtn.exists()).toBe(true)
    expect(exportBtn.text()).toBe('Export')
  })

  it('hides export button when disabled', () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData,
        exportEnabled: false
      }
    })

    const exportBtn = wrapper.find('.export-btn')
    expect(exportBtn.attributes('disabled')).toBeDefined()
  })

  it('changes chart type when selector changes', async () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData
      }
    })

    const selector = wrapper.find('.chart-type-selector')
    await selector.setValue('line')

    expect(wrapper.vm.chartType).toBe('line')
  })

  it('handles empty data gracefully', () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: []
      }
    })

    expect(wrapper.find('.frequency-chart').exists()).toBe(true)
    expect(wrapper.find('canvas').exists()).toBe(true)
  })

  it('emits chartClick event when interactive', async () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData,
        interactive: true
      }
    })

    // Simulate chart click by calling the onClick handler directly
    const chartOptions = wrapper.vm.chartOptions
    if (chartOptions.onClick) {
      const mockEvent = {} as any
      const mockElements = [{ index: 0 }] as any
      chartOptions.onClick(mockEvent, mockElements)
    }

    expect(wrapper.emitted('chartClick')).toBeTruthy()
  })

  it('does not emit events when not interactive', async () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData,
        interactive: false
      }
    })

    // Simulate chart click
    const chartOptions = wrapper.vm.chartOptions
    if (chartOptions.onClick) {
      const mockEvent = {} as any
      const mockElements = [{ index: 0 }] as any
      chartOptions.onClick(mockEvent, mockElements)
    }

    expect(wrapper.emitted('chartClick')).toBeFalsy()
  })

  it('generates correct colors for hot/cold numbers', () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData
      }
    })

    const hotColor = wrapper.vm.getNumberColor(mockData[0]) // Hot number
    const coldColor = wrapper.vm.getNumberColor(mockData[1]) // Cold number
    const normalColor = wrapper.vm.getNumberColor(mockData[2]) // Normal number

    expect(hotColor).toContain('255, 68, 68') // Red for hot
    expect(coldColor).toContain('68, 68, 255') // Blue for cold
    expect(normalColor).toContain('68, 255, 68') // Green for normal
  })

  it('handles export functionality', async () => {
    // Mock URL.createObjectURL and document.createElement
    global.URL.createObjectURL = vi.fn(() => 'mock-url')
    const mockLink = {
      click: vi.fn(),
      download: '',
      href: ''
    }
    vi.spyOn(document, 'createElement').mockReturnValue(mockLink as any)

    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData,
        exportEnabled: true
      }
    })

    await wrapper.vm.exportChart()

    expect(wrapper.emitted('exportComplete')).toBeTruthy()
    expect(mockLink.click).toHaveBeenCalled()
  })

  it('displays loading state', async () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData
      }
    })

    wrapper.vm.loading = true
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.loading-overlay').exists()).toBe(true)
    expect(wrapper.find('.spinner').exists()).toBe(true)
  })

  it('displays error state', async () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData
      }
    })

    wrapper.vm.error = 'Test error message'
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.error-message').exists()).toBe(true)
    expect(wrapper.find('.error-message p').text()).toBe('Test error message')
    expect(wrapper.find('.retry-btn').exists()).toBe(true)
  })

  it('handles retry functionality', async () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData
      }
    })

    wrapper.vm.error = 'Test error'
    await wrapper.vm.$nextTick()

    const retryBtn = wrapper.find('.retry-btn')
    await retryBtn.trigger('click')

    expect(wrapper.vm.error).toBe('')
  })

  it('processes doughnut chart data correctly', async () => {
    const wrapper = mount(FrequencyChart, {
      props: {
        data: mockData,
        initialChartType: 'doughnut'
      }
    })

    const chartData = wrapper.vm.chartData
    expect(chartData.datasets[0].data).toHaveLength(3) // All 3 numbers since we have only 3
    expect(chartData.labels[0]).toContain('Number')
  })

  it('sorts data by number for consistent display', () => {
    const unsortedData = [
      { number: 3, totalOccurrences: 20, percentage: 4.2, isHot: false, isCold: false, lastAppearance: '2023-12-10' },
      { number: 1, totalOccurrences: 25, percentage: 5.2, isHot: true, isCold: false, lastAppearance: '2023-12-01' },
      { number: 2, totalOccurrences: 15, percentage: 3.1, isHot: false, isCold: true, lastAppearance: '2023-11-15' }
    ]

    const wrapper = mount(FrequencyChart, {
      props: {
        data: unsortedData
      }
    })

    const chartData = wrapper.vm.chartData
    expect(chartData.labels).toEqual(['1', '2', '3'])
  })
})