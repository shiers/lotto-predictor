<template>
  <div class="draws-view">
    <div class="draws-header">
      <h1>Lottery Draws</h1>
    </div>



    <div v-if="draws.length > 0" class="draws-content">
      <!-- Top Pagination Controls -->
      <div class="pagination-controls top-pagination">
        <button 
          @click="goToPage(1)" 
          :disabled="!paginationData.hasPreviousPage || isLoading"
          class="pagination-button"
          title="First Page"
        >
          ⏮️
        </button>
        <button 
          @click="goToPage(currentPage - 1)" 
          :disabled="!paginationData.hasPreviousPage || isLoading"
          class="pagination-button"
          title="Previous Page"
        >
          ◀️
        </button>
        
        <div class="page-info">
          <span>Page {{ currentPage }} of {{ paginationData.totalPages }}</span>
        </div>
        
        <button 
          @click="goToPage(currentPage + 1)" 
          :disabled="!paginationData.hasNextPage || isLoading"
          class="pagination-button"
          title="Next Page"
        >
          ▶️
        </button>
        <button 
          @click="goToPage(paginationData.totalPages)" 
          :disabled="!paginationData.hasNextPage || isLoading"
          class="pagination-button"
          title="Last Page"
        >
          ⏭️
        </button>
      </div>

      <!-- Pagination Info -->
      <div class="pagination-info">
        <span>Showing {{ paginationData.firstItemIndex }} - {{ paginationData.lastItemIndex }} of {{ paginationData.totalItems }} draws</span>
      </div>

      <!-- Draws Table -->
      <div class="draws-table-container">
        <table class="draws-table">
          <thead>
            <tr>
              <th @click="setSortBy('draw')" class="sortable" :class="{ 'active': sortBy === 'draw' }">
                Draw #
                <span v-if="sortBy === 'draw'" class="sort-indicator">{{ sortDirection === 'desc' ? '↓' : '↑' }}</span>
              </th>
              <th @click="setSortBy('date')" class="sortable" :class="{ 'active': sortBy === 'date' }">
                Date
                <span v-if="sortBy === 'date'" class="sort-indicator">{{ sortDirection === 'desc' ? '↓' : '↑' }}</span>
              </th>
              <th>Winning Numbers</th>
              <th>Bonus</th>
              <th>Powerball</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="draw in draws" :key="draw.draw" class="draw-row">
              <td class="draw-number">{{ draw.draw }}</td>
              <td class="draw-date">{{ formatDate(draw.date) }}</td>
              <td class="winning-numbers">
                <div class="numbers-display">
                  <span v-for="number in draw.winningNumbers" :key="number" class="number-ball small">
                    {{ number }}
                  </span>
                </div>
              </td>
              <td class="bonus-number">
                <span class="number-ball small bonus">{{ draw.bonusNumber }}</span>
              </td>
              <td class="powerball-number">
                <span class="number-ball small powerball">{{ draw.powerball }}</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Bottom Pagination Controls -->
      <div class="pagination-controls bottom-pagination">
        <button 
          @click="goToPage(1)" 
          :disabled="!paginationData.hasPreviousPage || isLoading"
          class="pagination-button"
          title="First Page"
        >
          ⏮️
        </button>
        <button 
          @click="goToPage(currentPage - 1)" 
          :disabled="!paginationData.hasPreviousPage || isLoading"
          class="pagination-button"
          title="Previous Page"
        >
          ◀️
        </button>
        
        <div class="page-info">
          <span>Page {{ currentPage }} of {{ paginationData.totalPages }}</span>
        </div>
        
        <button 
          @click="goToPage(currentPage + 1)" 
          :disabled="!paginationData.hasNextPage || isLoading"
          class="pagination-button"
          title="Next Page"
        >
          ▶️
        </button>
        <button 
          @click="goToPage(paginationData.totalPages)" 
          :disabled="!paginationData.hasNextPage || isLoading"
          class="pagination-button"
          title="Last Page"
        >
          ⏭️
        </button>
      </div>

      <!-- Page Size Selector -->
      <div class="page-size-controls">
        <label for="page-size">Items per page:</label>
        <select id="page-size" v-model="pageSize" @change="changePageSize" class="page-size-select">
          <option value="25">25</option>
          <option value="50">50</option>
          <option value="100">100</option>
        </select>
      </div>
    </div>

    <div v-else-if="!isLoading && !error" class="no-draws">
      <div class="no-draws-icon">🎲</div>
      <h3>No Draws Available</h3>
      <p>Upload some lottery data to see the draws list.</p>
    </div>

    <div v-if="error" class="error-state">
      <div class="error-icon">❌</div>
      <h3>Failed to Load Draws</h3>
      <p>{{ error }}</p>
      <button @click="refreshDraws" class="retry-button">Try Again</button>
    </div>

    <div v-if="isLoading" class="loading-state">
      <div class="loading-spinner"></div>
      <p>Loading draws...</p>
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
const draws = ref<DrawData[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)
const currentPage = ref(1)
const pageSize = ref(50)
const sortBy = ref('date')
const sortDirection = ref('desc')
const paginationData = ref<PaginationData>({
  page: 1,
  pageSize: 50,
  totalItems: 0,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false,
  firstItemIndex: 0,
  lastItemIndex: 0
})

