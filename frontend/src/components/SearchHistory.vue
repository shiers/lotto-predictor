<template>
  <div class="search-history">
    <div class="history-header">
      <h3>Search History</h3>
      <div class="history-actions">
        <button 
          @click="refreshHistory" 
          :disabled="loading"
          class="btn btn-secondary"
        >
          <i class="icon-refresh"></i>
          Refresh
        </button>
        <button 
          @click="showClearDialog = true"
          :disabled="loading || searchHistory.length === 0"
          class="btn btn-danger"
        >
          <i class="icon-trash"></i>
          Clear All
        </button>
      </div>
    </div>

    <div v-if="loading" class="loading-state">
      <div class="spinner"></div>
      <p>Loading search history...</p>
    </div>

    <div v-else-if="error" class="error-state">
      <p class="error-message">{{ error }}</p>
      <button @click="refreshHistory" class="btn btn-secondary">
        Try Again
      </button>
    </div>

    <div v-else-if="searchHistory.length === 0" class="empty-state">
      <div class="empty-icon">🔍</div>
      <h4>No Search History</h4>
      <p>Your search history will appear here as you perform searches.</p>
    </div>

    <div v-else class="history-content">
      <!-- Filters and Controls -->
      <div class="history-controls">
        <div class="filter-controls">
          <div class="filter-group">
            <label for="typeFilter">Filter by type:</label>
            <select id="typeFilter" v-model="filters.searchType" class="filter-select">
              <option value="">All Types</option>
              <option value="number">Number Lookup</option>
              <option value="combination">Combination Search</option>
              <option value="range">Range Analysis</option>
              <option value="frequency">Frequency Analysis</option>
              <option value="advanced">Advanced Search</option>
            </select>
          </div>
          
          <div class="filter-group">
            <label for="dateFilter">Date range:</label>
            <select id="dateFilter" v-model="filters.dateRange" class="filter-select">
              <option value="">All Time</option>
              <option value="today">Today</option>
              <option value="week">This Week</option>
              <option value="month">This Month</option>
              <option value="custom">Custom Range</option>
            </select>
          </div>

          <div v-if="filters.dateRange === 'custom'" class="date-range-inputs">
            <input
              v-model="filters.startDate"
              type="date"
              class="date-input"
              placeholder="Start date"
            />
            <input
              v-model="filters.endDate"
              type="date"
              class="date-input"
              placeholder="End date"
            />
          </div>
        </div>

        <div class="view-controls">
          <div class="view-toggle">
            <button
              @click="viewMode = 'list'"
              :class="['view-btn', { active: viewMode === 'list' }]"
              title="List View"
            >
              <i class="icon-list"></i>
            </button>
            <button
              @click="viewMode = 'grid'"
              :class="['view-btn', { active: viewMode === 'grid' }]"
              title="Grid View"
            >
              <i class="icon-grid"></i>
            </button>
          </div>
          
          <div class="sort-controls">
            <label>Sort by:</label>
            <select v-model="sortBy" class="sort-select">
              <option value="searchedAt">Date</option>
              <option value="searchType">Type</option>
              <option value="resultCount">Results</option>
              <option value="executionTime">Speed</option>
            </select>
            <button 
              @click="sortOrder = sortOrder === 'asc' ? 'desc' : 'asc'"
              class="sort-order-btn"
              :title="sortOrder === 'asc' ? 'Sort Descending' : 'Sort Ascending'"
            >
              <i :class="sortOrder === 'asc' ? 'icon-arrow-up' : 'icon-arrow-down'"></i>
            </button>
          </div>
        </div>
      </div>

      <!-- Search History Items -->
      <div :class="['history-items', `view-${viewMode}`]">
        <div
          v-for="item in filteredAndSortedHistory"
          :key="item.id"
          class="history-item"
        >
          <div class="item-header">
            <div class="item-type">
              <i :class="getSearchTypeIcon(item.searchType)"></i>
              <span class="type-label">{{ getSearchTypeDisplayName(item.searchType) }}</span>
            </div>
            <div class="item-date">
              {{ formatDate(item.searchedAt) }}
            </div>
          </div>

          <div class="item-content">
            <div class="search-criteria">
              <h4>{{ formatSearchCriteria(item) }}</h4>
              <div v-if="item.parsedCriteria" class="criteria-details">
                <div v-if="item.searchType === 'number' && item.parsedCriteria.numbers" class="detail-item">
                  <span class="detail-label">Numbers:</span>
                  <div class="number-list">
                    <span 
                      v-for="number in item.parsedCriteria.numbers" 
                      :key="number"
                      class="number-tag"
                    >
                      {{ number }}
                    </span>
                  </div>
                </div>
                
                <div v-if="item.searchType === 'combination' && item.parsedCriteria.combination" class="detail-item">
                  <span class="detail-label">Combination:</span>
                  <div class="number-list">
                    <span 
                      v-for="number in item.parsedCriteria.combination" 
                      :key="number"
                      class="number-tag"
                    >
                      {{ number }}
                    </span>
                  </div>
                </div>
                
                <div v-if="item.searchType === 'range' && item.parsedCriteria.ranges" class="detail-item">
                  <span class="detail-label">Ranges:</span>
                  <div class="range-list">
                    <span 
                      v-for="range in item.parsedCriteria.ranges" 
                      :key="`${range.startNumber}-${range.endNumber}`"
                      class="range-tag"
                    >
                      {{ range.startNumber }}-{{ range.endNumber }}
                    </span>
                  </div>
                </div>

                <div v-if="item.parsedCriteria.startDate || item.parsedCriteria.endDate" class="detail-item">
                  <span class="detail-label">Date Range:</span>
                  <span class="detail-value">
                    {{ formatDateRange(item.parsedCriteria.startDate, item.parsedCriteria.endDate) }}
                  </span>
                </div>
              </div>
            </div>

            <div class="item-stats">
              <div class="stat-item">
                <span class="stat-label">Results:</span>
                <span class="stat-value">{{ item.resultCount.toLocaleString() }}</span>
              </div>
              <div class="stat-item">
                <span class="stat-label">Time:</span>
                <span class="stat-value">{{ formatExecutionTime(item.executionTime) }}</span>
              </div>
            </div>
          </div>

          <div class="item-actions">
            <button 
              @click="reuseSearch(item)"
              class="btn btn-sm btn-primary"
              title="Reuse this search"
            >
              <i class="icon-repeat"></i>
              Reuse
            </button>
            <button 
              @click="exportSearch(item)"
              class="btn btn-sm btn-secondary"
              title="Export results"
            >
              <i class="icon-download"></i>
              Export
            </button>
            <button 
              @click="deleteHistoryItem(item)"
              class="btn btn-sm btn-danger"
              title="Delete from history"
            >
              <i class="icon-trash"></i>
            </button>
          </div>
        </div>
      </div>

      <!-- Load More Button -->
      <div v-if="hasMoreItems" class="load-more">
        <button 
          @click="loadMoreItems"
          :disabled="loadingMore"
          class="btn btn-secondary"
        >
          <span v-if="loadingMore">Loading...</span>
          <span v-else>Load More</span>
        </button>
      </div>
    </div>

    <!-- Clear Confirmation Dialog -->
    <div v-if="showClearDialog" class="modal-overlay" @click="closeClearDialog">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>Clear Search History</h3>
          <button @click="closeClearDialog" class="close-btn">
            <i class="icon-x"></i>
          </button>
        </div>
        <div class="modal-body">
          <p>Are you sure you want to clear your entire search history?</p>
          <p class="warning-text">This action cannot be undone.</p>
        </div>
        <div class="modal-footer">
          <button 
            @click="closeClearDialog" 
            type="button" 
            class="btn btn-secondary"
          >
            Cancel
          </button>
          <button 
            @click="clearHistory" 
            type="button" 
            class="btn btn-danger"
            :disabled="clearing"
          >
            <span v-if="clearing">Clearing...</span>
            <span v-else>Clear History</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { 
  searchHistoryService, 
  type SearchHistoryItem, 
  type SearchHistoryFilter 
} from '../services/searchHistoryService'

