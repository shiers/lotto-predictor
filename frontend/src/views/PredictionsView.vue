<template>
  <main>
    <div class="predictions">
      <h1>Predictions</h1>
      <p>Generate and view lottery number predictions</p>
      
      <div class="prediction-controls">
        <div class="control-group">
          <label for="predictionCount">Number of Predictions:</label>
          <select id="predictionCount" v-model="predictionCount" class="prediction-select">
            <option v-for="n in 10" :key="n" :value="n">{{ n }}</option>
          </select>
        </div>
        
        <div class="button-group">
          <button @click="generatePredictions" :disabled="isLoading" class="generate-button primary">
            <span v-if="isLoading">Generating...</span>
            <span v-else>Generate New</span>
          </button>
          
          <button @click="loadStoredPredictions" :disabled="isLoading" class="generate-button secondary">
            <span v-if="isLoading">Loading...</span>
            <span v-else>Load Stored (Simple)</span>
          </button>
          
          <button @click="() => loadPaginatedPredictions(1)" :disabled="isLoading" class="generate-button secondary">
            <span v-if="isLoading">Loading...</span>
            <span v-else>Browse All Stored</span>
          </button>
          
          <button @click="updateScores" :disabled="isLoading || isUpdatingScores" class="generate-button tertiary">
            <span v-if="isUpdatingScores">Updating Scores...</span>
            <span v-else>Update Scores</span>
          </button>
        </div>
      </div>
      
      <div v-if="predictions.length > 0" class="predictions-table">
        <h3>Generated Predictions</h3>
        <table>
          <thead>
            <tr>
              <th>#</th>
              <th>Numbers</th>
              <th>Powerball</th>
              <th>Source</th>
              <th>Original Score</th>
              <th>Updated Score</th>
              <th>Created</th>
              <th v-if="showTargetDate">Target Draw</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(prediction, index) in displayedPredictions" :key="prediction.id || index" class="prediction-row">
              <td>{{ getDisplayIndex(index) }}</td>
              <td class="numbers-cell">
                <span v-for="number in prediction.numbers" :key="number" class="number-ball">
                  {{ number }}
                </span>
              </td>
              <td class="powerball-cell">
                <span v-if="prediction.powerball !== undefined && prediction.powerball !== null" class="powerball-ball">
                  {{ prediction.powerball }}
                </span>
                <span v-else class="no-powerball">-</span>
              </td>
              <td class="source-cell">{{ prediction.source }}</td>
              <td class="score-cell">{{ prediction.score?.toFixed(2) || 'N/A' }}</td>
              <td class="score-cell">
                <span v-if="prediction.hasScoreUpdates" class="updated-score">
                  {{ prediction.updatedScore?.toFixed(2) || 'N/A' }}
                  <span class="score-badge">Updated</span>
                </span>
                <span v-else class="no-update">-</span>
              </td>
              <td class="date-cell">{{ formatDate(prediction.createdAt) }}</td>
              <td v-if="showTargetDate" class="date-cell">
                {{ prediction.targetDrawDate ? formatDate(prediction.targetDrawDate) : 'N/A' }}
              </td>
              <td class="actions-cell">
                <button 
                  v-if="prediction.id && prediction.hasScoreUpdates" 
                  @click="viewScoreHistory(prediction.id)"
                  class="history-button"
                  title="View score history"
                >
                  📊 History
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      
      <!-- Pagination Controls -->
      <div v-if="paginatedMode && paginationData" class="pagination-controls">
        <div class="pagination-info">
          <span>Showing {{ paginationData.firstItemIndex }} - {{ paginationData.lastItemIndex }} of {{ paginationData.totalItems }} predictions</span>
        </div>
        
        <div class="pagination-buttons">
          <button 
            @click="goToPage(1)" 
            :disabled="!paginationData.hasPreviousPage || isLoading"
            class="pagination-button"
          >
            First
          </button>
          
          <button 
            @click="goToPage(currentPage - 1)" 
            :disabled="!paginationData.hasPreviousPage || isLoading"
            class="pagination-button"
          >
            Previous
          </button>
          
          <span class="page-info">
            Page {{ currentPage }} of {{ paginationData.totalPages }}
          </span>
          
          <button 
            @click="goToPage(currentPage + 1)" 
            :disabled="!paginationData.hasNextPage || isLoading"
            class="pagination-button"
          >
            Next
          </button>
          
          <button 
            @click="goToPage(paginationData.totalPages)" 
            :disabled="!paginationData.hasNextPage || isLoading"
            class="pagination-button"
          >
            Last
          </button>
        </div>
        
        <div class="page-size-control">
          <label for="pageSize">Per page:</label>
          <select id="pageSize" v-model="pageSize" @change="() => loadPaginatedPredictions(1)" class="prediction-select">
            <option value="10">10</option>
            <option value="25">25</option>
            <option value="50">50</option>
            <option value="100">100</option>
          </select>
        </div>
      </div>

      <div v-if="predictions.length === 0 && !isLoading" class="no-predictions">
        <p>No predictions loaded. Click "Generate New" to create fresh predictions, "Load Stored (Simple)" for recent ones, or "Browse All Stored" to see all with pagination.</p>
      </div>
      
      <div v-if="isLoading" class="loading-state">
        <div class="loading-spinner"></div>
        <p>Generating predictions...</p>
      </div>
    </div>

    <!-- Score History Modal -->
    <div v-if="scoreHistoryModal" class="modal-overlay" @click="closeScoreHistory">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>Score History - Prediction #{{ selectedPredictionId }}</h3>
          <button @click="closeScoreHistory" class="close-button">×</button>
        </div>
        
        <div class="modal-body">
          <div v-if="scoreHistory.length === 0" class="no-history">
            <p>No score updates found for this prediction.</p>
          </div>
          
          <div v-else class="history-table">
            <table>
              <thead>
                <tr>
                  <th>Date Updated</th>
                  <th>Original Score</th>
                  <th>Updated Score</th>
                  <th>Triggering Draw</th>
                  <th>Exact Matches</th>
                  <th>Overall Accuracy</th>
                  <th>Reason</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="history in scoreHistory" :key="history.id">
                  <td>{{ formatDate(history.updatedAt) }}</td>
                  <td>{{ history.originalScore.toFixed(3) }}</td>
                  <td>{{ history.updatedScore.toFixed(3) }}</td>
                  <td>{{ history.triggeringDrawId }}</td>
                  <td>{{ history.exactMatches }}/6</td>
                  <td>{{ (history.overallAccuracy * 100).toFixed(1) }}%</td>
                  <td class="reason-cell">{{ history.updateReason }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import { useAppStore } from '@/stores/app'

interface PredictionResult {
  id?: number
  numbers: number[]
  powerball?: number
  source: string
  score?: number
  updatedScore?: number
  lastScoreUpdate?: string
  hasScoreUpdates?: boolean
  createdAt: string
  targetDrawDate?: string
  confidenceScore?: number
  reasoningExplanation?: string
}

interface PaginationData {
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  firstItemIndex: number
  lastItemIndex: number
}

const appStore = useAppStore()

// Reactive state
const predictionCount = ref(10)
const predictions = ref<PredictionResult[]>([])
const isLoading = ref(false)
const isUpdatingScores = ref(false)
const paginatedMode = ref(false)
const currentPage = ref(1)
const pageSize = ref(50)
const paginationData = ref<PaginationData | null>(null)

// Computed properties
const displayedPredictions = computed(() => {
  return paginatedMode.value ? predictions.value : predictions.value
})

const showTargetDate = computed(() => {
  return predictions.value.some(p => p.targetDrawDate)
})

// Generate predictions
const generatePredictions = async () => {
  isLoading.value = true
  appStore.setLoading(true)
  paginatedMode.value = false
  paginationData.value = null
  
  try {
    // Use the dedicated predictions endpoint for generating new predictions
    const response = await api.get(`/predictions/generate?count=${predictionCount.value}`)
    predictions.value = response.data
    
    appStore.addNotification({
      type: 'success',
      title: 'Predictions Generated',
      message: `Generated ${predictions.value.length} predictions successfully`
    })
  } catch (error: any) {
    const errorMessage = error.response?.data?.error || error.response?.data?.message || error.message || 'Failed to generate predictions'
    
    appStore.addNotification({
      type: 'error',
      title: 'Prediction Failed',
      message: errorMessage
    })
    
    console.error('Failed to generate predictions:', error)
  } finally {
    isLoading.value = false
    appStore.setLoading(false)
  }
}

// Update prediction scores
const updateScores = async () => {
  isUpdatingScores.value = true
  appStore.setLoading(true)
  
  try {
    const response = await api.post('/predictions/update-scores?drawCount=10')
    const result = response.data
    
    appStore.addNotification({
      type: 'success',
      title: 'Scores Updated',
      message: `Updated ${result.predictionsUpdated} predictions, created ${result.scoreHistoryRecordsCreated} history records`
    })
    
    // Reload predictions to show updated scores
    if (predictions.value.length > 0) {
      if (paginatedMode.value) {
        await loadPaginatedPredictions(currentPage.value)
      } else {
        await loadStoredPredictions()
      }
    }
  } catch (error: any) {
    const errorMessage = error.response?.data?.error || error.response?.data?.message || error.message || 'Failed to update scores'
    
    appStore.addNotification({
      type: 'error',
      title: 'Score Update Failed',
      message: errorMessage
    })
    
    console.error('Failed to update scores:', error)
  } finally {
    isUpdatingScores.value = false
    appStore.setLoading(false)
  }
}

// Load stored predictions (simple)
const loadStoredPredictions = async () => {
  isLoading.value = true
  appStore.setLoading(true)
  paginatedMode.value = false
  paginationData.value = null
  
  try {
    const response = await api.get(`/predictions/stored?count=${predictionCount.value}`)
    predictions.value = response.data
    
    if (predictions.value.length > 0) {
      appStore.addNotification({
        type: 'info',
        title: 'Stored Predictions Loaded',
        message: `Loaded ${predictions.value.length} stored predictions`
      })
    }
  } catch (error: any) {
    // If no stored predictions, that's okay - just show empty state
    if (error.response?.status !== 404) {
      const errorMessage = error.response?.data?.error || error.response?.data?.message || error.message || 'Failed to load stored predictions'
      
      appStore.addNotification({
        type: 'error',
        title: 'Load Failed',
        message: errorMessage
      })
      
      console.error('Failed to load stored predictions:', error)
    }
  } finally {
    isLoading.value = false
    appStore.setLoading(false)
  }
}

// Load paginated predictions
const loadPaginatedPredictions = async (page: number = 1) => {
  isLoading.value = true
  appStore.setLoading(true)
  paginatedMode.value = true
  currentPage.value = page
  
  try {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.value.toString(),
      sortBy: 'createdAt',
      sortDirection: 'desc'
    })
    
    const response = await api.get(`/predictions/stored/paginated?${params}`)
    const data = response.data
    
    predictions.value = data.items
    paginationData.value = data.metadata || {
      page: data.page,
      pageSize: data.pageSize,
      totalItems: data.totalItems,
      totalPages: data.totalPages,
      hasPreviousPage: data.hasPreviousPage,
      hasNextPage: data.hasNextPage,
      firstItemIndex: data.firstItemIndex,
      lastItemIndex: data.lastItemIndex
    }
    
    if (predictions.value.length > 0) {
      appStore.addNotification({
        type: 'info',
        title: 'Paginated Predictions Loaded',
        message: `Loaded page ${page} of ${paginationData.value.totalPages} (${paginationData.value.totalItems} total predictions)`
      })
    }
  } catch (error: any) {
    const errorMessage = error.response?.data?.error || error.response?.data?.message || error.message || 'Failed to load paginated predictions'
    
    appStore.addNotification({
      type: 'error',
      title: 'Load Failed',
      message: errorMessage
    })
    
    console.error('Failed to load paginated predictions:', error)
    
    // Reset pagination state on error
    paginatedMode.value = false
    paginationData.value = null
    predictions.value = []
  } finally {
    isLoading.value = false
    appStore.setLoading(false)
  }
}

