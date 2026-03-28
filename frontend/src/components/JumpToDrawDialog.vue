<template>
  <div class="jump-dialog-overlay" @click="handleOverlayClick">
    <div class="jump-dialog" @click.stop>
      <div class="dialog-header">
        <h4>Jump to Draw</h4>
        <button @click="$emit('close')" class="close-button">
          <span class="icon">✕</span>
        </button>
      </div>

      <div class="dialog-content">
        <div class="jump-options">
          <div class="option-group">
            <label class="option-label">
              <input 
                type="radio" 
                v-model="jumpType" 
                value="drawNumber"
                @change="clearErrors"
              />
              <span class="option-text">Jump to Draw Number</span>
            </label>
            <div v-if="jumpType === 'drawNumber'" class="input-group">
              <input 
                ref="drawNumberInput"
                v-model.number="drawNumber" 
                type="number" 
                placeholder="Enter draw number (e.g., 1234)"
                min="1"
                max="9999"
                @keyup.enter="handleJump"
                @input="clearErrors"
                class="number-input"
              />
              <div v-if="drawNumberError" class="input-error">
                {{ drawNumberError }}
              </div>
            </div>
          </div>

          <div class="option-group">
            <label class="option-label">
              <input 
                type="radio" 
                v-model="jumpType" 
                value="date"
                @change="clearErrors"
              />
              <span class="option-text">Jump to Date</span>
            </label>
            <div v-if="jumpType === 'date'" class="input-group">
              <input 
                ref="dateInput"
                v-model="selectedDate" 
                type="date" 
                :min="minDate"
                :max="maxDate"
                @keyup.enter="handleJump"
                @input="clearErrors"
                class="date-input"
              />
              <div v-if="dateError" class="input-error">
                {{ dateError }}
              </div>
              <div class="date-help">
                System will find the closest available draw to this date
              </div>
            </div>
          </div>
        </div>

        <div v-if="recentDraws.length > 0" class="recent-draws">
          <h5>Recent Draws</h5>
          <div class="recent-list">
            <button 
              v-for="draw in recentDraws" 
              :key="draw.draw"
              @click="jumpToRecentDraw(draw)"
              class="recent-draw-button"
            >
              <div class="recent-draw-info">
                <span class="recent-draw-number">Draw #{{ draw.draw }}</span>
                <span class="recent-draw-date">{{ formatDate(draw.date) }}</span>
              </div>
            </button>
          </div>
        </div>

        <div v-if="suggestions.length > 0" class="suggestions">
          <h5>Suggestions</h5>
          <div class="suggestion-list">
            <button 
              v-for="suggestion in suggestions" 
              :key="suggestion.label"
              @click="applySuggestion(suggestion)"
              class="suggestion-button"
            >
              <span class="suggestion-label">{{ suggestion.label }}</span>
              <span class="suggestion-value">{{ suggestion.value }}</span>
            </button>
          </div>
        </div>
      </div>

      <div class="dialog-actions">
        <button @click="$emit('close')" class="cancel-button">
          Cancel
        </button>
        <button 
          @click="handleJump" 
          :disabled="!isValidInput || isLoading"
          class="jump-button"
        >
          <span v-if="isLoading" class="loading-spinner"></span>
          <span v-else class="icon">🎯</span>
          {{ isLoading ? 'Jumping...' : 'Jump' }}
        </button>
      </div>

      <div v-if="error" class="dialog-error">
        <div class="error-icon">❌</div>
        <p>{{ error }}</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue'
import navigationService, { type LottoDrawDto } from '@/services/navigationService'

interface Suggestion {
  label: string
  value: string | number
  type: 'drawNumber' | 'date'
}

const emit = defineEmits<{
  jump: [request: { drawNumber?: number; date?: string }]
  close: []
}>()

// Reactive state
const jumpType = ref<'drawNumber' | 'date'>('drawNumber')
const drawNumber = ref<number | null>(null)
const selectedDate = ref('')
const drawNumberError = ref('')
const dateError = ref('')
const error = ref('')
const isLoading = ref(false)
const recentDraws = ref<LottoDrawDto[]>([])

