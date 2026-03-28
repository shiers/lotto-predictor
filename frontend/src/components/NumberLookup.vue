<template>
  <div class="number-lookup" role="main" aria-labelledby="lookup-title">
    <!-- Skip Link -->
    <a href="#search-results" class="skip-link">Skip to search results</a>
    
    <div class="lookup-header">
      <h2 id="lookup-title">Number Lookup</h2>
      <p>Search for specific lottery numbers to see their historical occurrences</p>
    </div>

    <div class="lookup-form" role="search" aria-labelledby="lookup-title">
      <div class="input-group">
        <label for="numbers-input">Enter Numbers (1-40)</label>
        <input
          id="numbers-input"
          ref="numbersInputRef"
          v-model="numbersInput"
          type="text"
          placeholder="e.g., 1, 15, 23, 35"
          class="numbers-input"
          @input="handleInput"
          @keyup.enter="performLookup"
          :aria-invalid="validationErrors.length > 0"
          :aria-describedby="validationErrors.length > 0 ? 'input-errors' : 'input-help'"
          autocomplete="off"
        />
        <div id="input-help" class="input-help">
          Enter single numbers or multiple numbers separated by commas or spaces
        </div>
      </div>

      <div class="options-group">
        <div class="date-filters">
          <div class="date-input">
            <label for="start-date">Start Date</label>
            <input
              id="start-date"
              v-model="startDate"
              type="date"
              class="date-input-field"
            />
          </div>
          <div class="date-input">
            <label for="end-date">End Date</label>
            <input
              id="end-date"
              v-model="endDate"
              type="date"
              class="date-input-field"
            />
          </div>
        </div>

        <fieldset class="checkbox-group">
          <legend class="sr-only">Number Types to Include</legend>
          <label class="checkbox-label">
            <input 
              v-model="includeBonus" 
              type="checkbox" 
              id="include-bonus"
              aria-describedby="bonus-help"
            />
            Include Bonus Numbers
          </label>
          <div id="bonus-help" class="sr-only">
            Include bonus numbers in the search results
          </div>
          <label class="checkbox-label">
            <input 
              v-model="includePowerball" 
              type="checkbox" 
              id="include-powerball"
              aria-describedby="powerball-help"
            />
            Include Powerball Numbers
          </label>
          <div id="powerball-help" class="sr-only">
            Include powerball numbers in the search results
          </div>
        </fieldset>
      </div>

      <div class="action-buttons">
        <button
          @click="performLookup"
          :disabled="!canSearch || isLoading"
          class="search-button"
          type="submit"
          :aria-describedby="!canSearch ? 'search-disabled-reason' : undefined"
        >
          <span v-if="isLoading" class="loading-spinner" aria-hidden="true"></span>
          {{ isLoading ? 'Searching...' : 'Search Numbers' }}
        </button>
        <div v-if="!canSearch" id="search-disabled-reason" class="sr-only">
          Search is disabled because {{ validationErrors.length > 0 ? 'there are validation errors' : 'no numbers have been entered' }}
        </div>
        <button
          @click="clearForm"
          :disabled="isLoading"
          class="clear-button"
          type="button"
          aria-describedby="clear-help"
        >
          Clear
        </button>
        <div id="clear-help" class="sr-only">
          Clear all form fields and reset the search
        </div>
      </div>
    </div>

    <!-- Validation Errors -->
    <div 
      v-if="validationErrors.length > 0" 
      class="error-messages" 
      role="alert" 
      aria-live="polite"
      id="input-errors"
    >
      <div class="error-header">
        <span class="error-icon" aria-hidden="true">⚠️</span>
        Please fix the following errors:
      </div>
      <ul class="error-list">
        <li v-for="error in validationErrors" :key="error" class="error-item">
          {{ error }}
        </li>
      </ul>
    </div>

    <!-- Search Results -->
    <div 
      v-if="searchResults.length > 0" 
      class="results-section" 
      id="search-results"
      role="region" 
      aria-labelledby="results-title"
    >
      <div class="results-header">
        <h3 id="results-title">Search Results</h3>
        <div class="results-summary" role="status" aria-live="polite">
          Found {{ searchResults.length }} occurrence{{ searchResults.length !== 1 ? 's' : '' }}
          for {{ searchedNumbers.join(', ') }}
        </div>
      </div>

      <div class="results-table-container">
        <table class="results-table accessible-table" role="table" aria-labelledby="results-title">
          <caption class="sr-only">
            Search results showing {{ searchResults.length }} occurrences of numbers {{ searchedNumbers.join(', ') }}
          </caption>
          <thead>
            <tr>
              <th scope="col">Draw #</th>
              <th scope="col">Date</th>
              <th scope="col">Number</th>
              <th scope="col">Position</th>
              <th scope="col">Type</th>
              <th scope="col">Full Combination</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="result in paginatedResults"
              :key="`${result.drawNumber}-${result.number}-${result.position}`"
              class="result-row"
            >
              <td class="draw-number">{{ result.drawNumber }}</td>
              <td class="draw-date">{{ formatDate(result.drawDate) }}</td>
              <td class="number-cell">
                <span class="number-badge" :class="getNumberClass(result)">
                  {{ result.number }}
                </span>
              </td>
              <td class="position">{{ result.position }}</td>
              <td class="type">
                <span class="type-badge" :class="getTypeClass(result)">
                  {{ getNumberType(result) }}
                </span>
              </td>
              <td class="combination">
                <div class="combination-numbers">
                  <span
                    v-for="num in result.fullCombination"
                    :key="num"
                    class="combination-number"
                    :class="{ 'highlighted': searchedNumbers.includes(num) }"
                  >
                    {{ num }}
                  </span>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <nav v-if="totalPages > 1" class="pagination" role="navigation" aria-label="Search results pagination">
        <button
          @click="currentPage = 1"
          :disabled="currentPage === 1"
          class="page-button"
          aria-label="Go to first page"
        >
          First
        </button>
        <button
          @click="currentPage--"
          :disabled="currentPage === 1"
          class="page-button"
          aria-label="Go to previous page"
        >
          Previous
        </button>
        <span class="page-info" role="status" aria-live="polite">
          Page {{ currentPage }} of {{ totalPages }}
        </span>
        <button
          @click="currentPage++"
          :disabled="currentPage === totalPages"
          class="page-button"
          aria-label="Go to next page"
        >
          Next
        </button>
        <button
          @click="currentPage = totalPages"
          :disabled="currentPage === totalPages"
          class="page-button"
          aria-label="Go to last page"
        >
          Last
        </button>
      </nav>
    </div>

    <!-- No Results -->
    <div 
      v-else-if="hasSearched && !isLoading" 
      class="no-results" 
      role="region" 
      aria-labelledby="no-results-title"
      id="search-results"
    >
      <div class="no-results-icon" aria-hidden="true">🔍</div>
      <h3 id="no-results-title">No Results Found</h3>
      <p>
        No occurrences found for {{ searchedNumbers.join(', ') }}
        {{ dateRangeText ? ` in the specified date range` : '' }}.
      </p>
      <div class="no-results-suggestions">
        <p>Try:</p>
        <ul>
          <li>Checking different numbers</li>
          <li>Expanding the date range</li>
          <li>Including bonus or powerball numbers</li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { LookupService, type NumberOccurrence, type NumberLookupRequest } from '@/services/lookupService'
