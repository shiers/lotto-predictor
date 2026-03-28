<template>
  <div class="draw-navigation" role="main" aria-labelledby="navigation-title">
    <div class="navigation-header">
      <h3 id="navigation-title">Draw Navigation</h3>
      <div class="navigation-actions">
        <button 
          @click="showJumpDialog = true" 
          class="jump-button"
          :disabled="isLoading"
          aria-describedby="jump-help"
        >
          <span class="icon" aria-hidden="true">🎯</span>
          Jump To
        </button>
        <div id="jump-help" class="sr-only">
          Open dialog to jump to a specific draw by number or date
        </div>
        <button 
          @click="showBookmarkDialog = true" 
          class="bookmark-button"
          :disabled="isLoading || !currentDraw"
          aria-describedby="bookmark-help"
        >
          <span class="icon" aria-hidden="true">🔖</span>
          Bookmark
        </button>
        <div id="bookmark-help" class="sr-only">
          Bookmark the current draw for quick access later
        </div>
      </div>
    </div>

    <!-- Current Draw Display -->
    <div v-if="currentDraw" class="current-draw" role="region" aria-labelledby="current-draw-title">
      <h4 id="current-draw-title" class="sr-only">Current Draw Information</h4>
      <div class="draw-info">
        <div class="draw-number">
          <span class="label">Draw #</span>
          <span class="value">{{ currentDraw.draw }}</span>
        </div>
        <div class="draw-date">
          <span class="label">Date</span>
          <span class="value">{{ formatDate(currentDraw.date) }}</span>
        </div>
      </div>
      
      <div class="winning-numbers" role="group" aria-labelledby="winning-numbers-title">
        <h5 id="winning-numbers-title" class="sr-only">Winning Numbers</h5>
        <div class="main-numbers" role="group" aria-label="Main winning numbers">
          <span 
            v-for="(number, index) in currentDraw.winningNumbers" 
            :key="number" 
            class="number-ball main"
            :aria-label="`Main number ${index + 1}: ${number}`"
          >
            {{ number }}
          </span>
        </div>
        <div class="special-numbers">
          <div class="bonus-number">
            <span class="label">Bonus</span>
            <span class="number-ball bonus" :aria-label="`Bonus number: ${currentDraw.bonusNumber}`">
              {{ currentDraw.bonusNumber }}
            </span>
          </div>
          <div class="powerball-number">
            <span class="label">Powerball</span>
            <span class="number-ball powerball" :aria-label="`Powerball number: ${currentDraw.powerball}`">
              {{ currentDraw.powerball }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Navigation Controls -->
    <nav class="navigation-controls" role="navigation" aria-label="Draw navigation">
      <button 
        @click="navigatePrevious" 
        :disabled="isLoading || !navigationContext?.hasPrevious"
        class="nav-button previous"
        :aria-label="navigationContext?.hasPrevious ? 'Go to previous draw' : 'No previous draw available'"
      >
        <span class="icon" aria-hidden="true">⬅️</span>
        Previous
      </button>
      
      <div class="position-info" role="status" aria-live="polite">
        <span v-if="navigationContext" class="position">
          {{ navigationContext.currentPosition }} of {{ navigationContext.totalDraws }}
        </span>
        <span v-else class="position">-</span>
      </div>
      
      <button 
        @click="navigateNext" 
        :disabled="isLoading || !navigationContext?.hasNext"
        class="nav-button next"
        :aria-label="navigationContext?.hasNext ? 'Go to next draw' : 'No next draw available'"
      >
        Next
        <span class="icon" aria-hidden="true">➡️</span>
      </button>
    </nav>

    <!-- Navigation Context -->
    <NavigationContext 
      v-if="navigationContext && showContext" 
      :context="navigationContext"
      @jump-to-draw="handleJumpToDraw"
    />

    <!-- Loading State -->
    <div v-if="isLoading" class="loading-state" role="status" aria-live="polite">
      <div class="loading-spinner" aria-hidden="true"></div>
      <p>Loading draw...</p>
    </div>

    <!-- Error State -->
    <div v-if="error" class="error-state" role="alert" aria-live="assertive">
      <div class="error-icon" aria-hidden="true">❌</div>
      <h4>Navigation Error</h4>
      <p>{{ error }}</p>
      <button @click="clearError" class="retry-button">Dismiss</button>
    </div>

    <!-- Jump To Dialog -->
    <JumpToDrawDialog
      v-if="showJumpDialog"
      @jump="handleJumpToDraw"
      @close="showJumpDialog = false"
    />

    <!-- Bookmark Dialog -->
    <div 
      v-if="showBookmarkDialog" 
      class="bookmark-dialog-overlay" 
      @click="showBookmarkDialog = false"
      role="dialog"
      aria-modal="true"
      aria-labelledby="bookmark-dialog-title"
    >
      <div class="bookmark-dialog" @click.stop ref="bookmarkDialogRef">
        <h4 id="bookmark-dialog-title">Bookmark Draw #{{ currentDraw?.draw }}</h4>
        <form @submit.prevent="createBookmark">
          <div class="form-group">
            <label for="bookmark-label">Label:</label>
            <input 
              id="bookmark-label"
              ref="bookmarkLabelRef"
              v-model="bookmarkLabel" 
              type="text" 
              placeholder="Enter bookmark label"
              required
              maxlength="100"
              aria-describedby="bookmark-label-help"
            />
            <div id="bookmark-label-help" class="sr-only">
              Enter a descriptive label for this bookmark
            </div>
          </div>
          <div class="form-group">
            <label for="bookmark-description">Description (optional):</label>
            <textarea 
              id="bookmark-description"
              v-model="bookmarkDescription" 
              placeholder="Enter description"
              maxlength="500"
              aria-describedby="bookmark-desc-help"
            ></textarea>
            <div id="bookmark-desc-help" class="sr-only">
              Optional description for additional context about this bookmark
            </div>
          </div>
          <div class="dialog-actions">
            <button type="button" @click="closeBookmarkDialog" class="cancel-button">
              Cancel
            </button>
            <button type="submit" :disabled="!bookmarkLabel.trim()" class="save-button">
              Save Bookmark
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue'
import navigationService, { 
  type LottoDrawDto, 
  type NavigationContext as NavigationContextType,
  NavigationDirection 
} from '@/services/navigationService'
import NavigationContext from './NavigationContext.vue'
import JumpToDrawDialog from './JumpToDrawDialog.vue'
import accessibilityService from '@/services/accessibilityService'

interface Props {
  initialDrawNumber?: number
  showContext?: boolean
  enableKeyboardShortcuts?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  showContext: true,
  enableKeyboardShortcuts: true
})

