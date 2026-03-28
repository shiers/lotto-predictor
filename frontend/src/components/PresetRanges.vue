<template>
  <div class="preset-ranges">
    <div class="preset-ranges-header" v-if="showHeader">
      <h3 class="ranges-title">{{ title }}</h3>
      <p v-if="description" class="ranges-description">{{ description }}</p>
    </div>
    
    <div class="ranges-grid" :class="gridClass">
      <button
        v-for="range in availableRanges"
        :key="range.id"
        @click="selectRange(range)"
        class="range-button"
        :class="{
          'range-selected': isSelected(range),
          'range-disabled': isDisabled(range),
          [`range-category-${range.category}`]: range.category
        }"
        :disabled="isDisabled(range)"
        :aria-pressed="isSelected(range)"
        :title="range.tooltip || range.description"
      >
        <div class="range-content">
          <div class="range-main">
            <span class="range-text">{{ range.label }}</span>
            <span class="range-numbers">{{ range.range }}</span>
          </div>
          
          <div v-if="showStats && range.stats" class="range-stats">
            <div class="stat-item" v-if="range.stats.frequency">
              <span class="stat-label">Frequency:</span>
              <span class="stat-value">{{ range.stats.frequency }}%</span>
            </div>
            <div class="stat-item" v-if="range.stats.lastAppearance">
              <span class="stat-label">Last:</span>
              <span class="stat-value">{{ formatDate(range.stats.lastAppearance) }}</span>
            </div>
          </div>
          
          <div v-if="showPreview && range.preview" class="range-preview">
            {{ range.preview }}
          </div>
        </div>
        
        <div v-if="showIcons" class="range-icon">
          {{ getRangeIcon(range) }}
        </div>
      </button>
    </div>
    
    <div v-if="showCustomRange" class="custom-range-section">
      <div class="custom-range-header">
        <h4>Custom Range</h4>
      </div>
      
      <div class="custom-range-inputs">
        <div class="range-input-group">
          <label for="custom-start">From</label>
          <input
            id="custom-start"
            v-model.number="customStart"
            type="number"
            min="1"
            max="40"
            placeholder="1"
            class="range-input"
            @input="validateCustomRange"
          />
        </div>
        
        <div class="range-separator">-</div>
        
        <div class="range-input-group">
          <label for="custom-end">To</label>
          <input
            id="custom-end"
            v-model.number="customEnd"
            type="number"
            min="1"
            max="40"
            placeholder="40"
            class="range-input"
            @input="validateCustomRange"
          />
        </div>
        
        <button
          @click="addCustomRange"
          :disabled="!isCustomRangeValid"
          class="add-custom-button"
          title="Add custom range"
        >
          Add Range
        </button>
      </div>
      
      <div v-if="customRangeError" class="custom-range-error">
        {{ customRangeError }}
      </div>
    </div>
    
    <div v-if="showQuickActions" class="quick-actions">
      <button
        v-if="allowMultiSelect && selectedRanges.length > 0"
        @click="clearSelection"
        class="quick-action-button clear-button"
      >
        Clear Selection ({{ selectedRanges.length }})
      </button>
      
      <button
        v-if="allowMultiSelect && selectedRanges.length > 1"
        @click="combineRanges"
        class="quick-action-button combine-button"
      >
        Combine Ranges
      </button>
      
      <button
        @click="selectPopularRanges"
        class="quick-action-button popular-button"
      >
        Select Popular
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { LookupService } from '@/services/lookupService'

// Types
interface RangeStats {
  frequency?: number
  lastAppearance?: string
  totalOccurrences?: number
  averagePerDraw?: number
}

interface PresetRange {
  id: string
  label: string
  range: string
  startNumber: number
  endNumber: number
  description: string
  category?: 'basic' | 'advanced' | 'popular' | 'custom'
  tooltip?: string
  preview?: string
  stats?: RangeStats
  isPopular?: boolean
  isRecommended?: boolean
}