import { useAppStore } from '@/stores/app'
import accessibilityService from '@/services/accessibilityService'

// Props
interface Props {
  initialNumbers?: number[]
  maxNumbers?: number
  showFrequencyData?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  initialNumbers: () => [],
  maxNumbers: 6,
  showFrequencyData: false
})

// Store
const appStore = useAppStore()

// Reactive state
const numbersInput = ref('')
const startDate = ref('')
const endDate = ref('')
const includeBonus = ref(true)
const includePowerball = ref(true)
const isLoading = ref(false)
const hasSearched = ref(false)
const searchResults = ref<NumberOccurrence[]>([])
const searchedNumbers = ref<number[]>([])
const validationErrors = ref<string[]>([])
const currentPage = ref(1)
const itemsPerPage = 20

// Template refs
const numbersInputRef = ref<HTMLInputElement>()

// Keyboard navigation cleanup function
let keyboardCleanup: (() => void) | null = null

// Initialize with props
if (props.initialNumbers.length > 0) {
  numbersInput.value = props.initialNumbers.join(', ')
}

// Computed properties
const parsedNumbers = computed(() => {
  return LookupService.parseNumberInput(numbersInput.value) || []
})

const canSearch = computed(() => {
  return parsedNumbers.value.length > 0 && validationErrors.value.length === 0
})