const emit = defineEmits<{
  drawChanged: [draw: LottoDrawDto]
  navigationError: [error: string]
  bookmarkCreated: [bookmark: any]
}>()

// Reactive state
const currentDraw = ref<LottoDrawDto | null>(null)
const navigationContext = ref<NavigationContextType | null>(null)
const isLoading = ref(false)
const error = ref<string | null>(null)
const showJumpDialog = ref(false)
const showBookmarkDialog = ref(false)
const bookmarkLabel = ref('')
const bookmarkDescription = ref('')

// Template refs for focus management
const bookmarkDialogRef = ref<HTMLElement>()
const bookmarkLabelRef = ref<HTMLInputElement>()

// Focus trap cleanup function
let focusTrapCleanup: (() => void) | null = null

// Load initial draw
const loadDraw = async (drawNumber?: number) => {
  if (!drawNumber) return

  isLoading.value = true
  error.value = null

  try {
    const [draw, context] = await Promise.all([
      navigationService.getDrawByNumber(drawNumber),
      navigationService.getNavigationContext(drawNumber)
    ])

    currentDraw.value = draw
    navigationContext.value = context
    emit('drawChanged', draw)
  } catch (err: any) {
    const errorMessage = err.response?.data?.error || err.message || 'Failed to load draw'
    error.value = errorMessage
    emit('navigationError', errorMessage)
  } finally {
    isLoading.value = false
  }
}