interface Emits {
  (e: 'reuse-search', criteria: any): void
  (e: 'export-search', item: SearchHistoryItem): void
}

const emit = defineEmits<Emits>()
const router = useRouter()

// Reactive state
const searchHistory = ref<SearchHistoryItem[]>([])
const loading = ref(false)
const loadingMore = ref(false)
const error = ref<string | null>(null)
const hasMoreItems = ref(true)
const currentOffset = ref(0)
const itemsPerPage = 20

// View and filter state
const viewMode = ref<'list' | 'grid'>('list')
const sortBy = ref<'searchedAt' | 'searchType' | 'resultCount' | 'executionTime'>('searchedAt')
const sortOrder = ref<'asc' | 'desc'>('desc')

const filters = ref<{
  searchType: string
  dateRange: string
  startDate: string
  endDate: string
}>({
  searchType: '',
  dateRange: '',
  startDate: '',
  endDate: ''
})

// Clear dialog state
const showClearDialog = ref(false)
const clearing = ref(false)

// Computed properties
const filteredAndSortedHistory = computed(() => {
  let filtered = [...searchHistory.value]

  // Apply type filter
  if (filters.value.searchType) {
    filtered = filtered.filter(item => item.searchType === filters.value.searchType)
  }

  // Apply date filter
  if (filters.value.dateRange) {
    const now = new Date()
    let startDate: Date | null = null

    switch (filters.value.dateRange) {
      case 'today':
        startDate = new Date(now.getFullYear(), now.getMonth(), now.getDate())
        break
      case 'week':
        startDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000)
        break
      case 'month':
        startDate = new Date(now.getFullYear(), now.getMonth(), 1)
        break
      case 'custom':
        if (filters.value.startDate) {
          startDate = new Date(filters.value.startDate)
        }
        break
    }

    if (startDate) {
      filtered = filtered.filter(item => new Date(item.searchedAt) >= startDate!)
    }

    if (filters.value.dateRange === 'custom' && filters.value.endDate) {
      const endDate = new Date(filters.value.endDate)
      endDate.setHours(23, 59, 59, 999) // End of day
      filtered = filtered.filter(item => new Date(item.searchedAt) <= endDate)
    }
  }

  // Sort
  filtered.sort((a, b) => {
    let aValue: any
    let bValue: any

    switch (sortBy.value) {
      case 'searchedAt':
        aValue = new Date(a.searchedAt)
        bValue = new Date(b.searchedAt)
        break
      case 'searchType':
        aValue = a.searchType
        bValue = b.searchType
        break
      case 'resultCount':
        aValue = a.resultCount
        bValue = b.resultCount
        break
      case 'executionTime':
        aValue = parseFloat(a.executionTime)
        bValue = parseFloat(b.executionTime)
        break
      default:
        return 0
    }

    if (aValue < bValue) return sortOrder.value === 'asc' ? -1 : 1
    if (aValue > bValue) return sortOrder.value === 'asc' ? 1 : -1
    return 0
  })

  return filtered
})