// Load draws data
const loadDraws = async (page: number = currentPage.value) => {
  isLoading.value = true
  error.value = null
  
  try {
    const response = await api.get('/lotto/draws', {
      params: {
        page,
        pageSize: pageSize.value,
        sortBy: sortBy.value,
        sortDirection: sortDirection.value
      }
    })
    
    draws.value = response.data.items
    paginationData.value = response.data.metadata
    currentPage.value = page
  } catch (err: any) {
    if (err.response?.status === 404) {
      draws.value = []
      error.value = null
    } else {
      error.value = err.response?.data?.message || err.message || 'Failed to load draws'
      console.error('Failed to fetch draws:', err)
    }
  } finally {
    isLoading.value = false
  }
}

// Refresh draws
const refreshDraws = async () => {
  await loadDraws(currentPage.value)
  
  if (draws.value.length > 0) {
    appStore.addNotification({
      type: 'success',
      title: 'Draws Updated',
      message: `Loaded ${draws.value.length} draws successfully`
    })
  }
}

// Pagination methods
const goToPage = async (page: number) => {
  if (page >= 1 && page <= paginationData.value.totalPages) {
    await loadDraws(page)
  }
}

const changePageSize = async () => {
  currentPage.value = 1
  await loadDraws(1)
}

// Sorting methods
const setSortBy = async (field: string) => {
  if (sortBy.value === field) {
    toggleSortDirection()
  } else {
    sortBy.value = field
    await loadDraws(1)
  }
}

const toggleSortDirection = async () => {
  sortDirection.value = sortDirection.value === 'desc' ? 'asc' : 'desc'
  await loadDraws(1)
}

// Navigation methods (removed viewDraw as Actions column was removed)

// Format date for display
const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-NZ', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}



// Load draws on component mount
onMounted(() => {
  loadDraws()
})
</script>