const totalPages = computed(() => {
  return Math.ceil(searchResults.value.length / itemsPerPage)
})

const paginatedResults = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage
  const end = start + itemsPerPage
  return searchResults.value.slice(start, end)
})

const dateRangeText = computed(() => {
  if (startDate.value && endDate.value) {
    return `${formatDate(startDate.value)} to ${formatDate(endDate.value)}`
  } else if (startDate.value) {
    return `from ${formatDate(startDate.value)}`
  } else if (endDate.value) {
    return `until ${formatDate(endDate.value)}`
  }
  return ''
})

// Watch for input changes to validate
watch(numbersInput, () => {
  validateInput()
})

// Methods
const handleInput = () => {
  validateInput()
}

const validateInput = () => {
  validationErrors.value = []
  
  if (numbersInput.value.trim() === '') {
    return
  }
  
  const numbers = parsedNumbers.value
  
  if (numbers.length === 0) {
    validationErrors.value.push('Please enter valid numbers')
    return
  }
  
  if (numbers.length > props.maxNumbers) {
    validationErrors.value.push(`Maximum ${props.maxNumbers} numbers allowed`)
  }
  
  const validation = LookupService.validateCombination(numbers)
  if (!validation.isValid) {
    validationErrors.value.push(...validation.errors)
  }
  
  // Date validation
  if (startDate.value && endDate.value && startDate.value > endDate.value) {
    validationErrors.value.push('Start date must be before end date')
  }
}

const performLookup = async () => {
  if (!canSearch.value || isLoading.value) return
  
  isLoading.value = true
  hasSearched.value = true
  searchedNumbers.value = [...parsedNumbers.value]
  currentPage.value = 1
  
  // Announce search start
  accessibilityService.announce('Starting number lookup search')
  
  try {
    const request: NumberLookupRequest = {
      numbers: parsedNumbers.value,
      includeBonus: includeBonus.value,
      includePowerball: includePowerball.value
    }
    
    if (startDate.value) {
      request.startDate = startDate.value
    }
    
    if (endDate.value) {
      request.endDate = endDate.value
    }
    
    searchResults.value = await LookupService.lookupNumbers(request)
    
    // Announce results
    const resultMessage = `Search complete. Found ${searchResults.value.length} occurrence${searchResults.value.length !== 1 ? 's' : ''} for numbers ${searchedNumbers.value.join(', ')}`
    accessibilityService.announce(resultMessage)
    
    appStore.addNotification({
      type: 'success',
      title: 'Search Complete',
      message: `Found ${searchResults.value.length} occurrence${searchResults.value.length !== 1 ? 's' : ''}`
    })

    // Focus results after search completes
    await nextTick()
    const resultsElement = document.getElementById('search-results')
    if (resultsElement) {
      resultsElement.focus()
    }
  } catch (error) {
    console.error('Lookup error:', error)
    accessibilityService.announce('Search failed. Please try again.', 'assertive')
    appStore.addNotification({
      type: 'error',
      title: 'Search Failed',
      message: 'Failed to perform number lookup. Please try again.'
    })
    searchResults.value = []
  } finally {
    isLoading.value = false
  }
}