// Watchers
watch([() => filters.value.searchType, () => filters.value.dateRange], () => {
  // Reset pagination when filters change
  currentOffset.value = 0
  hasMoreItems.value = true
})

// Methods
const refreshHistory = async () => {
  loading.value = true
  error.value = null
  currentOffset.value = 0
  hasMoreItems.value = true

  try {
    const filter: SearchHistoryFilter = {
      limit: itemsPerPage,
      offset: 0
    }
    
    searchHistory.value = await searchHistoryService.getUserSearchHistory(filter)
    hasMoreItems.value = searchHistory.value.length === itemsPerPage
  } catch (err) {
    error.value = 'Failed to load search history. Please try again.'
    console.error('Error loading search history:', err)
  } finally {
    loading.value = false
  }
}

const loadMoreItems = async () => {
  if (loadingMore.value || !hasMoreItems.value) return

  loadingMore.value = true
  currentOffset.value += itemsPerPage

  try {
    const filter: SearchHistoryFilter = {
      limit: itemsPerPage,
      offset: currentOffset.value
    }
    
    const moreItems = await searchHistoryService.getUserSearchHistory(filter)
    searchHistory.value.push(...moreItems)
    hasMoreItems.value = moreItems.length === itemsPerPage
  } catch (err) {
    error.value = 'Failed to load more items. Please try again.'
    console.error('Error loading more items:', err)
  } finally {
    loadingMore.value = false
  }
}

const reuseSearch = (item: SearchHistoryItem) => {
  const criteria = searchHistoryService.createSearchCriteriaForReuse(item)
  emit('reuse-search', criteria)
  
  // Navigate to appropriate search page based on type
  switch (item.searchType.toLowerCase()) {
    case 'number':
    case 'combination':
      router.push({ name: 'lookup', query: { reuse: 'true' } })
      break
    case 'frequency':
    case 'range':
      router.push({ name: 'frequency', query: { reuse: 'true' } })
      break
    default:
      router.push({ name: 'lookup', query: { reuse: 'true' } })
  }
}