<style scoped>
.draws-view {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.draws-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.draws-header h1 {
  color: var(--color-heading);
  margin: 0;
  font-size: 2rem;
}



.header-controls {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.sort-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}



.sort-select {
  padding: 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  background: var(--color-background);
  color: var(--color-text);
  font-size: 0.9rem;
}

.sort-direction-button {
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  padding: 0.5rem;
  border-radius: 4px;
  cursor: pointer;
  color: var(--color-text);
  font-size: 1rem;
  transition: all 0.3s ease;
}

.sort-direction-button:hover {
  background: var(--color-background-mute);
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

.draws-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.pagination-info {
  text-align: center;
  color: var(--color-text);
  font-size: 0.9rem;
}

.draws-table-container {
  overflow-x: auto;
  border: 1px solid var(--color-border);
  border-radius: 8px;
}

.draws-table {
  width: 100%;
  border-collapse: collapse;
  background: var(--color-background);
}

.draws-table th,
.draws-table td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid var(--color-border);
}

.draws-table th {
  background: var(--color-background-soft);
  color: var(--color-heading);
  font-weight: 600;
  position: sticky;
  top: 0;
}

.draws-table th.sortable {
  cursor: pointer;
  user-select: none;
  transition: background-color 0.3s ease;
}

.draws-table th.sortable:hover {
  background: var(--color-background-mute);
}

.draws-table th.active {
  background: var(--color-background-mute);
}

.sort-indicator {
  margin-left: 0.5rem;
  font-size: 0.8rem;
}

.draw-row:hover {
  background: var(--color-background-soft);
}

.draw-number {
  font-weight: 600;
  color: var(--color-heading);
}

.draw-date {
  color: var(--color-text);
}

.numbers-display {
  display: flex;
  gap: 0.25rem;
  flex-wrap: wrap;
}

.number-ball {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-weight: bold;
  color: white;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
}

.number-ball.small {
  width: 1.75rem;
  height: 1.75rem;
  font-size: 0.75rem;
}

.number-ball:not(.bonus):not(.powerball) {
  background: linear-gradient(135deg, #42b883, #369870);
}

.number-ball.bonus {
  background: linear-gradient(135deg, #ffc107, #e0a800);
}

.number-ball.powerball {
  background: linear-gradient(135deg, #dc3545, #c82333);
}

.pagination-controls {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 0.5rem;
}

.pagination-controls.top-pagination {
  padding-bottom: 0.5rem;
  border-bottom: 1px solid var(--color-border);
}

.pagination-controls.bottom-pagination {
  padding-top: 0.5rem;
  border-top: 1px solid var(--color-border);
}

.pagination-button {
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  padding: 0.5rem 0.75rem;
  border-radius: 4px;
  cursor: pointer;
  color: var(--color-text);
  font-size: 0.9rem;
  transition: all 0.3s ease;
}

.pagination-button:hover:not(:disabled) {
  background: var(--color-background-mute);
}

.pagination-button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.page-info {
  margin: 0 1rem;
  color: var(--color-text);
  font-size: 0.9rem;
}

.page-size-controls {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 0.5rem;
}

.page-size-controls label {
  font-size: 0.9rem;
  color: var(--color-text);
}

.page-size-select {
  padding: 0.25rem 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  background: var(--color-background);
  color: var(--color-text);
  font-size: 0.9rem;
}

.no-draws,
.error-state,
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 3rem 1rem;
  text-align: center;
}

.no-draws-icon,
.error-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}

.no-draws h3,
.error-state h3 {
  color: var(--color-heading);
  margin: 0 0 1rem 0;
}

.no-draws p,
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
  .draws-view {
    padding: 1rem;
  }
  
  .draws-header {
    flex-direction: column;
    align-items: stretch;
  }
  
  .header-controls {
    justify-content: center;
  }
  

  
  .draws-table th,
  .draws-table td {
    padding: 0.5rem;
    font-size: 0.85rem;
  }
  
  .numbers-display {
    gap: 0.125rem;
  }
  
  .number-ball.small {
    width: 1.5rem;
    height: 1.5rem;
    font-size: 0.7rem;
  }
  
  .pagination-controls {
    flex-wrap: wrap;
  }
  
  .page-info {
    margin: 0.5rem 0;
  }
}

@media (max-width: 480px) {
  .draws-table {
    font-size: 0.8rem;
  }
  
  .draws-table th,
  .draws-table td {
    padding: 0.25rem;
  }
  
  .number-ball.small {
    width: 1.25rem;
    height: 1.25rem;
    font-size: 0.65rem;
  }
}
</style>