import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import NavigationContext from '../NavigationContext.vue'

describe('NavigationContext', () => {
  const mockContext = {
    currentDraw: {
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
    },
    currentPosition: 500,
    totalDraws: 1000,
    hasPrevious: true,
    hasNext: true,
    earliestDate: '2020-01-01T00:00:00Z',
    latestDate: '2023-12-31T00:00:00Z',
    missingDrawNumbers: [1230, 1231, 1232, 1233, 1234, 1235, 1236, 1237, 1238, 1239, 1240, 1241]
  }

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('renders correctly', () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    expect(wrapper.find('.navigation-context').exists()).toBe(true)
    expect(wrapper.find('.context-header h4').text()).toBe('Navigation Context')
    expect(wrapper.find('.toggle-button').exists()).toBe(true)
  })

  it('starts collapsed by default', () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    expect(wrapper.vm.isExpanded).toBe(false)
    expect(wrapper.find('.context-content').exists()).toBe(false)
    expect(wrapper.find('.toggle-button').text()).toContain('Expand')
  })

  it('expands when toggle button is clicked', async () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    const toggleButton = wrapper.find('.toggle-button')
    await toggleButton.trigger('click')
    
    expect(wrapper.vm.isExpanded).toBe(true)
    expect(wrapper.find('.context-content').exists()).toBe(true)
    expect(wrapper.find('.toggle-button').text()).toContain('Collapse')
  })

  it('displays position information correctly', async () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    expect(wrapper.find('.info-item .value').text()).toBe('500 of 1000')
  })

  it('calculates progress percentage correctly', () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    expect(wrapper.vm.progressPercentage).toBe(50) // 500/1000 * 100
  })

  it('displays progress bar with correct width', async () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    const progressFill = wrapper.find('.progress-fill')
    expect(progressFill.attributes('style')).toContain('width: 50%')
  })

  it('shows boundary status correctly when at boundaries', async () => {
    const contextAtFirst = {
      ...mockContext,
      hasPrevious: false,
      hasNext: true
    }
    
    const wrapper = mount(NavigationContext, {
      props: {
        context: contextAtFirst
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    const boundaryItems = wrapper.findAll('.boundary-item')
    expect(boundaryItems[0].classes()).toContain('active') // At first draw
    expect(boundaryItems[1].classes()).not.toContain('active') // Can go next
  })

  it('displays date range information correctly', async () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    const dateItems = wrapper.findAll('.date-item')
    expect(dateItems).toHaveLength(2)
    
    // Check that dates are formatted and displayed
    const dateValues = wrapper.findAll('.date-value')
    expect(dateValues[0].text()).toMatch(/\w{3}.*\d{1,2}.*\d{4}/) // Earliest date
    expect(dateValues[1].text()).toMatch(/\w{3}.*\d{1,2}.*\d{4}/) // Latest date
  })

  it('calculates total days span correctly', () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    // Should calculate days between 2020-01-01 and 2023-12-31
    expect(wrapper.vm.totalDaysSpan).toBeGreaterThan(1400) // Approximately 4 years
  })

  it('calculates average frequency correctly', () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    // Should calculate draws per week
    expect(wrapper.vm.averageFrequency).toMatch(/^\d+\.\d$/) // Should be a decimal number
  })

  it('displays missing draws when present', async () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    expect(wrapper.find('.missing-draws').exists()).toBe(true)
    expect(wrapper.find('.missing-description').exists()).toBe(true)
    
    const missingNumbers = wrapper.findAll('.missing-number')
    expect(missingNumbers).toHaveLength(10) // Default maxDisplayedMissing
  })

  it('shows "more missing" indicator when there are more than max displayed', async () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext,
        maxDisplayedMissing: 5
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    const missingNumbers = wrapper.findAll('.missing-number')
    expect(missingNumbers).toHaveLength(5)
    
    const moreMissing = wrapper.find('.more-missing')
    expect(moreMissing.exists()).toBe(true)
    expect(moreMissing.text()).toContain('+7 more') // 12 total - 5 displayed = 7 more
  })

  it('does not show missing draws section when no missing draws', async () => {
    const contextNoMissing = {
      ...mockContext,
      missingDrawNumbers: []
    }
    
    const wrapper = mount(NavigationContext, {
      props: {
        context: contextNoMissing
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    expect(wrapper.find('.missing-draws').exists()).toBe(false)
  })

  it('emits jumpToDraw event when missing draw is clicked', async () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    const firstMissingNumber = wrapper.find('.missing-number')
    await firstMissingNumber.trigger('click')
    
    expect(wrapper.emitted('jumpToDraw')).toBeTruthy()
    expect(wrapper.emitted('jumpToDraw')[0]).toEqual([1230]) // First missing draw number
  })

  it('displays quick navigation buttons', async () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    const quickNavButtons = wrapper.findAll('.quick-nav-button')
    expect(quickNavButtons).toHaveLength(3)
    expect(quickNavButtons[0].text()).toContain('First Draw')
    expect(quickNavButtons[1].text()).toContain('Random Draw')
    expect(quickNavButtons[2].text()).toContain('Last Draw')
  })

  it('disables first draw button when at first draw', async () => {
    const contextAtFirst = {
      ...mockContext,
      hasPrevious: false
    }
    
    const wrapper = mount(NavigationContext, {
      props: {
        context: contextAtFirst
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    const firstDrawButton = wrapper.findAll('.quick-nav-button')[0]
    expect(firstDrawButton.attributes('disabled')).toBeDefined()
  })

  it('disables last draw button when at last draw', async () => {
    const contextAtLast = {
      ...mockContext,
      hasNext: false
    }
    
    const wrapper = mount(NavigationContext, {
      props: {
        context: contextAtLast
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    const lastDrawButton = wrapper.findAll('.quick-nav-button')[2]
    expect(lastDrawButton.attributes('disabled')).toBeDefined()
  })

  it('emits jumpToDraw event when quick nav buttons are clicked', async () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    const randomDrawButton = wrapper.findAll('.quick-nav-button')[1]
    await randomDrawButton.trigger('click')
    
    expect(wrapper.emitted('jumpToDraw')).toBeTruthy()
    expect(typeof wrapper.emitted('jumpToDraw')[0][0]).toBe('number')
  })

  it('displays statistics correctly', async () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    // Expand to see content
    await wrapper.vm.toggleExpanded()
    
    const statCards = wrapper.findAll('.stat-card')
    expect(statCards).toHaveLength(3)
    
    const statNumbers = wrapper.findAll('.stat-number')
    expect(statNumbers[0].text()).toBe('1000') // Total draws
    expect(statNumbers[1].text()).toBe('500') // Remaining draws
    expect(statNumbers[2].text()).toBe('50%') // Completion percentage
  })

  it('calculates remaining draws correctly', () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    expect(wrapper.vm.remainingDraws).toBe(500) // 1000 - 500
  })

  it('calculates completion percentage correctly', () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    expect(wrapper.vm.completionPercentage).toBe(50) // Rounded progress percentage
  })

  it('formats dates correctly', () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    const formattedDate = wrapper.vm.formatDate('2023-01-15T00:00:00Z')
    
    // The exact format may vary based on locale, but it should be a readable date
    expect(formattedDate).toMatch(/\w{3}.*\d{1,2}.*\d{4}/)
  })

  it('generates random draw number within valid range', () => {
    const wrapper = mount(NavigationContext, {
      props: {
        context: mockContext
      }
    })
    
    const randomDraw = wrapper.vm.jumpToRandomDraw()
    
    // Should generate a number between 1 and totalDraws
    expect(wrapper.emitted('jumpToDraw')).toBeTruthy()
    const emittedDrawNumber = wrapper.emitted('jumpToDraw')[0][0]
    expect(emittedDrawNumber).toBeGreaterThanOrEqual(1)
    expect(emittedDrawNumber).toBeLessThanOrEqual(mockContext.totalDraws)
  })

  it('handles zero total draws gracefully', () => {
    const contextZeroDraws = {
      ...mockContext,
      totalDraws: 0,
      currentPosition: 0
    }
    
    const wrapper = mount(NavigationContext, {
      props: {
        context: contextZeroDraws
      }
    })
    
    expect(wrapper.vm.progressPercentage).toBe(0)
  })
})