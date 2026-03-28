<template>
  <div class="navigation-context">
    <div class="context-header">
      <h4>Navigation Context</h4>
      <button @click="toggleExpanded" class="toggle-button">
        <span class="icon">{{ isExpanded ? '▼' : '▶' }}</span>
        {{ isExpanded ? 'Collapse' : 'Expand' }}
      </button>
    </div>

    <div v-if="isExpanded" class="context-content">
      <!-- Position Information -->
      <div class="context-section">
        <h5>Position Information</h5>
        <div class="info-grid">
          <div class="info-item">
            <span class="label">Current Position:</span>
            <span class="value">{{ context.currentPosition }} of {{ context.totalDraws }}</span>
          </div>
          <div class="info-item">
            <span class="label">Progress:</span>
            <div class="progress-container">
              <div class="progress-bar">
                <div 
                  class="progress-fill" 
                  :style="{ width: progressPercentage + '%' }"
                ></div>
              </div>
              <span class="progress-text">{{ progressPercentage.toFixed(1) }}%</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Boundary Information -->
      <div class="context-section">
        <h5>Boundary Status</h5>
        <div class="boundary-info">
          <div class="boundary-item" :class="{ active: !context.hasPrevious }">
            <span class="boundary-icon">⏮️</span>
            <span class="boundary-text">
              {{ !context.hasPrevious ? 'At First Draw' : 'Can Go Previous' }}
            </span>
          </div>
          <div class="boundary-item" :class="{ active: !context.hasNext }">
            <span class="boundary-icon">⏭️</span>
            <span class="boundary-text">
              {{ !context.hasNext ? 'At Last Draw' : 'Can Go Next' }}
            </span>
          </div>
        </div>
      </div>

      <!-- Date Range Information -->
      <div class="context-section">
        <h5>Available Data Range</h5>
        <div class="date-range">
          <div class="date-item">
            <span class="date-label">Earliest Draw:</span>
            <span class="date-value">{{ formatDate(context.earliestDate) }}</span>
          </div>
          <div class="date-separator">
            <span class="separator-line"></span>
            <span class="separator-text">to</span>
            <span class="separator-line"></span>
          </div>
          <div class="date-item">
            <span class="date-label">Latest Draw:</span>
            <span class="date-value">{{ formatDate(context.latestDate) }}</span>
          </div>
        </div>
        <div class="date-stats">
          <span class="stat-item">
            <span class="stat-label">Total Span:</span>
            <span class="stat-value">{{ totalDaysSpan }} days</span>
          </span>
          <span class="stat-item">
            <span class="stat-label">Average Frequency:</span>
            <span class="stat-value">{{ averageFrequency }} draws/week</span>
          </span>
        </div>
      </div>

      <!-- Missing Draws Information -->
      <div v-if="context.missingDrawNumbers.length > 0" class="context-section">
        <h5>Missing Draws</h5>
        <div class="missing-draws">
          <p class="missing-description">
            The following draw numbers are missing from the sequence around the current draw:
          </p>
          <div class="missing-numbers">
            <span 
              v-for="drawNumber in displayedMissingDraws" 
              :key="drawNumber"
              class="missing-number"
              @click="jumpToMissingDraw(drawNumber)"
            >
              {{ drawNumber }}
            </span>
            <span v-if="context.missingDrawNumbers.length > maxDisplayedMissing" class="more-missing">
              +{{ context.missingDrawNumbers.length - maxDisplayedMissing }} more
            </span>
          </div>
        </div>
      </div>

      <!-- Quick Navigation -->
      <div class="context-section">
        <h5>Quick Navigation</h5>
        <div class="quick-nav">
          <button 
            @click="jumpToFirstDraw" 
            :disabled="!context.hasPrevious"
            class="quick-nav-button"
          >
            <span class="icon">⏮️</span>
            First Draw
          </button>
          <button 
            @click="jumpToRandomDraw" 
            class="quick-nav-button"
          >
            <span class="icon">🎲</span>
            Random Draw
          </button>
          <button 
            @click="jumpToLastDraw" 
            :disabled="!context.hasNext"
            class="quick-nav-button"
          >
            <span class="icon">⏭️</span>
            Last Draw
          </button>
        </div>
      </div>

      <!-- Navigation Statistics -->
      <div class="context-section">
        <h5>Statistics</h5>
        <div class="stats-grid">
          <div class="stat-card">
            <div class="stat-number">{{ context.totalDraws }}</div>
            <div class="stat-label">Total Draws</div>
          </div>
          <div class="stat-card">
            <div class="stat-number">{{ remainingDraws }}</div>
            <div class="stat-label">Remaining</div>
          </div>
          <div class="stat-card">
            <div class="stat-number">{{ completionPercentage }}%</div>
            <div class="stat-label">Complete</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { NavigationContext as NavigationContextType } from '@/services/navigationService'

