<template>
  <div class="autocomplete-container" ref="containerRef">
    <div class="autocomplete-input-wrapper">
      <input
        ref="inputRef"
        v-model="inputValue"
        :type="inputType"
        :placeholder="placeholder"
        :disabled="disabled"
        class="autocomplete-input"
        :class="{
          'has-suggestions': showSuggestions && suggestions.length > 0,
          'is-loading': isLoading,
          'has-error': hasError
        }"
        @input="handleInput"
        @focus="handleFocus"
        @blur="handleBlur"
        @keydown="handleKeyDown"
        :aria-expanded="showSuggestions"
        :aria-haspopup="true"
        :aria-autocomplete="'list'"
        :aria-describedby="errorId"
      />
      
      <div class="autocomplete-icons">
        <div v-if="isLoading" class="loading-spinner"></div>
        <button
          v-else-if="inputValue && clearable"
          @click="clearInput"
          class="clear-button"
          type="button"
          aria-label="Clear input"
        >
          ✕
        </button>
        <div v-if="searchType" class="search-type-indicator" :class="`type-${searchType}`">
          {{ getTypeIcon(searchType) }}
        </div>
      </div>
    </div>
    
    <div
      v-if="showSuggestions && suggestions.length > 0"
      class="autocomplete-dropdown"
      :class="{ 'dropdown-above': dropdownPosition === 'above' }"
    >
      <SearchSuggestions
        ref="suggestionsRef"
        :suggestions="suggestions"
        :show-header="showSuggestionsHeader"
        :show-preview="showPreview"
        :show-metadata="showMetadata"
        :show-relevance="showRelevance"
        :has-more-suggestions="hasMoreSuggestions"
        :is-loading="isLoadingMore"
        :max-height="suggestionsMaxHeight"
        @select="handleSuggestionSelect"
        @highlight="handleSuggestionHighlight"
        @load-more="handleLoadMore"
        @keydown="handleSuggestionsKeyDown"
      />
    </div>
    
    <div v-if="hasError && errorMessage" :id="errorId" class="autocomplete-error">
      {{ errorMessage }}
    </div>
    
    <div v-if="showHint && hintText" class="autocomplete-hint">
      {{ hintText }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from 'vue'
import SearchSuggestions from './SearchSuggestions.vue'
import { LookupService, type SearchSuggestion, type AutoCompleteRequest } from '@/services/lookupService'

// Props
interface Props {
  modelValue: string
  searchType: 'number' | 'combination' | 'range' | 'date' | 'drawNumber'
  placeholder?: string
  disabled?: boolean
  clearable?: boolean
  inputType?: string
  minLength?: number
  maxSuggestions?: number
  debounceMs?: number
  showPreview?: boolean
  showMetadata?: boolean
  showRelevance?: boolean
  showSuggestionsHeader?: boolean
  showHint?: boolean
  hintText?: string
  suggestionsMaxHeight?: string
  errorMessage?: string
  cacheResults?: boolean
  validateInput?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  placeholder: 'Start typing...',
  disabled: false,
  clearable: true,
  inputType: 'text',
  minLength: 1,
  maxSuggestions: 10,
  debounceMs: 300,
  showPreview: true,
  showMetadata: false,
  showRelevance: false,
  showSuggestionsHeader: false,
  showHint: true,
  hintText: '',
  suggestionsMaxHeight: '300px',
  errorMessage: '',
  cacheResults: true,
  validateInput: true
})

// Emits
const emit = defineEmits<{
  'update:modelValue': [value: string]
  select: [suggestion: SearchSuggestion]
  search: [query: string]
  focus: [event: FocusEvent]
  blur: [event: FocusEvent]
  error: [error: string]
  clear: []
}>()

// Refs
const containerRef = ref<HTMLElement>()
const inputRef = ref<HTMLInputElement>()
const suggestionsRef = ref<InstanceType<typeof SearchSuggestions>>()

