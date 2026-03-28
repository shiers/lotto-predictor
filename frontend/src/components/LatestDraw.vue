<template>
  <div id="latest" class="latest-draw">
    <div class="draw-header">
      <h2>{{ isNavigating ? `Draw #${currentDraw?.draw}` : 'Latest Lottery Draw' }}</h2>
      <div class="header-controls">
        <div v-if="currentDraw" class="navigation-controls">
          <button @click="goToPrevious" :disabled="isLoading || !hasPrevious" class="nav-button">
            <span class="nav-icon">◀</span>
            Previous
          </button>
          <button @click="goToNext" :disabled="isLoading || !hasNext" class="nav-button">
            Next
            <span class="nav-icon">▶</span>
          </button>
          <button v-if="isNavigating" @click="goToLatest" :disabled="isLoading" class="nav-button latest-button">
            Latest
          </button>
        </div>
        <button @click="refreshDraw" :disabled="isLoading" class="refresh-button">
          <span class="refresh-icon" :class="{ 'spinning': isLoading }">🔄</span>
          Refresh
        </button>
      </div>
    </div>
    
    <div v-if="currentDraw" class="draw-content">
      <div class="draw-info">
        <div class="draw-number">
          <span class="label">Draw #</span>
          <span class="value">{{ currentDraw.draw }}</span>
        </div>
        <div class="draw-date">
          <span class="label">Date</span>
          <span class="value">{{ formatDate(currentDraw.date) }}</span>
        </div>
      </div>
      
      <div class="winning-numbers">
        <h3>Winning Numbers</h3>
        <div class="numbers-container">
          <div class="main-numbers">
            <span v-for="number in currentDraw.winningNumbers" :key="number" class="number-ball main">
              {{ number }}
            </span>
          </div>
          <div class="special-numbers">
            <div class="bonus-number">
              <span class="label">Bonus</span>
              <span class="number-ball bonus">{{ currentDraw.bonusNumber }}</span>
            </div>
            <div class="powerball-number">
              <span class="label">Powerball</span>
              <span class="number-ball powerball">{{ currentDraw.powerball }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <div v-else-if="!isLoading && !error" class="no-draw">
      <div class="no-draw-icon">🎲</div>
      <h3>No Draw Data Available</h3>
      <p>Upload some lottery data to see the draw information.</p>
    </div>
    
    <div v-if="error" class="error-state">
      <div class="error-icon">❌</div>
      <h3>Failed to Load Draw Data</h3>
      <p>{{ error }}</p>
      <button @click="refreshDraw" class="retry-button">Try Again</button>
    </div>
    
    <div v-if="isLoading" class="loading-state">
      <div class="loading-spinner"></div>
      <p>Loading draw...</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import api from '@/services/api'
import { useAppStore } from '@/stores/app'

interface DrawData {
  draw: number
  date: string
  winningNumbers: number[]
  bonusNumber: number
  powerball: number
}

const appStore = useAppStore()

// Reactive state
const currentDraw = ref<DrawData | null>(null)
const latestDrawNumber = ref<number | null>(null)
const firstDrawNumber = ref<number | null>(null)
const isLoading = ref(false)
const error = ref<string | null>(null)

// Computed properties
const isNavigating = computed(() => {
  return currentDraw.value && latestDrawNumber.value && currentDraw.value.draw !== latestDrawNumber.value
})

const hasPrevious = computed(() => {
  return currentDraw.value && firstDrawNumber.value && currentDraw.value.draw > firstDrawNumber.value
})

const hasNext = computed(() => {
  return currentDraw.value && latestDrawNumber.value && currentDraw.value.draw < latestDrawNumber.value
})

// Fetch draw boundaries (first and latest draw numbers)
const fetchDrawBoundaries = async () => {
  try {
    // Get latest draw to determine the upper boundary
    const latestResponse = await api.get('/lotto/latest')
    latestDrawNumber.value = latestResponse.data.draw
    
    // Get first page of draws sorted by draw number ascending to find the first draw
    const firstResponse = await api.get('/lotto/draws?page=1&pageSize=1&sortBy=draw&sortDirection=asc')
    if (firstResponse.data.items && firstResponse.data.items.length > 0) {
      firstDrawNumber.value = firstResponse.data.items[0].draw
    }
  } catch (err: any) {
    console.error('Failed to fetch draw boundaries:', err)
    // Don't throw - this is supplementary information
  }
}

// Fetch latest draw data
const fetchLatestDraw = async () => {
  isLoading.value = true
  error.value = null
  
  try {
    const response = await api.get('/lotto/latest')
    currentDraw.value = response.data
    latestDrawNumber.value = response.data.draw
    
    // Fetch boundaries if we don't have them yet
    if (firstDrawNumber.value === null) {
      await fetchDrawBoundaries()
    }
  } catch (err: any) {
    if (err.response?.status === 404) {
      // No draws available - this is expected when no data has been uploaded
      currentDraw.value = null
      latestDrawNumber.value = null
      firstDrawNumber.value = null
      error.value = null
    } else {
      error.value = err.response?.data?.message || err.message || 'Failed to load latest draw'
      console.error('Failed to fetch latest draw:', err)
    }
  } finally {
    isLoading.value = false
  }
}

// Fetch specific draw
const fetchDraw = async (drawNumber: number) => {
  isLoading.value = true
  error.value = null
  
  try {
    const response = await api.get(`/lotto/draw/${drawNumber}`)
    currentDraw.value = response.data
    
    // Fetch boundaries if we don't have them yet
    if (latestDrawNumber.value === null || firstDrawNumber.value === null) {
      await fetchDrawBoundaries()
    }
  } catch (err: any) {
    if (err.response?.status === 404) {
      error.value = `Draw #${drawNumber} not found`
    } else {
      error.value = err.response?.data?.message || err.message || 'Failed to load draw'
      console.error('Failed to fetch draw:', err)
    }
  } finally {
    isLoading.value = false
  }
}

// Navigation methods
const goToPrevious = async () => {
  if (!currentDraw.value || !hasPrevious.value) return
  
  isLoading.value = true
  try {
    const response = await api.get(`/lotto/draw/${currentDraw.value.draw}/previous`)
    currentDraw.value = response.data
  } catch (err: any) {
    error.value = err.response?.data?.message || err.message || 'Failed to load previous draw'
    console.error('Failed to fetch previous draw:', err)
  } finally {
    isLoading.value = false
  }
}

const goToNext = async () => {
  if (!currentDraw.value || !hasNext.value) return
  
  isLoading.value = true
  try {
    const response = await api.get(`/lotto/draw/${currentDraw.value.draw}/next`)
    currentDraw.value = response.data
  } catch (err: any) {
    error.value = err.response?.data?.message || err.message || 'Failed to load next draw'
    console.error('Failed to fetch next draw:', err)
  } finally {
    isLoading.value = false
  }
}

const goToLatest = async () => {
  await fetchLatestDraw()
}

// Refresh current draw (with user feedback)
const refreshDraw = async () => {
  if (isNavigating.value && currentDraw.value) {
    await fetchDraw(currentDraw.value.draw)
  } else {
    await fetchLatestDraw()
  }
  
  if (currentDraw.value) {
    appStore.addNotification({
      type: 'success',
      title: 'Draw Updated',
      message: `Draw #${currentDraw.value.draw} loaded successfully`
    })
  }
}

// Format date for display
const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-NZ', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

// Component events
const emit = defineEmits<{
  drawLoaded: [draw: DrawData]
}>()

// Load latest draw on component mount
onMounted(() => {
  fetchLatestDraw()
})

// Expose methods for parent components
defineExpose({
  refreshDraw,
  goToDraw: fetchDraw,
  goToLatest,
  goToPrevious,
  goToNext
})
</script>

<style scoped>
.latest-draw {
  max-width: 600px;
  margin: 2rem auto;
  padding: 2rem;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.draw-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.draw-header h2 {
  color: var(--color-heading);
  margin: 0;
  font-size: 1.75rem;
}

.header-controls {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.navigation-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.nav-button {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  padding: 0.5rem 0.75rem;
  border-radius: 6px;
  cursor: pointer;
  color: var(--color-text);
  font-size: 0.85rem;
  transition: all 0.3s ease;
}

.nav-button:hover:not(:disabled) {
  background: var(--color-background-mute);
  transform: translateY(-1px);
}

.nav-button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.nav-button.latest-button {
  background: #42b883;
  color: white;
  border-color: #42b883;
}

.nav-button.latest-button:hover:not(:disabled) {
  background: #369870;
}

.nav-icon {
  font-size: 0.75rem;
}

.refresh-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  padding: 0.5rem 1rem;
  border-radius: 6px;
  cursor: pointer;
  color: var(--color-text);
  font-size: 0.9rem;
  transition: all 0.3s ease;
}

.refresh-button:hover:not(:disabled) {
  background: var(--color-background-mute);
}

.refresh-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.refresh-icon {
  font-size: 1rem;
  transition: transform 0.3s ease;
}

.refresh-icon.spinning {
  animation: spin 1s linear infinite;
}

.draw-content {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.draw-info {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.draw-number,
.draw-date {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 1rem;
  background: var(--color-background-soft);
  border-radius: 8px;
}

.draw-info .label {
  font-size: 0.875rem;
  color: var(--color-text);
  margin-bottom: 0.5rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.draw-info .value {
  font-size: 1.25rem;
  font-weight: bold;
  color: var(--color-heading);
}

.winning-numbers h3 {
  color: var(--color-heading);
  margin: 0 0 1.5rem 0;
  text-align: center;
}

.numbers-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  align-items: center;
}

.main-numbers {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  justify-content: center;
}

.number-ball {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 3rem;
  height: 3rem;
  border-radius: 50%;
  font-weight: bold;
  font-size: 1.1rem;
  color: white;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.number-ball.main {
  background: linear-gradient(135deg, #42b883, #369870);
}

.number-ball.bonus {
  background: linear-gradient(135deg, #ffc107, #e0a800);
  width: 2.5rem;
  height: 2.5rem;
  font-size: 1rem;
}

.number-ball.powerball {
  background: linear-gradient(135deg, #dc3545, #c82333);
  width: 2.5rem;
  height: 2.5rem;
  font-size: 1rem;
}

.special-numbers {
  display: flex;
  gap: 2rem;
  align-items: center;
}

.bonus-number,
.powerball-number {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
}

.special-numbers .label {
  font-size: 0.75rem;
  color: var(--color-text);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.no-draw,
.error-state,
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 3rem 1rem;
  text-align: center;
}

.no-draw-icon,
.error-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}

.no-draw h3,
.error-state h3 {
  color: var(--color-heading);
  margin: 0 0 1rem 0;
}

.no-draw p,
.error-state p {
  color: var(--color-text);
  margin: 0 0 1rem 0;
  line-height: 1.5;
}

.retry-button {
  background: #42b883;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 1rem;
  transition: background-color 0.3s ease;
}

.retry-button:hover {
  background: #369870;
}

.loading-spinner {
  width: 2rem;
  height: 2rem;
  border: 3px solid var(--color-border);
  border-top: 3px solid #42b883;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

/* Responsive */
@media (max-width: 768px) {
  .latest-draw {
    margin: 1rem;
    padding: 1.5rem;
  }
  
  .draw-header {
    flex-direction: column;
    gap: 1rem;
    align-items: stretch;
  }
  
  .header-controls {
    justify-content: center;
  }
  
  .navigation-controls {
    flex-wrap: wrap;
    justify-content: center;
  }
  
  .nav-button {
    font-size: 0.8rem;
    padding: 0.4rem 0.6rem;
  }
  
  .draw-info {
    grid-template-columns: 1fr;
  }
  
  .main-numbers {
    gap: 0.5rem;
  }
  
  .number-ball {
    width: 2.5rem;
    height: 2.5rem;
    font-size: 1rem;
  }
  
  .special-numbers {
    gap: 1rem;
  }
}
</style>