// Pagination navigation
const goToPage = (page: number) => {
  if (page >= 1 && paginationData.value && page <= paginationData.value.totalPages) {
    loadPaginatedPredictions(page)
  }
}

// Score history functionality
const scoreHistoryModal = ref(false)
const scoreHistory = ref<any[]>([])
const selectedPredictionId = ref<number | null>(null)

const viewScoreHistory = async (predictionId: number) => {
  try {
    // Validate prediction ID
    if (!predictionId || predictionId <= 0) {
      appStore.addNotification({
        type: 'error',
        title: 'Invalid Prediction',
        message: 'Cannot load history for this prediction'
      })
      return
    }

    selectedPredictionId.value = predictionId
    const response = await api.get(`/predictionScore/${predictionId}/history`)
    
    // Check if history exists
    if (!response.data || response.data.length === 0) {
      appStore.addNotification({
        type: 'info',
        title: 'No History Available',
        message: 'This prediction has no score update history yet'
      })
      return
    }
    
    scoreHistory.value = response.data
    scoreHistoryModal.value = true
  } catch (error: any) {
    const errorMessage = error.response?.status === 404 
      ? 'No score history found for this prediction'
      : 'Could not load score history for this prediction'
    
    appStore.addNotification({
      type: 'error',
      title: 'Failed to Load History',
      message: errorMessage
    })
    console.error('Failed to load score history:', error)
  }
}