// Props
interface Props {
  modelValue?: PresetRange[]
  title?: string
  description?: string
  showHeader?: boolean
  showStats?: boolean
  showPreview?: boolean
  showIcons?: boolean
  showCustomRange?: boolean
  showQuickActions?: boolean
  allowMultiSelect?: boolean
  maxSelections?: number
  gridColumns?: number
  loadStatsOnMount?: boolean
  filterCategory?: string
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: () => [],
  title: 'Quick Range Selection',
  description: 'Choose from preset number ranges or create your own',
  showHeader: true,
  showStats: false,
  showPreview: true,
  showIcons: true,
  showCustomRange: true,
  showQuickActions: true,
  allowMultiSelect: false,
  maxSelections: 5,
  gridColumns: 3,
  loadStatsOnMount: false,
  filterCategory: ''
})

// Emits
const emit = defineEmits<{
  'update:modelValue': [ranges: PresetRange[]]
  select: [range: PresetRange]
  deselect: [range: PresetRange]
  clear: []
  combine: [ranges: PresetRange[]]
  'custom-add': [range: PresetRange]
}>()

// Reactive state
const selectedRanges = ref<PresetRange[]>([...props.modelValue])
const customStart = ref<number>()
const customEnd = ref<number>()
const customRangeError = ref('')
const isLoadingStats = ref(false)

// Preset ranges data
const presetRanges = ref<PresetRange[]>([
  {
    id: 'low',
    label: 'Low Numbers',
    range: '1-10',
    startNumber: 1,
    endNumber: 10,
    description: 'Numbers 1 through 10',
    category: 'basic',
    tooltip: 'Traditional low number range',
    preview: 'Often considered "cold" numbers',
    isPopular: true
  },
  {
    id: 'mid-low',
    label: 'Mid-Low',
    range: '11-20',
    startNumber: 11,
    endNumber: 20,
    description: 'Numbers 11 through 20',
    category: 'basic',
    tooltip: 'Middle-low range numbers',
    preview: 'Balanced frequency range'
  },
  {
    id: 'mid-high',
    label: 'Mid-High',
    range: '21-30',
    startNumber: 21,
    endNumber: 30,
    description: 'Numbers 21 through 30',
    category: 'basic',
    tooltip: 'Middle-high range numbers',
    preview: 'Popular selection range'
  },
  {
    id: 'high',
    label: 'High Numbers',
    range: '31-40',
    startNumber: 31,
    endNumber: 40,
    description: 'Numbers 31 through 40',
    category: 'basic',
    tooltip: 'Traditional high number range',
    preview: 'Often considered "hot" numbers',
    isPopular: true
  },
  {
    id: 'lower-half',
    label: 'Lower Half',
    range: '1-20',
    startNumber: 1,
    endNumber: 20,
    description: 'First half of all numbers',
    category: 'advanced',
    tooltip: 'Lower 50% of number range',
    preview: 'Conservative selection strategy',
    isRecommended: true
  },
  {
    id: 'upper-half',
    label: 'Upper Half',
    range: '21-40',
    startNumber: 21,
    endNumber: 40,
    description: 'Second half of all numbers',
    category: 'advanced',
    tooltip: 'Upper 50% of number range',
    preview: 'Aggressive selection strategy',
    isRecommended: true
  },
  {
    id: 'first-quarter',
    label: 'First Quarter',
    range: '1-15',
    startNumber: 1,
    endNumber: 15,
    description: 'First quarter of numbers',
    category: 'advanced',
    tooltip: 'Numbers 1-15 (37.5% of range)'
  },
  {
    id: 'middle-half',
    label: 'Middle Range',
    range: '16-30',
    startNumber: 16,
    endNumber: 30,
    description: 'Middle section of numbers',
    category: 'advanced',
    tooltip: 'Central number range',
    preview: 'Statistically balanced'
  },
  {
    id: 'last-quarter',
    label: 'Last Quarter',
    range: '31-40',
    startNumber: 31,
    endNumber: 40,
    description: 'Last quarter of numbers',
    category: 'advanced',
    tooltip: 'Numbers 31-40 (25% of range)'
  },
  {
    id: 'teens',
    label: 'Teen Numbers',
    range: '13-19',
    startNumber: 13,
    endNumber: 19,
    description: 'Teen numbers only',
    category: 'popular',
    tooltip: 'Numbers ending in teen',
    preview: 'Narrow focused range',
    isPopular: true
  },
  {
    id: 'twenties',
    label: 'Twenties',
    range: '20-29',
    startNumber: 20,
    endNumber: 29,
    description: 'All twenty numbers',
    category: 'popular',
    tooltip: 'Complete twenties decade',
    preview: 'Decade-based selection'
  },
  {
    id: 'thirties',
    label: 'Thirties',
    range: '30-39',
    startNumber: 30,
    endNumber: 39,
    description: 'All thirty numbers',
    category: 'popular',
    tooltip: 'Complete thirties decade',
    preview: 'High-end decade range'
  }
])