const exportSearch = (item: SearchHistoryItem) => {
  emit('export-search', item)
}

const deleteHistoryItem = async (item: SearchHistoryItem) => {
  try {
    const success = await searchHistoryService.deleteSearchHistoryItem(item.id)
    if (success) {
      searchHistory.value = searchHistory.value.filter(h => h.id !== item.id)
    } else {
      error.value = 'Failed to delete search history item.'
    }
  } catch (err) {
    error.value = 'Failed to delete search history item.'
    console.error('Error deleting search history item:', err)
  }
}

const clearHistory = async () => {
  clearing.value = true

  try {
    const success = await searchHistoryService.clearSearchHistory()
    if (success) {
      searchHistory.value = []
      closeClearDialog()
    } else {
      error.value = 'Failed to clear search history.'
    }
  } catch (err) {
    error.value = 'Failed to clear search history.'
    console.error('Error clearing search history:', err)
  } finally {
    clearing.value = false
  }
}

const closeClearDialog = () => {
  showClearDialog.value = false
  clearing.value = false
}

// Helper methods
const getSearchTypeIcon = (searchType: string): string => {
  switch (searchType.toLowerCase()) {
    case 'number':
      return 'icon-hash'
    case 'combination':
      return 'icon-layers'
    case 'range':
      return 'icon-bar-chart-2'
    case 'frequency':
      return 'icon-trending-up'
    case 'advanced':
      return 'icon-settings'
    default:
      return 'icon-search'
  }
}

const getSearchTypeDisplayName = (searchType: string): string => {
  return searchHistoryService.getSearchTypeDisplayName(searchType)
}

const formatSearchCriteria = (item: SearchHistoryItem): string => {
  return searchHistoryService.formatSearchCriteria(item)
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) {
    return date.toLocaleTimeString('en-NZ', { 
      hour: '2-digit', 
      minute: '2-digit' 
    })
  } else if (diffDays === 1) {
    return 'Yesterday'
  } else if (diffDays < 7) {
    return `${diffDays} days ago`
  } else {
    return date.toLocaleDateString('en-NZ', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }
}

const formatDateRange = (startDate?: string, endDate?: string): string => {
  if (!startDate && !endDate) return 'All time'
  
  const formatDate = (dateStr: string) => 
    new Date(dateStr).toLocaleDateString('en-NZ', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric' 
    })

  if (startDate && endDate) {
    return `${formatDate(startDate)} - ${formatDate(endDate)}`
  } else if (startDate) {
    return `From ${formatDate(startDate)}`
  } else {
    return `Until ${formatDate(endDate!)}`
  }
}

const formatExecutionTime = (timeString: string): string => {
  // Parse timespan format (e.g., "00:00:01.2345678")
  const match = timeString.match(/(\d+):(\d+):(\d+)\.?(\d+)?/)
  if (!match) return timeString

  const hours = parseInt(match[1])
  const minutes = parseInt(match[2])
  const seconds = parseInt(match[3])
  const milliseconds = match[4] ? parseInt(match[4].substring(0, 3)) : 0

  if (hours > 0) {
    return `${hours}h ${minutes}m ${seconds}s`
  } else if (minutes > 0) {
    return `${minutes}m ${seconds}s`
  } else if (seconds > 0) {
    return `${seconds}.${milliseconds.toString().padStart(3, '0')}s`
  } else {
    return `${milliseconds}ms`
  }
}

// Lifecycle
onMounted(() => {
  refreshHistory()
})
</script>

<style scoped>
.search-history {
  max-width: 1000px;
  margin: 0 auto;
  padding: 1rem;
}

.history-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #e0e0e0;
}

.history-header h3 {
  margin: 0;
  color: #333;
}

.history-actions {
  display: flex;
  gap: 0.5rem;
}