// Navigation methods
const navigatePrevious = async () => {
  if (!currentDraw.value || isLoading.value) return

  isLoading.value = true
  error.value = null

  try {
    const previousDraw = await navigationService.getPreviousDraw(currentDraw.value.draw)
    const context = await navigationService.getNavigationContext(previousDraw.draw)
    
    currentDraw.value = previousDraw
    navigationContext.value = context
    
    // Announce navigation
    accessibilityService.announce(`Navigated to draw ${previousDraw.draw} from ${formatDate(previousDraw.date)}`)
    
    emit('drawChanged', previousDraw)
  } catch (err: any) {
    const errorMessage = err.response?.data?.error || err.message || 'Failed to navigate to previous draw'
    error.value = errorMessage
    accessibilityService.announce(`Navigation failed: ${errorMessage}`, 'assertive')
    emit('navigationError', errorMessage)
  } finally {
    isLoading.value = false
  }
}

const navigateNext = async () => {
  if (!currentDraw.value || isLoading.value) return

  isLoading.value = true
  error.value = null

  try {
    const nextDraw = await navigationService.getNextDraw(currentDraw.value.draw)
    const context = await navigationService.getNavigationContext(nextDraw.draw)
    
    currentDraw.value = nextDraw
    navigationContext.value = context
    
    // Announce navigation
    accessibilityService.announce(`Navigated to draw ${nextDraw.draw} from ${formatDate(nextDraw.date)}`)
    
    emit('drawChanged', nextDraw)
  } catch (err: any) {
    const errorMessage = err.response?.data?.error || err.message || 'Failed to navigate to next draw'
    error.value = errorMessage
    accessibilityService.announce(`Navigation failed: ${errorMessage}`, 'assertive')
    emit('navigationError', errorMessage)
  } finally {
    isLoading.value = false
  }
}

const handleJumpToDraw = async (request: { drawNumber?: number; date?: string }) => {
  showJumpDialog.value = false
  isLoading.value = true
  error.value = null

  try {
    const draw = await navigationService.jumpToDraw({
      ...request,
      findClosest: true
    })
    const context = await navigationService.getNavigationContext(draw.draw)
    
    currentDraw.value = draw
    navigationContext.value = context
    
    // Announce jump
    const jumpType = request.drawNumber ? `draw ${request.drawNumber}` : `date ${request.date}`
    accessibilityService.announce(`Jumped to ${jumpType}. Now viewing draw ${draw.draw} from ${formatDate(draw.date)}`)
    
    emit('drawChanged', draw)
  } catch (err: any) {
    const errorMessage = err.response?.data?.error || err.message || 'Failed to jump to draw'
    error.value = errorMessage
    accessibilityService.announce(`Jump failed: ${errorMessage}`, 'assertive')
    emit('navigationError', errorMessage)
  } finally {
    isLoading.value = false
  }
}

// Bookmark methods
const createBookmark = async () => {
  if (!currentDraw.value || !bookmarkLabel.value.trim()) return

  try {
    const bookmark = await navigationService.createBookmark({
      drawNumber: currentDraw.value.draw,
      label: bookmarkLabel.value.trim(),
      description: bookmarkDescription.value.trim() || undefined
    })

    closeBookmarkDialog()
    accessibilityService.announce(`Bookmark created for draw ${currentDraw.value.draw}`)
    emit('bookmarkCreated', bookmark)
  } catch (err: any) {
    const errorMessage = err.response?.data?.error || err.message || 'Failed to create bookmark'
    error.value = errorMessage
    accessibilityService.announce(`Bookmark creation failed: ${errorMessage}`, 'assertive')
    emit('navigationError', errorMessage)
  }
}

const closeBookmarkDialog = () => {
  showBookmarkDialog.value = false
  bookmarkLabel.value = ''
  bookmarkDescription.value = ''
  
  // Clean up focus trap
  if (focusTrapCleanup) {
    focusTrapCleanup()
    focusTrapCleanup = null
  }
  
  // Return focus to bookmark button
  accessibilityService.popFocus()
}