const closeScoreHistory = () => {
  scoreHistoryModal.value = false
  scoreHistory.value = []
  selectedPredictionId.value = null
}

// Helper functions
const getDisplayIndex = (index: number): number => {
  if (paginatedMode.value && paginationData.value) {
    return paginationData.value.firstItemIndex + index
  }
  return index + 1
}

const formatDate = (dateString: string): string => {
  if (!dateString) return 'N/A'
  
  try {
    const date = new Date(dateString)
    return date.toLocaleDateString('en-NZ', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  } catch {
    return 'Invalid Date'
  }
}

// Component mounted - no automatic loading
onMounted(() => {
  // Don't automatically load predictions - wait for user action
  // User can click "Generate New" or "Load Stored" buttons
})
</script>

<style scoped>
.predictions {
  padding: 2rem;
  max-width: 1300px;
  margin: 0 auto;
}

h1 {
  font-size: 2.5rem;
  margin-bottom: 1rem;
  color: var(--color-heading);
}

.prediction-controls {
  display: flex;
  align-items: center;
  gap: 2rem;
  margin: 2rem 0;
  padding: 1.5rem;
  background: var(--color-background-soft);
  border-radius: 8px;
  flex-wrap: wrap;
}

@media (max-width: 768px) {
  .prediction-controls {
    flex-direction: column;
    align-items: stretch;
    gap: 1rem;
  }
  
  .control-group {
    justify-content: center;
  }
  
  .button-group {
    justify-content: center;
  }
}

.control-group {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.control-group label {
  font-weight: 500;
  color: var(--color-heading);
}

.prediction-select {
  padding: 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  background: var(--color-background);
  color: var(--color-text);
  font-size: 1rem;
}

.button-group {
  display: flex;
  gap: 1rem;
}

.generate-button {
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 1rem;
  transition: all 0.3s ease;
  font-weight: 500;
}

.generate-button.primary {
  background: #42b883;
  color: white;
}

.generate-button.primary:hover:not(:disabled) {
  background: #369870;
}

.generate-button.secondary {
  background: var(--color-background-soft);
  color: var(--color-text);
  border: 1px solid var(--color-border);
}

.generate-button.secondary:hover:not(:disabled) {
  background: var(--color-background-mute);
}

.generate-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.predictions-table {
  margin-top: 2rem;
}

.predictions-table h3 {
  color: var(--color-heading);
  margin-bottom: 1rem;
}

table {
  width: 100%;
  border-collapse: collapse;
  background: var(--color-background);
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

th, td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid var(--color-border);
}

th {
  background: var(--color-background-soft);
  font-weight: 600;
  color: var(--color-heading);
}

.prediction-row:hover {
  background: var(--color-background-soft);
}

.numbers-cell {
  display: flex;
  gap: 0.1rem;
}

.number-ball {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  background: #42b883;
  color: white;
  border-radius: 50%;
  font-weight: bold;
  font-size: 0.9rem;
}

.powerball-cell {
  text-align: center;
}

.powerball-ball {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  background: #e74c3c;
  color: white;
  border-radius: 50%;
  font-weight: bold;
  font-size: 0.9rem;
  border: 2px solid #c0392b;
}

.no-powerball {
  color: var(--color-text-muted);
  font-style: italic;
}

.source-cell {
  font-weight: 500;
  color: var(--color-text);
  flex-wrap: wrap;
}

.score-cell {
  font-family: monospace;
  color: var(--color-text);
}

.date-cell {
  font-size: 0.9rem;
  color: var(--color-text);
  white-space: nowrap;
}

/* Pagination Controls */
.pagination-controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 2rem 0;
  padding: 1rem;
  background: var(--color-background-soft);
  border-radius: 8px;
  flex-wrap: wrap;
  gap: 1rem;
}

@media (max-width: 768px) {
  .pagination-controls {
    flex-direction: column;
    align-items: stretch;
  }
  
  .pagination-buttons {
    justify-content: center;
  }
  
  .pagination-info,
  .page-size-control {
    text-align: center;
  }
}

.pagination-info {
  font-size: 0.9rem;
  color: var(--color-text);
}

.pagination-buttons {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.pagination-button {
  padding: 0.5rem 1rem;
  border: 1px solid var(--color-border);
  background: var(--color-background);
  color: var(--color-text);
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  transition: all 0.3s ease;
}

.pagination-button:hover:not(:disabled) {
  background: var(--color-background-soft);
  border-color: #42b883;
}

.pagination-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.page-info {
  margin: 0 1rem;
  font-weight: 500;
  color: var(--color-heading);
}

.page-size-control {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.page-size-control label {
  font-size: 0.9rem;
  color: var(--color-text);
}

.no-predictions {
  text-align: center;
  padding: 3rem;
  color: var(--color-text);
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 3rem;
  gap: 1rem;
}

.loading-spinner {
  width: 2rem;
  height: 2rem;
  border: 3px solid var(--color-border);
  border-top: 3px solid #42b883;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

/* Score Update Styles */
.updated-score {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.score-badge {
  background: #42b883;
  color: white;
  padding: 0.2rem 0.5rem;
  border-radius: 12px;
  font-size: 0.7rem;
  font-weight: bold;
}

.no-update {
  color: var(--color-text-muted);
  font-style: italic;
}

.actions-cell {
  text-align: center;
}

.history-button {
  background: #f39c12;
  color: white;
  border: none;
  padding: 0.4rem 0.8rem;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.8rem;
  transition: background 0.3s ease;
}

.history-button:hover {
  background: #e67e22;
}

/* Modal Styles */
.modal-overlay {
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
  padding: 1rem;
}

.modal-content {
  background: var(--color-background);
  border-radius: 8px;
  max-width: 90vw;
  max-height: 90vh;
  overflow: hidden;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid var(--color-border);
  background: var(--color-background-soft);
}

.modal-header h3 {
  margin: 0;
  color: var(--color-heading);
}

.close-button {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: var(--color-text);
  padding: 0.25rem;
  line-height: 1;
}

.close-button:hover {
  color: var(--color-heading);
}

.modal-body {
  padding: 1.5rem;
  max-height: 70vh;
  overflow-y: auto;
}

.no-history {
  text-align: center;
  padding: 2rem;
  color: var(--color-text);
}

.history-table {
  overflow-x: auto;
}

.history-table table {
  min-width: 800px;
}

.reason-cell {
  max-width: 200px;
  word-wrap: break-word;
  font-size: 0.9rem;
}

@media (max-width: 768px) {
  .modal-content {
    max-width: 95vw;
    margin: 0.5rem;
  }
  
  .history-table table {
    font-size: 0.8rem;
  }
  
  th, td {
    padding: 0.5rem;
  }
  
  .reason-cell {
    max-width: 150px;
  }
}
</style>