const clearForm = () => {
  numbersInput.value = ''
  startDate.value = ''
  endDate.value = ''
  includeBonus.value = true
  includePowerball.value = true
  searchResults.value = []
  searchedNumbers.value = []
  validationErrors.value = []
  hasSearched.value = false
  currentPage.value = 1
  
  // Announce form cleared and focus input
  accessibilityService.announce('Form cleared')
  nextTick(() => {
    numbersInputRef.value?.focus()
  })
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-NZ', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

const getNumberClass = (result: NumberOccurrence): string => {
  if (result.isPowerball) return 'powerball'
  if (result.isBonus) return 'bonus'
  return 'main'
}

const getTypeClass = (result: NumberOccurrence): string => {
  if (result.isPowerball) return 'type-powerball'
  if (result.isBonus) return 'type-bonus'
  return 'type-main'
}

const getNumberType = (result: NumberOccurrence): string => {
  if (result.isPowerball) return 'Powerball'
  if (result.isBonus) return 'Bonus'
  return 'Main'
}

// Keyboard navigation setup
const setupKeyboardNavigation = () => {
  const container = document.querySelector('.number-lookup')
  if (!container) return

  const handleKeyDown = (event: KeyboardEvent) => {
    // Handle pagination keyboard shortcuts
    if (totalPages.value > 1) {
      switch (event.key) {
        case 'PageUp':
          if (currentPage.value > 1) {
            event.preventDefault()
            currentPage.value--
            accessibilityService.announce(`Page ${currentPage.value} of ${totalPages.value}`)
          }
          break
        case 'PageDown':
          if (currentPage.value < totalPages.value) {
            event.preventDefault()
            currentPage.value++
            accessibilityService.announce(`Page ${currentPage.value} of ${totalPages.value}`)
          }
          break
      }
    }

    // Handle form shortcuts
    if (event.ctrlKey || event.metaKey) {
      switch (event.key) {
        case 'Enter':
          event.preventDefault()
          if (canSearch.value && !isLoading.value) {
            performLookup()
          }
          break
        case 'Backspace':
          event.preventDefault()
          clearForm()
          break
      }
    }
  }

  container.addEventListener('keydown', handleKeyDown)
  
  return () => {
    container.removeEventListener('keydown', handleKeyDown)
  }
}

// Lifecycle hooks
onMounted(() => {
  keyboardCleanup = setupKeyboardNavigation()
  
  // Focus the input field on mount
  nextTick(() => {
    numbersInputRef.value?.focus()
  })
})

onUnmounted(() => {
  if (keyboardCleanup) {
    keyboardCleanup()
  }
})

// Expose methods for testing
defineExpose({
  performLookup,
  clearForm,
  keyboardCleanup: () => keyboardCleanup
})
</script>

<style scoped>
.number-lookup {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.lookup-header {
  text-align: center;
  margin-bottom: 2rem;
}

.lookup-header h2 {
  color: var(--color-heading);
  margin-bottom: 0.5rem;
}

.lookup-header p {
  color: var(--color-text);
  font-size: 1.1rem;
}

.lookup-form {
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  padding: 2rem;
  margin-bottom: 2rem;
}

.input-group {
  margin-bottom: 1.5rem;
}

.input-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: var(--color-heading);
}

.numbers-input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-size: 1rem;
  background: var(--color-background);
  color: var(--color-text);
}

.numbers-input:focus {
  outline: none;
  border-color: var(--color-border-hover);
  box-shadow: 0 0 0 3px rgba(0, 123, 255, 0.1);
}

.input-help {
  font-size: 0.875rem;
  color: var(--color-text);
  margin-top: 0.25rem;
  opacity: 0.8;
}

.options-group {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
  margin-bottom: 1.5rem;
}

.date-filters {
  display: flex;
  gap: 1rem;
}

.date-input {
  flex: 1;
}

.date-input label {
  display: block;
  margin-bottom: 0.25rem;
  font-size: 0.875rem;
  color: var(--color-heading);
}

.date-input-field {
  width: 100%;
  padding: 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-background);
  color: var(--color-text);
}

.checkbox-group {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  color: var(--color-text);
  cursor: pointer;
}

.checkbox-label input[type="checkbox"] {
  margin: 0;
}

.action-buttons {
  display: flex;
  gap: 1rem;
}

.search-button {
  background: var(--color-border-hover);
  color: white;
  border: none;
  padding: 0.75rem 2rem;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: background-color 0.3s ease;
}

.search-button:hover:not(:disabled) {
  background: var(--color-border);
}

.search-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.clear-button {
  background: transparent;
  color: var(--color-text);
  border: 1px solid var(--color-border);
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  font-size: 1rem;
  cursor: pointer;
  transition: all 0.3s ease;
}

.clear-button:hover:not(:disabled) {
  background: var(--color-background-soft);
}

.loading-spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid transparent;
  border-top: 2px solid currentColor;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.error-messages {
  background: #fee;
  border: 1px solid #fcc;
  border-radius: 8px;
  padding: 1rem;
  margin-bottom: 2rem;
}