// Keyboard shortcuts
const handleKeydown = (event: KeyboardEvent) => {
  if (!props.enableKeyboardShortcuts || isLoading.value) return

  // Prevent shortcuts when typing in inputs
  if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
    return
  }

  switch (event.key) {
    case 'ArrowLeft':
      event.preventDefault()
      if (navigationContext.value?.hasPrevious) {
        navigatePrevious()
      }
      break
    case 'ArrowRight':
      event.preventDefault()
      if (navigationContext.value?.hasNext) {
        navigateNext()
      }
      break
    case 'PageUp':
      event.preventDefault()
      if (navigationContext.value?.hasPrevious) {
        navigatePrevious()
      }
      break
    case 'PageDown':
      event.preventDefault()
      if (navigationContext.value?.hasNext) {
        navigateNext()
      }
      break
    case 'j':
    case 'J':
      event.preventDefault()
      showJumpDialog.value = true
      break
    case 'b':
    case 'B':
      event.preventDefault()
      if (currentDraw.value) {
        openBookmarkDialog()
      }
      break
    case 'Escape':
      event.preventDefault()
      showJumpDialog.value = false
      closeBookmarkDialog()
      break
  }
}

// Utility methods
const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-NZ', {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

const clearError = () => {
  error.value = null
}

const openBookmarkDialog = () => {
  if (!currentDraw.value) return
  
  // Store current focus
  accessibilityService.pushFocus(document.activeElement as any)
  
  showBookmarkDialog.value = true
  
  // Set up focus trap and focus first input
  nextTick(() => {
    if (bookmarkDialogRef.value) {
      focusTrapCleanup = accessibilityService.trapFocus(bookmarkDialogRef.value)
    }
    if (bookmarkLabelRef.value) {
      bookmarkLabelRef.value.focus()
    }
  })
}

// Lifecycle
onMounted(() => {
  if (props.initialDrawNumber) {
    loadDraw(props.initialDrawNumber)
  }
  
  if (props.enableKeyboardShortcuts) {
    document.addEventListener('keydown', handleKeydown)
  }
})

onUnmounted(() => {
  if (props.enableKeyboardShortcuts) {
    document.removeEventListener('keydown', handleKeydown)
  }
})

// Watch for prop changes
watch(() => props.initialDrawNumber, (newDrawNumber) => {
  if (newDrawNumber && newDrawNumber !== currentDraw.value?.draw) {
    loadDraw(newDrawNumber)
  }
})

// Expose methods for parent components
defineExpose({
  loadDraw,
  navigatePrevious,
  navigateNext,
  jumpToDraw: handleJumpToDraw,
  getCurrentDraw: () => currentDraw.value,
  getNavigationContext: () => navigationContext.value,
  openBookmarkDialog,
  closeBookmarkDialog
})
</script>

<style scoped>
.draw-navigation {
  max-width: 800px;
  margin: 0 auto;
  padding: 1.5rem;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.navigation-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.navigation-header h3 {
  color: var(--color-heading);
  margin: 0;
  font-size: 1.5rem;
}

.navigation-actions {
  display: flex;
  gap: 0.75rem;
}

.jump-button,
.bookmark-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  padding: 0.5rem 1rem;
  border-radius: 6px;
  cursor: pointer;
  color: var(--color-text);
  font-size: 0.9rem;
  transition: all 0.3s ease;
}

.jump-button:hover:not(:disabled),
.bookmark-button:hover:not(:disabled) {
  background: var(--color-background-mute);
}

.jump-button:disabled,
.bookmark-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.current-draw {
  margin-bottom: 2rem;
  padding: 1.5rem;
  background: var(--color-background-soft);
  border-radius: 8px;
}