// Template refs
const drawNumberInput = ref<HTMLInputElement>()
const dateInput = ref<HTMLInputElement>()

// Date constraints
const minDate = '1987-08-01' // Approximate start of NZ Lotto
const maxDate = new Date().toISOString().split('T')[0] // Today

// Computed properties
const isValidInput = computed(() => {
  if (jumpType.value === 'drawNumber') {
    return drawNumber.value && drawNumber.value > 0 && drawNumber.value <= 9999
  } else {
    return selectedDate.value && selectedDate.value >= minDate && selectedDate.value <= maxDate
  }
})

const suggestions = computed<Suggestion[]>(() => {
  const today = new Date()
  const suggestions: Suggestion[] = []

  // Date suggestions
  suggestions.push({
    label: 'Today',
    value: today.toISOString().split('T')[0],
    type: 'date'
  })

  suggestions.push({
    label: 'Last Week',
    value: new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    type: 'date'
  })

  suggestions.push({
    label: 'Last Month',
    value: new Date(today.getFullYear(), today.getMonth() - 1, today.getDate()).toISOString().split('T')[0],
    type: 'date'
  })

  suggestions.push({
    label: 'New Year 2024',
    value: '2024-01-01',
    type: 'date'
  })

  // Draw number suggestions (if we have recent draws)
  if (recentDraws.value.length > 0) {
    const latestDraw = recentDraws.value[0]
    if (latestDraw.draw > 100) {
      suggestions.push({
        label: 'Draw 100',
        value: 100,
        type: 'drawNumber'
      })
    }
    if (latestDraw.draw > 1000) {
      suggestions.push({
        label: 'Draw 1000',
        value: 1000,
        type: 'drawNumber'
      })
    }
  }

  return suggestions
})

// Methods
const handleJump = async () => {
  clearErrors()

  if (!isValidInput.value) {
    if (jumpType.value === 'drawNumber') {
      drawNumberError.value = 'Please enter a valid draw number (1-9999)'
    } else {
      dateError.value = 'Please select a valid date'
    }
    return
  }

  isLoading.value = true
  error.value = ''

  try {
    const request: { drawNumber?: number; date?: string } = {}
    
    if (jumpType.value === 'drawNumber' && drawNumber.value) {
      request.drawNumber = drawNumber.value
    } else if (jumpType.value === 'date' && selectedDate.value) {
      request.date = selectedDate.value
    }

    emit('jump', request)
  } catch (err: any) {
    error.value = err.message || 'Failed to jump to draw'
  } finally {
    isLoading.value = false
  }
}

const jumpToRecentDraw = (draw: LottoDrawDto) => {
  emit('jump', { drawNumber: draw.draw })
}

const applySuggestion = (suggestion: Suggestion) => {
  jumpType.value = suggestion.type
  
  if (suggestion.type === 'drawNumber') {
    drawNumber.value = suggestion.value as number
    nextTick(() => {
      drawNumberInput.value?.focus()
    })
  } else {
    selectedDate.value = suggestion.value as string
    nextTick(() => {
      dateInput.value?.focus()
    })
  }
  
  clearErrors()
}

const clearErrors = () => {
  drawNumberError.value = ''
  dateError.value = ''
  error.value = ''
}

const handleOverlayClick = () => {
  emit('close')
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-NZ', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
}

const loadRecentDraws = async () => {
  try {
    // Get draws from the last 30 days
    const endDate = new Date().toISOString().split('T')[0]
    const startDate = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
    
    const draws = await navigationService.getDrawsInRange(startDate, endDate)
    recentDraws.value = draws.slice(0, 5) // Show only the 5 most recent
  } catch (err) {
    // Silently fail - recent draws are optional
    console.warn('Failed to load recent draws:', err)
  }
}

// Lifecycle
onMounted(async () => {
  // Focus the appropriate input
  await nextTick()
  if (jumpType.value === 'drawNumber') {
    drawNumberInput.value?.focus()
  } else {
    dateInput.value?.focus()
  }

  // Load recent draws
  loadRecentDraws()
})
</script>

