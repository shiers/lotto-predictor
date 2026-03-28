import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import TimelineChart from '../TimelineChart.vue'

// Mock Chart.js
vi.mock('chart.js', () => ({
  Chart: vi.fn().mockImplementation(() => ({
    destroy: vi.fn(),
    update: vi.fn(),
    toBase64Image: vi.fn(() => 'data:image/png;base64,mock-image-data'),
    config: { type: 'line' },
    data: {},
    options: {}
  })),
  CategoryScale: vi.fn(),
  LinearScale: vi.fn(),
  TimeScale: vi.fn(),
  LineElement: vi.fn(),
  PointElement: vi.fn(),
  Title: vi.fn(),
  Tooltip: vi.fn(),
  Legend: vi.fn(),
  ScatterController: vi.fn(),
  register: vi.fn()
}))

// Mock chartjs-adapter-date-fns
vi.mock('chartjs-adapter-date-fns', () => ({}))

describe('TimelineChart', () => {
  const mockData = [
    {
      date: '2023-12-01',
      value: 25,
      movingAverage: 22.5,
      isHotPeriod: true,
      isColdPeriod: false,
      annotation: 'Peak period'
    },
    {
      date: '2023-12-02',
      value: 20,
      movingAverage: 21.0,
      isHotPeriod: false,
      isColdPeriod: false
    },
    {
      date: '2023-12-03',
      value: 15,
      movingAverage: 19.5,
      isHotPeriod: false,
      isColdPeriod: true,
      annotation: 'Low period'
    },
    {
      date: '2023-12-04',
      value: 18,
      movingAverage: 20.0,
      isHotPeriod: false,
      isColdPeriod: false
    }
  ]

  const mockHotColdPeriods = [
    {
      id: '1',
      startDate: '2023-12-01',
      endDate: '2023-12-01',
      type: 'hot' as const,
      label: 'Hot Period',
      color: '#ff4444'
    },
    {
      id: '2',
      startDate: '2023-12-03',
      endDate: '2023-12-03',
      type: 'cold' as const,
      label: 'Cold Period',
      color: '#4444ff'
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders with default props', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    expect(wrapper.find('.timeline-chart').exists()).toBe(true)
    expect(wrapper.find('h3').text()).toBe('Historical Pattern Timeline')
    expect(wrapper.find('canvas').exists()).toBe(true)
  })

  it('renders with custom title', () => {
    const customTitle = 'Custom Timeline Chart'
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData,
        title: customTitle
      }
    })

    expect(wrapper.find('h3').text()).toBe(customTitle)
  })

  it('displays time scale selector', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    const selector = wrapper.find('.time-scale-selector select')
    expect(selector.exists()).toBe(true)
    
    const options = selector.findAll('option')
    expect(options).toHaveLength(3)
    expect(options[0].text()).toBe('Daily')
    expect(options[1].text()).toBe('Weekly')
    expect(options[2].text()).toBe('Monthly')
  })

  it('displays date range selectors', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    const dateInputs = wrapper.findAll('.date-input')
    expect(dateInputs).toHaveLength(2)
    expect(dateInputs[0].attributes('type')).toBe('date')
    expect(dateInputs[1].attributes('type')).toBe('date')
  })

  it('displays export button', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    const exportBtn = wrapper.find('.export-btn')
    expect(exportBtn.exists()).toBe(true)
    expect(exportBtn.text()).toBe('Export')
  })

  it('shows legend when hot/cold periods are provided', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData,
        hotColdPeriods: mockHotColdPeriods,
        showHotColdPeriods: true
      }
    })

    expect(wrapper.find('.chart-legend').exists()).toBe(true)
    const legendItems = wrapper.findAll('.legend-item')
    expect(legendItems).toHaveLength(2)
  })

  it('hides legend when no hot/cold periods', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData,
        hotColdPeriods: [],
        showHotColdPeriods: false
      }
    })

    expect(wrapper.find('.chart-legend').exists()).toBe(false)
  })

  it('changes time scale when selector changes', async () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    const selector = wrapper.find('.time-scale-selector select')
    await selector.setValue('weekly')

    expect(wrapper.vm.timeScale).toBe('weekly')
  })

  it('emits timeScaleChange event', async () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    await wrapper.vm.updateTimeScale()

    expect(wrapper.emitted('timeScaleChange')).toBeTruthy()
  })

  it('emits dateRangeChange event', async () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    wrapper.vm.startDate = '2023-12-01'
    wrapper.vm.endDate = '2023-12-04'
    await wrapper.vm.updateDateRange()

    expect(wrapper.emitted('dateRangeChange')).toBeTruthy()
    expect(wrapper.emitted('dateRangeChange')[0]).toEqual(['2023-12-01', '2023-12-04'])
  })

  it('handles empty data gracefully', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: []
      }
    })

    expect(wrapper.find('.timeline-chart').exists()).toBe(true)
    expect(wrapper.find('canvas').exists()).toBe(true)
    expect(wrapper.vm.processedData).toEqual([])
  })

  it('processes data correctly for different time scales', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData,
        initialTimeScale: 'daily'
      }
    })

    // Daily should return original data (sorted)
    expect(wrapper.vm.processedData).toHaveLength(4)
    
    // Test weekly aggregation
    wrapper.vm.timeScale = 'weekly'
    const weeklyData = wrapper.vm.processedData
    expect(weeklyData.length).toBeGreaterThan(0)
    
    // Test monthly aggregation
    wrapper.vm.timeScale = 'monthly'
    const monthlyData = wrapper.vm.processedData
    expect(monthlyData.length).toBeGreaterThan(0)
  })

  it('filters data by date range', async () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    wrapper.vm.startDate = '2023-12-02'
    wrapper.vm.endDate = '2023-12-03'
    
    const filteredData = wrapper.vm.processedData
    expect(filteredData).toHaveLength(2)
    expect(filteredData[0].date).toBe('2023-12-02')
    expect(filteredData[1].date).toBe('2023-12-03')
  })

  it('aggregates data by week correctly', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    const weeklyData = wrapper.vm.aggregateByWeek(mockData)
    expect(weeklyData.length).toBeGreaterThan(0)
    expect(weeklyData[0]).toHaveProperty('date')
    expect(weeklyData[0]).toHaveProperty('value')
    expect(weeklyData[0]).toHaveProperty('movingAverage')
  })

  it('aggregates data by month correctly', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    const monthlyData = wrapper.vm.aggregateByMonth(mockData)
    expect(monthlyData.length).toBeGreaterThan(0)
    expect(monthlyData[0]).toHaveProperty('date')
    expect(monthlyData[0]).toHaveProperty('value')
    expect(monthlyData[0]).toHaveProperty('movingAverage')
  })

  it('includes moving average dataset when enabled', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData,
        showMovingAverage: true
      }
    })

    const chartData = wrapper.vm.chartData
    expect(chartData.datasets).toHaveLength(2)
    expect(chartData.datasets[1].label).toBe('Moving Average')
  })

  it('excludes moving average dataset when disabled', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData,
        showMovingAverage: false
      }
    })

    const chartData = wrapper.vm.chartData
    expect(chartData.datasets).toHaveLength(1)
    expect(chartData.datasets[0].label).toBe('Frequency')
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

    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    await wrapper.vm.exportChart()

    expect(wrapper.emitted('exportComplete')).toBeTruthy()
    expect(mockLink.click).toHaveBeenCalled()
  })

  it('emits dataPointClick event when interactive', async () => {
    const wrapper = mount(TimelineChart, {
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

    expect(wrapper.emitted('dataPointClick')).toBeTruthy()
  })

  it('initializes date range from data', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    expect(wrapper.vm.startDate).toBe('2023-12-01')
    expect(wrapper.vm.endDate).toBe('2023-12-04')
  })

  it('displays loading state', async () => {
    const wrapper = mount(TimelineChart, {
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
    const wrapper = mount(TimelineChart, {
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
    const wrapper = mount(TimelineChart, {
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

  it('exposes setDateRange method', () => {
    const wrapper = mount(TimelineChart, {
      props: {
        data: mockData
      }
    })

    wrapper.vm.setDateRange('2023-12-01', '2023-12-02')
    
    expect(wrapper.vm.startDate).toBe('2023-12-01')
    expect(wrapper.vm.endDate).toBe('2023-12-02')
  })
})