interface Props {
  context: NavigationContextType
  maxDisplayedMissing?: number
}

const props = withDefaults(defineProps<Props>(), {
  maxDisplayedMissing: 10
})

const emit = defineEmits<{
  jumpToDraw: [drawNumber: number]
}>()

// Reactive state
const isExpanded = ref(false)

// Computed properties
const progressPercentage = computed(() => {
  if (props.context.totalDraws === 0) return 0
  return (props.context.currentPosition / props.context.totalDraws) * 100
})

const totalDaysSpan = computed(() => {
  const earliest = new Date(props.context.earliestDate)
  const latest = new Date(props.context.latestDate)
  const diffTime = Math.abs(latest.getTime() - earliest.getTime())
  return Math.ceil(diffTime / (1000 * 60 * 60 * 24))
})

const averageFrequency = computed(() => {
  if (totalDaysSpan.value === 0) return 0
  const weeksSpan = totalDaysSpan.value / 7
  return (props.context.totalDraws / weeksSpan).toFixed(1)
})

const remainingDraws = computed(() => {
  return props.context.totalDraws - props.context.currentPosition
})

const completionPercentage = computed(() => {
  return Math.round(progressPercentage.value)
})

const displayedMissingDraws = computed(() => {
  return props.context.missingDrawNumbers.slice(0, props.maxDisplayedMissing)
})

// Methods
const toggleExpanded = () => {
  isExpanded.value = !isExpanded.value
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-NZ', {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

const jumpToFirstDraw = () => {
  // Emit jump to the earliest available draw
  // This would need to be calculated based on the earliest date
  emit('jumpToDraw', 1) // Placeholder - should be the actual first draw number
}

const jumpToLastDraw = () => {
  // Emit jump to the latest available draw
  emit('jumpToDraw', props.context.currentDraw.draw + remainingDraws.value)
}

const jumpToRandomDraw = () => {
  // Generate a random draw number within the available range
  const minDraw = 1
  const maxDraw = props.context.totalDraws
  const randomDraw = Math.floor(Math.random() * (maxDraw - minDraw + 1)) + minDraw
  emit('jumpToDraw', randomDraw)
}

const jumpToMissingDraw = (drawNumber: number) => {
  emit('jumpToDraw', drawNumber)
}
</script>

<style scoped>
.navigation-context {
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  overflow: hidden;
}

.context-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.5rem;
  background: var(--color-background);
  border-bottom: 1px solid var(--color-border);
}

.context-header h4 {
  color: var(--color-heading);
  margin: 0;
  font-size: 1.1rem;
}

.toggle-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: none;
  border: none;
  cursor: pointer;
  color: var(--color-text);
  font-size: 0.9rem;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  transition: background-color 0.3s ease;
}

.toggle-button:hover {
  background: var(--color-background-soft);
}

.context-content {
  padding: 1.5rem;
}

.context-section {
  margin-bottom: 2rem;
}

.context-section:last-child {
  margin-bottom: 0;
}

.context-section h5 {
  color: var(--color-heading);
  margin: 0 0 1rem 0;
  font-size: 1rem;
  font-weight: 600;
}

