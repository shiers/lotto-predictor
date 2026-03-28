import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import DrawNavigation from '../DrawNavigation.vue'
import navigationService from '@/services/navigationService'

// Mock the navigation service
vi.mock('@/services/navigationService', () => ({
  default: {
    getDrawByNumber: vi.fn(),
    getNavigationContext: vi.fn(),
    getPreviousDraw: vi.fn(),
    getNextDraw: vi.fn(),
    jumpToDraw: vi.fn(),
    createBookmark: vi.fn()
  }
}))

// Mock child components
vi.mock('../NavigationContext.vue', () => ({
  default: {
    name: 'NavigationContext',
    template: '<div class="navigation-context-mock">NavigationContext</div>',
    props: ['context'],
    emits: ['jump-to-draw']
  }
}))

vi.mock('../JumpToDrawDialog.vue', () => ({
  default: {
    name: 'JumpToDrawDialog',
    template: '<div class="jump-dialog-mock">JumpToDrawDialog</div>',
    emits: ['jump', 'close']
  }
}))

describe('DrawNavigation', () => {
  const mockDraw = {
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
  }

  const mockContext = {
    currentDraw: mockDraw,
    currentPosition: 500,
    totalDraws: 1000,
    hasPrevious: true,
    hasNext: true,
    earliestDate: '2020-01-01T00:00:00Z',
    latestDate: '2023-12-31T00:00:00Z',
    missingDrawNumbers: [1230, 1231]
  }

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    
    // Set default mock return values
    vi.mocked(navigationService.getDrawByNumber).mockResolvedValue(mockDraw)
    vi.mocked(navigationService.getNavigationContext).mockResolvedValue(mockContext)
    vi.mocked(navigationService.getPreviousDraw).mockResolvedValue({
      ...mockDraw,
      draw: 1233
    })
    vi.mocked(navigationService.getNextDraw).mockResolvedValue({
      ...mockDraw,
      draw: 1235
    })
  })

  it('renders correctly with default props', () => {
    const wrapper = mount(DrawNavigation)
    
    expect(wrapper.find('.draw-navigation').exists()).toBe(true)
    expect(wrapper.find('.navigation-header h3').text()).toBe('Draw Navigation')
    expect(wrapper.find('.jump-button').exists()).toBe(true)
    expect(wrapper.find('.bookmark-button').exists()).toBe(true)
    expect(wrapper.find('.navigation-controls').exists()).toBe(true)
  })

  it('loads initial draw when initialDrawNumber is provided', async () => {
    const wrapper = mount(DrawNavigation, {
      props: {
        initialDrawNumber: 1234
      }
    })

    // Wait for the component to mount and load data
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    expect(navigationService.getDrawByNumber).toHaveBeenCalledWith(1234)
    expect(navigationService.getNavigationContext).toHaveBeenCalledWith(1234)
  })

  it('displays current draw information correctly', async () => {
    const wrapper = mount(DrawNavigation, {
      props: {
        initialDrawNumber: 1234
      }
    })

    // Set the component state directly for testing
    wrapper.vm.currentDraw = mockDraw
    wrapper.vm.navigationContext = mockContext
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.draw-number .value').text()).toBe('1234')
    expect(wrapper.find('.main-numbers').exists()).toBe(true)
    expect(wrapper.findAll('.number-ball.main')).toHaveLength(6)
    expect(wrapper.find('.number-ball.bonus').text()).toBe('7')
    expect(wrapper.find('.number-ball.powerball').text()).toBe('8')
  })

  it('shows position information correctly', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.navigationContext = mockContext
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.position').text()).toBe('500 of 1000')
  })

  it('enables/disables navigation buttons based on context', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.navigationContext = mockContext
    await wrapper.vm.$nextTick()

    const previousButton = wrapper.find('.nav-button.previous')
    const nextButton = wrapper.find('.nav-button.next')

    expect(previousButton.attributes('disabled')).toBeUndefined()
    expect(nextButton.attributes('disabled')).toBeUndefined()
  })

  it('disables previous button when at first draw', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.navigationContext = {
      ...mockContext,
      hasPrevious: false
    }
    await wrapper.vm.$nextTick()

    const previousButton = wrapper.find('.nav-button.previous')
    expect(previousButton.attributes('disabled')).toBeDefined()
  })

  it('disables next button when at last draw', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.navigationContext = {
      ...mockContext,
      hasNext: false
    }
    await wrapper.vm.$nextTick()

    const nextButton = wrapper.find('.nav-button.next')
    expect(nextButton.attributes('disabled')).toBeDefined()
  })

  it('navigates to previous draw when previous button is clicked', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.currentDraw = mockDraw
    wrapper.vm.navigationContext = mockContext
    await wrapper.vm.$nextTick()

    const previousButton = wrapper.find('.nav-button.previous')
    await previousButton.trigger('click')

    expect(navigationService.getPreviousDraw).toHaveBeenCalledWith(1234)
  })

  it('navigates to next draw when next button is clicked', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.currentDraw = mockDraw
    wrapper.vm.navigationContext = mockContext
    await wrapper.vm.$nextTick()

    const nextButton = wrapper.find('.nav-button.next')
    await nextButton.trigger('click')

    expect(navigationService.getNextDraw).toHaveBeenCalledWith(1234)
  })

  it('shows jump dialog when jump button is clicked', async () => {
    const wrapper = mount(DrawNavigation)
    
    const jumpButton = wrapper.find('.jump-button')
    await jumpButton.trigger('click')

    expect(wrapper.vm.showJumpDialog).toBe(true)
  })

  it('shows bookmark dialog when bookmark button is clicked', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.currentDraw = mockDraw
    await wrapper.vm.$nextTick()

    const bookmarkButton = wrapper.find('.bookmark-button')
    await bookmarkButton.trigger('click')

    expect(wrapper.vm.showBookmarkDialog).toBe(true)
  })

  it('disables bookmark button when no current draw', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.currentDraw = null
    await wrapper.vm.$nextTick()

    const bookmarkButton = wrapper.find('.bookmark-button')
    expect(bookmarkButton.attributes('disabled')).toBeDefined()
  })

  it('handles jump to draw correctly', async () => {
    const wrapper = mount(DrawNavigation)
    
    const jumpRequest = { drawNumber: 1500 }
    await wrapper.vm.handleJumpToDraw(jumpRequest)

    expect(navigationService.jumpToDraw).toHaveBeenCalledWith({
      ...jumpRequest,
      findClosest: true
    })
  })

  it('creates bookmark with correct data', async () => {
    const mockBookmark = {
      id: 1,
      userId: 'anonymous',
      drawNumber: 1234,
      label: 'Test Bookmark',
      description: 'Test Description',
      createdAt: '2023-01-15T00:00:00Z'
    }

    vi.mocked(navigationService.createBookmark).mockResolvedValue(mockBookmark)

    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.currentDraw = mockDraw
    wrapper.vm.bookmarkLabel = 'Test Bookmark'
    wrapper.vm.bookmarkDescription = 'Test Description'
    await wrapper.vm.$nextTick()

    await wrapper.vm.createBookmark()

    expect(navigationService.createBookmark).toHaveBeenCalledWith({
      drawNumber: 1234,
      label: 'Test Bookmark',
      description: 'Test Description'
    })
  })

  it('emits drawChanged event when draw changes', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.currentDraw = mockDraw
    wrapper.vm.navigationContext = mockContext
    
    // Simulate navigation to next draw
    await wrapper.vm.navigateNext()
    
    // Check if the event was emitted
    expect(wrapper.emitted('drawChanged')).toBeTruthy()
  })

  it('emits navigationError event on error', async () => {
    vi.mocked(navigationService.getDrawByNumber).mockRejectedValue(new Error('API Error'))

    const wrapper = mount(DrawNavigation, {
      props: {
        initialDrawNumber: 1234
      }
    })

    // Wait for the error to be handled
    await new Promise(resolve => setTimeout(resolve, 0))

    expect(wrapper.emitted('navigationError')).toBeTruthy()
  })

  it('shows loading state during navigation', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.isLoading = true
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.loading-state').exists()).toBe(true)
    expect(wrapper.find('.loading-spinner').exists()).toBe(true)
  })

  it('shows error state when error occurs', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.error = 'Test error message'
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.error-state').exists()).toBe(true)
    expect(wrapper.find('.error-state p').text()).toBe('Test error message')
  })

  it('clears error when clearError is called', async () => {
    const wrapper = mount(DrawNavigation)
    
    wrapper.vm.error = 'Test error'
    await wrapper.vm.clearError()

    expect(wrapper.vm.error).toBeNull()
  })

  it('formats date correctly', () => {
    const wrapper = mount(DrawNavigation)
    
    const formattedDate = wrapper.vm.formatDate('2023-01-15T00:00:00Z')
    
    // The exact format may vary based on locale, but it should be a readable date
    expect(formattedDate).toMatch(/\w{3}.*\d{1,2}.*\d{4}/)
  })

  it('handles keyboard shortcuts when enabled', async () => {
    const wrapper = mount(DrawNavigation, {
      props: {
        enableKeyboardShortcuts: true
      }
    })

    wrapper.vm.currentDraw = mockDraw
    wrapper.vm.navigationContext = mockContext
    await wrapper.vm.$nextTick()

    // Simulate arrow left key press
    const event = new KeyboardEvent('keydown', { key: 'ArrowLeft' })
    document.dispatchEvent(event)

    // The navigation should be triggered
    expect(navigationService.getPreviousDraw).toHaveBeenCalled()
  })

  it('does not handle keyboard shortcuts when disabled', async () => {
    vi.clearAllMocks() // Clear any previous calls
    
    const wrapper = mount(DrawNavigation, {
      props: {
        enableKeyboardShortcuts: false
      }
    })

    wrapper.vm.currentDraw = mockDraw
    wrapper.vm.navigationContext = mockContext
    await wrapper.vm.$nextTick()

    // Simulate arrow left key press
    const event = new KeyboardEvent('keydown', { key: 'ArrowLeft' })
    document.dispatchEvent(event)

    // Wait a bit for any async operations
    await new Promise(resolve => setTimeout(resolve, 10))

    // The navigation should not be triggered after the event
    expect(navigationService.getPreviousDraw).not.toHaveBeenCalledWith(1234)
  })

  it('shows NavigationContext when showContext is true', async () => {
    const wrapper = mount(DrawNavigation, {
      props: {
        showContext: true
      }
    })

    wrapper.vm.navigationContext = mockContext
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.navigation-context-mock').exists()).toBe(true)
  })

  it('hides NavigationContext when showContext is false', async () => {
    const wrapper = mount(DrawNavigation, {
      props: {
        showContext: false
      }
    })

    wrapper.vm.navigationContext = mockContext
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.navigation-context-mock').exists()).toBe(false)
  })

  it('exposes methods for parent components', () => {
    const wrapper = mount(DrawNavigation)
    
    expect(typeof wrapper.vm.loadDraw).toBe('function')
    expect(typeof wrapper.vm.navigatePrevious).toBe('function')
    expect(typeof wrapper.vm.navigateNext).toBe('function')
    expect(typeof wrapper.vm.jumpToDraw).toBe('function')
    expect(typeof wrapper.vm.getCurrentDraw).toBe('function')
    expect(typeof wrapper.vm.getNavigationContext).toBe('function')
  })
})