<style scoped>
.jump-dialog-overlay {
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

.jump-dialog {
  background: var(--color-background);
  border-radius: 12px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
  max-width: 500px;
  width: 90%;
  max-height: 90vh;
  overflow-y: auto;
}

.dialog-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem 1.5rem 0 1.5rem;
  border-bottom: 1px solid var(--color-border);
  margin-bottom: 1.5rem;
}

.dialog-header h4 {
  color: var(--color-heading);
  margin: 0;
  font-size: 1.25rem;
}

.close-button {
  background: none;
  border: none;
  cursor: pointer;
  padding: 0.5rem;
  color: var(--color-text);
  font-size: 1.25rem;
  border-radius: 4px;
  transition: background-color 0.3s ease;
}

.close-button:hover {
  background: var(--color-background-soft);
}

.dialog-content {
  padding: 0 1.5rem;
}

.jump-options {
  margin-bottom: 2rem;
}

.option-group {
  margin-bottom: 1.5rem;
}

.option-label {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  cursor: pointer;
  margin-bottom: 1rem;
}

.option-label input[type="radio"] {
  margin: 0;
}

.option-text {
  font-weight: 500;
  color: var(--color-heading);
}

.input-group {
  margin-left: 2rem;
}

.number-input,
.date-input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-background-soft);
  color: var(--color-text);
  font-size: 1rem;
}

.number-input:focus,
.date-input:focus {
  outline: none;
  border-color: #42b883;
  box-shadow: 0 0 0 2px rgba(66, 184, 131, 0.2);
}

.input-error {
  color: #dc3545;
  font-size: 0.875rem;
  margin-top: 0.5rem;
}

.date-help {
  color: var(--color-text);
  font-size: 0.875rem;
  margin-top: 0.5rem;
  font-style: italic;
}

.recent-draws,
.suggestions {
  margin-bottom: 1.5rem;
}

.recent-draws h5,
.suggestions h5 {
  color: var(--color-heading);
  margin: 0 0 1rem 0;
  font-size: 1rem;
}

.recent-list,
.suggestion-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.recent-draw-button,
.suggestion-button {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem;
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 6px;
  cursor: pointer;
  color: var(--color-text);
  text-align: left;
  transition: all 0.3s ease;
}

.recent-draw-button:hover,
.suggestion-button:hover {
  background: var(--color-background-mute);
  border-color: #42b883;
}

.recent-draw-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.recent-draw-number {
  font-weight: 500;
  color: var(--color-heading);
}

.recent-draw-date {
  font-size: 0.875rem;
  color: var(--color-text);
}

.suggestion-label {
  font-weight: 500;
  color: var(--color-heading);
}

.suggestion-value {
  font-size: 0.875rem;
  color: var(--color-text);
}

.dialog-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  padding: 1.5rem;
  border-top: 1px solid var(--color-border);
  margin-top: 1.5rem;
}

.cancel-button,
.jump-button {
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 1rem;
  border: none;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.cancel-button {
  background: var(--color-background-soft);
  color: var(--color-text);
  border: 1px solid var(--color-border);
}

.cancel-button:hover {
  background: var(--color-background-mute);
}

.jump-button {
  background: #42b883;
  color: white;
}

.jump-button:hover:not(:disabled) {
  background: #369870;
}

.jump-button:disabled {
  background: var(--color-border);
  cursor: not-allowed;
}

.loading-spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid transparent;
  border-top: 2px solid currentColor;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

.dialog-error {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem 1.5rem;
  background: #fee;
  border-top: 1px solid #fcc;
  color: #c33;
}

.error-icon {
  font-size: 1.25rem;
}

.dialog-error p {
  margin: 0;
  font-size: 0.9rem;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

/* Responsive */
@media (max-width: 768px) {
  .jump-dialog {
    width: 95%;
    margin: 1rem;
  }
  
  .dialog-header,
  .dialog-content,
  .dialog-actions {
    padding-left: 1rem;
    padding-right: 1rem;
  }
  
  .input-group {
    margin-left: 1rem;
  }
  
  .dialog-actions {
    flex-direction: column;
  }
  
  .cancel-button,
  .jump-button {
    width: 100%;
    justify-content: center;
  }
}
</style>