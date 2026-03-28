import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import PresetRanges from '../PresetRanges.vue'

describe('PresetRanges', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders correctly with default props', () => {
    const wrapper = mount(PresetRanges)

    expect(wrapper.find('.preset-ranges').exists()).toBe(true)
    expect(wrapper.find('.ranges-title').text()).toBe('Quick Range Selection')
    expect(wrapper.find('.ranges-grid').exists()).toBe(true)
    expect(wrapper.findAll('.range-button').length).toBeGreaterThan(0)
  })

  it('displays custom title and description', () => {
    const wrapper = mount(PresetRanges, {
      props: {
        title: 'Custom Title',
        description: 'Custom description text'
      }
    })

    expect(wrapper.find('.ranges-title').text()).toBe('Custom Title')
    expect(wrapper.find('.ranges-description').text()).toBe('Custom description text')
  })

  it('hides header when showHeader is false', () => {
    const wrapper = mount(PresetRanges, {
      props: {
        showHeader: false
      }
    })

    expect(wrapper.find('.preset-ranges-header').exists()).toBe(false)
  })

  it('applies correct grid class based on gridColumns prop', () => {
    const wrapper = mount(PresetRanges, {
      props: {
        gridColumns: 4
      }
    })

    expect(wrapper.find('.ranges-grid').classes()).toContain('grid-cols-4')
  })

  it('selects range in single select mode', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        allowMultiSelect: false
      }
    })

    const firstRange = wrapper.find('.range-button')
    await firstRange.trigger('click')

    expect(wrapper.emitted('select')).toBeTruthy()
    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(firstRange.classes()).toContain('range-selected')
  })

  it('selects multiple ranges in multi select mode', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        allowMultiSelect: true,
        maxSelections: 3
      }
    })

    const ranges = wrapper.findAll('.range-button')
    
    // Select first range
    await ranges[0].trigger('click')
    expect(wrapper.emitted('select')).toBeTruthy()
    
    // Select second range
    await ranges[1].trigger('click')
    expect(wrapper.emitted('select')).toHaveLength(2)
    
    // Both should be selected
    expect(ranges[0].classes()).toContain('range-selected')
    expect(ranges[1].classes()).toContain('range-selected')
  })

  it('deselects range when clicked again in multi select mode', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        allowMultiSelect: true
      }
    })

    const firstRange = wrapper.find('.range-button')
    
    // Select
    await firstRange.trigger('click')
    expect(firstRange.classes()).toContain('range-selected')
    
    // Deselect
    await firstRange.trigger('click')
    expect(firstRange.classes()).not.toContain('range-selected')
    expect(wrapper.emitted('deselect')).toBeTruthy()
  })

  it('respects maxSelections limit', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        allowMultiSelect: true,
        maxSelections: 2
      }
    })

    const ranges = wrapper.findAll('.range-button')
    
    // Select first two ranges
    await ranges[0].trigger('click')
    await ranges[1].trigger('click')
    
    // Third range should be disabled
    expect(ranges[2].classes()).toContain('range-disabled')
    
    // Clicking disabled range should not select it
    await ranges[2].trigger('click')
    expect(ranges[2].classes()).not.toContain('range-selected')
  })

  it('shows statistics when showStats is true', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        showStats: true
      }
    })

    // Mock some stats data
    wrapper.vm.presetRanges[0].stats = {
      frequency: 25,
      lastAppearance: '2023-12-01'
    }
    
    await wrapper.vm.$nextTick()

    const firstRange = wrapper.find('.range-button')
    expect(firstRange.find('.range-stats').exists()).toBe(true)
  })

  it('shows preview information when showPreview is true', () => {
    const wrapper = mount(PresetRanges, {
      props: {
        showPreview: true
      }
    })

    const ranges = wrapper.findAll('.range-button')
    const rangeWithPreview = ranges.find(range => 
      range.find('.range-preview').exists()
    )
    
    expect(rangeWithPreview).toBeTruthy()
  })

  it('shows icons when showIcons is true', () => {
    const wrapper = mount(PresetRanges, {
      props: {
        showIcons: true
      }
    })

    const firstRange = wrapper.find('.range-button')
    expect(firstRange.find('.range-icon').exists()).toBe(true)
  })

  it('shows custom range section when showCustomRange is true', () => {
    const wrapper = mount(PresetRanges, {
      props: {
        showCustomRange: true
      }
    })

    expect(wrapper.find('.custom-range-section').exists()).toBe(true)
    expect(wrapper.find('#custom-start').exists()).toBe(true)
    expect(wrapper.find('#custom-end').exists()).toBe(true)
    expect(wrapper.find('.add-custom-button').exists()).toBe(true)
  })

  it('validates custom range input correctly', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        showCustomRange: true
      }
    })

    // Set invalid range (start > end)
    const startInput = wrapper.find('#custom-start')
    const endInput = wrapper.find('#custom-end')
    
    await startInput.setValue(30)
    await endInput.setValue(20)
    
    wrapper.vm.validateCustomRange()
    
    expect(wrapper.vm.customRangeError).toBe('Start number must be less than or equal to end number')
    expect(wrapper.find('.add-custom-button').attributes('disabled')).toBeDefined()
  })

  it('adds custom range when valid', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        showCustomRange: true
      }
    })

    const startInput = wrapper.find('#custom-start')
    const endInput = wrapper.find('#custom-end')
    const addButton = wrapper.find('.add-custom-button')
    
    await startInput.setValue(5)
    await endInput.setValue(15)
    
    wrapper.vm.validateCustomRange()
    expect(wrapper.vm.isCustomRangeValid).toBe(true)
    
    await addButton.trigger('click')
    
    expect(wrapper.emitted('custom-add')).toBeTruthy()
    expect(wrapper.vm.customStart).toBeUndefined()
    expect(wrapper.vm.customEnd).toBeUndefined()
  })

  it('shows quick actions when showQuickActions is true', () => {
    const wrapper = mount(PresetRanges, {
      props: {
        showQuickActions: true,
        allowMultiSelect: true
      }
    })

    expect(wrapper.find('.quick-actions').exists()).toBe(true)
    expect(wrapper.find('.popular-button').exists()).toBe(true)
  })

  it('clears selection when clear button is clicked', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        allowMultiSelect: true,
        showQuickActions: true
      }
    })

    // Select a range first
    const firstRange = wrapper.find('.range-button')
    await firstRange.trigger('click')
    
    expect(wrapper.vm.selectedRanges.length).toBe(1)
    
    // Clear selection
    const clearButton = wrapper.find('.clear-button')
    await clearButton.trigger('click')
    
    expect(wrapper.vm.selectedRanges.length).toBe(0)
    expect(wrapper.emitted('clear')).toBeTruthy()
  })

  it('combines ranges when combine button is clicked', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        allowMultiSelect: true,
        showQuickActions: true
      }
    })

    // Select multiple ranges
    const ranges = wrapper.findAll('.range-button')
    await ranges[0].trigger('click') // 1-10
    await ranges[1].trigger('click') // 11-20
    
    expect(wrapper.vm.selectedRanges.length).toBe(2)
    
    // Combine ranges
    const combineButton = wrapper.find('.combine-button')
    await combineButton.trigger('click')
    
    expect(wrapper.emitted('combine')).toBeTruthy()
    expect(wrapper.vm.selectedRanges.length).toBe(1)
    expect(wrapper.vm.selectedRanges[0].range).toBe('1-20')
  })

  it('selects popular ranges when popular button is clicked', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        allowMultiSelect: true,
        showQuickActions: true
      }
    })

    const popularButton = wrapper.find('.popular-button')
    await popularButton.trigger('click')
    
    // Should select ranges marked as popular
    const popularRanges = wrapper.vm.selectedRanges.filter(r => r.isPopular)
    expect(popularRanges.length).toBeGreaterThan(0)
  })

  it('filters ranges by category when filterCategory is provided', () => {
    const wrapper = mount(PresetRanges, {
      props: {
        filterCategory: 'basic'
      }
    })

    const availableRanges = wrapper.vm.availableRanges
    expect(availableRanges.every(r => r.category === 'basic')).toBe(true)
  })

  it('applies category-specific CSS classes', () => {
    const wrapper = mount(PresetRanges)

    const ranges = wrapper.findAll('.range-button')
    const popularRange = ranges.find(range => 
      range.classes().some(cls => cls.includes('range-category-'))
    )
    
    expect(popularRange).toBeTruthy()
  })

  it('formats dates correctly in stats', () => {
    const wrapper = mount(PresetRanges, {
      props: {
        showStats: true
      }
    })

    const formattedDate = wrapper.vm.formatDate('2023-12-01T00:00:00Z')
    expect(formattedDate).toMatch(/\d{1,2}\s\w{3}/)
  })

  it('returns correct type icons', () => {
    const wrapper = mount(PresetRanges)

    expect(wrapper.vm.getRangeIcon({ category: 'custom' })).toBe('⚙️')
    expect(wrapper.vm.getRangeIcon({ isPopular: true })).toBe('⭐')
    expect(wrapper.vm.getRangeIcon({ isRecommended: true })).toBe('👍')
    expect(wrapper.vm.getRangeIcon({ category: 'basic' })).toBe('📊')
  })

  it('loads range statistics when loadStatsOnMount is true', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        loadStatsOnMount: true
      }
    })

    // Verify the component has the loadRangeStats method
    expect(wrapper.vm.loadRangeStats).toBeDefined()
    expect(typeof wrapper.vm.loadRangeStats).toBe('function')
  })

  it('exposes correct methods', () => {
    const wrapper = mount(PresetRanges)

    expect(wrapper.vm.selectRange).toBeDefined()
    expect(wrapper.vm.clearSelection).toBeDefined()
    expect(wrapper.vm.loadRangeStats).toBeDefined()
    expect(wrapper.vm.addCustomRange).toBeDefined()
  })

  it('handles model value updates correctly', async () => {
    const initialRanges = [
      {
        id: 'test',
        label: 'Test Range',
        range: '1-5',
        startNumber: 1,
        endNumber: 5,
        description: 'Test'
      }
    ]

    const wrapper = mount(PresetRanges, {
      props: {
        modelValue: []
      }
    })

    await wrapper.setProps({ modelValue: initialRanges })
    expect(wrapper.vm.selectedRanges).toEqual(initialRanges)
  })

  it('prevents selection when range is disabled', async () => {
    const wrapper = mount(PresetRanges, {
      props: {
        allowMultiSelect: true,
        maxSelections: 1
      }
    })

    const ranges = wrapper.findAll('.range-button')
    
    // Select first range (should reach limit)
    await ranges[0].trigger('click')
    
    // Second range should be disabled
    expect(ranges[1].classes()).toContain('range-disabled')
    
    // Clicking disabled range should not work
    const initialSelectionCount = wrapper.vm.selectedRanges.length
    await ranges[1].trigger('click')
    expect(wrapper.vm.selectedRanges.length).toBe(initialSelectionCount)
  })
})