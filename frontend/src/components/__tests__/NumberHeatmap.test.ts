import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import NumberHeatmap from '../NumberHeatmap.vue'
import type { NumberFrequency } from '@/services/frequencyService'

describe('NumberHeatmap', () => {
  const mockFrequencyData: NumberFrequency[] = [
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
    },
    {
      number: 3,
      totalOccurrences: 18,
      lastAppearance: '2023-11-20T00:00:00Z',
      firstAppearance: '2020-03-01T00:00:00Z',
      longestGap: 20,
      currentGap: 5,
      percentage: 3.8,
      averageFrequency: 1.8,
      isHot: false,
      isCold: false
    }
  ]

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('renders correctly with default props', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    expect(wrapper.find('h3').text()).toBe('Number Frequency Heatmap')
    expect(wrapper.find('.heatmap-grid').exists()).toBe(true)
    expect(wrapper.find('.heatmap-legend').exists()).toBe(true)
    expect(wrapper.find('.heatmap-stats').exists()).toBe(true)
  })

  it('displays all 40 lottery numbers', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    const cells = wrapper.findAll('.heatmap-cell')
    expect(cells).toHaveLength(40)
  })

  it('shows labels when showLabels is true', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData,
        showLabels: true
      }
    })
    
    const labels = wrapper.findAll('.cell-label')
    expect(labels.length).toBeGreaterThan(0)
    expect(labels[0].text()).toBe('1')
  })

  it('hides labels when showLabels is false', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData,
        showLabels: false
      }
    })
    
    const labels = wrapper.findAll('.cell-label')
    expect(labels).toHaveLength(0)
  })

  it('applies correct frequency classes', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    const hotCell = wrapper.find('.heatmap-cell.hot')
    const coldCell = wrapper.find('.heatmap-cell.cold')
    const normalCell = wrapper.find('.heatmap-cell.normal')
    
    expect(hotCell.exists()).toBe(true)
    expect(coldCell.exists()).toBe(true)
    expect(normalCell.exists()).toBe(true)
  })

  it('calculates statistics correctly', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    expect(wrapper.vm.minFrequency).toBe(12)
    expect(wrapper.vm.maxFrequency).toBe(25)
    expect(wrapper.vm.averageFrequency).toBeCloseTo(18.33, 1)
    expect(wrapper.vm.mostFrequentNumber).toBe(1)
    expect(wrapper.vm.leastFrequentNumber).toBe(2)
  })

  it('displays statistics in the stats panel', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    const statsSection = wrapper.find('.heatmap-stats')
    expect(statsSection.exists()).toBe(true)
    
    const statItems = wrapper.findAll('.stat-item')
    expect(statItems.length).toBeGreaterThan(0)
    
    // Check that statistics are displayed
    const statsText = statsSection.text()
    expect(statsText).toContain('Total Numbers')
    expect(statsText).toContain('Average Frequency')
    expect(statsText).toContain('Most Frequent')
    expect(statsText).toContain('Least Frequent')
  })

  it('emits number-click event when cell is clicked', async () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    const firstCell = wrapper.find('.heatmap-cell')
    await firstCell.trigger('click')
    
    expect(wrapper.emitted('number-click')).toBeTruthy()
    expect(wrapper.emitted('number-click')![0]).toEqual([1])
  })

  it('shows tooltip on mouse enter when showTooltips is true', async () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    wrapper.vm.showTooltips = true
    
    const firstCell = wrapper.find('.heatmap-cell')
    await firstCell.trigger('mouseenter', { clientX: 100, clientY: 100 })
    
    expect(wrapper.vm.tooltip.visible).toBe(true)
    expect(wrapper.vm.tooltip.number).toBe(1)
  })

  it('hides tooltip on mouse leave', async () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    wrapper.vm.showTooltips = true
    
    const firstCell = wrapper.find('.heatmap-cell')
    await firstCell.trigger('mouseenter', { clientX: 100, clientY: 100 })
    await firstCell.trigger('mouseleave')
    
    expect(wrapper.vm.tooltip.visible).toBe(false)
  })

  it('displays tooltip with correct information', async () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    wrapper.vm.showTooltips = true
    wrapper.vm.tooltip = {
      visible: true,
      x: 100,
      y: 100,
      number: 1,
      frequency: mockFrequencyData[0]
    }
    
    await wrapper.vm.$nextTick()
    
    const tooltip = wrapper.find('.heatmap-tooltip')
    expect(tooltip.exists()).toBe(true)
    expect(tooltip.text()).toContain('Number 1')
    expect(tooltip.text()).toContain('25') // occurrences
    expect(tooltip.text()).toContain('5.20%') // percentage
  })

  it('applies hot-cold color scheme correctly', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData,
        colorScheme: 'hot-cold'
      }
    })
    
    const hotFrequency = mockFrequencyData[0]
    const coldFrequency = mockFrequencyData[1]
    const normalFrequency = mockFrequencyData[2]
    
    expect(wrapper.vm.getHotColdColor(hotFrequency)).toBe('#dc3545')
    expect(wrapper.vm.getHotColdColor(coldFrequency)).toBe('#007bff')
    expect(wrapper.vm.getHotColdColor(normalFrequency)).toBe('#28a745')
  })

  it('applies gradient color scheme correctly', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData,
        colorScheme: 'gradient'
      }
    })
    
    const highFrequency = mockFrequencyData[0] // 25 occurrences
    const lowFrequency = mockFrequencyData[1] // 12 occurrences
    
    const highColor = wrapper.vm.getGradientColor(highFrequency)
    const lowColor = wrapper.vm.getGradientColor(lowFrequency)
    
    expect(highColor).toMatch(/rgb\(\d+, \d+, \d+\)/)
    expect(lowColor).toMatch(/rgb\(\d+, \d+, \d+\)/)
    expect(highColor).not.toBe(lowColor)
  })

  it('applies discrete color scheme correctly', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData,
        colorScheme: 'discrete'
      }
    })
    
    const frequency = mockFrequencyData[0]
    const color = wrapper.vm.getDiscreteColor(frequency)
    
    expect(color).toMatch(/#[0-9a-f]{6}/i)
  })

  it('handles empty frequency data gracefully', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: []
      }
    })
    
    expect(wrapper.vm.minFrequency).toBe(0)
    expect(wrapper.vm.maxFrequency).toBe(0)
    expect(wrapper.vm.averageFrequency).toBe(0)
    expect(wrapper.vm.mostFrequentNumber).toBe(0)
    expect(wrapper.vm.leastFrequentNumber).toBe(0)
  })

  it('displays legend correctly', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    const legend = wrapper.find('.heatmap-legend')
    expect(legend.exists()).toBe(true)
    expect(legend.text()).toContain('Frequency')
    
    const legendScale = wrapper.find('.legend-scale')
    expect(legendScale.exists()).toBe(true)
  })

  it('shows hot-cold legend categories for hot-cold color scheme', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData,
        colorScheme: 'hot-cold'
      }
    })
    
    const legendCategories = wrapper.find('.legend-categories')
    expect(legendCategories.exists()).toBe(true)
    expect(legendCategories.text()).toContain('Hot')
    expect(legendCategories.text()).toContain('Normal')
    expect(legendCategories.text()).toContain('Cold')
  })

  it('emits update events for color scheme changes', async () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    const colorSchemeSelect = wrapper.find('#color-scheme')
    await colorSchemeSelect.setValue('gradient')
    
    expect(wrapper.emitted('update:colorScheme')).toBeTruthy()
    expect(wrapper.emitted('update:colorScheme')![0]).toEqual(['gradient'])
  })

  it('emits update events for show labels changes', async () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    const showLabelsCheckbox = wrapper.find('input[type="checkbox"]')
    await showLabelsCheckbox.setChecked(false)
    
    expect(wrapper.emitted('update:showLabels')).toBeTruthy()
    expect(wrapper.emitted('update:showLabels')![0]).toEqual([false])
  })

  it('formats dates correctly', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    const formattedDate = wrapper.vm.formatDate('2023-12-01T00:00:00Z')
    expect(formattedDate).toMatch(/\d{1,2}\/\d{1,2}\/\d{4}/)
    
    const neverDate = wrapper.vm.formatDate(undefined)
    expect(neverDate).toBe('Never')
  })

  it('classifies frequencies correctly', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    const hotFrequency = mockFrequencyData[0]
    const coldFrequency = mockFrequencyData[1]
    const normalFrequency = mockFrequencyData[2]
    
    expect(wrapper.vm.getClassification(hotFrequency)).toBe('Hot')
    expect(wrapper.vm.getClassification(coldFrequency)).toBe('Cold')
    expect(wrapper.vm.getClassification(normalFrequency)).toBe('Normal')
    expect(wrapper.vm.getClassification(null)).toBe('No Data')
  })

  it('applies correct CSS classes for classifications', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    const hotFrequency = mockFrequencyData[0]
    const coldFrequency = mockFrequencyData[1]
    const normalFrequency = mockFrequencyData[2]
    
    expect(wrapper.vm.getClassificationClass(hotFrequency)).toBe('hot')
    expect(wrapper.vm.getClassificationClass(coldFrequency)).toBe('cold')
    expect(wrapper.vm.getClassificationClass(normalFrequency)).toBe('normal')
    expect(wrapper.vm.getClassificationClass(null)).toBe('no-data')
  })

  it('handles numbers without frequency data', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData // Only has data for numbers 1, 2, 3
      }
    })
    
    // Number 40 should not have frequency data
    expect(wrapper.vm.getNumberFrequency(40)).toBe(0)
    expect(wrapper.vm.getFrequencyClass(40)).toBe('no-data')
  })

  it('counts hot and cold numbers correctly', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    expect(wrapper.vm.hotNumbers).toHaveLength(1)
    expect(wrapper.vm.coldNumbers).toHaveLength(1)
    expect(wrapper.vm.hotNumbers[0].isHot).toBe(true)
    expect(wrapper.vm.coldNumbers[0].isCold).toBe(true)
  })

  it('generates correct legend gradient for different color schemes', async () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData,
        colorScheme: 'hot-cold'
      }
    })
    
    const hotColdGradient = wrapper.vm.getLegendGradient()
    expect(hotColdGradient).toContain('linear-gradient')
    expect(hotColdGradient).toContain('#007bff')
    expect(hotColdGradient).toContain('#dc3545')
    
    // Test gradient scheme
    wrapper.setProps({ colorScheme: 'gradient' })
    await wrapper.vm.$nextTick()
    
    const gradientGradient = wrapper.vm.getLegendGradient()
    expect(gradientGradient).toContain('linear-gradient')
    expect(gradientGradient).toContain('#0066ff')
  })

  it('handles window resize events', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData
      }
    })
    
    // Set tooltip visible
    wrapper.vm.tooltip.visible = true
    
    // Simulate window resize
    window.dispatchEvent(new Event('resize'))
    
    expect(wrapper.vm.tooltip.visible).toBe(false)
  })

  it('applies responsive grid columns correctly', () => {
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: mockFrequencyData,
        gridColumns: 10
      }
    })
    
    // The grid should still display all 40 numbers regardless of gridColumns prop
    const cells = wrapper.findAll('.heatmap-cell')
    expect(cells).toHaveLength(40)
  })

  it('handles equal min and max frequencies in gradient mode', () => {
    const equalFrequencyData: NumberFrequency[] = [
      { ...mockFrequencyData[0], totalOccurrences: 20 },
      { ...mockFrequencyData[1], totalOccurrences: 20 },
      { ...mockFrequencyData[2], totalOccurrences: 20 }
    ]
    
    const wrapper = mount(NumberHeatmap, {
      props: {
        frequencyData: equalFrequencyData,
        colorScheme: 'gradient'
      }
    })
    
    const color = wrapper.vm.getGradientColor(equalFrequencyData[0])
    expect(color).toBe('#28a745') // Should return default color
  })
})