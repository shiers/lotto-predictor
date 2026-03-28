import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import SearchCriteriaBuilder from '../SearchCriteriaBuilder.vue'
import { AdvancedSearchService } from '@/services/advancedSearchService'
import type { SearchCriteria } from '@/types/advancedSearch'

// Mock the advanced search service
vi.mock('@/services/advancedSearchService', () => ({
  AdvancedSearchService: {
    validateSearchCriteria: vi.fn()
  }
}))

describe('SearchCriteriaBuilder', () => {
  let wrapper: any
  let pinia: any

  const mockCriteria: SearchCriteria = {
    conditions: [],
    logic: 'AND',
    dateRange: {
      startDate: null,
      endDate: null
    },
    frequencyFilters: {
      minOccurrences: null,
      maxOccurrences: null
    },
    includeBonus: true,
    includePowerball: true
  }

  beforeEach(() => {
    pinia = createPinia()
    setActivePinia(pinia)
    vi.clearAllMocks()
    vi.mocked(AdvancedSearchService.validateSearchCriteria).mockReturnValue([])
  })

  it('renders correctly with initial state', () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    expect(wrapper.find('h3').text()).toBe('Search Criteria')
    expect(wrapper.find('.logic-selector select').element.value).toBe('AND')
    expect(wrapper.find('.btn-primary').text()).toContain('Add Condition')
  })

  it('shows empty state when no conditions exist', () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    expect(wrapper.find('.empty-state').exists()).toBe(true)
    expect(wrapper.find('.empty-state').text()).toContain('No search conditions defined')
  })

  it('adds a new condition when Add Condition is clicked', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    expect(wrapper.vm.criteria.conditions).toHaveLength(1)
    expect(wrapper.vm.criteria.conditions[0].type).toBe('number')
    expect(wrapper.find('.condition-item').exists()).toBe(true)
  })

  it('removes a condition when Remove button is clicked', async () => {
    const criteriaWithCondition = {
      ...mockCriteria,
      conditions: [
        { id: 'test-1', type: 'number', value: { number: 15, position: '' } },
        { id: 'test-2', type: 'number', value: { number: 20, position: '' } }
      ]
    }

    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: criteriaWithCondition },
      global: {
        plugins: [pinia]
      }
    })

    expect(wrapper.vm.criteria.conditions).toHaveLength(2)

    await wrapper.findAll('.btn-danger')[0].trigger('click')

    expect(wrapper.vm.criteria.conditions).toHaveLength(1)
    expect(wrapper.vm.criteria.conditions[0].id).toBe('test-2')
  })

  it('disables remove button when only one condition exists', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    const removeButton = wrapper.find('.btn-danger')
    expect(removeButton.attributes('disabled')).toBeDefined()
  })

  it('changes condition type and resets value', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    const typeSelect = wrapper.find('.condition-header select')
    await typeSelect.setValue('combination')

    expect(wrapper.vm.criteria.conditions[0].type).toBe('combination')
    expect(wrapper.vm.criteria.conditions[0].value.numbersInput).toBe('')
    expect(wrapper.vm.criteria.conditions[0].value.numbers).toEqual([])
  })

  it('renders number condition form correctly', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    expect(wrapper.find('input[type="number"]').exists()).toBe(true)
    expect(wrapper.find('select').exists()).toBe(true)
    expect(wrapper.find('label').text()).toContain('Number (1-40)')
  })

  it('renders combination condition form correctly', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    const typeSelect = wrapper.find('.condition-header select')
    await typeSelect.setValue('combination')

    expect(wrapper.find('input[type="text"]').exists()).toBe(true)
    expect(wrapper.find('input[type="text"]').attributes('placeholder')).toContain('e.g., 1, 15, 23, 34')
    expect(wrapper.find('select[value="exact"]').exists()).toBe(true)
  })

  it('parses combination numbers correctly', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    const typeSelect = wrapper.find('.condition-header select')
    await typeSelect.setValue('combination')

    const condition = wrapper.vm.criteria.conditions[0]
    condition.value.numbersInput = '1, 15, 23, 34'
    
    await wrapper.vm.updateCombinationNumbers(condition)

    expect(condition.value.numbers).toEqual([1, 15, 23, 34])
    expect(wrapper.find('.numbers-preview').text()).toContain('Numbers: 1, 15, 23, 34')
  })

  it('removes duplicate numbers from combination', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    const typeSelect = wrapper.find('.condition-header select')
    await typeSelect.setValue('combination')

    const condition = wrapper.vm.criteria.conditions[0]
    condition.value.numbersInput = '1, 15, 15, 23, 1'
    
    await wrapper.vm.updateCombinationNumbers(condition)

    expect(condition.value.numbers).toEqual([1, 15, 23])
  })

  it('renders range condition form correctly', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    const typeSelect = wrapper.find('.condition-header select')
    await typeSelect.setValue('range')

    const numberInputs = wrapper.findAll('input[type="number"]')
    expect(numberInputs).toHaveLength(2)
    expect(wrapper.find('input[type="checkbox"]').exists()).toBe(true)
  })

  it('renders frequency condition form correctly', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    const typeSelect = wrapper.find('.condition-header select')
    await typeSelect.setValue('frequency')

    expect(wrapper.find('select').exists()).toBe(true)
    expect(wrapper.find('input[type="number"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Timeframe')
  })

  it('shows second value input for between frequency operator', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    const typeSelect = wrapper.find('.condition-header select')
    await typeSelect.setValue('frequency')

    const condition = wrapper.vm.criteria.conditions[0]
    condition.value.operator = 'between'
    await wrapper.vm.$nextTick()

    const numberInputs = wrapper.findAll('input[type="number"]')
    expect(numberInputs.length).toBeGreaterThan(1)
  })

  it('renders date condition form correctly', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    const typeSelect = wrapper.find('.condition-header select')
    await typeSelect.setValue('date')

    const dateInputs = wrapper.findAll('input[type="date"]')
    expect(dateInputs).toHaveLength(2)
  })

  it('renders gap condition form correctly', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    const typeSelect = wrapper.find('.condition-header select')
    await typeSelect.setValue('gap')

    expect(wrapper.text()).toContain('Gap Type')
    expect(wrapper.text()).toContain('Current gap')
    expect(wrapper.text()).toContain('Days')
  })

  it('updates logic selector correctly', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    const logicSelect = wrapper.find('.logic-selector select')
    await logicSelect.setValue('OR')

    expect(wrapper.vm.criteria.logic).toBe('OR')
  })

  it('renders global filters correctly', () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    expect(wrapper.find('.global-filters').exists()).toBe(true)
    expect(wrapper.text()).toContain('Date Range (applies to all conditions)')
    expect(wrapper.text()).toContain('Frequency Filters')
    expect(wrapper.text()).toContain('Number Types')
  })

  it('updates global date range', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    const dateInputs = wrapper.findAll('.global-filters input[type="date"]')
    await dateInputs[0].setValue('2023-01-01')
    await dateInputs[1].setValue('2023-12-31')

    expect(wrapper.vm.criteria.dateRange.startDate).toBe('2023-01-01')
    expect(wrapper.vm.criteria.dateRange.endDate).toBe('2023-12-31')
  })

  it('updates frequency filters', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    const frequencyInputs = wrapper.findAll('.global-filters input[type="number"]')
    await frequencyInputs[0].setValue('5')
    await frequencyInputs[1].setValue('50')

    expect(wrapper.vm.criteria.frequencyFilters.minOccurrences).toBe(5)
    expect(wrapper.vm.criteria.frequencyFilters.maxOccurrences).toBe(50)
  })

  it('updates number type checkboxes', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    const checkboxes = wrapper.findAll('.global-filters input[type="checkbox"]')
    await checkboxes[0].setChecked(false)
    await checkboxes[1].setChecked(false)

    expect(wrapper.vm.criteria.includeBonus).toBe(false)
    expect(wrapper.vm.criteria.includePowerball).toBe(false)
  })

  it('emits validation events', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.validateCriteria()

    expect(wrapper.emitted('validate')).toBeTruthy()
    expect(AdvancedSearchService.validateSearchCriteria).toHaveBeenCalledWith(wrapper.vm.criteria)
  })

  it('emits criteria updates', async () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.btn-primary').trigger('click')

    expect(wrapper.emitted('update:criteria')).toBeTruthy()
  })

  it('initializes with at least one condition on mount', () => {
    wrapper = mount(SearchCriteriaBuilder, {
      props: { criteria: mockCriteria },
      global: {
        plugins: [pinia]
      }
    })

    expect(wrapper.vm.criteria.conditions).toHaveLength(1)
  })
})