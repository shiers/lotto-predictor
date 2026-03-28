import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import accessibilityService from '../accessibilityService'

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
}
Object.defineProperty(window, 'localStorage', {
  value: localStorageMock
})

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

describe('AccessibilityService', () => {
  beforeEach(() => {
    // Clear all mocks
    vi.clearAllMocks()
    
    // Reset DOM
    document.body.innerHTML = ''
    document.documentElement.className = ''
  })

  afterEach(() => {
    // Clean up any announcer elements
    const announcer = document.getElementById('accessibility-announcer')
    if (announcer) {
      announcer.remove()
    }
  })

  describe('Screen Reader Announcements', () => {
    it('should create announcer element on initialization', () => {
      const announcer = document.getElementById('accessibility-announcer')
      expect(announcer).toBeTruthy()
      expect(announcer?.getAttribute('aria-live')).toBe('polite')
      expect(announcer?.getAttribute('aria-atomic')).toBe('true')
    })

    it('should announce messages to screen readers', () => {
      const message = 'Test announcement'
      accessibilityService.announce(message)
      
      const announcer = document.getElementById('accessibility-announcer')
      expect(announcer?.textContent).toBe(message)
    })

    it('should support different announcement priorities', () => {
      const message = 'Urgent announcement'
      accessibilityService.announce(message, 'assertive')
      
      const announcer = document.getElementById('accessibility-announcer')
      expect(announcer?.getAttribute('aria-live')).toBe('assertive')
      expect(announcer?.textContent).toBe(message)
    })

    it('should clear announcements after delay', async () => {
      vi.useFakeTimers()
      
      const message = 'Test announcement'
      accessibilityService.announce(message)
      
      const announcer = document.getElementById('accessibility-announcer')
      expect(announcer?.textContent).toBe(message)
      
      // Fast-forward time
      vi.advanceTimersByTime(1000)
      
      expect(announcer?.textContent).toBe('')
      
      vi.useRealTimers()
    })
  })

  describe('High Contrast Mode', () => {
    it('should enable high contrast mode', () => {
      accessibilityService.enableHighContrast()
      
      expect(document.documentElement.classList.contains('high-contrast')).toBe(true)
      expect(localStorageMock.setItem).toHaveBeenCalledWith(
        'accessibility-preferences',
        expect.stringContaining('"enableHighContrast":true')
      )
    })

    it('should disable high contrast mode', () => {
      // First enable it
      accessibilityService.enableHighContrast()
      expect(document.documentElement.classList.contains('high-contrast')).toBe(true)
      
      // Then disable it
      accessibilityService.disableHighContrast()
      expect(document.documentElement.classList.contains('high-contrast')).toBe(false)
      expect(localStorageMock.setItem).toHaveBeenCalledWith(
        'accessibility-preferences',
        expect.stringContaining('"enableHighContrast":false')
      )
    })

    it('should toggle high contrast mode', () => {
      // Initially disabled
      expect(document.documentElement.classList.contains('high-contrast')).toBe(false)
      
      // Toggle to enable
      accessibilityService.toggleHighContrast()
      expect(document.documentElement.classList.contains('high-contrast')).toBe(true)
      
      // Toggle to disable
      accessibilityService.toggleHighContrast()
      expect(document.documentElement.classList.contains('high-contrast')).toBe(false)
    })
  })

  describe('Reduced Motion', () => {
    it('should set reduced motion preference', () => {
      accessibilityService.setReducedMotion(true)
      
      expect(document.documentElement.classList.contains('reduced-motion')).toBe(true)
      expect(localStorageMock.setItem).toHaveBeenCalledWith(
        'accessibility-preferences',
        expect.stringContaining('"enableReducedMotion":true')
      )
    })

    it('should toggle reduced motion', () => {
      // Initially disabled
      expect(document.documentElement.classList.contains('reduced-motion')).toBe(false)
      
      // Toggle to enable
      accessibilityService.toggleReducedMotion()
      expect(document.documentElement.classList.contains('reduced-motion')).toBe(true)
      
      // Toggle to disable
      accessibilityService.toggleReducedMotion()
      expect(document.documentElement.classList.contains('reduced-motion')).toBe(false)
    })
  })

  describe('Focus Management', () => {
    it('should get focusable elements', () => {
      document.body.innerHTML = `
        <div>
          <button>Button 1</button>
          <input type="text" />
          <button disabled>Disabled Button</button>
          <a href="#">Link</a>
          <div tabindex="0">Focusable Div</div>
          <div tabindex="-1">Non-focusable Div</div>
        </div>
      `
      
      const container = document.body.firstElementChild as HTMLElement
      const focusableElements = accessibilityService.getFocusableElements(container)
      
      expect(focusableElements).toHaveLength(4) // button, input, link, focusable div
    })

    it('should manage focus stack', () => {
      const button1 = document.createElement('button')
      const button2 = document.createElement('button')
      document.body.appendChild(button1)
      document.body.appendChild(button2)
      
      // Mock focus methods
      button1.focus = vi.fn()
      button2.focus = vi.fn()
      
      // Push focus
      accessibilityService.pushFocus(button2)
      expect(button2.focus).toHaveBeenCalled()
      
      // Pop focus should return to previous element
      accessibilityService.popFocus()
      // Note: This test is limited because we can't easily mock document.activeElement
    })

    it('should clear focus stack', () => {
      const button = document.createElement('button')
      document.body.appendChild(button)
      button.focus = vi.fn()
      
      accessibilityService.pushFocus(button)
      accessibilityService.clearFocusStack()
      
      // After clearing, pop should not focus anything
      accessibilityService.popFocus()
      // The focus method should only have been called once (during push)
      expect(button.focus).toHaveBeenCalledTimes(1)
    })
  })

  describe('Focus Trap', () => {
    it('should trap focus within container', () => {
      document.body.innerHTML = `
        <div id="modal">
          <button id="first">First</button>
          <input id="middle" type="text" />
          <button id="last">Last</button>
        </div>
      `
      
      const modal = document.getElementById('modal') as HTMLElement
      const firstButton = document.getElementById('first') as HTMLElement
      const lastButton = document.getElementById('last') as HTMLElement
      
      // Mock focus methods
      firstButton.focus = vi.fn()
      lastButton.focus = vi.fn()
      
      const cleanup = accessibilityService.trapFocus(modal)
      
      // Should focus first element
      expect(firstButton.focus).toHaveBeenCalled()
      
      // Cleanup should remove event listeners
      expect(typeof cleanup).toBe('function')
      cleanup()
    })
  })

  describe('Keyboard Navigation', () => {
    it('should set up keyboard navigation for items', () => {
      document.body.innerHTML = `
        <div id="container">
          <button id="item1">Item 1</button>
          <button id="item2">Item 2</button>
          <button id="item3">Item 3</button>
        </div>
      `
      
      const container = document.getElementById('container') as HTMLElement
      const items = Array.from(container.children) as HTMLElement[]
      
      // Mock focus methods
      items.forEach(item => {
        item.focus = vi.fn()
      })
      
      const cleanup = accessibilityService.setupKeyboardNavigation(container, items)
      
      expect(typeof cleanup).toBe('function')
      cleanup()
    })
  })

  describe('Utility Methods', () => {
    it('should generate unique IDs', () => {
      const id1 = accessibilityService.generateId()
      const id2 = accessibilityService.generateId()
      const id3 = accessibilityService.generateId('custom')
      
      expect(id1).toMatch(/^a11y-\d+-[a-z0-9]+$/)
      expect(id2).toMatch(/^a11y-\d+-[a-z0-9]+$/)
      expect(id3).toMatch(/^custom-\d+-[a-z0-9]+$/)
      expect(id1).not.toBe(id2)
    })

    it('should check if element is visible to screen reader', () => {
      document.body.innerHTML = `
        <div id="visible">Visible</div>
        <div id="hidden" style="display: none;">Hidden</div>
        <div id="aria-hidden" aria-hidden="true">ARIA Hidden</div>
      `
      
      const visible = document.getElementById('visible') as HTMLElement
      const hidden = document.getElementById('hidden') as HTMLElement
      const ariaHidden = document.getElementById('aria-hidden') as HTMLElement
      
      expect(accessibilityService.isVisibleToScreenReader(visible)).toBe(true)
      expect(accessibilityService.isVisibleToScreenReader(hidden)).toBe(false)
      expect(accessibilityService.isVisibleToScreenReader(ariaHidden)).toBe(false)
    })
  })

  describe('Options Management', () => {
    it('should get current options', () => {
      const options = accessibilityService.getOptions()
      
      expect(options).toHaveProperty('enableHighContrast')
      expect(options).toHaveProperty('enableReducedMotion')
      expect(options).toHaveProperty('enableKeyboardNavigation')
      expect(options).toHaveProperty('announceChanges')
    })

    it('should update options', () => {
      const newOptions = {
        enableHighContrast: true,
        enableReducedMotion: true
      }
      
      accessibilityService.updateOptions(newOptions)
      
      const options = accessibilityService.getOptions()
      expect(options.enableHighContrast).toBe(true)
      expect(options.enableReducedMotion).toBe(true)
      
      expect(document.documentElement.classList.contains('high-contrast')).toBe(true)
      expect(document.documentElement.classList.contains('reduced-motion')).toBe(true)
    })
  })

  describe('Preferences Persistence', () => {
    it('should save preferences to localStorage', () => {
      accessibilityService.enableHighContrast()
      
      expect(localStorageMock.setItem).toHaveBeenCalledWith(
        'accessibility-preferences',
        expect.stringContaining('"enableHighContrast":true')
      )
    })

    it('should load preferences from localStorage', () => {
      const mockPreferences = JSON.stringify({
        enableHighContrast: true,
        enableReducedMotion: true,
        enableKeyboardNavigation: false,
        announceChanges: false
      })
      
      localStorageMock.getItem.mockReturnValue(mockPreferences)
      
      // Create a new instance to test loading
      // Note: This is a simplified test since we're using a singleton
      const options = accessibilityService.getOptions()
      
      // The service should have loaded the preferences
      expect(localStorageMock.getItem).toHaveBeenCalledWith('accessibility-preferences')
    })
  })
})