<template>
  <div class="combination-search" role="main" aria-labelledby="combination-title">
    <!-- Skip Link -->
    <a href="#combination-results" class="skip-link">Skip to search results</a>
    
    <div class="search-header">
      <h2 id="combination-title">Combination Search</h2>
      <p>Search for number combinations to see if they have appeared together in historical draws</p>
    </div>

    <div class="search-form" role="search" aria-labelledby="combination-title">
      <div class="input-group">
        <label for="combination-input">Enter Combination (2-6 numbers)</label>
        <input
          id="combination-input"
          ref="combinationInputRef"
          v-model="combinationInput"
          type="text"
          placeholder="e.g., 1, 15, 23, 35, 40"
          class="combination-input"
          @input="handleInput"
          @keyup.enter="performSearch"
          :aria-invalid="validationErrors.length > 0"
          :aria-describedby="validationErrors.length > 0 ? 'combination-errors' : 'combination-help'"
          autocomplete="off"
        />
        <div id="combination-help" class="input-help">
          Enter 2-6 unique numbers separated by commas or spaces
        </div>
      </div>

      <div class="options-group">
        <fieldset class="search-options">
          <legend class="sr-only">Search Options</legend>
          <label class="checkbox-label">
            <input 
              v-model="includePartialMatches" 
              type="checkbox" 
              id="include-partial"
              aria-describedby="partial-help"
            />
            Include Partial Matches
          </label>
          <div id="partial-help" class="sr-only">
            Include draws that contain some but not all of the searched numbers
          </div>
          <div v-if="includePartialMatches" class="minimum-matches">
            <label for="min-matches">Minimum Matches:</label>
            <select 
              id="min-matches" 
              v-model="minimumMatches" 
              class="min-matches-select"
              aria-describedby="min-matches-help"
            >
              <option v-for="n in availableMinMatches" :key="n" :value="n">
                {{ n }} number{{ n !== 1 ? 's' : '' }}
              </option>
            </select>
            <div id="min-matches-help" class="sr-only">
              Minimum number of matching numbers required for partial matches
            </div>
          </div>
        </fieldset>

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
      </div>

      <div class="action-buttons">
        <button
          @click="performSearch"
          :disabled="!canSearch || isLoading"
          class="search-button"
          type="submit"
          :aria-describedby="!canSearch ? 'search-disabled-reason' : undefined"
        >
          <span v-if="isLoading" class="loading-spinner" aria-hidden="true"></span>
          {{ isLoading ? 'Searching...' : 'Search Combination' }}
        </button>
        <div v-if="!canSearch" id="search-disabled-reason" class="sr-only">
          Search is disabled because {{ validationErrors.length > 0 ? 'there are validation errors' : 'insufficient numbers have been entered' }}
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
      id="combination-errors"
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
      v-if="searchResult && hasSearched" 
      class="results-section" 
      id="combination-results"
      role="region" 
      aria-labelledby="combination-results-title"
    >
      <div class="results-header">
        <h3 id="combination-results-title">Search Results for {{ searchResult.searchedCombination.join(', ') }}</h3>
        <div class="results-summary" role="status" aria-live="polite">
          <div class="summary-item">
            <strong>{{ searchResult.totalExactMatches }}</strong> exact match{{ searchResult.totalExactMatches !== 1 ? 'es' : '' }}
          </div>
          <div v-if="includePartialMatches" class="summary-item">
            <strong>{{ searchResult.totalPartialMatches }}</strong> partial match{{ searchResult.totalPartialMatches !== 1 ? 'es' : '' }}
          </div>
        </div>
      </div>

      <!-- Exact Matches -->
      <div v-if="searchResult.exactMatches.length > 0" class="matches-section">
        <h4 class="matches-title">
          <span class="match-icon exact">🎯</span>
          Exact Matches ({{ searchResult.exactMatches.length }})
        </h4>
        <div class="matches-table-container">
          <table class="matches-table">
            <thead>
              <tr>
                <th>Draw #</th>
                <th>Date</th>
                <th>Winning Combination</th>
                <th>Matched Numbers</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="match in searchResult.exactMatches"
                :key="`exact-${match.drawNumber}`"
                class="match-row exact-match"
              >
                <td class="draw-number">{{ match.drawNumber }}</td>
                <td class="draw-date">{{ formatDate(match.drawDate) }}</td>
                <td class="combination">
                  <div class="combination-numbers">
                    <span
                      v-for="num in match.winningCombination"
                      :key="num"
                      class="combination-number"
                      :class="{ 'matched': match.matchedNumbers.includes(num) }"
                    >
                      {{ num }}
                    </span>
                  </div>
                </td>
                <td class="matched-numbers">
                  <div class="match-info">
                    <span class="match-count exact">{{ match.matchCount }}/{{ searchResult.searchedCombination.length }}</span>
                    <div class="matched-list">
                      {{ match.matchedNumbers.join(', ') }}
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Partial Matches -->
      <div v-if="includePartialMatches && searchResult.partialMatches.length > 0" class="matches-section">
        <h4 class="matches-title">
          <span class="match-icon partial">🎲</span>
          Partial Matches ({{ searchResult.partialMatches.length }})
        </h4>
        <div class="matches-table-container">
          <table class="matches-table">
            <thead>
              <tr>
                <th>Draw #</th>
                <th>Date</th>
                <th>Winning Combination</th>
                <th>Matched Numbers</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="match in paginatedPartialMatches"
                :key="`partial-${match.drawNumber}`"
                class="match-row partial-match"
              >
                <td class="draw-number">{{ match.drawNumber }}</td>
                <td class="draw-date">{{ formatDate(match.drawDate) }}</td>
                <td class="combination">
                  <div class="combination-numbers">
                    <span
                      v-for="num in match.winningCombination"
                      :key="num"
                      class="combination-number"
                      :class="{ 'matched': match.matchedNumbers.includes(num) }"
                    >
                      {{ num }}
                    </span>
                  </div>
                </td>
                <td class="matched-numbers">
                  <div class="match-info">
                    <span class="match-count partial">{{ match.matchCount }}/{{ searchResult.searchedCombination.length }}</span>
                    <div class="matched-list">
                      {{ match.matchedNumbers.join(', ') }}
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination for Partial Matches -->
        <div v-if="partialMatchPages > 1" class="pagination">
          <button
            @click="partialMatchPage = 1"
            :disabled="partialMatchPage === 1"
            class="page-button"
          >
            First
          </button>
          <button
            @click="partialMatchPage--"
            :disabled="partialMatchPage === 1"
            class="page-button"
          >
            Previous
          </button>
          <span class="page-info">
            Page {{ partialMatchPage }} of {{ partialMatchPages }}
          </span>
          <button
            @click="partialMatchPage++"
            :disabled="partialMatchPage === partialMatchPages"
            class="page-button"
          >
            Next
          </button>
          <button
            @click="partialMatchPage = partialMatchPages"
            :disabled="partialMatchPage === partialMatchPages"
            class="page-button"
          >
            Last
          </button>
        </div>
      </div>

      <!-- No Results within search results -->
      <div v-if="searchResult.totalExactMatches === 0 && (!includePartialMatches || searchResult.totalPartialMatches === 0)" class="no-results">
      <div class="no-results-icon">🔍</div>
      <h3>No Matches Found</h3>
      <p>
        The combination {{ searchedCombination.join(', ') }} has never appeared
        {{ includePartialMatches ? ` with ${minimumMatches} or more matching numbers` : ' as an exact match' }}
        {{ dateRangeText ? ` in the specified date range` : '' }}.
      </p>
      <div class="no-results-suggestions">
        <p>Try:</p>
        <ul>
          <li>Different number combinations</li>
          <li>Including partial matches</li>
          <li>Lowering the minimum match count</li>
          <li>Expanding the date range</li>
        </ul>
      </div>
    </div>
    </div>

    <!-- No Results (when no search has been performed) -->
    <div v-else-if="hasSearched && !isLoading && !searchResult" class="no-results">
      <div class="no-results-icon">🔍</div>
      <h3>Search Failed</h3>
      <p>Unable to perform search. Please try again.</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { LookupService, type CombinationSearchRequest, type CombinationSearchResult } from '@/services/lookupService'