// Reactive state
const inputValue = ref(props.modelValue)
const suggestions = ref<SearchSuggestion[]>([])
const isLoading = ref(false)
const isLoadingMore = ref(false)
const showSuggestions = ref(false)
const isFocused = ref(false)
const hasMoreSuggestions = ref(false)
const dropdownPosition = ref<'below' | 'above'>('below')
const debounceTimer = ref<number>()
const suggestionCache = ref<Map<string, SearchSuggestion[]>>(new Map())

// Computed properties
const hasError = computed(() => !!props.errorMessage)
const errorId = computed(() => `autocomplete-error-${Math.random().toString(36).substr(2, 9)}`)

// Watch for model value changes
watch(() => props.modelValue, (newValue) => {
  if (newValue !== inputValue.value) {
    inputValue.value = newValue
  }
})

// Watch for input value changes
watch(inputValue, (newValue) => {
  emit('update:modelValue', newValue)
  debouncedSearch(newValue)
})

// Methods
const handleInput = (event: Event) => {
  const target = event.target as HTMLInputElement
  inputValue.value = target.value
  
  if (props.validateInput) {
    validateCurrentInput()
  }
}

const handleFocus = (event: FocusEvent) => {
  isFocused.value = true
  emit('focus', event)
  
  if (inputValue.value.length >= props.minLength) {
    showSuggestions.value = true
    if (suggestions.value.length === 0) {
      performSearch(inputValue.value)
    }
  }
}

const handleBlur = (event: FocusEvent) => {
  // Delay hiding suggestions to allow for suggestion clicks
  setTimeout(() => {
    isFocused.value = false
    showSuggestions.value = false
    emit('blur', event)
  }, 150)
}

const handleKeyDown = (event: KeyboardEvent) => {
  if (!showSuggestions.value || suggestions.value.length === 0) {
    return
  }
  
  // Let SearchSuggestions handle navigation keys
  if (['ArrowDown', 'ArrowUp', 'Enter', 'Escape'].includes(event.key)) {
    event.preventDefault()
    suggestionsRef.value?.handleKeyDown?.(event)
  }
}

const handleSuggestionSelect = (suggestion: SearchSuggestion) => {
  inputValue.value = suggestion.text
  showSuggestions.value = false
  emit('select', suggestion)
  
  // Focus back to input
  nextTick(() => {
    inputRef.value?.focus()
  })
}

const handleSuggestionHighlight = (suggestion: SearchSuggestion | null) => {
  // Could be used for preview or other functionality
}

const handleLoadMore = () => {
  // Implementation for loading more suggestions
  isLoadingMore.value = true
  // This would typically make another API call with offset/pagination
  setTimeout(() => {
    isLoadingMore.value = false
  }, 1000)
}

const handleSuggestionsKeyDown = (event: KeyboardEvent) => {
  // Handle any additional key events from suggestions
}

const debouncedSearch = (query: string) => {
  if (debounceTimer.value) {
    clearTimeout(debounceTimer.value)
  }
  
  debounceTimer.value = setTimeout(() => {
    if (query.length >= props.minLength && isFocused.value) {
      performSearch(query)
    } else {
      suggestions.value = []
      showSuggestions.value = false
    }
  }, props.debounceMs)
}

const performSearch = async (query: string) => {
  if (!query || query.length < props.minLength) {
    suggestions.value = []
    showSuggestions.value = false
    return
  }
  
  // Check cache first
  if (props.cacheResults && suggestionCache.value.has(query)) {
    suggestions.value = suggestionCache.value.get(query) || []
    showSuggestions.value = suggestions.value.length > 0
    return
  }
  
  isLoading.value = true
  
  try {
    const request: AutoCompleteRequest = {
      query,
      type: props.searchType,
      maxSuggestions: props.maxSuggestions,
      includePreview: props.showPreview
    }
    
    const results = await LookupService.getSearchSuggestions(request)
    suggestions.value = results
    
    // Cache results
    if (props.cacheResults) {
      suggestionCache.value.set(query, results)
    }
    
    showSuggestions.value = results.length > 0 && isFocused.value
    hasMoreSuggestions.value = results.length === props.maxSuggestions
    
    // Calculate dropdown position
    calculateDropdownPosition()
    
    emit('search', query)
  } catch (error) {
    console.error('Autocomplete search error:', error)
    suggestions.value = []
    showSuggestions.value = false
    emit('error', 'Failed to load suggestions')
  } finally {
    isLoading.value = false
  }
}

