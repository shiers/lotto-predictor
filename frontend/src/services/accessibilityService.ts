/**
 * Accessibility Service
 * Provides utilities for managing accessibility features including focus management,
 * keyboard navigation, screen reader announcements, and high contrast mode.
 */

export interface FocusableElement extends HTMLElement {
  focus(): void;
  blur(): void;
}

export interface AccessibilityOptions {
  enableHighContrast?: boolean;
  enableReducedMotion?: boolean;
  enableKeyboardNavigation?: boolean;
  announceChanges?: boolean;
}

class AccessibilityService {
  private focusStack: FocusableElement[] = [];
  private announcer: HTMLElement | null = null;
  private options: AccessibilityOptions = {
    enableHighContrast: false,
    enableReducedMotion: false,
    enableKeyboardNavigation: true,
    announceChanges: true
  };

  constructor() {
    this.initializeAnnouncer();
    this.loadUserPreferences();
    this.setupMediaQueryListeners();
  }

  /**
   * Initialize the screen reader announcer element
   */
  private initializeAnnouncer(): void {
    this.announcer = document.createElement('div');
    this.announcer.setAttribute('aria-live', 'polite');
    this.announcer.setAttribute('aria-atomic', 'true');
    this.announcer.setAttribute('id', 'accessibility-announcer');
    this.announcer.style.position = 'absolute';
    this.announcer.style.left = '-10000px';
    this.announcer.style.width = '1px';
    this.announcer.style.height = '1px';
    this.announcer.style.overflow = 'hidden';
    document.body.appendChild(this.announcer);
  }

  /**
   * Load user accessibility preferences from localStorage
   */
  private loadUserPreferences(): void {
    const stored = localStorage.getItem('accessibility-preferences');
    if (stored) {
      try {
        this.options = { ...this.options, ...JSON.parse(stored) };
        this.applyPreferences();
      } catch (error) {
        console.warn('Failed to load accessibility preferences:', error);
      }
    }
  }

  /**
   * Save user accessibility preferences to localStorage
   */
  private saveUserPreferences(): void {
    try {
      localStorage.setItem('accessibility-preferences', JSON.stringify(this.options));
    } catch (error) {
      console.warn('Failed to save accessibility preferences:', error);
    }
  }

  /**
   * Setup media query listeners for system preferences
   */
  private setupMediaQueryListeners(): void {
    // Check if matchMedia is available (not available in some test environments)
    if (typeof window.matchMedia !== 'function') {
      return;
    }

    // High contrast preference
    const highContrastQuery = window.matchMedia('(prefers-contrast: high)');
    highContrastQuery.addEventListener('change', (e) => {
      if (e.matches) {
        this.enableHighContrast();
      }
    });

    // Reduced motion preference
    const reducedMotionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    reducedMotionQuery.addEventListener('change', (e) => {
      this.setReducedMotion(e.matches);
    });

    // Apply initial states
    if (highContrastQuery.matches) {
      this.enableHighContrast();
    }
    if (reducedMotionQuery.matches) {
      this.setReducedMotion(true);
    }
  }

  /**
   * Apply current accessibility preferences to the document
   */
  private applyPreferences(): void {
    if (this.options.enableHighContrast) {
      this.enableHighContrast();
    } else {
      this.disableHighContrast();
    }

    if (this.options.enableReducedMotion) {
      this.setReducedMotion(true);
    } else {
      this.setReducedMotion(false);
    }
  }

  /**
   * Announce a message to screen readers
   */
  announce(message: string, priority: 'polite' | 'assertive' = 'polite'): void {
    if (!this.options.announceChanges || !this.announcer) return;

    this.announcer.setAttribute('aria-live', priority);
    this.announcer.textContent = message;

    // Clear the message after a short delay to allow for re-announcements
    setTimeout(() => {
      if (this.announcer) {
        this.announcer.textContent = '';
      }
    }, 1000);
  }

  /**
   * Push current focus to stack and set new focus
   */
  pushFocus(element: FocusableElement): void {
    const currentFocus = document.activeElement as FocusableElement;
    if (currentFocus && currentFocus !== document.body) {
      this.focusStack.push(currentFocus);
    }
    element.focus();
  }

  /**
   * Restore focus to the previous element in the stack
   */
  popFocus(): void {
    const previousFocus = this.focusStack.pop();
    if (previousFocus) {
      previousFocus.focus();
    }
  }

  /**
   * Clear the focus stack
   */
  clearFocusStack(): void {
    this.focusStack = [];
  }

  /**
   * Get all focusable elements within a container
   */
  getFocusableElements(container: HTMLElement): FocusableElement[] {
    const focusableSelectors = [
      'button:not([disabled])',
      'input:not([disabled])',
      'select:not([disabled])',
      'textarea:not([disabled])',
      'a[href]',
      '[tabindex]:not([tabindex="-1"])',
      '[contenteditable="true"]'
    ].join(', ');

    return Array.from(container.querySelectorAll(focusableSelectors)) as FocusableElement[];
  }

  /**
   * Trap focus within a container (useful for modals)
   */
  trapFocus(container: HTMLElement): () => void {
    const focusableElements = this.getFocusableElements(container);
    if (focusableElements.length === 0) return () => {};

    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Tab') return;

      if (event.shiftKey) {
        // Shift + Tab
        if (document.activeElement === firstElement) {
          event.preventDefault();
          lastElement.focus();
        }
      } else {
        // Tab
        if (document.activeElement === lastElement) {
          event.preventDefault();
          firstElement.focus();
        }
      }
    };

