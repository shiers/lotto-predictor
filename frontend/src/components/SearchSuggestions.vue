<template>
  <div class="search-suggestions" v-if="suggestions.length > 0">
    <div class="suggestions-header" v-if="showHeader">
      <span class="suggestions-title">Suggestions</span>
      <span class="suggestions-count">({{ suggestions.length }})</span>
    </div>
    
    <ul class="suggestions-list" role="listbox">
      <li
        v-for="(suggestion, index) in suggestions"
        :key="`${suggestion.type}-${suggestion.text}-${index}`"
        class="suggestion-item"
        :class="{
          'suggestion-highlighted': index === highlightedIndex,
          [`suggestion-type-${suggestion.type}`]: true
        }"
        role="option"
        :aria-selected="index === highlightedIndex"
        @click="selectSuggestion(suggestion)"
        @mouseenter="highlightedIndex = index"
      >
        <div class="suggestion-content">
          <div class="suggestion-main">
            <span class="suggestion-text">{{ suggestion.text }}</span>
            <span class="suggestion-type-badge" :class="`type-${suggestion.type}`">
              {{ getTypeLabel(suggestion.type) }}
            </span>
          </div>
          
          <div v-if="suggestion.previewInfo && showPreview" class="suggestion-preview">
            {{ suggestion.previewInfo }}
          </div>
          
          <div v-if="suggestion.metadata && showMetadata" class="suggestion-metadata">
            <span v-if="suggestion.metadata.frequency" class="metadata-item">
              {{ suggestion.metadata.frequency }} occurrences
            </span>
            <span v-if="suggestion.metadata.lastAppearance" class="metadata-item">
              Last: {{ formatDate(suggestion.metadata.lastAppearance) }}
            </span>
          </div>
        </div>
        
        <div class="suggestion-relevance" v-if="showRelevance">
          <div class="relevance-bar">
            <div 
              class="relevance-fill" 
              :style="{ width: `${suggestion.relevance}%` }"
            ></div>
          </div>
          <span class="relevance-score">{{ suggestion.relevance }}%</span>
        </div>
      </li>
    </ul>
    
    <div v-if="hasMoreSuggestions" class="suggestions-footer">
      <button 
        @click="$emit('loadMore')" 
        class="load-more-button"
        :disabled="isLoading"
      >
        <span v-if="isLoading" class="loading-spinner"></span>
        {{ isLoading ? 'Loading...' : 'Load More' }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import type { SearchSuggestion } from '@/services/lookupService'

// Props
interface Props {
  suggestions: SearchSuggestion[]
  showHeader?: boolean
  showPreview?: boolean
  showMetadata?: boolean
  showRelevance?: boolean
  hasMoreSuggestions?: boolean
  isLoading?: boolean
  maxHeight?: string
  highlightFirst?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  suggestions: () => [],
  showHeader: true,
  showPreview: true,
  showMetadata: false,
  showRelevance: false,
  hasMoreSuggestions: false,
  isLoading: false,
  maxHeight: '300px',
  highlightFirst: true
})

// Emits
const emit = defineEmits<{
  select: [suggestion: SearchSuggestion]
  highlight: [suggestion: SearchSuggestion | null]
  loadMore: []
  keydown: [event: KeyboardEvent]
}>()

// Reactive state
const highlightedIndex = ref(-1)

// Computed properties
const highlightedSuggestion = computed(() => {
  return highlightedIndex.value >= 0 && highlightedIndex.value < props.suggestions.length
    ? props.suggestions[highlightedIndex.value]
    : null
})

// Watch for suggestions changes
watch(() => props.suggestions, (newSuggestions) => {
  if (newSuggestions.length > 0 && props.highlightFirst) {
    highlightedIndex.value = 0
  } else {
    highlightedIndex.value = -1
  }
}, { immediate: true })

// Watch for highlighted suggestion changes
watch(highlightedSuggestion, (suggestion) => {
  emit('highlight', suggestion)
})

// Methods
const selectSuggestion = (suggestion: SearchSuggestion) => {
  emit('select', suggestion)
}

const handleKeyDown = (event: KeyboardEvent) => {
  switch (event.key) {
    case 'ArrowDown':
      event.preventDefault()
      moveHighlight(1)
      break
    case 'ArrowUp':
      event.preventDefault()
      moveHighlight(-1)
      break
    case 'Enter':
      event.preventDefault()
      if (highlightedSuggestion.value) {
        selectSuggestion(highlightedSuggestion.value)
      }
      break
    case 'Escape':
      event.preventDefault()
      highlightedIndex.value = -1
      break
  }
  
  emit('keydown', event)
}

const moveHighlight = (direction: number) => {
  const newIndex = highlightedIndex.value + direction
  
  if (newIndex < 0) {
    highlightedIndex.value = props.suggestions.length - 1
  } else if (newIndex >= props.suggestions.length) {
    highlightedIndex.value = 0
  } else {
    highlightedIndex.value = newIndex
  }
  
  // Scroll highlighted item into view
  scrollToHighlighted()
}