.error-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 500;
  color: #c33;
  margin-bottom: 0.5rem;
}

.error-list {
  margin: 0;
  padding-left: 1.5rem;
}

.error-item {
  color: #c33;
  margin-bottom: 0.25rem;
}

.results-section {
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  overflow: hidden;
}

.results-header {
  padding: 1.5rem;
  border-bottom: 1px solid var(--color-border);
  background: var(--color-background-soft);
}

.results-header h3 {
  margin: 0 0 0.5rem 0;
  color: var(--color-heading);
}

.results-summary {
  color: var(--color-text);
  font-size: 0.9rem;
}

.results-table-container {
  overflow-x: auto;
}

.results-table {
  width: 100%;
  border-collapse: collapse;
}

.results-table th {
  background: var(--color-background-soft);
  padding: 1rem;
  text-align: left;
  font-weight: 500;
  color: var(--color-heading);
  border-bottom: 1px solid var(--color-border);
}

.results-table td {
  padding: 1rem;
  border-bottom: 1px solid var(--color-border);
}

.result-row:hover {
  background: var(--color-background-soft);
}

.draw-number {
  font-weight: 500;
  color: var(--color-heading);
}

.number-badge {
  display: inline-block;
  padding: 0.25rem 0.5rem;
  border-radius: 6px;
  font-weight: 500;
  font-size: 0.9rem;
}

.number-badge.main {
  background: #e3f2fd;
  color: #1976d2;
}

.number-badge.bonus {
  background: #fff3e0;
  color: #f57c00;
}

.number-badge.powerball {
  background: #fce4ec;
  color: #c2185b;
}

.type-badge {
  display: inline-block;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  font-size: 0.8rem;
  font-weight: 500;
}

.type-badge.type-main {
  background: #e8f5e8;
  color: #2e7d32;
}

.type-badge.type-bonus {
  background: #fff8e1;
  color: #f57c00;
}

.type-badge.type-powerball {
  background: #fce4ec;
  color: #c2185b;
}

.combination-numbers {
  display: flex;
  gap: 0.25rem;
  flex-wrap: wrap;
}

.combination-number {
  display: inline-block;
  padding: 0.2rem 0.4rem;
  background: var(--color-background-soft);
  border-radius: 4px;
  font-size: 0.8rem;
  font-weight: 500;
}

.combination-number.highlighted {
  background: #e3f2fd;
  color: #1976d2;
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  padding: 1.5rem;
  border-top: 1px solid var(--color-border);
}

.page-button {
  padding: 0.5rem 1rem;
  border: 1px solid var(--color-border);
  background: var(--color-background);
  color: var(--color-text);
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.3s ease;
}

.page-button:hover:not(:disabled) {
  background: var(--color-background-soft);
}

.page-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.page-info {
  color: var(--color-text);
  font-size: 0.9rem;
}

.no-results {
  text-align: center;
  padding: 3rem 2rem;
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 12px;
}

.no-results-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}

.no-results h3 {
  color: var(--color-heading);
  margin-bottom: 1rem;
}

.no-results p {
  color: var(--color-text);
  margin-bottom: 1.5rem;
}

.no-results-suggestions {
  text-align: left;
  max-width: 300px;
  margin: 0 auto;
}

.no-results-suggestions p {
  margin-bottom: 0.5rem;
  font-weight: 500;
}

.no-results-suggestions ul {
  margin: 0;
  padding-left: 1.5rem;
}

.no-results-suggestions li {
  margin-bottom: 0.25rem;
  color: var(--color-text);
}

/* Responsive */
@media (max-width: 768px) {
  .number-lookup {
    padding: 1rem;
  }
  
  .lookup-form {
    padding: 1.5rem;
  }
  
  .options-group {
    grid-template-columns: 1fr;
    gap: 1rem;
  }
  
  .date-filters {
    flex-direction: column;
  }
  
  .action-buttons {
    flex-direction: column;
  }
  
  .results-table {
    font-size: 0.875rem;
  }
  
  .results-table th,
  .results-table td {
    padding: 0.75rem 0.5rem;
  }
  
  .combination-numbers {
    flex-direction: column;
    gap: 0.125rem;
  }
  
  .pagination {
    flex-wrap: wrap;
    gap: 0.5rem;
  }
}
</style>