<template>
  <div class="frequency-analysis">
    <div class="analysis-header">
      <h2>Frequency Analysis</h2>
      <p>Analyze number frequency patterns across different ranges and time periods</p>
    </div>

    <div class="analysis-controls">
      <div class="control-section">
        <h3>Analysis Type</h3>
        <div class="analysis-type-tabs">
          <button
            v-for="type in analysisTypes"
            :key="type.value"
            @click="selectedAnalysisType = type.value"
            :class="['tab-button', { active: selectedAnalysisType === type.value }]"
          >
            {{ type.label }}
          </button>
        </div>
      </div>

      <div class="action-buttons">
        <button
          @click="performAnalysis"
          :disabled="!canAnalyze || isLoading"
          class="analyze-button"
        >
          {{ isLoading ? 'Analyzing...' : 'Analyze' }}
        </button>
        <button
          @click="clearResults"
          :disabled="!hasResults"
          class="clear-button"
        >
          Clear Results
        </button>
      </div>
    </div>

    <div v-if="hasResults" class="results-section">
      <div class="results-display">
        <div v-if="selectedAnalysisType === 'individual'" class="individual-results">
          <h3>Individual Number Frequencies</h3>
          <div class="frequency-grid">
            <div
              v-for="frequency in individualResults"
              :key="frequency.number"
              class="frequency-card"
            >
              <div class="frequency-number">{{ frequency.number }}</div>
              <div class="frequency-details">
                <div>Occurrences: {{ frequency.totalOccurrences }}</div>
                <div>Percentage: {{ frequency.percentage.toFixed(2) }}%</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-if="individualResults.length > 0" class="heatmap-section">
        <NumberHeatmap
          :frequency-data="individualResults"
          @number-click="onHeatmapNumberClick"
        />
      </div>
    </div>

    <div v-if="error" class="error-message">
      <h3>Error</h3>
      <p>{{ error }}</p>
      <button @click="clearError">Dismiss</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { FrequencyService, type NumberFrequency } from '../services/frequencyService'
import NumberHeatmap from './NumberHeatmap.vue'

// Props
interface Props {
  initialAnalysisType?: 'individual' | 'ranges' | 'hotcold'
  showFrequencyData?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  initialAnalysisType: 'individual',
  showFrequencyData: true
})

// Reactive data
const selectedAnalysisType = ref(props.initialAnalysisType)
const isLoading = ref(false)
const error = ref('')
const individualResults = ref<NumberFrequency[]>([])

// Analysis types
const analysisTypes = [
  { value: 'individual', label: 'Individual Numbers' },
  { value: 'ranges', label: 'Number Ranges' },
  { value: 'hotcold', label: 'Hot & Cold' }
]

// Computed properties
const canAnalyze = computed(() => true)
const hasResults = computed(() => individualResults.value.length > 0)

// Methods
const performAnalysis = async () => {
  isLoading.value = true
  error.value = ''
  
  try {
    if (selectedAnalysisType.value === 'individual') {
      individualResults.value = await FrequencyService.getNumberFrequencies()
    }
  } catch (err: any) {
    error.value = err.response?.data?.error || err.message || 'An error occurred during analysis'
  } finally {
    isLoading.value = false
  }
}

const clearResults = () => {
  individualResults.value = []
  error.value = ''
}

const clearError = () => {
  error.value = ''
}

const onHeatmapNumberClick = (number: number) => {
  console.log('Clicked number:', number)
}
</script>

<style scoped>
.frequency-analysis {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

.analysis-header {
  text-align: center;
  margin-bottom: 30px;
}

.analysis-controls {
  background: #f8f9fa;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 30px;
}

.control-section {
  margin-bottom: 25px;
}

.analysis-type-tabs {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.tab-button {
  padding: 10px 20px;
  border: 2px solid #e9ecef;
  background: white;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
}

.tab-button.active {
  background: #007bff;
  color: white;
  border-color: #007bff;
}

.action-buttons {
  display: flex;
  gap: 15px;
  flex-wrap: wrap;
}

.analyze-button, .clear-button {
  padding: 12px 24px;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 500;
}

.analyze-button {
  background: #007bff;
  color: white;
}

.clear-button {
  background: #6c757d;
  color: white;
}

.results-section {
  margin-top: 30px;
}

.results-display {
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.frequency-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 20px;
}

.frequency-card {
  border: 2px solid #e9ecef;
  border-radius: 8px;
  padding: 15px;
  text-align: center;
}

.frequency-number {
  font-size: 2em;
  font-weight: bold;
  margin-bottom: 10px;
  color: #2c3e50;
}

.heatmap-section {
  margin-top: 30px;
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.error-message {
  background: #f8d7da;
  border: 1px solid #f5c6cb;
  color: #721c24;
  padding: 20px;
  border-radius: 8px;
  margin-top: 20px;
}
</style>