.info-grid {
  display: grid;
  gap: 1rem;
}

.info-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem;
  background: var(--color-background);
  border-radius: 6px;
}

.info-item .label {
  color: var(--color-text);
  font-weight: 500;
}

.info-item .value {
  color: var(--color-heading);
  font-weight: 600;
}

.progress-container {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
  margin-left: 1rem;
}

.progress-bar {
  flex: 1;
  height: 8px;
  background: var(--color-border);
  border-radius: 4px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #42b883, #369870);
  transition: width 0.3s ease;
}

.progress-text {
  font-size: 0.875rem;
  color: var(--color-text);
  font-weight: 500;
  min-width: 3rem;
  text-align: right;
}

.boundary-info {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.boundary-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
  background: var(--color-background);
  border-radius: 6px;
  border: 2px solid transparent;
  transition: all 0.3s ease;
}

.boundary-item.active {
  border-color: #42b883;
  background: rgba(66, 184, 131, 0.1);
}

.boundary-icon {
  font-size: 1.25rem;
}

.boundary-text {
  color: var(--color-text);
  font-weight: 500;
}

.boundary-item.active .boundary-text {
  color: var(--color-heading);
  font-weight: 600;
}

.date-range {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.date-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 1rem;
  background: var(--color-background);
  border-radius: 6px;
  flex: 1;
}

.date-label {
  font-size: 0.875rem;
  color: var(--color-text);
  margin-bottom: 0.5rem;
}

.date-value {
  font-weight: 600;
  color: var(--color-heading);
}

.date-separator {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--color-text);
}

.separator-line {
  width: 1rem;
  height: 1px;
  background: var(--color-border);
}

.separator-text {
  font-size: 0.875rem;
}

.date-stats {
  display: flex;
  gap: 2rem;
  justify-content: center;
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.25rem;
}

.stat-label {
  font-size: 0.875rem;
  color: var(--color-text);
}

.stat-value {
  font-weight: 600;
  color: var(--color-heading);
}

.missing-draws {
  background: var(--color-background);
  padding: 1rem;
  border-radius: 6px;
  border-left: 4px solid #ffc107;
}

.missing-description {
  color: var(--color-text);
  margin: 0 0 1rem 0;
  font-size: 0.9rem;
}

.missing-numbers {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.missing-number {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.25rem 0.5rem;
  background: #fff3cd;
  color: #856404;
  border-radius: 4px;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s ease;
}

.missing-number:hover {
  background: #ffc107;
  color: white;
}

.more-missing {
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.5rem;
  color: var(--color-text);
  font-size: 0.875rem;
  font-style: italic;
}

.quick-nav {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 0.75rem;
}

.quick-nav-button {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 6px;
  cursor: pointer;
  color: var(--color-text);
  font-size: 0.9rem;
  transition: all 0.3s ease;
}

.quick-nav-button:hover:not(:disabled) {
  background: #42b883;
  color: white;
  border-color: #42b883;
}

.quick-nav-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(100px, 1fr));
  gap: 1rem;
}

.stat-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 1rem;
  background: var(--color-background);
  border-radius: 6px;
  border: 1px solid var(--color-border);
}

.stat-number {
  font-size: 1.5rem;
  font-weight: bold;
  color: #42b883;
  margin-bottom: 0.25rem;
}

.stat-card .stat-label {
  font-size: 0.875rem;
  color: var(--color-text);
  text-align: center;
}

/* Responsive */
@media (max-width: 768px) {
  .context-content {
    padding: 1rem;
  }
  
  .boundary-info {
    grid-template-columns: 1fr;
  }
  
  .date-range {
    flex-direction: column;
  }
  
  .date-separator {
    transform: rotate(90deg);
  }
  
  .date-stats {
    flex-direction: column;
    gap: 1rem;
  }
  
  .quick-nav {
    grid-template-columns: 1fr;
  }
  
  .stats-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}
</style>