// Computed properties
const availableRanges = computed(() => {
  let ranges = presetRanges.value
  
  if (props.filterCategory) {
    ranges = ranges.filter(r => r.category === props.filterCategory)
  }
  
  return ranges
})

const gridClass = computed(() => {
  return `grid-cols-${props.gridColumns}`
})

const isCustomRangeValid = computed(() => {
  return customStart.value !== undefined &&
         customEnd.value !== undefined &&
         customStart.value >= 1 &&
         customEnd.value <= 40 &&
         customStart.value <= customEnd.value &&
         !customRangeError.value
})

// Watch for model value changes
watch(() => props.modelValue, (newValue) => {
  selectedRanges.value = [...newValue]
})

// Watch for selection changes
watch(selectedRanges, (newSelection) => {
  emit('update:modelValue', [...newSelection])
}, { deep: true })

// Methods
const selectRange = (range: PresetRange) => {
  if (isDisabled(range)) return
  
  if (props.allowMultiSelect) {
    const index = selectedRanges.value.findIndex(r => r.id === range.id)
    
    if (index >= 0) {
      // Deselect
      selectedRanges.value.splice(index, 1)
      emit('deselect', range)
    } else {
      // Select (if under limit)
      if (selectedRanges.value.length < props.maxSelections) {
        selectedRanges.value.push(range)
        emit('select', range)
      }
    }
  } else {
    // Single select
    selectedRanges.value = [range]
    emit('select', range)
  }
}

const isSelected = (range: PresetRange): boolean => {
  return selectedRanges.value.some(r => r.id === range.id)
}

const isDisabled = (range: PresetRange): boolean => {
  if (!props.allowMultiSelect) return false
  
  return !isSelected(range) && selectedRanges.value.length >= props.maxSelections
}

const validateCustomRange = () => {
  customRangeError.value = ''
  
  if (customStart.value === undefined || customEnd.value === undefined) {
    return
  }
  
  if (customStart.value < 1 || customStart.value > 40) {
    customRangeError.value = 'Start number must be between 1 and 40'
    return
  }
  
  if (customEnd.value < 1 || customEnd.value > 40) {
    customRangeError.value = 'End number must be between 1 and 40'
    return
  }
  
  if (customStart.value > customEnd.value) {
    customRangeError.value = 'Start number must be less than or equal to end number'
    return
  }
  
  // Check if range already exists
  const rangeText = `${customStart.value}-${customEnd.value}`
  const exists = presetRanges.value.some(r => r.range === rangeText)
  if (exists) {
    customRangeError.value = 'This range already exists in the preset list'
  }
}

const addCustomRange = () => {
  if (!isCustomRangeValid.value || !customStart.value || !customEnd.value) return
  
  const customRange: PresetRange = {
    id: `custom-${customStart.value}-${customEnd.value}`,
    label: `Custom ${customStart.value}-${customEnd.value}`,
    range: `${customStart.value}-${customEnd.value}`,
    startNumber: customStart.value,
    endNumber: customEnd.value,
    description: `Custom range from ${customStart.value} to ${customEnd.value}`,
    category: 'custom',
    tooltip: 'User-defined custom range'
  }
  
  // Add to preset ranges
  presetRanges.value.push(customRange)
  
  // Select the new range
  selectRange(customRange)
  
  // Clear inputs
  customStart.value = undefined
  customEnd.value = undefined
  
  emit('custom-add', customRange)
}