const scrollToHighlighted = () => {
  const highlightedElement = document.querySelector('.suggestion-highlighted')
  if (highlightedElement) {
    highlightedElement.scrollIntoView({
      block: 'nearest',
      behavior: 'smooth'
    })
  }
}

const getTypeLabel = (type: string): string => {
  const typeLabels: Record<string, string> = {
    number: 'Number',
    combination: 'Combo',
    range: 'Range',
    date: 'Date',
    drawNumber: 'Draw'
  }
  return typeLabels[type] || type
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-NZ', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

// Expose methods for parent component
defineExpose({
  selectHighlighted: () => {
    if (highlightedSuggestion.value) {
      selectSuggestion(highlightedSuggestion.value)
    }
  },
  moveHighlight,
  clearHighlight: () => {
    highlightedIndex.value = -1
  },
  highlightFirst: () => {
    if (props.suggestions.length > 0) {
      highlightedIndex.value = 0
    }
  }
})

// Lifecycle
onMounted(() => {
  document.addEventListener('keydown', handleKeyDown)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeyDown)
})
</script>

<style scoped>
.search-suggestions {
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  z-index: 1000;
}

.suggestions-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1rem;
  background: var(--color-background-soft);
  border-bottom: 1px solid var(--color-border);
}

.suggestions-title {
  font-weight: 500;
  color: var(--color-heading);
  font-size: 0.875rem;
}

.suggestions-count {
  color: var(--color-text);
  font-size: 0.75rem;
  opacity: 0.7;
}

.suggestions-list {
  list-style: none;
  margin: 0;
  padding: 0;
  max-height: v-bind(maxHeight);
  overflow-y: auto;
}

.suggestion-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1rem;
  cursor: pointer;
  border-bottom: 1px solid var(--color-border);
  transition: background-color 0.2s ease;
}

.suggestion-item:last-child {
  border-bottom: none;
}

.suggestion-item:hover,
.suggestion-highlighted {
  background: var(--color-background-soft);
}

.suggestion-content {
  flex: 1;
  min-width: 0;
}

.suggestion-main {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.25rem;
}

.suggestion-text {
  font-weight: 500;
  color: var(--color-heading);
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.suggestion-type-badge {
  display: inline-block;
  padding: 0.125rem 0.375rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 500;
  text-transform: uppercase;
  letter-spacing: 0.025em;
}

.type-number {
  background: #e3f2fd;
  color: #1976d2;
}

.type-combination {
  background: #e8f5e8;
  color: #2e7d32;
}

.type-range {
  background: #fff3e0;
  color: #f57c00;
}

.type-date {
  background: #fce4ec;
  color: #c2185b;
}

.type-drawNumber {
  background: #f3e5f5;
  color: #7b1fa2;
}

.suggestion-preview {
  font-size: 0.8rem;
  color: var(--color-text);
  opacity: 0.8;
  margin-bottom: 0.25rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.suggestion-metadata {
  display: flex;
  gap: 1rem;
  font-size: 0.75rem;
  color: var(--color-text);
  opacity: 0.7;
}

.metadata-item {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.suggestion-relevance {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-left: 1rem;
}

.relevance-bar {
  width: 60px;
  height: 4px;
  background: var(--color-border);
  border-radius: 2px;
  overflow: hidden;
}

.relevance-fill {
  height: 100%;
  background: linear-gradient(90deg, #f57c00, #4caf50);
  transition: width 0.3s ease;
}

.relevance-score {
  font-size: 0.75rem;
  color: var(--color-text);
  opacity: 0.7;
  min-width: 30px;
  text-align: right;
}

.suggestions-footer {
  padding: 0.75rem 1rem;
  border-top: 1px solid var(--color-border);
  background: var(--color-background-soft);
  text-align: center;
}

.load-more-button {
  background: transparent;
  color: var(--color-border-hover);
  border: 1px solid var(--color-border);
  padding: 0.5rem 1rem;
  border-radius: 6px;
  font-size: 0.875rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0 auto;
  transition: all 0.3s ease;
}

.load-more-button:hover:not(:disabled) {
  background: var(--color-background);
  border-color: var(--color-border-hover);
}

.load-more-button:disabled {
  opacity: 0.6;
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

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Type-specific styling */
.suggestion-type-number .suggestion-text {
  font-family: 'Monaco', 'Menlo', monospace;
}

.suggestion-type-combination .suggestion-text {
  font-family: 'Monaco', 'Menlo', monospace;
}

.suggestion-type-range .suggestion-text {
  font-weight: 600;
}

/* Accessibility */
.suggestion-item:focus {
  outline: 2px solid var(--color-border-hover);
  outline-offset: -2px;
}

/* Responsive */
@media (max-width: 768px) {
  .suggestion-item {
    padding: 0.625rem 0.75rem;
  }
  
  .suggestion-main {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
  }
  
  .suggestion-relevance {
    margin-left: 0;
    margin-top: 0.25rem;
  }
  
  .relevance-bar {
    width: 40px;
  }
  
  .suggestion-metadata {
    flex-direction: column;
    gap: 0.25rem;
  }
}
</style>