const validateCurrentInput = () => {
  if (!inputValue.value) return
  
  switch (props.searchType) {
    case 'number':
      validateNumber()
      break
    case 'combination':
      validateCombination()
      break
    case 'range':
      validateRange()
      break
    case 'drawNumber':
      validateDrawNumber()
      break
    case 'date':
      validateDate()
      break
  }
}

const validateNumber = () => {
  const numbers = LookupService.parseNumberInput(inputValue.value)
  if (numbers && numbers.length > 0) {
    const invalidNumbers = numbers.filter(n => !LookupService.validateNumber(n))
    if (invalidNumbers.length > 0) {
      emit('error', `Invalid numbers: ${invalidNumbers.join(', ')}. Numbers must be between 1 and 40`)
    }
  }
}

const validateCombination = () => {
  const numbers = LookupService.parseNumberInput(inputValue.value)
  if (numbers && numbers.length > 0) {
    const validation = LookupService.validateCombination(numbers)
    if (!validation.isValid) {
      emit('error', validation.errors[0])
    }
  }
}

const validateRange = () => {
  if (inputValue.value.includes('-')) {
    const parts = inputValue.value.split('-')
    if (parts.length === 2) {
      const start = parseInt(parts[0].trim())
      const end = parseInt(parts[1].trim())
      if (isNaN(start) || isNaN(end) || start < 1 || end > 40 || start > end) {
        emit('error', 'Invalid range. Use format: 1-10 (numbers between 1 and 40)')
      }
    }
  }
}

const validateDrawNumber = () => {
  const drawNumber = parseInt(inputValue.value)
  if (!isNaN(drawNumber) && drawNumber < 1) {
    emit('error', 'Draw number must be positive')
  }
}

const validateDate = () => {
  if (inputValue.value && !Date.parse(inputValue.value)) {
    emit('error', 'Invalid date format')
  }
}

const calculateDropdownPosition = () => {
  if (!containerRef.value) return
  
  const rect = containerRef.value.getBoundingClientRect()
  const viewportHeight = window.innerHeight
  const spaceBelow = viewportHeight - rect.bottom
  const spaceAbove = rect.top
  
  // Show above if there's more space above and not enough below
  dropdownPosition.value = spaceBelow < 200 && spaceAbove > spaceBelow ? 'above' : 'below'
}

const clearInput = () => {
  inputValue.value = ''
  suggestions.value = []
  showSuggestions.value = false
  emit('clear')
  
  nextTick(() => {
    inputRef.value?.focus()
  })
}

const getTypeIcon = (type: string): string => {
  const icons: Record<string, string> = {
    number: '🔢',
    combination: '🎯',
    range: '📊',
    date: '📅',
    drawNumber: '🎲'
  }
  return icons[type] || '🔍'
}

// Public methods
const focus = () => {
  inputRef.value?.focus()
}

const blur = () => {
  inputRef.value?.blur()
}

const clearCache = () => {
  suggestionCache.value.clear()
}

// Expose methods
defineExpose({
  focus,
  blur,
  clearCache,
  performSearch
})

// Lifecycle
onMounted(() => {
  // Handle clicks outside to close suggestions
  document.addEventListener('click', handleOutsideClick)
  window.addEventListener('resize', calculateDropdownPosition)
})

onUnmounted(() => {
  document.removeEventListener('click', handleOutsideClick)
  window.removeEventListener('resize', calculateDropdownPosition)
  
  if (debounceTimer.value) {
    clearTimeout(debounceTimer.value)
  }
})