const clearSelection = () => {
  selectedRanges.value = []
  emit('clear')
}

const combineRanges = () => {
  if (selectedRanges.value.length < 2) return
  
  const sortedRanges = [...selectedRanges.value].sort((a, b) => a.startNumber - b.startNumber)
  const minStart = sortedRanges[0].startNumber
  const maxEnd = sortedRanges[sortedRanges.length - 1].endNumber
  
  const combinedRange: PresetRange = {
    id: `combined-${minStart}-${maxEnd}`,
    label: `Combined Range`,
    range: `${minStart}-${maxEnd}`,
    startNumber: minStart,
    endNumber: maxEnd,
    description: `Combined from ${selectedRanges.value.length} ranges`,
    category: 'custom',
    tooltip: 'Combined from multiple selected ranges'
  }
  
  // Add to preset ranges if not exists
  const exists = presetRanges.value.some(r => r.range === combinedRange.range)
  if (!exists) {
    presetRanges.value.push(combinedRange)
  }
  
  // Replace selection with combined range
  selectedRanges.value = [combinedRange]
  
  emit('combine', sortedRanges)
}

const selectPopularRanges = () => {
  const popularRanges = presetRanges.value.filter(r => r.isPopular)
  
  if (props.allowMultiSelect) {
    selectedRanges.value = popularRanges.slice(0, props.maxSelections)
  } else if (popularRanges.length > 0) {
    selectedRanges.value = [popularRanges[0]]
  }
}

const getRangeIcon = (range: PresetRange): string => {
  if (range.category === 'custom') return '⚙️'
  if (range.isPopular) return '⭐'
  if (range.isRecommended) return '👍'
  
  const icons: Record<string, string> = {
    'basic': '📊',
    'advanced': '🎯',
    'popular': '🔥'
  }
  
  return icons[range.category || 'basic'] || '📈'
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-NZ', {
    month: 'short',
    day: 'numeric'
  })
}

const loadRangeStats = async () => {
  if (!props.showStats) return
  
  isLoadingStats.value = true
  
  try {
    // This would typically call the frequency analysis service
    // For now, we'll simulate with mock data
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    // Mock stats - in real implementation, this would come from the API
    presetRanges.value.forEach(range => {
      range.stats = {
        frequency: Math.floor(Math.random() * 30) + 10,
        lastAppearance: new Date(Date.now() - Math.random() * 365 * 24 * 60 * 60 * 1000).toISOString(),
        totalOccurrences: Math.floor(Math.random() * 100) + 20,
        averagePerDraw: Math.random() * 2 + 0.5
      }
    })
  } catch (error) {
    console.error('Failed to load range statistics:', error)
  } finally {
    isLoadingStats.value = false
  }
}

// Lifecycle
onMounted(() => {
  if (props.loadStatsOnMount) {
    loadRangeStats()
  }
})

// Expose methods
defineExpose({
  selectRange,
  clearSelection,
  loadRangeStats,
  addCustomRange
})
</script>

<style scoped>
.preset-ranges {
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  padding: 1.5rem;
}

.preset-ranges-header {
  margin-bottom: 1.5rem;
  text-align: center;
}

.ranges-title {
  color: var(--color-heading);
  margin: 0 0 0.5rem 0;
  font-size: 1.25rem;
}

.ranges-description {
  color: var(--color-text);
  margin: 0;
  font-size: 0.9rem;
  opacity: 0.8;
}