.draw-info {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.draw-number,
.draw-date {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 1rem;
  background: var(--color-background);
  border-radius: 6px;
}

.draw-info .label {
  font-size: 0.875rem;
  color: var(--color-text);
  margin-bottom: 0.5rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.draw-info .value {
  font-size: 1.25rem;
  font-weight: bold;
  color: var(--color-heading);
}

.winning-numbers {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: center;
}

.main-numbers {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  justify-content: center;
}

.number-ball {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 50%;
  font-weight: bold;
  font-size: 1rem;
  color: white;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.number-ball.main {
  background: linear-gradient(135deg, #42b883, #369870);
}

.number-ball.bonus {
  background: linear-gradient(135deg, #ffc107, #e0a800);
  width: 2rem;
  height: 2rem;
  font-size: 0.9rem;
}

.number-ball.powerball {
  background: linear-gradient(135deg, #dc3545, #c82333);
  width: 2rem;
  height: 2rem;
  font-size: 0.9rem;
}

.special-numbers {
  display: flex;
  gap: 1.5rem;
  align-items: center;
}

.bonus-number,
.powerball-number {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
}

.special-numbers .label {
  font-size: 0.75rem;
  color: var(--color-text);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.navigation-controls {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem;
  background: var(--color-background-soft);
  border-radius: 8px;
  margin-bottom: 1rem;
}

.nav-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: #42b883;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 1rem;
  font-weight: 500;
  transition: all 0.3s ease;
}

.nav-button:hover:not(:disabled) {
  background: #369870;
  transform: translateY(-1px);
}

.nav-button:disabled {
  background: var(--color-border);
  color: var(--color-text);
  cursor: not-allowed;
  transform: none;
}

.position-info {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.position {
  font-size: 1rem;
  font-weight: 500;
  color: var(--color-heading);
}

.loading-state,
.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 2rem;
  text-align: center;
}

.loading-spinner {
  width: 2rem;
  height: 2rem;
  border: 3px solid var(--color-border);
  border-top: 3px solid #42b883;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 1rem;
}

.error-icon {
  font-size: 2rem;
  margin-bottom: 1rem;
}

.error-state h4 {
  color: var(--color-heading);
  margin: 0 0 0.5rem 0;
}

.error-state p {
  color: var(--color-text);
  margin: 0 0 1rem 0;
}

.retry-button {
  background: #42b883;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.9rem;
}

.retry-button:hover {
  background: #369870;
}

.bookmark-dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.bookmark-dialog {
  background: var(--color-background);
  padding: 2rem;
  border-radius: 12px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
  max-width: 400px;
  width: 90%;
}

.bookmark-dialog h4 {
  color: var(--color-heading);
  margin: 0 0 1.5rem 0;
  text-align: center;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  color: var(--color-text);
  font-weight: 500;
}

.form-group input,
.form-group textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-background-soft);
  color: var(--color-text);
  font-size: 1rem;
}

.form-group textarea {
  resize: vertical;
  min-height: 80px;
}

.dialog-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 1.5rem;
}

.cancel-button,
.save-button {
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 1rem;
  border: none;
}

.cancel-button {
  background: var(--color-background-soft);
  color: var(--color-text);
  border: 1px solid var(--color-border);
}

.save-button {
  background: #42b883;
  color: white;
}

.save-button:disabled {
  background: var(--color-border);
  cursor: not-allowed;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

/* Responsive */
@media (max-width: 768px) {
  .draw-navigation {
    margin: 1rem;
    padding: 1rem;
  }
  
  .navigation-header {
    flex-direction: column;
    gap: 1rem;
    align-items: stretch;
  }
  
  .navigation-actions {
    justify-content: center;
  }
  
  .draw-info {
    grid-template-columns: 1fr;
  }
  
  .navigation-controls {
    flex-direction: column;
    gap: 1rem;
  }
  
  .nav-button {
    width: 100%;
    justify-content: center;
  }
  
  .main-numbers {
    gap: 0.5rem;
  }
  
  .number-ball {
    width: 2rem;
    height: 2rem;
    font-size: 0.9rem;
  }
  
  .special-numbers {
    gap: 1rem;
  }
}
</style>