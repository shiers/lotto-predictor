<template>
  <div class="number-heatmap">
    <div class="heatmap-header">
      <h3>Number Frequency Heatmap</h3>
      <div class="heatmap-controls">
        <div class="control-group">
          <label for="color-scheme">Color Scheme:</label>
          <select 
            id="color-scheme" 
            :value="colorScheme" 
            @change="$emit('update:colorScheme', ($event.target as HTMLSelectElement).value)"
            class="control-select"
          >
            <option value="hot-cold">Hot/Cold</option>
            <option value="gradient">Gradient</option>
            <option value="discrete">Discrete</option>
          </select>
        </div>
        <div class="control-group">
          <label class="checkbox-label">
            <input 
              type="checkbox" 
              :checked="showLabels" 
              @change="$emit('update:showLabels', ($event.target as HTMLInputElement).checked)"
            />
            Show Labels
          </label>
        </div>
        <div class="control-group">
          <label class="checkbox-label">
            <input 
              type="checkbox" 
              v-model="showTooltips"
            />
            Show Tooltips
          </label>
        </div>
      </div>
    </div>

    <div class="heatmap-container">
      <!-- Legend -->
      <div class="heatmap-legend">
        <div class="legend-title">Frequency</div>
        <div class="legend-scale">
          <div class="legend-item">
            <div class="legend-color" :style="{ backgroundColor: getLegendColor('min') }"></div>
            <span class="legend-label">{{ minFrequency }}</span>
          </div>
          <div class="legend-gradient" :style="{ background: getLegendGradient() }"></div>
          <div class="legend-item">
            <div class="legend-color" :style="{ backgroundColor: getLegendColor('max') }"></div>
            <span class="legend-label">{{ maxFrequency }}</span>
          </div>
        </div>
        <div v-if="colorScheme === 'hot-cold'" class="legend-categories">
          <div class="legend-category">
            <div class="legend-color hot"></div>
            <span>Hot</span>
          </div>
          <div class="legend-category">
            <div class="legend-color normal"></div>
            <span>Normal</span>
          </div>
          <div class="legend-category">
            <div class="legend-color cold"></div>
            <span>Cold</span>
          </div>
        </div>
      </div>

      <!-- Heatmap Grid -->
      <div class="heatmap-grid" :class="{ 'show-labels': showLabels }">
        <div
          v-for="number in numbers"
          :key="number"
          :class="['heatmap-cell', getFrequencyClass(number)]"
          :style="{ backgroundColor: getNumberColor(number) }"
          @click="handleNumberClick(number)"
          @mouseenter="showTooltip($event, number)"
          @mouseleave="hideTooltip"
        >
          <span v-if="showLabels" class="cell-label">{{ number }}</span>
          <div v-if="showLabels" class="cell-frequency">{{ getNumberFrequency(number) }}</div>
        </div>
      </div>

      <!-- Statistics Panel -->
      <div class="heatmap-stats">
        <h4>Statistics</h4>
        <div class="stats-list">
          <div class="stat-item">
            <span class="stat-label">Total Numbers:</span>
            <span class="stat-value">{{ frequencyData.length }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">Average Frequency:</span>
            <span class="stat-value">{{ averageFrequency.toFixed(1) }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">Most Frequent:</span>
            <span class="stat-value">{{ mostFrequentNumber }} ({{ maxFrequency }}x)</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">Least Frequent:</span>
            <span class="stat-value">{{ leastFrequentNumber }} ({{ minFrequency }}x)</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">Hot Numbers:</span>
            <span class="stat-value">{{ hotNumbers.length }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">Cold Numbers:</span>
            <span class="stat-value">{{ coldNumbers.length }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Tooltip -->
    <div
      v-if="tooltip.visible && showTooltips"
      class="heatmap-tooltip"
      :style="{ left: tooltip.x + 'px', top: tooltip.y + 'px' }"
    >
      <div class="tooltip-header">Number {{ tooltip.number }}</div>
      <div class="tooltip-content">
        <div class="tooltip-item">
          <span class="tooltip-label">Occurrences:</span>
          <span class="tooltip-value">{{ tooltip.frequency?.totalOccurrences || 0 }}</span>
        </div>
        <div class="tooltip-item">
          <span class="tooltip-label">Percentage:</span>
          <span class="tooltip-value">{{ (tooltip.frequency?.percentage || 0).toFixed(2) }}%</span>
        </div>
        <div class="tooltip-item">
          <span class="tooltip-label">Last Seen:</span>
          <span class="tooltip-value">{{ formatDate(tooltip.frequency?.lastAppearance) }}</span>
        </div>
        <div class="tooltip-item">
          <span class="tooltip-label">Current Gap:</span>
          <span class="tooltip-value">{{ tooltip.frequency?.currentGap || 0 }} draws</span>
        </div>
        <div class="tooltip-item">
          <span class="tooltip-label">Classification:</span>
          <span class="tooltip-value" :class="getClassificationClass(tooltip.frequency)">
            {{ getClassification(tooltip.frequency) }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import type { NumberFrequency } from '../services/frequencyService'

// Props
interface Props {
  frequencyData: NumberFrequency[]
  colorScheme?: 'hot-cold' | 'gradient' | 'discrete'
  showLabels?: boolean
  gridColumns?: number
}

const props = withDefaults(defineProps<Props>(), {
  colorScheme: 'hot-cold',
  showLabels: true,
  gridColumns: 8
})

// Emits
const emit = defineEmits<{
  'number-click': [number: number]
  'update:colorScheme': [scheme: string]
  'update:showLabels': [show: boolean]
}>()

// Reactive data
const showTooltips = ref(true)
const tooltip = ref({
  visible: false,
  x: 0,
  y: 0,
  number: 0,
  frequency: null as NumberFrequency | null
})

// Computed properties
const numbers = computed(() => {
  // Generate numbers 1-40
  return Array.from({ length: 40 }, (_, i) => i + 1)
})

const frequencyMap = computed(() => {
  const map = new Map<number, NumberFrequency>()
  props.frequencyData.forEach(freq => {
    map.set(freq.number, freq)
  })
  return map
})

const minFrequency = computed(() => {
  if (props.frequencyData.length === 0) return 0
  return Math.min(...props.frequencyData.map(f => f.totalOccurrences))
})

const maxFrequency = computed(() => {
  if (props.frequencyData.length === 0) return 0
  return Math.max(...props.frequencyData.map(f => f.totalOccurrences))
})

const averageFrequency = computed(() => {
  if (props.frequencyData.length === 0) return 0
  return props.frequencyData.reduce((sum, f) => sum + f.totalOccurrences, 0) / props.frequencyData.length
})

const mostFrequentNumber = computed(() => {
  if (props.frequencyData.length === 0) return 0
  return props.frequencyData.reduce((max, f) => 
    f.totalOccurrences > max.totalOccurrences ? f : max
  ).number
})

const leastFrequentNumber = computed(() => {
  if (props.frequencyData.length === 0) return 0
  return props.frequencyData.reduce((min, f) => 
    f.totalOccurrences < min.totalOccurrences ? f : min
  ).number
})

const hotNumbers = computed(() => 
  props.frequencyData.filter(f => f.isHot)
)

const coldNumbers = computed(() => 
  props.frequencyData.filter(f => f.isCold)
)

// Methods
const getNumberFrequency = (number: number): number => {
  return frequencyMap.value.get(number)?.totalOccurrences || 0
}

const getFrequencyClass = (number: number): string => {
  const frequency = frequencyMap.value.get(number)
  if (!frequency) return 'no-data'
  
  if (frequency.isHot) return 'hot'
  if (frequency.isCold) return 'cold'
  return 'normal'
}

const getNumberColor = (number: number): string => {
  const frequency = frequencyMap.value.get(number)
  if (!frequency) return '#f8f9fa'
  
  switch (props.colorScheme) {
    case 'hot-cold':
      return getHotColdColor(frequency)
    case 'gradient':
      return getGradientColor(frequency)
    case 'discrete':
      return getDiscreteColor(frequency)
    default:
      return getHotColdColor(frequency)
  }
}

const getHotColdColor = (frequency: NumberFrequency): string => {
  if (frequency.isHot) return '#dc3545'
  if (frequency.isCold) return '#007bff'
  return '#28a745'
}

const getGradientColor = (frequency: NumberFrequency): string => {
  if (maxFrequency.value === minFrequency.value) return '#28a745'
  
  const ratio = (frequency.totalOccurrences - minFrequency.value) / (maxFrequency.value - minFrequency.value)
  
  // Interpolate between blue (cold) and red (hot)
  const red = Math.round(255 * ratio)
  const blue = Math.round(255 * (1 - ratio))
  const green = Math.round(128 * (1 - Math.abs(ratio - 0.5) * 2))
  
  return `rgb(${red}, ${green}, ${blue})`
}

const getDiscreteColor = (frequency: NumberFrequency): string => {
  const range = maxFrequency.value - minFrequency.value
  const step = range / 5
  const value = frequency.totalOccurrences - minFrequency.value
  
  if (value <= step) return '#e3f2fd'
  if (value <= step * 2) return '#90caf9'
  if (value <= step * 3) return '#42a5f5'
  if (value <= step * 4) return '#1e88e5'
  return '#1565c0'
}

const getLegendColor = (type: 'min' | 'max'): string => {
  const dummyFreq = {
    number: 1,
    totalOccurrences: type === 'min' ? minFrequency.value : maxFrequency.value,
    lastAppearance: '',
    firstAppearance: '',
    longestGap: 0,
    currentGap: 0,
    percentage: 0,
    averageFrequency: 0,
    isHot: type === 'max',
    isCold: type === 'min'
  }
  
  return getNumberColor(dummyFreq.number)
}

const getLegendGradient = (): string => {
  switch (props.colorScheme) {
    case 'hot-cold':
      return 'linear-gradient(to right, #007bff, #28a745, #dc3545)'
    case 'gradient':
      return 'linear-gradient(to right, #0066ff, #8040ff, #ff0066)'
    case 'discrete':
      return 'linear-gradient(to right, #e3f2fd, #90caf9, #42a5f5, #1e88e5, #1565c0)'
    default:
      return 'linear-gradient(to right, #007bff, #28a745, #dc3545)'
  }
}

const getClassification = (frequency: NumberFrequency | null): string => {
  if (!frequency) return 'No Data'
  if (frequency.isHot) return 'Hot'
  if (frequency.isCold) return 'Cold'
  return 'Normal'
}

const getClassificationClass = (frequency: NumberFrequency | null): string => {
  if (!frequency) return 'no-data'
  if (frequency.isHot) return 'hot'
  if (frequency.isCold) return 'cold'
  return 'normal'
}

const handleNumberClick = (number: number) => {
  emit('number-click', number)
}

const showTooltip = (event: MouseEvent, number: number) => {
  if (!showTooltips.value) return
  
  const frequency = frequencyMap.value.get(number) || null
  
  tooltip.value = {
    visible: true,
    x: event.clientX + 10,
    y: event.clientY - 10,
    number,
    frequency
  }
}

const hideTooltip = () => {
  tooltip.value.visible = false
}

const formatDate = (dateString?: string): string => {
  if (!dateString) return 'Never'
  return new Date(dateString).toLocaleDateString()
}

// Handle window resize for tooltip positioning
const handleResize = () => {
  if (tooltip.value.visible) {
    hideTooltip()
  }
}

// Lifecycle
onMounted(() => {
  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  window.removeEventListener('resize', handleResize)
})
</script>

<style scoped>
.number-heatmap {
  width: 100%;
  max-width: 1000px;
  margin: 0 auto;
}

.heatmap-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  flex-wrap: wrap;
  gap: 15px;
}

.heatmap-header h3 {
  color: #2c3e50;
  margin: 0;
}

.heatmap-controls {
  display: flex;
  gap: 20px;
  align-items: center;
  flex-wrap: wrap;
}

.control-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.control-group label {
  font-weight: 500;
  color: #495057;
  white-space: nowrap;
}

.control-select {
  padding: 6px 10px;
  border: 1px solid #ced4da;
  border-radius: 4px;
  background: white;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  user-select: none;
}

.heatmap-container {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 30px;
  align-items: start;
}

.heatmap-legend {
  background: #f8f9fa;
  border-radius: 8px;
  padding: 15px;
  margin-bottom: 20px;
  grid-column: 1 / -1;
}

.legend-title {
  font-weight: 600;
  color: #2c3e50;
  margin-bottom: 10px;
  text-align: center;
}

.legend-scale {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 15px;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 5px;
}

.legend-color {
  width: 20px;
  height: 20px;
  border-radius: 4px;
  border: 1px solid #dee2e6;
}

.legend-gradient {
  flex: 1;
  height: 20px;
  border-radius: 4px;
  border: 1px solid #dee2e6;
}

.legend-label {
  font-size: 14px;
  color: #495057;
  font-weight: 500;
}

.legend-categories {
  display: flex;
  justify-content: center;
  gap: 20px;
}

.legend-category {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  color: #495057;
}

.legend-color.hot {
  background-color: #dc3545;
}

.legend-color.normal {
  background-color: #28a745;
}

.legend-color.cold {
  background-color: #007bff;
}

.heatmap-grid {
  display: grid;
  grid-template-columns: repeat(8, 1fr);
  gap: 4px;
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.heatmap-cell {
  aspect-ratio: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s ease;
  border: 2px solid transparent;
  position: relative;
  min-height: 60px;
}

.heatmap-cell:hover {
  transform: scale(1.05);
  border-color: #2c3e50;
  z-index: 10;
  box-shadow: 0 4px 8px rgba(0,0,0,0.2);
}

.heatmap-cell.no-data {
  background-color: #f8f9fa !important;
  border-color: #dee2e6;
  cursor: default;
}

.heatmap-cell.no-data:hover {
  transform: none;
  border-color: #dee2e6;
  box-shadow: none;
}

.cell-label {
  font-weight: bold;
  font-size: 16px;
  color: white;
  text-shadow: 1px 1px 2px rgba(0,0,0,0.5);
  margin-bottom: 2px;
}

.cell-frequency {
  font-size: 12px;
  color: white;
  text-shadow: 1px 1px 2px rgba(0,0,0,0.5);
  opacity: 0.9;
}

.heatmap-grid:not(.show-labels) .heatmap-cell {
  min-height: 40px;
}

.heatmap-stats {
  background: #f8f9fa;
  border-radius: 8px;
  padding: 20px;
  min-width: 250px;
}

.heatmap-stats h4 {
  color: #2c3e50;
  margin-bottom: 15px;
  text-align: center;
}

.stats-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.stat-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;
  border-bottom: 1px solid #dee2e6;
}

.stat-item:last-child {
  border-bottom: none;
}

.stat-label {
  color: #6c757d;
  font-size: 14px;
}

.stat-value {
  font-weight: 600;
  color: #2c3e50;
}

.heatmap-tooltip {
  position: fixed;
  background: rgba(0, 0, 0, 0.9);
  color: white;
  padding: 12px;
  border-radius: 6px;
  font-size: 14px;
  z-index: 1000;
  pointer-events: none;
  max-width: 250px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.3);
}

.tooltip-header {
  font-weight: bold;
  margin-bottom: 8px;
  color: #fff;
  border-bottom: 1px solid rgba(255,255,255,0.3);
  padding-bottom: 4px;
}

.tooltip-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.tooltip-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.tooltip-label {
  color: #ccc;
}

.tooltip-value {
  font-weight: 500;
  color: white;
}

.tooltip-value.hot {
  color: #ff6b6b;
}

.tooltip-value.cold {
  color: #74c0fc;
}

.tooltip-value.normal {
  color: #69db7c;
}

.tooltip-value.no-data {
  color: #adb5bd;
}

/* Responsive Design */
@media (max-width: 768px) {
  .heatmap-header {
    flex-direction: column;
    align-items: stretch;
  }

  .heatmap-controls {
    justify-content: center;
    flex-wrap: wrap;
  }

  .heatmap-container {
    grid-template-columns: 1fr;
    gap: 20px;
  }

  .heatmap-grid {
    grid-template-columns: repeat(6, 1fr);
    padding: 15px;
  }

  .heatmap-cell {
    min-height: 50px;
  }

  .cell-label {
    font-size: 14px;
  }

  .cell-frequency {
    font-size: 10px;
  }

  .heatmap-stats {
    min-width: auto;
  }

  .legend-categories {
    flex-wrap: wrap;
    gap: 15px;
  }
}

@media (max-width: 480px) {
  .heatmap-grid {
    grid-template-columns: repeat(5, 1fr);
    gap: 3px;
    padding: 10px;
  }

  .heatmap-cell {
    min-height: 40px;
  }

  .cell-label {
    font-size: 12px;
  }

  .cell-frequency {
    font-size: 9px;
  }

  .heatmap-controls {
    flex-direction: column;
    align-items: stretch;
  }

  .control-group {
    justify-content: space-between;
  }

  .heatmap-tooltip {
    font-size: 12px;
    padding: 8px;
    max-width: 200px;
  }
}

/* Animation for data loading */
@keyframes fadeIn {
  from {
    opacity: 0;
    transform: scale(0.9);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

.heatmap-cell {
  animation: fadeIn 0.3s ease-out;
}

/* High contrast mode support */
@media (prefers-contrast: high) {
  .heatmap-cell {
    border-width: 3px;
  }
  
  .legend-color, .legend-gradient {
    border-width: 2px;
    border-color: #000;
  }
  
  .heatmap-tooltip {
    border: 2px solid #fff;
  }
}

/* Reduced motion support */
@media (prefers-reduced-motion: reduce) {
  .heatmap-cell {
    transition: none;
    animation: none;
  }
  
  .heatmap-cell:hover {
    transform: none;
  }
}
</style>