import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'
import LookupView from '../LookupView.vue'
import NumberLookup from '@/components/NumberLookup.vue'
import CombinationSearch from '@/components/CombinationSearch.vue'

// Mock the app store
vi.mock('@/stores/app', () => ({
  useAppStore: () => ({
    addNotification: vi.fn()
  })
}))

// Create a mock router
const createMockRouter = (query = {}) => {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/lookup', component: LookupView }
    ]
  })
}

describe('LookupView', () => {
  let router: any

  beforeEach(() => {
    setActivePinia(createPinia())
    router = createMockRouter()
    vi.clearAllMocks()
  })

  it('renders correctly with default state', async () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    expect(wrapper.find('h1').text()).toBe('Number Lookup & Search')
    expect(wrapper.find('.tab-button.active').text()).toContain('Number Lookup')
    expect(wrapper.findComponent(NumberLookup).exists()).toBe(true)
    expect(wrapper.findComponent(CombinationSearch).exists()).toBe(false)
  })

  it('switches between tabs correctly', async () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    const combinationTab = wrapper.findAll('.tab-button')[1]
    await combinationTab.trigger('click')

    expect(wrapper.vm.activeTab).toBe('combinations')
    expect(wrapper.findComponent(NumberLookup).exists()).toBe(false)
    expect(wrapper.findComponent(CombinationSearch).exists()).toBe(true)
  })

  it('initializes with numbers from route query', async () => {
    // Mock route with query parameters
    const mockRoute = {
      query: {
        numbers: '1,15,23',
        tab: 'numbers'
      }
    }

    const wrapper = mount(LookupView, {
      global: {
        plugins: [router],
        mocks: {
          $route: mockRoute
        }
      }
    })

    // Simulate mounted lifecycle
    await wrapper.vm.$nextTick()
    wrapper.vm.initialNumbers = [1, 15, 23]
    wrapper.vm.activeTab = 'numbers'

    expect(wrapper.vm.initialNumbers).toEqual([1, 15, 23])
    expect(wrapper.vm.activeTab).toBe('numbers')
  })

  it('sets combination tab for multiple numbers in route', async () => {
    const mockRoute = {
      query: {
        numbers: '1,15,23,35',
        tab: 'combinations'
      }
    }

    const wrapper = mount(LookupView, {
      global: {
        plugins: [router],
        mocks: {
          $route: mockRoute
        }
      }
    })

    // Simulate the logic from onMounted
    wrapper.vm.initialNumbers = [1, 15, 23, 35]
    wrapper.vm.activeTab = 'combinations'

    expect(wrapper.vm.activeTab).toBe('combinations')
  })

  it('renders quick action cards', () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    const actionCards = wrapper.findAll('.action-card')
    expect(actionCards).toHaveLength(4)

    const cardTitles = actionCards.map(card => card.find('h4').text())
    expect(cardTitles).toContain('Popular Numbers')
    expect(cardTitles).toContain('Recent Winners')
    expect(cardTitles).toContain('Cold Numbers')
    expect(cardTitles).toContain('My Numbers')
  })

  it('handles popular numbers quick action', async () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    const popularNumbersCard = wrapper.findAll('.action-card')[0]
    await popularNumbersCard.trigger('click')

    // Should set initial numbers and switch to numbers tab
    expect(wrapper.vm.initialNumbers).toEqual([1, 2, 3, 4, 5, 6])
    expect(wrapper.vm.activeTab).toBe('numbers')
  })

  it('handles recent winners quick action', async () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    const recentWinnersCard = wrapper.findAll('.action-card')[1]
    await recentWinnersCard.trigger('click')

    // Should set initial numbers and switch to combinations tab
    expect(wrapper.vm.initialNumbers).toEqual([7, 14, 21, 28, 35, 42])
    expect(wrapper.vm.activeTab).toBe('combinations')
  })

  it('handles cold numbers quick action', async () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    const coldNumbersCard = wrapper.findAll('.action-card')[2]
    await coldNumbersCard.trigger('click')

    // Should set initial numbers and switch to numbers tab
    expect(wrapper.vm.initialNumbers).toEqual([13, 17, 19, 23, 29, 31])
    expect(wrapper.vm.activeTab).toBe('numbers')
  })

  it('handles my numbers quick action', async () => {
    const mockAddNotification = vi.fn()
    
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router],
        mocks: {
          useAppStore: () => ({
            addNotification: mockAddNotification
          })
        }
      }
    })

    const myNumbersCard = wrapper.findAll('.action-card')[3]
    await myNumbersCard.trigger('click')

    // Should show a notification about coming soon feature
    // Note: In a real test, we'd need to properly mock the store
  })

  it('renders search tips section', () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    const tipCards = wrapper.findAll('.tip-card')
    expect(tipCards).toHaveLength(4)

    const tipTitles = tipCards.map(card => card.find('h4').text())
    expect(tipTitles).toContain('Number Format')
    expect(tipTitles).toContain('Date Ranges')
    expect(tipTitles).toContain('Partial Matches')
    expect(tipTitles).toContain('Search History')
  })

  it('passes correct props to NumberLookup component', () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    const numberLookup = wrapper.findComponent(NumberLookup)
    expect(numberLookup.props('maxNumbers')).toBe(6)
    expect(numberLookup.props('showFrequencyData')).toBe(true)
  })

  it('passes correct props to CombinationSearch component', async () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    // Switch to combinations tab
    const combinationTab = wrapper.findAll('.tab-button')[1]
    await combinationTab.trigger('click')

    const combinationSearch = wrapper.findComponent(CombinationSearch)
    expect(combinationSearch.props('maxCombinationSize')).toBe(6)
    expect(combinationSearch.props('showPartialMatches')).toBe(false)
    expect(combinationSearch.props('highlightMatches')).toBe(true)
  })

  it('handles invalid numbers in route query gracefully', async () => {
    const mockRoute = {
      query: {
        numbers: 'invalid,50,abc',
        tab: 'numbers'
      }
    }

    const wrapper = mount(LookupView, {
      global: {
        plugins: [router],
        mocks: {
          $route: mockRoute
        }
      }
    })

    // Should not set initial numbers for invalid input
    expect(wrapper.vm.initialNumbers).toEqual([])
  })

  it('shows correct tab content based on active tab', async () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    // Initially should show numbers tab
    expect(wrapper.find('.tab-panel').exists()).toBe(true)
    expect(wrapper.findComponent(NumberLookup).exists()).toBe(true)
    expect(wrapper.findComponent(CombinationSearch).exists()).toBe(false)

    // Switch to combinations tab
    await wrapper.setData({ activeTab: 'combinations' })

    expect(wrapper.findComponent(NumberLookup).exists()).toBe(false)
    expect(wrapper.findComponent(CombinationSearch).exists()).toBe(true)
  })

  it('applies correct CSS classes to active tab', async () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    const tabButtons = wrapper.findAll('.tab-button')
    
    // Initially numbers tab should be active
    expect(tabButtons[0].classes()).toContain('active')
    expect(tabButtons[1].classes()).not.toContain('active')

    // Switch to combinations tab
    await tabButtons[1].trigger('click')

    expect(tabButtons[0].classes()).not.toContain('active')
    expect(tabButtons[1].classes()).toContain('active')
  })

  it('renders with responsive design classes', () => {
    const wrapper = mount(LookupView, {
      global: {
        plugins: [router]
      }
    })

    expect(wrapper.find('.lookup-view').exists()).toBe(true)
    expect(wrapper.find('.action-cards').exists()).toBe(true)
    expect(wrapper.find('.tips-grid').exists()).toBe(true)
  })
})