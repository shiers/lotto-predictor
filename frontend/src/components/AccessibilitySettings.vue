<template>
  <div class="accessibility-settings">
    <div class="settings-header">
      <h3 id="accessibility-settings-title">Accessibility Settings</h3>
      <p>Customize the interface to meet your accessibility needs</p>
    </div>

    <div class="settings-content" role="group" aria-labelledby="accessibility-settings-title">
      <div class="setting-group">
        <h4 id="visual-settings">Visual Settings</h4>
        <div class="setting-item" role="group" aria-labelledby="visual-settings">
          <label class="setting-label">
            <input
              type="checkbox"
              v-model="settings.enableHighContrast"
              @change="updateHighContrast"
              aria-describedby="high-contrast-desc"
              data-testid="high-contrast-checkbox"
              id="high-contrast-setting"
            />
            <span class="setting-text">High Contrast Mode</span>
          </label>
          <p id="high-contrast-desc" class="setting-description">
            Increases color contrast for better visibility
          </p>
        </div>

        <div class="setting-item">
          <label class="setting-label">
            <input
              type="checkbox"
              v-model="settings.enableReducedMotion"
              @change="updateReducedMotion"
              aria-describedby="reduced-motion-desc"
              data-testid="reduced-motion-checkbox"
              id="reduced-motion-setting"
            />
            <span class="setting-text">Reduce Motion</span>
          </label>
          <p id="reduced-motion-desc" class="setting-description">
            Minimizes animations and transitions
          </p>
        </div>
      </div>

      <div class="setting-group">
        <h4 id="navigation-settings">Navigation Settings</h4>
        <div class="setting-item" role="group" aria-labelledby="navigation-settings">
          <label class="setting-label">
            <input
              type="checkbox"
              v-model="settings.enableKeyboardNavigation"
              @change="updateKeyboardNavigation"
              aria-describedby="keyboard-nav-desc"
              data-testid="keyboard-nav-checkbox"
              id="keyboard-nav-setting"
            />
            <span class="setting-text">Enhanced Keyboard Navigation</span>
          </label>
          <p id="keyboard-nav-desc" class="setting-description">
            Enables arrow key navigation and keyboard shortcuts
          </p>
        </div>

        <div class="setting-item">
          <label class="setting-label">
            <input
              type="checkbox"
              v-model="settings.announceChanges"
              @change="updateAnnouncements"
              aria-describedby="announcements-desc"
              data-testid="announcements-checkbox"
              id="announcements-setting"
            />
            <span class="setting-text">Screen Reader Announcements</span>
          </label>
          <p id="announcements-desc" class="setting-description">
            Announces important changes and updates to screen readers
          </p>
        </div>
      </div>

      <div class="setting-group">
        <h4 id="keyboard-shortcuts">Keyboard Shortcuts</h4>
        <div class="shortcuts-list" role="group" aria-labelledby="keyboard-shortcuts">
          <div class="shortcut-item">
            <kbd>Tab</kbd>
            <span>Navigate between interactive elements</span>
          </div>
          <div class="shortcut-item">
            <kbd>Shift + Tab</kbd>
            <span>Navigate backwards</span>
          </div>
          <div class="shortcut-item">
            <kbd>Enter</kbd>
            <span>Activate buttons and links</span>
          </div>
          <div class="shortcut-item">
            <kbd>Space</kbd>
            <span>Toggle checkboxes and buttons</span>
          </div>
          <div class="shortcut-item">
            <kbd>Arrow Keys</kbd>
            <span>Navigate within components (when enabled)</span>
          </div>
          <div class="shortcut-item">
            <kbd>Escape</kbd>
            <span>Close dialogs and menus</span>
          </div>
          <div class="shortcut-item">
            <kbd>Home / End</kbd>
            <span>Jump to first/last item in lists</span>
          </div>
        </div>
      </div>
    </div>

    <div class="settings-actions">
      <button
        @click="resetToDefaults"
        class="btn btn-secondary"
        aria-describedby="reset-desc"
      >
        Reset to Defaults
      </button>
      <p id="reset-desc" class="sr-only">
        Resets all accessibility settings to their default values
      </p>
    </div>

    <div v-if="showConfirmation" class="confirmation-message" role="alert" aria-live="polite">
      Settings have been saved and applied
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import accessibilityService, { type AccessibilityOptions } from '@/services/accessibilityService'

// Reactive state
const settings = ref<AccessibilityOptions>({
  enableHighContrast: false,
  enableReducedMotion: false,
  enableKeyboardNavigation: true,
  announceChanges: true
})

const showConfirmation = ref(false)

// Methods
const loadSettings = () => {
  settings.value = accessibilityService.getOptions()
}

const updateHighContrast = () => {
  if (settings.value.enableHighContrast) {
    accessibilityService.enableHighContrast()
  } else {
    accessibilityService.disableHighContrast()
  }
  showConfirmationMessage()
}

const updateReducedMotion = () => {
  accessibilityService.setReducedMotion(settings.value.enableReducedMotion!)
  showConfirmationMessage()
}