import { useAppStore } from '@/stores/app'
import accessibilityService from '@/services/accessibilityService'

// Props
interface Props {
  maxCombinationSize?: number
  showPartialMatches?: boolean
  highlightMatches?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  maxCombinationSize: 6,
  showPartialMatches: false,
  highlightMatches: true
})

// Store
const appStore = useAppStore()

// Reactive state
const combinationInput = ref('')
const includePartialMatches = ref(props.showPartialMatches)
const minimumMatches = ref(2)
const startDate = ref('')
const endDate = ref('')
const isLoading = ref(false)
const hasSearched = ref(false)
const searchResult = ref<CombinationSearchResult | null>(null)
const searchedCombination = ref<number[]>([])
const validationErrors = ref<string[]>([])
const partialMatchPage = ref(1)
const itemsPerPage = 10

// Template refs
const combinationInputRef = ref<HTMLInputElement>()

// Keyboard navigation cleanup function
let keyboardCleanup: (() => void) | null = null

// Computed properties
const parsedCombination = computed(() => {
  return LookupService.parseNumberInput(combinationInput.value) || []
})

const availableMinMatches = computed(() => {
  const maxMatches = Math.min(parsedCombination.value.length - 1, 5)
  return Array.from({ length: maxMatches }, (_, i) => i + 2)
})