.ranges-grid {
  display: grid;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.grid-cols-2 {
  grid-template-columns: repeat(2, 1fr);
}

.grid-cols-3 {
  grid-template-columns: repeat(3, 1fr);
}

.grid-cols-4 {
  grid-template-columns: repeat(4, 1fr);
}

.range-button {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem;
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s ease;
  text-align: left;
}

.range-button:hover:not(:disabled) {
  background: var(--color-background);
  border-color: var(--color-border-hover);
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.range-button.range-selected {
  background: #e3f2fd;
  border-color: #1976d2;
  color: #1976d2;
}

.range-button.range-disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.range-button:disabled {
  transform: none;
  box-shadow: none;
}

.range-content {
  flex: 1;
  min-width: 0;
}

.range-main {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-bottom: 0.5rem;
}

.range-text {
  font-weight: 500;
  font-size: 0.9rem;
}

.range-numbers {
  font-family: 'Monaco', 'Menlo', monospace;
  font-size: 0.8rem;
  color: var(--color-text);
  opacity: 0.8;
}

.range-stats {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-bottom: 0.5rem;
}

.stat-item {
  display: flex;
  justify-content: space-between;
  font-size: 0.75rem;
}

.stat-label {
  color: var(--color-text);
  opacity: 0.7;
}

.stat-value {
  font-weight: 500;
  color: var(--color-heading);
}

.range-preview {
  font-size: 0.75rem;
  color: var(--color-text);
  opacity: 0.7;
  font-style: italic;
}

.range-icon {
  font-size: 1.25rem;
  margin-left: 0.5rem;
}

/* Category-specific styling */
.range-category-popular {
  border-left: 3px solid #ff9800;
}

.range-category-basic {
  border-left: 3px solid #2196f3;
}

.range-category-advanced {
  border-left: 3px solid #9c27b0;
}

.range-category-custom {
  border-left: 3px solid #4caf50;
}

.custom-range-section {
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: 1rem;
  margin-bottom: 1.5rem;
}

.custom-range-header h4 {
  margin: 0 0 1rem 0;
  color: var(--color-heading);
  font-size: 1rem;
}

.custom-range-inputs {
  display: flex;
  align-items: end;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.range-input-group {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.range-input-group label {
  font-size: 0.8rem;
  color: var(--color-text);
  font-weight: 500;
}

.range-input {
  width: 80px;
  padding: 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-background);
  color: var(--color-text);
  text-align: center;
}

.range-input:focus {
  outline: none;
  border-color: var(--color-border-hover);
}

.range-separator {
  font-weight: bold;
  color: var(--color-text);
  padding: 0.5rem 0;
  font-size: 1.2rem;
}

.add-custom-button {
  background: var(--color-border-hover);
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  font-size: 0.875rem;
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.add-custom-button:hover:not(:disabled) {
  background: var(--color-border);
}

.add-custom-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.custom-range-error {
  margin-top: 0.5rem;
  padding: 0.5rem;
  background: #fee;
  border: 1px solid #fcc;
  border-radius: 4px;
  color: #c33;
  font-size: 0.8rem;
}

.quick-actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  justify-content: center;
  padding-top: 1rem;
  border-top: 1px solid var(--color-border);
}

.quick-action-button {
  padding: 0.5rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-background);
  color: var(--color-text);
  cursor: pointer;
  font-size: 0.875rem;
  transition: all 0.3s ease;
}

.quick-action-button:hover {
  background: var(--color-background-soft);
  border-color: var(--color-border-hover);
}

.clear-button {
  border-color: #dc3545;
  color: #dc3545;
}

.clear-button:hover {
  background: #dc3545;
  color: white;
}

.combine-button {
  border-color: #28a745;
  color: #28a745;
}

.combine-button:hover {
  background: #28a745;
  color: white;
}

.popular-button {
  border-color: #ffc107;
  color: #856404;
}

.popular-button:hover {
  background: #ffc107;
  color: #212529;
}

/* Responsive */
@media (max-width: 768px) {
  .ranges-grid {
    grid-template-columns: 1fr;
  }
  
  .range-button {
    padding: 0.75rem;
  }
  
  .range-main {
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
  }
  
  .range-stats {
    flex-direction: row;
    gap: 1rem;
  }
  
  .custom-range-inputs {
    flex-direction: column;
    align-items: stretch;
  }
  
  .range-input-group {
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
  }
  
  .range-input {
    width: 100px;
  }
  
  .quick-actions {
    flex-direction: column;
  }
}

/* Accessibility */
.range-button:focus {
  outline: 2px solid var(--color-border-hover);
  outline-offset: 2px;
}

/* High contrast mode */
@media (prefers-contrast: high) {
  .range-button {
    border-width: 2px;
  }
  
  .range-selected {
    border-width: 3px;
  }
}
</style>