const handleOutsideClick = (event: Event) => {
  if (containerRef.value && !containerRef.value.contains(event.target as Node)) {
    showSuggestions.value = false
  }
}
</script>

<style scoped>
.autocomplete-container {
  position: relative;
  width: 100%;
}

.autocomplete-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.autocomplete-input {
  width: 100%;
  padding: 0.75rem 3rem 0.75rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-size: 1rem;
  background: var(--color-background);
  color: var(--color-text);
  transition: all 0.3s ease;
}

.autocomplete-input:focus {
  outline: none;
  border-color: var(--color-border-hover);
  box-shadow: 0 0 0 3px rgba(0, 123, 255, 0.1);
}

.autocomplete-input.has-suggestions {
  border-bottom-left-radius: 0;
  border-bottom-right-radius: 0;
}

.autocomplete-input.is-loading {
  background-image: linear-gradient(90deg, transparent, rgba(0, 123, 255, 0.1), transparent);
  background-size: 200% 100%;
  animation: loading-shimmer 1.5s infinite;
}

.autocomplete-input.has-error {
  border-color: #dc3545;
}

.autocomplete-input:disabled {
  background: var(--color-background-soft);
  color: var(--color-text);
  opacity: 0.6;
  cursor: not-allowed;
}

@keyframes loading-shimmer {
  0% {
    background-position: -200% 0;
  }
  100% {
    background-position: 200% 0;
  }
}

.autocomplete-icons {
  position: absolute;
  right: 0.75rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  pointer-events: none;
}

.autocomplete-icons > * {
  pointer-events: auto;
}

.loading-spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid transparent;
  border-top: 2px solid var(--color-border-hover);
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.clear-button {
  background: none;
  border: none;
  color: var(--color-text);
  cursor: pointer;
  padding: 0.25rem;
  border-radius: 4px;
  font-size: 0.875rem;
  opacity: 0.6;
  transition: opacity 0.3s ease;
}

.clear-button:hover {
  opacity: 1;
  background: var(--color-background-soft);
}

.search-type-indicator {
  font-size: 0.875rem;
  opacity: 0.6;
  padding: 0.25rem;
}

.autocomplete-dropdown {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  z-index: 1000;
  border-top: none;
  border-top-left-radius: 0;
  border-top-right-radius: 0;
}

.dropdown-above {
  top: auto;
  bottom: 100%;
  border-top: 1px solid var(--color-border);
  border-bottom: none;
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  border-bottom-left-radius: 0;
  border-bottom-right-radius: 0;
}

.autocomplete-error {
  margin-top: 0.5rem;
  padding: 0.5rem;
  background: #fee;
  border: 1px solid #fcc;
  border-radius: 6px;
  color: #c33;
  font-size: 0.875rem;
}

.autocomplete-hint {
  margin-top: 0.5rem;
  font-size: 0.875rem;
  color: var(--color-text);
  opacity: 0.7;
}

/* Type-specific input styling */
.autocomplete-input[data-type="number"],
.autocomplete-input[data-type="combination"] {
  font-family: 'Monaco', 'Menlo', monospace;
}

/* Responsive */
@media (max-width: 768px) {
  .autocomplete-input {
    padding: 0.625rem 2.5rem 0.625rem 0.625rem;
    font-size: 0.9rem;
  }
  
  .autocomplete-icons {
    right: 0.625rem;
  }
  
  .search-type-indicator {
    display: none;
  }
}

/* High contrast mode */
@media (prefers-contrast: high) {
  .autocomplete-input {
    border-width: 2px;
  }
  
  .autocomplete-input:focus {
    box-shadow: 0 0 0 3px rgba(0, 0, 0, 0.3);
  }
}

/* Reduced motion */
@media (prefers-reduced-motion: reduce) {
  .autocomplete-input,
  .loading-spinner,
  .clear-button {
    transition: none;
    animation: none;
  }
}
</style>