.loading-state, .error-state, .empty-state {
  text-align: center;
  padding: 3rem 1rem;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #007bff;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.empty-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}

.history-controls {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.5rem;
  gap: 1rem;
  flex-wrap: wrap;
}

.filter-controls {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
  align-items: flex-end;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.filter-group label {
  font-size: 0.85rem;
  color: #666;
  font-weight: 500;
}

.filter-select, .date-input {
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.9rem;
}

.date-range-inputs {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.view-controls {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.view-toggle {
  display: flex;
  border: 1px solid #ddd;
  border-radius: 4px;
  overflow: hidden;
}

.view-btn {
  padding: 0.5rem;
  border: none;
  background: white;
  cursor: pointer;
  transition: all 0.2s ease;
}

.view-btn:hover {
  background: #f8f9fa;
}

.view-btn.active {
  background: #007bff;
  color: white;
}

.sort-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
}

.sort-select {
  padding: 0.25rem 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.sort-order-btn {
  padding: 0.25rem 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  background: white;
  cursor: pointer;
}

/* History Items */
.history-items.view-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.history-items.view-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 1rem;
}

.history-item {
  background: white;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 1rem;
  transition: all 0.2s ease;
}

.history-item:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.item-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
}

.item-type {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.item-type i {
  color: #007bff;
  font-size: 1.1rem;
}

.type-label {
  font-weight: 600;
  color: #333;
}

.item-date {
  font-size: 0.85rem;
  color: #666;
}

.item-content {
  margin-bottom: 1rem;
}

.search-criteria h4 {
  margin: 0 0 0.5rem 0;
  color: #333;
  font-size: 1rem;
}

.criteria-details {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.detail-label {
  font-size: 0.85rem;
  color: #666;
  font-weight: 500;
  min-width: 80px;
}

.detail-value {
  font-size: 0.85rem;
  color: #333;
}

.number-list, .range-list {
  display: flex;
  gap: 0.25rem;
  flex-wrap: wrap;
}

.number-tag {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 24px;
  height: 24px;
  background: #007bff;
  color: white;
  border-radius: 50%;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0 0.25rem;
}

.range-tag {
  background: #f8f9fa;
  border: 1px solid #dee2e6;
  border-radius: 12px;
  padding: 0.25rem 0.5rem;
  font-size: 0.75rem;
  color: #495057;
}

.item-stats {
  display: flex;
  gap: 1rem;
  margin-bottom: 0.75rem;
  padding-top: 0.75rem;
  border-top: 1px solid #f0f0f0;
}

.stat-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.stat-label {
  font-size: 0.75rem;
  color: #666;
  font-weight: 500;
}

.stat-value {
  font-size: 0.85rem;
  color: #333;
  font-weight: 600;
}

.item-actions {
  display: flex;
  gap: 0.5rem;
  justify-content: flex-end;
}

.load-more {
  text-align: center;
  margin-top: 2rem;
}

/* Modal styles */
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
}

.modal-content {
  background: white;
  border-radius: 8px;
  width: 90%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.5rem;
  border-bottom: 1px solid #e0e0e0;
}

.modal-header h3 {
  margin: 0;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #666;
}

.modal-body {
  padding: 1.5rem;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  padding: 1rem 1.5rem;
  border-top: 1px solid #e0e0e0;
}

.warning-text {
  color: #dc3545;
  font-size: 0.9rem;
  margin-top: 0.5rem;
}

/* Button styles */
.btn {
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  font-weight: 500;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  transition: all 0.2s ease;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background: #007bff;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #0056b3;
}

.btn-secondary {
  background: #6c757d;
  color: white;
}

.btn-secondary:hover:not(:disabled) {
  background: #545b62;
}

.btn-danger {
  background: #dc3545;
  color: white;
}

.btn-danger:hover:not(:disabled) {
  background: #c82333;
}

.btn-sm {
  padding: 0.25rem 0.5rem;
  font-size: 0.8rem;
}

.error-message {
  color: #dc3545;
  margin-bottom: 1rem;
}

/* Responsive design */
@media (max-width: 768px) {
  .search-history {
    padding: 0.5rem;
  }
  
  .history-header {
    flex-direction: column;
    gap: 1rem;
    align-items: stretch;
  }
  
  .history-controls {
    flex-direction: column;
    gap: 1rem;
  }
  
  .filter-controls {
    flex-direction: column;
    gap: 0.75rem;
  }
  
  .view-controls {
    justify-content: space-between;
  }
  
  .history-items.view-grid {
    grid-template-columns: 1fr;
  }
  
  .item-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }
  
  .item-actions {
    justify-content: flex-start;
    flex-wrap: wrap;
  }
  
  .modal-content {
    width: 95%;
    margin: 1rem;
  }
}
</style>