const updateKeyboardNavigation = () => {
  accessibilityService.setKeyboardNavigation(settings.value.enableKeyboardNavigation!)
  showConfirmationMessage()
}

const updateAnnouncements = () => {
  accessibilityService.setAnnouncements(settings.value.announceChanges!)
  showConfirmationMessage()
}

const resetToDefaults = () => {
  const defaults: AccessibilityOptions = {
    enableHighContrast: false,
    enableReducedMotion: false,
    enableKeyboardNavigation: true,
    announceChanges: true
  }
  
  accessibilityService.updateOptions(defaults)
  settings.value = defaults
  showConfirmationMessage()
  accessibilityService.announce('Accessibility settings reset to defaults')
}

const showConfirmationMessage = () => {
  showConfirmation.value = true
  setTimeout(() => {
    showConfirmation.value = false
  }, 3000)
}

// Lifecycle
onMounted(() => {
  loadSettings()
})
</script>

<style scoped>
.accessibility-settings {
  max-width: 600px;
  margin: 0 auto;
  padding: 2rem;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 8px;
}

.settings-header {
  text-align: center;
  margin-bottom: 2rem;
}

.settings-header h3 {
  color: var(--color-heading);
  margin-bottom: 0.5rem;
  font-size: 1.5rem;
}

.settings-header p {
  color: var(--color-text);
  font-size: 1rem;
}

.settings-content {
  margin-bottom: 2rem;
}

.setting-group {
  margin-bottom: 2rem;
  padding: 1.5rem;
  background: var(--color-background-soft);
  border-radius: 6px;
  border: 1px solid var(--color-border);
}

.setting-group h4 {
  color: var(--color-heading);
  margin: 0 0 1rem 0;
  font-size: 1.1rem;
  font-weight: 600;
}

.setting-item {
  margin-bottom: 1.5rem;
}

.setting-item:last-child {
  margin-bottom: 0;
}

.setting-label {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  cursor: pointer;
  font-weight: 500;
}

.setting-label input[type="checkbox"] {
  margin: 0;
  width: 1.25rem;
  height: 1.25rem;
  flex-shrink: 0;
  margin-top: 0.125rem;
}

.setting-text {
  color: var(--color-heading);
  font-size: 1rem;
  line-height: 1.4;
}

.setting-description {
  margin: 0.5rem 0 0 2rem;
  color: var(--color-text);
  font-size: 0.875rem;
  line-height: 1.4;
}

.shortcuts-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.shortcut-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 4px;
}

.shortcut-item kbd {
  background: var(--color-heading);
  color: var(--color-background);
  padding: 0.25rem 0.5rem;
  border-radius: 3px;
  font-family: monospace;
  font-size: 0.875rem;
  font-weight: bold;
  min-width: 3rem;
  text-align: center;
  border: 1px solid var(--color-border);
}

.shortcut-item span {
  color: var(--color-text);
  font-size: 0.9rem;
  flex: 1;
}

.settings-actions {
  text-align: center;
  padding-top: 1rem;
  border-top: 1px solid var(--color-border);
}

.btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 1rem;
  font-weight: 500;
  transition: all 0.3s ease;
  min-height: 44px;
  min-width: 44px;
}

.btn-secondary {
  background: var(--color-background-soft);
  color: var(--color-text);
  border: 1px solid var(--color-border);
}

.btn-secondary:hover:not(:disabled) {
  background: var(--color-background-mute);
}

.btn-secondary:focus {
  outline: 2px solid var(--color-border-hover);
  outline-offset: 2px;
}

.confirmation-message {
  background: #d4edda;
  border: 1px solid #c3e6cb;
  color: #155724;
  padding: 1rem;
  border-radius: 6px;
  text-align: center;
  font-weight: 500;
  margin-top: 1rem;
}

/* High contrast mode overrides */
.high-contrast .setting-group {
  border: 2px solid var(--color-border);
}

.high-contrast .shortcut-item {
  border: 2px solid var(--color-border);
}

.high-contrast .shortcut-item kbd {
  background: var(--color-text);
  color: var(--color-background);
  border: 2px solid var(--color-border);
  font-weight: bold;
}

.high-contrast .btn {
  border: 2px solid var(--color-border);
  font-weight: bold;
}

.high-contrast .confirmation-message {
  background: var(--color-background);
  border: 2px solid var(--color-success, #008000);
  color: var(--color-text);
  font-weight: bold;
}

/* Responsive design */
@media (max-width: 768px) {
  .accessibility-settings {
    margin: 1rem;
    padding: 1rem;
  }
  
  .setting-group {
    padding: 1rem;
  }
  
  .setting-description {
    margin-left: 0;
    margin-top: 0.75rem;
  }
  
  .shortcut-item {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }
  
  .shortcut-item kbd {
    min-width: auto;
  }
}

/* Reduced motion overrides */
.reduced-motion .btn,
.reduced-motion .setting-label,
.reduced-motion .shortcut-item {
  transition: none;
}
</style>