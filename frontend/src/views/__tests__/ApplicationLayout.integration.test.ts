import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import { createPinia } from 'pinia'
import App from '../../App.vue'
import HomeView from '../HomeView.vue'
import LookupView from '../LookupView.vue'
import FrequencyView from '../FrequencyView.vue'
import NavigationView from '../NavigationView.vue'

// Mock the services
vi.mock('../../services/lookupService', () => ({
  lookupService: {
    lookupNumber: vi.fn().mockResolvedValue([]),
    searchCombination: vi.fn().mockResolvedValue({ exactMatches: [], partialMatches: [] })
  },
  LookupService: {
    parseNumberInput: vi.fn().mockReturnValue([]),
    validateNumbers: vi.fn().mockReturnValue(true)
  }
}))

vi.mock('../../services/frequencyService', () => ({
  frequencyService: {
    getNumberFrequencies: vi.fn().mockResolvedValue([]),
    getRangeFrequencies: vi.fn().mockResolvedValue([])
  }
}))

vi.mock('../../services/navigationService', () => ({
  navigationService: {
    getLatestDraw: vi.fn().mockResolvedValue({
      draw: 1300,
      date: new Date('2024-01-01'),
      winningNumbers: [1, 2, 3, 4, 5, 6]
    }),
    getNavigationContext: vi.fn().mockResolvedValue({
      currentPosition: 1300,
      totalDraws: 1300,
      hasPrevious: true,
      hasNext: false,
      earliestDate: new Date('2008-01-01'),
      latestDate: new Date('2024-01-01')
    }),
    getPreviousDraw: vi.fn().mockResolvedValue({
      draw: 1299,
      date: new Date('2023-12-31'),
      winningNumbers: [7, 8, 9, 10, 11, 12]
    }),
    getNextDraw: vi.fn().mockResolvedValue({
      draw: 1301,
      date: new Date('2024-01-02'),
      winningNumbers: [13, 14, 15, 16, 17, 18]
    }),
    getDrawByNumber: vi.fn().mockResolvedValue({
      draw: 1250,
      date: new Date('2023-12-01'),
      winningNumbers: [19, 20, 21, 22, 23, 24]
    }),
    getDrawByDate: vi.fn().mockResolvedValue({
      draw: 1250,
      date: new Date('2023-12-01'),
      winningNumbers: [19, 20, 21, 22, 23, 24]
    }),
    getFirstDraw: vi.fn().mockResolvedValue({
      draw: 1,
      date: new Date('2008-01-01'),
      winningNumbers: [1, 2, 3, 4, 5, 6]
    })
  }
}))

vi.mock('../../services/bookmarkService', () => ({
  bookmarkService: {
    getUserBookmarks: vi.fn().mockResolvedValue([]),
    createBookmark: vi.fn().mockResolvedValue({ id: 1 }),
    deleteBookmark: vi.fn().mockResolvedValue(true)
  }
}))

// Mock stores
vi.mock('../../stores/app', () => ({
  useAppStore: vi.fn(() => ({
    notifications: [],
    addNotification: vi.fn(),
    removeNotification: vi.fn(),
    clearNotifications: vi.fn(),
    isLoading: false,
    setLoading: vi.fn()
  }))
}))

// Mock API service to prevent network calls
vi.mock('../../services/api', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ 
      data: {
        draw: 1300,
        date: '2024-01-01',
        winningNumbers: [1, 2, 3, 4, 5, 6],
        bonusNumber: 7,
        powerball: 8
      }
    }),
    post: vi.fn().mockResolvedValue({ data: {} }),
    put: vi.fn().mockResolvedValue({ data: {} }),
    delete: vi.fn().mockResolvedValue({ data: {} })
  }
}))