    container.addEventListener('keydown', handleKeyDown);
    firstElement.focus();

    // Return cleanup function
    return () => {
      container.removeEventListener('keydown', handleKeyDown);
    };
  }

  /**
   * Enable high contrast mode
   */
  enableHighContrast(): void {
    document.documentElement.classList.add('high-contrast');
    this.options.enableHighContrast = true;
    this.saveUserPreferences();
    this.announce('High contrast mode enabled');
  }

  /**
   * Disable high contrast mode
   */
  disableHighContrast(): void {
    document.documentElement.classList.remove('high-contrast');
    this.options.enableHighContrast = false;
    this.saveUserPreferences();
    this.announce('High contrast mode disabled');
  }

  /**
   * Toggle high contrast mode
   */
  toggleHighContrast(): void {
    if (this.options.enableHighContrast) {
      this.disableHighContrast();
    } else {
      this.enableHighContrast();
    }
  }

  /**
   * Set reduced motion preference
   */
  setReducedMotion(enabled: boolean): void {
    if (enabled) {
      document.documentElement.classList.add('reduced-motion');
    } else {
      document.documentElement.classList.remove('reduced-motion');
    }
    this.options.enableReducedMotion = enabled;
    this.saveUserPreferences();
  }

  /**
   * Toggle reduced motion
   */
  toggleReducedMotion(): void {
    this.setReducedMotion(!this.options.enableReducedMotion);
    this.announce(`Reduced motion ${this.options.enableReducedMotion ? 'enabled' : 'disabled'}`);
  }

  /**
   * Set keyboard navigation preference
   */
  setKeyboardNavigation(enabled: boolean): void {
    this.options.enableKeyboardNavigation = enabled;
    this.saveUserPreferences();
  }

  /**
   * Set screen reader announcements preference
   */
  setAnnouncements(enabled: boolean): void {
    this.options.announceChanges = enabled;
    this.saveUserPreferences();
  }

  /**
   * Get current accessibility options
   */
  getOptions(): AccessibilityOptions {
    return { ...this.options };
  }

  /**
   * Update accessibility options
   */
  updateOptions(newOptions: Partial<AccessibilityOptions>): void {
    this.options = { ...this.options, ...newOptions };
    this.applyPreferences();
    this.saveUserPreferences();
  }

  /**
   * Generate a unique ID for accessibility purposes
   */
  generateId(prefix: string = 'a11y'): string {
    return `${prefix}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Check if an element is visible to screen readers
   */
  isVisibleToScreenReader(element: HTMLElement): boolean {
    const style = window.getComputedStyle(element);
    return !(
      style.display === 'none' ||
      style.visibility === 'hidden' ||
      element.hasAttribute('aria-hidden') ||
      element.getAttribute('aria-hidden') === 'true'
    );
  }

  /**
   * Set up keyboard navigation for a list of items
   */
  setupKeyboardNavigation(
    container: HTMLElement,
    items: HTMLElement[],
    options: {
      orientation?: 'horizontal' | 'vertical' | 'both';
      wrap?: boolean;
      activateOnFocus?: boolean;
    } = {}
  ): () => void {
    const {
      orientation = 'vertical',
      wrap = true,
      activateOnFocus = false
    } = options;

    let currentIndex = 0;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (!this.options.enableKeyboardNavigation) return;

      let handled = false;
      const maxIndex = items.length - 1;

      switch (event.key) {
        case 'ArrowDown':
          if (orientation === 'vertical' || orientation === 'both') {
            currentIndex = wrap && currentIndex === maxIndex ? 0 : Math.min(currentIndex + 1, maxIndex);
            handled = true;
          }
          break;
        case 'ArrowUp':
          if (orientation === 'vertical' || orientation === 'both') {
            currentIndex = wrap && currentIndex === 0 ? maxIndex : Math.max(currentIndex - 1, 0);
            handled = true;
          }
          break;
        case 'ArrowRight':
          if (orientation === 'horizontal' || orientation === 'both') {
            currentIndex = wrap && currentIndex === maxIndex ? 0 : Math.min(currentIndex + 1, maxIndex);
            handled = true;
          }
          break;
        case 'ArrowLeft':
          if (orientation === 'horizontal' || orientation === 'both') {
            currentIndex = wrap && currentIndex === 0 ? maxIndex : Math.max(currentIndex - 1, 0);
            handled = true;
          }
          break;
        case 'Home':
          currentIndex = 0;
          handled = true;
          break;
        case 'End':
          currentIndex = maxIndex;
          handled = true;
          break;
        case 'Enter':
        case ' ':
          if (activateOnFocus) {
            items[currentIndex].click();
            handled = true;
          }
          break;
      }

      if (handled) {
        event.preventDefault();
        items[currentIndex].focus();
      }
    };

    container.addEventListener('keydown', handleKeyDown);

    // Return cleanup function
    return () => {
      container.removeEventListener('keydown', handleKeyDown);
    };
  }

  /**
   * Clean up accessibility service
   */
  destroy(): void {
    if (this.announcer && this.announcer.parentNode) {
      this.announcer.parentNode.removeChild(this.announcer);
    }
    this.clearFocusStack();
  }
}

// Create and export singleton instance
export const accessibilityService = new AccessibilityService();
export default accessibilityService;