const canSearch = computed(() => {
  return parsedCombination.value.length >= 2 && validationErrors.value.length === 0
})

const partialMatchPages = computed(() => {
  if (!searchResult.value) return 0
  return Math.ceil(searchResult.value.partialMatches.length / itemsPerPage)
})

const paginatedPartialMatches = computed(() => {
  if (!searchResult.value) return []
  const start = (partialMatchPage.value - 1) * itemsPerPage
  const end = start + itemsPerPage
  return searchResult.value.partialMatches.slice(start, end)
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

// Watch for input changes
watch(combinationInput, () => {
  validateInput()
})

watch(parsedCombination, () => {
  // Adjust minimum matches if combination size changes
  if (minimumMatches.value >= parsedCombination.value.length) {
    minimumMatches.value = Math.max(2, parsedCombination.value.length - 1)
  }
})

// Methods
const handleInput = () => {
  validateInput()
}

const validateInput = () => {
  validationErrors.value = []
  
  if (combinationInput.value.trim() === '') {
    return
  }
  
  const combination = parsedCombination.value
  
  if (combination.length < 2) {
    validationErrors.value.push('Combination must contain at least 2 numbers')
    return
  }
  
  if (combination.length > props.maxCombinationSize) {
    validationErrors.value.push(`Maximum ${props.maxCombinationSize} numbers allowed`)
  }
  
  const validation = LookupService.validateCombination(combination)
  if (!validation.isValid) {
    validationErrors.value.push(...validation.errors)
  }
  
  // Date validation
  if (startDate.value && endDate.value && startDate.value > endDate.value) {
    validationErrors.value.push('Start date must be before end date')
  }
}

const performSearch = async () => {
  if (!canSearch.value || isLoading.value) return
  
  isLoading.value = true
  hasSearched.value = true
  searchedCombination.value = [...parsedCombination.value]
  partialMatchPage.value = 1
  
  // Announce search start
  accessibilityService.announce('Starting combination search')
  
  try {
    const request: CombinationSearchRequest = {
      combination: parsedCombination.value,
      includePartialMatches: includePartialMatches.value,
      minimumMatches: minimumMatches.value
    }
    
    if (startDate.value) {
      request.startDate = startDate.value
    }
    
    if (endDate.value) {
      request.endDate = endDate.value
    }
    
    searchResult.value = await LookupService.searchCombination(request)
    
    const totalMatches = searchResult.value.totalExactMatches + 
      (includePartialMatches.value ? searchResult.value.totalPartialMatches : 0)
    
    // Announce results
    const resultMessage = `Search complete. Found ${searchResult.value.totalExactMatches} exact matches and ${includePartialMatches.value ? searchResult.value.totalPartialMatches : 0} partial matches for combination ${searchedCombination.value.join(', ')}`
    accessibilityService.announce(resultMessage)
    
    appStore.addNotification({
      type: 'success',
      title: 'Search Complete',
      message: `Found ${totalMatches} match${totalMatches !== 1 ? 'es' : ''}`
    })

    // Focus results after search completes
    await nextTick()
    const resultsElement = document.getElementById('combination-results')
    if (resultsElement) {
      resultsElement.focus()
    }
  } catch (error) {
    console.error('Combination search error:', error)
    accessibilityService.announce('Search failed. Please try again.', 'assertive')
    appStore.addNotification({
      type: 'error',
      title: 'Search Failed',
      message: 'Failed to perform combination search. Please try again.'
    })
    searchResult.value = null
  } finally {
    isLoading.value = false
  }
}

const clearForm = () => {
  combinationInput.value = ''
  includePartialMatches.value = props.showPartialMatches
  minimumMatches.value = 2
  startDate.value = ''
  endDate.value = ''
  searchResult.value = null
  searchedCombination.value = []
  validationErrors.value = []
  hasSearched.value = false
  partialMatchPage.value = 1
  
  // Announce form cleared and focus input
  accessibilityService.announce('Form cleared')
  nextTick(() => {
    combinationInputRef.value?.focus()
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

// Keyboard navigation setup
const setupKeyboardNavigation = () => {
  const container = document.querySelector('.combination-search')
  if (!container) return

  const handleKeyDown = (event: KeyboardEvent) => {
    // Handle pagination keyboard shortcuts
    if (partialMatchPages.value > 1) {
      switch (event.key) {
        case 'PageUp':
          if (partialMatchPage.value > 1) {
            event.preventDefault()
            partialMatchPage.value--
            accessibilityService.announce(`Page ${partialMatchPage.value} of ${partialMatchPages.value}`)
          }
          break
        case 'PageDown':
          if (partialMatchPage.value < partialMatchPages.value) {
            event.preventDefault()
            partialMatchPage.value++
            accessibilityService.announce(`Page ${partialMatchPage.value} of ${partialMatchPages.value}`)
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
            performSearch()
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
    combinationInputRef.value?.focus()
  })
})

onUnmounted(() => {
  if (keyboardCleanup) {
    keyboardCleanup()
  }
})
</script>

<style scoped>
.combination-search {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.search-header {
  text-align: center;
  margin-bottom: 2rem;
}

.search-header h2 {
  color: var(--color-heading);
  margin-bottom: 0.5rem;
}

.search-header p {
  color: var(--color-text);
  font-size: 1.1rem;
}

.search-form {
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

.combination-input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-size: 1rem;
  background: var(--color-background);
  color: var(--color-text);
}

.combination-input:focus {
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

.search-options {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  color: var(--color-text);
  cursor: pointer;
}

.minimum-matches {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: var(--color-text);
}

.min-matches-select {
  padding: 0.25rem 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  background: var(--color-background);
  color: var(--color-text);
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
  margin: 0 0 1rem 0;
  color: var(--color-heading);
}

.results-summary {
  display: flex;
  gap: 2rem;
}

.summary-item {
  color: var(--color-text);
  font-size: 0.9rem;
}

.matches-section {
  border-bottom: 1px solid var(--color-border);
}

.matches-section:last-child {
  border-bottom: none;
}

.matches-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0;
  padding: 1rem 1.5rem;
  background: var(--color-background-soft);
  border-bottom: 1px solid var(--color-border);
  color: var(--color-heading);
  font-size: 1.1rem;
}

.match-icon {
  font-size: 1.2rem;
}

.matches-table-container {
  overflow-x: auto;
}

.matches-table {
  width: 100%;
  border-collapse: collapse;
}

.matches-table th {
  background: var(--color-background-soft);
  padding: 1rem;
  text-align: left;
  font-weight: 500;
  color: var(--color-heading);
  border-bottom: 1px solid var(--color-border);
}

.matches-table td {
  padding: 1rem;
  border-bottom: 1px solid var(--color-border);
}

.match-row:hover {
  background: var(--color-background-soft);
}

.exact-match {
  background: rgba(76, 175, 80, 0.05);
}

.partial-match {
  background: rgba(255, 193, 7, 0.05);
}

.draw-number {
  font-weight: 500;
  color: var(--color-heading);
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

.combination-number.matched {
  background: #e3f2fd;
  color: #1976d2;
  font-weight: 600;
}

.match-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.match-count {
  display: inline-block;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  font-size: 0.8rem;
  font-weight: 600;
}

.match-count.exact {
  background: #e8f5e8;
  color: #2e7d32;
}

.match-count.partial {
  background: #fff8e1;
  color: #f57c00;
}

.matched-list {
  font-size: 0.8rem;
  color: var(--color-text);
  font-weight: 500;
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  padding: 1.5rem;
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
  .combination-search {
    padding: 1rem;
  }
  
  .search-form {
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
  
  .results-summary {
    flex-direction: column;
    gap: 0.5rem;
  }
  
  .matches-table {
    font-size: 0.875rem;
  }
  
  .matches-table th,
  .matches-table td {
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