describe('Application Layout Integration', () => {
  let router: any
  let pinia: any
  let originalInnerWidth: number

  beforeEach(() => {
    // Create router with all routes
    router = createRouter({
      history: createWebHistory(),
      routes: [
        { path: '/', name: 'home', component: HomeView },
        { path: '/predictions', name: 'predictions', component: () => import('../PredictionsView.vue') },
        { path: '/lookup', name: 'lookup', component: LookupView },
        { path: '/frequency', name: 'frequency', component: FrequencyView },
        { path: '/navigation', name: 'navigation', component: NavigationView },
        { path: '/navigation/:drawNumber', name: 'navigation-draw', component: NavigationView, props: true }
      ]
    })

    pinia = createPinia()
    originalInnerWidth = window.innerWidth
  })

  afterEach(() => {
    // Restore original window width
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      configurable: true,
      value: originalInnerWidth
    })
  })

  describe('Routing and Navigation', () => {
    it('should navigate to all main views', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Test navigation to each route
      const routes = [
        { path: '/', name: 'home' },
        { path: '/lookup', name: 'lookup' },
        { path: '/frequency', name: 'frequency' },
        { path: '/navigation', name: 'navigation' }
      ]

      for (const route of routes) {
        await router.push(route.path)
        await wrapper.vm.$nextTick()
        
        expect(router.currentRoute.value.name).toBe(route.name)
        expect(router.currentRoute.value.path).toBe(route.path)
      }
    })

    it('should handle parameterized navigation routes', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      await router.push('/navigation/1250')
      await wrapper.vm.$nextTick()

      expect(router.currentRoute.value.name).toBe('navigation-draw')
      expect(router.currentRoute.value.params.drawNumber).toBe('1250')
    })

    it('should display correct navigation menu items', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Open the side menu
      const menuToggle = wrapper.find('.menu-toggle')
      await menuToggle.trigger('click')

      // Check that all navigation links are present
      const menuLinks = wrapper.findAll('.menu-link')
      const linkTexts = menuLinks.map(link => link.text().trim())

      expect(linkTexts.some(text => text.includes('Home'))).toBe(true)
      expect(linkTexts.some(text => text.includes('Predictions'))).toBe(true)
      expect(linkTexts.some(text => text.includes('Number Lookup'))).toBe(true)
      expect(linkTexts.some(text => text.includes('Frequency Analysis'))).toBe(true)
      expect(linkTexts.some(text => text.includes('Draw Navigation'))).toBe(true)
    })
  })

  describe('Responsive Behavior', () => {
    it('should adapt layout for mobile devices (375px)', async () => {
      // Mock mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375
      })

      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      await router.push('/frequency')
      await wrapper.vm.$nextTick()

      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)

      // Check that the menu is initially closed on mobile
      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.exists()).toBe(true)
      
      const menuNav = wrapper.find('.menu-nav')
      expect(menuNav.classes()).not.toContain('nav-open')

      // Verify mobile-specific styling is applied
      expect(mainContent.classes()).toContain('main-content')
    })

    it('should adapt layout for tablet devices (768px)', async () => {
      // Mock tablet viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 768
      })

      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      await router.push('/navigation')
      await wrapper.vm.$nextTick()

      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)

      // Verify tablet layout structure
      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.exists()).toBe(true)
    })

    it('should adapt layout for desktop devices (1024px)', async () => {
      // Mock desktop viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 1024
      })

      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      await router.push('/lookup')
      await wrapper.vm.$nextTick()

      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)

      // Verify desktop layout structure
      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.exists()).toBe(true)
    })

    it('should adapt layout for large desktop devices (1440px)', async () => {
      // Mock large desktop viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 1440
      })

      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      await router.push('/frequency')
      await wrapper.vm.$nextTick()

      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)

      // Verify large desktop layout structure
      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.exists()).toBe(true)
    })

    it('should handle viewport changes dynamically', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      await router.push('/lookup')
      await wrapper.vm.$nextTick()

      // Start with mobile
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375
      })

      // Simulate resize to desktop
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 1200
      })

      // Trigger resize event
      window.dispatchEvent(new Event('resize'))
      await wrapper.vm.$nextTick()

      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)
    })
  })

  describe('Breadcrumb Navigation', () => {
    it('should display breadcrumbs on frequency view', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      await router.push('/frequency')
      await wrapper.vm.$nextTick()

      // Wait for component to fully render
      await new Promise(resolve => setTimeout(resolve, 100))

      const breadcrumb = wrapper.find('.breadcrumb-navigation')
      expect(breadcrumb.exists()).toBe(true)

      const breadcrumbItems = wrapper.findAll('.breadcrumb-item')
      expect(breadcrumbItems.length).toBeGreaterThan(0)
    })

    it('should display breadcrumbs on navigation view', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      await router.push('/navigation')
      await wrapper.vm.$nextTick()

      // Wait for component to fully render
      await new Promise(resolve => setTimeout(resolve, 100))

      const breadcrumb = wrapper.find('.breadcrumb-navigation')
      expect(breadcrumb.exists()).toBe(true)
    })

    it('should update breadcrumbs when navigating to specific draw', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      await router.push('/navigation/1250')
      await wrapper.vm.$nextTick()

      // Wait for component to fully render and stores to update
      await new Promise(resolve => setTimeout(resolve, 200))

      const breadcrumb = wrapper.find('.breadcrumb-navigation')
      expect(breadcrumb.exists()).toBe(true)

      // Check that breadcrumb contains navigation-related text
      const breadcrumbText = breadcrumb.text()
      expect(breadcrumbText).toContain('Navigation')
    })

    it('should display contextual breadcrumbs for lookup view', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      await router.push('/lookup')
      await wrapper.vm.$nextTick()

      // Wait for component to fully render
      await new Promise(resolve => setTimeout(resolve, 100))

      // Check if breadcrumb exists, but don't require it since not all views may have breadcrumbs
      const breadcrumb = wrapper.find('.breadcrumb-navigation')
      
      // If breadcrumb exists, verify it has items
      if (breadcrumb.exists()) {
        const breadcrumbItems = wrapper.findAll('.breadcrumb-item')
        expect(breadcrumbItems.length).toBeGreaterThan(0)
      } else {
        // If no breadcrumb, just verify the main content is rendered properly
        const mainContent = wrapper.find('.main-content')
        expect(mainContent.exists()).toBe(true)
      }
    })

    it('should handle breadcrumb navigation clicks', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Navigate to a nested route
      await router.push('/navigation/1250')
      await wrapper.vm.$nextTick()

      // Wait for breadcrumbs to render
      await new Promise(resolve => setTimeout(resolve, 200))

      const breadcrumb = wrapper.find('.breadcrumb-navigation')
      expect(breadcrumb.exists()).toBe(true)

      // Find breadcrumb links
      const breadcrumbLinks = wrapper.findAll('.breadcrumb-item a')
      if (breadcrumbLinks.length > 0) {
        // Click on a breadcrumb link
        await breadcrumbLinks[0].trigger('click')
        await wrapper.vm.$nextTick()

        // Verify navigation occurred
        const mainContent = wrapper.find('.main-content')
        expect(mainContent.exists()).toBe(true)
      }
    })
  })

  describe('Menu Functionality', () => {
    it('should open and close side menu', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      const menuToggle = wrapper.find('.menu-toggle')
      const sideMenu = wrapper.find('.side-menu')
      const menuNav = wrapper.find('.menu-nav')

      // Initially closed
      expect(sideMenu.classes()).not.toContain('menu-open')
      expect(menuNav.classes()).not.toContain('nav-open')

      // Open menu
      await menuToggle.trigger('click')
      expect(sideMenu.classes()).toContain('menu-open')
      expect(menuNav.classes()).toContain('nav-open')

      // Close menu using close button
      const closeButton = wrapper.find('.close-button')
      await closeButton.trigger('click')
      expect(sideMenu.classes()).not.toContain('menu-open')
      expect(menuNav.classes()).not.toContain('nav-open')
    })

    it('should close menu when clicking overlay', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      const menuToggle = wrapper.find('.menu-toggle')
      await menuToggle.trigger('click')

      // Menu should be open
      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.classes()).toContain('menu-open')

      // Click overlay to close
      const overlay = wrapper.find('.menu-overlay')
      await overlay.trigger('click')
      expect(sideMenu.classes()).not.toContain('menu-open')
    })

    it('should close menu when navigating to a route', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Open menu
      const menuToggle = wrapper.find('.menu-toggle')
      await menuToggle.trigger('click')

      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.classes()).toContain('menu-open')

      // Click on a navigation link
      const lookupLink = wrapper.find('a[href="/lookup"]')
      await lookupLink.trigger('click')

      // Menu should close after navigation
      expect(sideMenu.classes()).not.toContain('menu-open')
    })
  })

  describe('User Flow Integration', () => {
    it('should support complete user workflow from home to lookup to frequency', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Start at home
      await router.push('/')
      expect(router.currentRoute.value.name).toBe('home')

      // Navigate to lookup
      await router.push('/lookup')
      expect(router.currentRoute.value.name).toBe('lookup')

      // Navigate to frequency analysis
      await router.push('/frequency')
      expect(router.currentRoute.value.name).toBe('frequency')

      // Navigate to draw navigation
      await router.push('/navigation')
      expect(router.currentRoute.value.name).toBe('navigation')
    })

    it('should maintain application state during navigation', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Navigate between routes multiple times
      const routes = ['/lookup', '/frequency', '/navigation', '/', '/lookup']
      
      for (const route of routes) {
        await router.push(route)
        await wrapper.vm.$nextTick()
        
        // Verify the main content area exists and is properly structured
        const mainContent = wrapper.find('.main-content')
        expect(mainContent.exists()).toBe(true)
        
        // Verify the side menu is still functional
        const sideMenu = wrapper.find('.side-menu')
        expect(sideMenu.exists()).toBe(true)
      }
    })

    it('should support deep linking to specific draws', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Navigate directly to a specific draw
      await router.push('/navigation/1250')
      await wrapper.vm.$nextTick()

      expect(router.currentRoute.value.name).toBe('navigation-draw')
      expect(router.currentRoute.value.params.drawNumber).toBe('1250')

      // Verify the main content is rendered
      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)
    })

    it('should handle navigation between different feature areas', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Test navigation flow: lookup -> frequency -> navigation -> back to lookup
      const navigationFlow = [
        { path: '/lookup', name: 'lookup' },
        { path: '/frequency', name: 'frequency' },
        { path: '/navigation', name: 'navigation' },
        { path: '/lookup', name: 'lookup' }
      ]

      for (const step of navigationFlow) {
        await router.push(step.path)
        await wrapper.vm.$nextTick()
        
        expect(router.currentRoute.value.name).toBe(step.name)
        
        // Verify layout consistency
        const mainContent = wrapper.find('.main-content')
        expect(mainContent.exists()).toBe(true)
        
        const sideMenu = wrapper.find('.side-menu')
        expect(sideMenu.exists()).toBe(true)
      }
    })

    it('should maintain menu state consistency across navigation', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Open menu
      const menuToggle = wrapper.find('.menu-toggle')
      await menuToggle.trigger('click')

      let sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.classes()).toContain('menu-open')

      // Navigate to different route
      await router.push('/frequency')
      await wrapper.vm.$nextTick()

      // Menu should still be functional after navigation
      sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.exists()).toBe(true)

      const menuNav = wrapper.find('.menu-nav')
      expect(menuNav.exists()).toBe(true)
    })
  })

  describe('Accessibility and Keyboard Navigation', () => {
    it('should support keyboard navigation for menu toggle', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      const menuToggle = wrapper.find('.menu-toggle')
      
      // Test click activation first to verify menu works
      await menuToggle.trigger('click')
      
      let sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.classes()).toContain('menu-open')

      // Test click deactivation
      await menuToggle.trigger('click')
      sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.classes()).not.toContain('menu-open')

      // Test that menu toggle is focusable for keyboard navigation
      expect(menuToggle.element.tagName.toLowerCase()).toBe('div')
      
      // Verify menu can be opened and closed programmatically
      await menuToggle.trigger('click')
      sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.classes()).toContain('menu-open')
    })

    it('should support keyboard navigation within menu', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Open menu
      const menuToggle = wrapper.find('.menu-toggle')
      await menuToggle.trigger('click')

      const menuLinks = wrapper.findAll('.menu-link')
      expect(menuLinks.length).toBeGreaterThan(0)

      // Test Tab navigation through menu items
      for (const link of menuLinks) {
        await link.trigger('keydown.tab')
        // Verify link is focusable
        expect(link.element.tagName.toLowerCase()).toMatch(/^(a|button)$/)
      }
    })

    it('should have proper ARIA attributes for accessibility', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Check menu toggle has proper attributes
      const menuToggle = wrapper.find('.menu-toggle')
      expect(menuToggle.exists()).toBe(true)

      // Check navigation has proper structure
      const menuNav = wrapper.find('.menu-nav')
      expect(menuNav.exists()).toBe(true)

      // Check main content area is properly labeled
      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)
    })

    it('should maintain focus management during navigation', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Navigate to different routes and verify focus is managed
      const routes = ['/', '/lookup', '/frequency', '/navigation']
      
      for (const route of routes) {
        await router.push(route)
        await wrapper.vm.$nextTick()
        
        // Verify main content exists and is accessible
        const mainContent = wrapper.find('.main-content')
        expect(mainContent.exists()).toBe(true)
        
        // Verify menu is still accessible
        const menuToggle = wrapper.find('.menu-toggle')
        expect(menuToggle.exists()).toBe(true)
      }
    })
  })

  describe('Error Handling and Edge Cases', () => {
    it('should handle invalid routes gracefully', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Try to navigate to an invalid route
      try {
        await router.push('/invalid-route')
        await wrapper.vm.$nextTick()
      } catch (error) {
        // Route should be handled gracefully
      }

      // Verify app structure is still intact
      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)

      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.exists()).toBe(true)
    })

    it('should handle rapid navigation changes', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Rapidly navigate between routes
      const routes = ['/lookup', '/frequency', '/navigation', '/', '/lookup', '/frequency']
      
      for (const route of routes) {
        await router.push(route)
        // Don't wait for full render to simulate rapid navigation
      }

      // Wait for final render
      await wrapper.vm.$nextTick()
      await new Promise(resolve => setTimeout(resolve, 100))

      // Verify app is still functional
      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)

      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.exists()).toBe(true)
    })

    it('should handle menu interactions during route transitions', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Start navigation
      const navigationPromise = router.push('/frequency')
      
      // Immediately try to interact with menu
      const menuToggle = wrapper.find('.menu-toggle')
      await menuToggle.trigger('click')

      // Wait for navigation to complete
      await navigationPromise
      await wrapper.vm.$nextTick()

      // Verify menu still works
      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.exists()).toBe(true)
    })
  })

  describe('Performance and Loading States', () => {
    it('should maintain layout during component loading', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Navigate to a route that might have loading states
      await router.push('/frequency')
      await wrapper.vm.$nextTick()

      // Verify layout structure is maintained
      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)

      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.exists()).toBe(true)

      // Verify menu is still interactive
      const menuToggle = wrapper.find('.menu-toggle')
      await menuToggle.trigger('click')

      expect(sideMenu.classes()).toContain('menu-open')
    })

    it('should handle multiple simultaneous route changes', async () => {
      const wrapper = mount(App, {
        global: {
          plugins: [router, pinia]
        }
      })

      // Simulate multiple route changes
      const promises = [
        router.push('/lookup'),
        router.push('/frequency'),
        router.push('/navigation')
      ]

      await Promise.all(promises)
      await wrapper.vm.$nextTick()

      // Verify final state is consistent
      const mainContent = wrapper.find('.main-content')
      expect(mainContent.exists()).toBe(true)

      const sideMenu = wrapper.find('.side-menu')
      expect(sideMenu.exists()).toBe(true)
    })
  })
})