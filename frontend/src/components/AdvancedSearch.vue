<template>
  <div class="advanced-search">
    <div class="search-header">
      <h2>Advanced Search</h2>
      <p>Build complex search criteria with multiple conditions and filters</p>
    </div>

    <div class="search-builder">
      <SearchCriteriaBuilder
        v-model:criteria="searchCriteria"
        @validate="handleValidation"
      />
    </div>

    <div class="search-actions">
      <button 
        class="btn btn-primary"
        :disabled="!isValidCriteria || isSearching"
        @click="executeSearch"
      >
        <span v-if="isSearching">Searching...</span>
        <span v-else>Execute Search</span>
      </button>
      
      <button 
        class="btn btn-secondary"
        :disabled="!hasResults"
        @click="showExportDialog = true"
      >
        Export Results
      </button>
      
      <button 
        class="btn btn-outline"
        :disabled="!isValidCriteria"
        @click="showSaveDialog = true"
      >
        Save Search
      </button>
      
      <button 
        class="btn btn-outline"
        @click="showSavedSearches = true"
      >
        Saved Searches
      </button>
    </div>

    <div v-if="validationErrors.length > 0" class="validation-errors">
      <h4>Validation Errors:</h4>
      <ul>
        <li v-for="error in validationErrors" :key="error">{{ error }}</li>
      </ul>
    </div>

    <div v-if="searchResults" class="search-results">
      <div class="results-header">
        <h3>Search Results</h3>
        <span class="result-count">{{ searchResults.totalResults }} results found</span>
      </div>
      
      <div class="results-content">
        <div v-if="searchResults.numberOccurrences?.length > 0" class="number-results">
          <h4>Number Occurrences ({{ searchResults.numberOccurrences.length }})</h4>
          <div class="results-table">
            <table>
              <thead>
                <tr>
                  <th>Draw #</th>
                  <th>Date</th>
                  <th>Number</th>
                  <th>Position</th>
                  <th>Type</th>
                  <th>Full Combination</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="occurrence in searchResults.numberOccurrences" :key="`${occurrence.drawNumber}-${occurrence.number}`">
                  <td>{{ occurrence.drawNumber }}</td>
                  <td>{{ formatDate(occurrence.drawDate) }}</td>
                  <td class="number-cell">{{ occurrence.number }}</td>
                  <td>{{ occurrence.position }}</td>
                  <td>
                    <span v-if="occurrence.isBonus" class="badge bonus">Bonus</span>
                    <span v-else-if="occurrence.isPowerball" class="badge powerball">Powerball</span>
                    <span v-else class="badge main">Main</span>
                  </td>
                  <td class="combination-cell">{{ occurrence.fullCombination.join(', ') }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div v-if="searchResults.combinationMatches?.length > 0" class="combination-results">
          <h4>Combination Matches ({{ searchResults.combinationMatches.length }})</h4>
          <div class="results-table">
            <table>
              <thead>
                <tr>
                  <th>Draw #</th>
                  <th>Date</th>
                  <th>Winning Combination</th>
                  <th>Matched Numbers</th>
                  <th>Match Count</th>
                  <th>Type</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="match in searchResults.combinationMatches" :key="match.drawNumber">
                  <td>{{ match.drawNumber }}</td>
                  <td>{{ formatDate(match.drawDate) }}</td>
                  <td class="combination-cell">{{ match.winningCombination.join(', ') }}</td>
                  <td class="matched-numbers">{{ match.matchedNumbers.join(', ') }}</td>
                  <td>{{ match.matchCount }}</td>
                  <td>
                    <span :class="['badge', match.isExactMatch ? 'exact' : 'partial']">
                      {{ match.isExactMatch ? 'Exact' : 'Partial' }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div v-if="searchResults.frequencyData?.length > 0" class="frequency-results">
          <h4>Frequency Analysis ({{ searchResults.frequencyData.length }} numbers)</h4>
          <div class="results-table">
            <table>
              <thead>
                <tr>
                  <th>Number</th>
                  <th>Occurrences</th>
                  <th>Percentage</th>
                  <th>Last Appearance</th>
                  <th>Current Gap</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="freq in searchResults.frequencyData" :key="freq.number">
                  <td class="number-cell">{{ freq.number }}</td>
                  <td>{{ freq.totalOccurrences }}</td>
                  <td>{{ freq.percentage.toFixed(2) }}%</td>
                  <td>{{ formatDate(freq.lastAppearance) }}</td>
                  <td>{{ freq.currentGap }}</td>
                  <td>
                    <span v-if="freq.isHot" class="badge hot">Hot</span>
                    <span v-else-if="freq.isCold" class="badge cold">Cold</span>
                    <span v-else class="badge normal">Normal</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>

    <!-- Export Dialog -->
    <ExportDialog
      v-if="showExportDialog"
      :data="searchResults"
      :search-criteria="searchCriteria"
      @close="showExportDialog = false"
      @export="handleExport"
    />

    <!-- Save Search Dialog -->
    <div v-if="showSaveDialog" class="modal-overlay" @click="showSaveDialog = false">
      <div class="modal-content" @click.stop>
        <h3>Save Search Configuration</h3>
        <form @submit.prevent="saveSearchConfiguration">
          <div class="form-group">
            <label for="searchName">Search Name:</label>
            <input
              id="searchName"
              v-model="saveSearchForm.name"
              type="text"
              required
              placeholder="Enter a name for this search"
            />
          </div>
          <div class="form-group">
            <label for="searchDescription">Description (optional):</label>
            <textarea
              id="searchDescription"
              v-model="saveSearchForm.description"
              placeholder="Describe what this search is for"
            ></textarea>
          </div>
          <div class="form-actions">
            <button type="submit" class="btn btn-primary">Save</button>
            <button type="button" class="btn btn-secondary" @click="showSaveDialog = false">Cancel</button>
          </div>
        </form>
      </div>
    </div>

    <!-- Saved Searches Dialog -->
    <SavedSearches
      v-if="showSavedSearches"
      @close="showSavedSearches = false"
      @load="loadSearchConfiguration"
      @delete="deleteSearchConfiguration"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import SearchCriteriaBuilder from './SearchCriteriaBuilder.vue'
import ExportDialog from './ExportDialog.vue'
import SavedSearches from './SavedSearches.vue'
import { AdvancedSearchService } from '../services/advancedSearchService'
import type { 
  SearchCriteria, 
  AdvancedSearchResult, 
  SearchConfiguration,
  ExportRequest 
} from '../types/advancedSearch'

// Reactive state
const searchCriteria = ref<SearchCriteria>({
  conditions: [],
  logic: 'AND',
  dateRange: {
    startDate: null,
    endDate: null
  },
  frequencyFilters: {
    minOccurrences: null,
    maxOccurrences: null
  },
  includeBonus: true,
  includePowerball: true
})

const searchResults = ref<AdvancedSearchResult | null>(null)
const isSearching = ref(false)
const validationErrors = ref<string[]>([])
const isValidCriteria = ref(false)

// Dialog states
const showExportDialog = ref(false)
const showSaveDialog = ref(false)
const showSavedSearches = ref(false)

// Save search form
const saveSearchForm = ref({
  name: '',
  description: ''
})

// Computed properties
const hasResults = computed(() => {
  return searchResults.value && searchResults.value.totalResults > 0
})

// Methods
const handleValidation = (errors: string[]) => {
  validationErrors.value = errors
  isValidCriteria.value = errors.length === 0
}

const executeSearch = async () => {
  if (!isValidCriteria.value) return
  
  isSearching.value = true
  try {
    searchResults.value = await AdvancedSearchService.executeSearch(searchCriteria.value)
  } catch (error) {
    console.error('Search failed:', error)
    // Handle error - show notification
  } finally {
    isSearching.value = false
  }
}

const handleExport = async (exportRequest: ExportRequest) => {
  try {
    await AdvancedSearchService.exportResults(exportRequest)
    showExportDialog.value = false
    // Show success notification
  } catch (error) {
    console.error('Export failed:', error)
    // Handle error
  }
}

const saveSearchConfiguration = async () => {
  try {
    const config: SearchConfiguration = {
      name: saveSearchForm.value.name,
      description: saveSearchForm.value.description,
      criteria: searchCriteria.value,
      createdAt: new Date().toISOString()
    }
    
    await AdvancedSearchService.saveSearchConfiguration(config)
    showSaveDialog.value = false
    saveSearchForm.value = { name: '', description: '' }
    // Show success notification
  } catch (error) {
    console.error('Save failed:', error)
    // Handle error
  }
}

const loadSearchConfiguration = (config: SearchConfiguration) => {
  searchCriteria.value = { ...config.criteria }
  showSavedSearches.value = false
}

const deleteSearchConfiguration = async (configId: string) => {
  try {
    await AdvancedSearchService.deleteSearchConfiguration(configId)
    // Show success notification
  } catch (error) {
    console.error('Delete failed:', error)
    // Handle error
  }
}

const formatDate = (date: string | Date): string => {
  if (typeof date === 'string') {
    return new Date(date).toLocaleDateString()
  }
  return date.toLocaleDateString()
}

// Lifecycle
onMounted(() => {
  // Initialize with default criteria
})
</script>

<style scoped>
.advanced-search {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

.search-header {
  margin-bottom: 30px;
}

.search-header h2 {
  color: #2c3e50;
  margin-bottom: 10px;
}

.search-header p {
  color: #7f8c8d;
  font-size: 16px;
}

.search-builder {
  background: #f8f9fa;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
}

.search-actions {
  display: flex;
  gap: 10px;
  margin-bottom: 20px;
  flex-wrap: wrap;
}

.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.2s;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background: #3498db;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #2980b9;
}

.btn-secondary {
  background: #95a5a6;
  color: white;
}

.btn-secondary:hover:not(:disabled) {
  background: #7f8c8d;
}

.btn-outline {
  background: transparent;
  color: #3498db;
  border: 2px solid #3498db;
}

.btn-outline:hover:not(:disabled) {
  background: #3498db;
  color: white;
}

.validation-errors {
  background: #fee;
  border: 1px solid #fcc;
  border-radius: 6px;
  padding: 15px;
  margin-bottom: 20px;
}

.validation-errors h4 {
  color: #c0392b;
  margin-bottom: 10px;
}

.validation-errors ul {
  margin: 0;
  padding-left: 20px;
}

.validation-errors li {
  color: #e74c3c;
  margin-bottom: 5px;
}

.search-results {
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  overflow: hidden;
}

.results-header {
  background: #34495e;
  color: white;
  padding: 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.results-header h3 {
  margin: 0;
}

.result-count {
  background: rgba(255,255,255,0.2);
  padding: 5px 10px;
  border-radius: 4px;
  font-size: 14px;
}

.results-content {
  padding: 20px;
}

.results-content > div {
  margin-bottom: 30px;
}

.results-content h4 {
  color: #2c3e50;
  margin-bottom: 15px;
  padding-bottom: 10px;
  border-bottom: 2px solid #ecf0f1;
}

.results-table {
  overflow-x: auto;
}

.results-table table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}

.results-table th,
.results-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ecf0f1;
}

.results-table th {
  background: #f8f9fa;
  font-weight: 600;
  color: #2c3e50;
}

.number-cell {
  font-weight: 600;
  color: #3498db;
}

.combination-cell {
  font-family: monospace;
  font-size: 13px;
}

.matched-numbers {
  font-family: monospace;
  font-size: 13px;
  color: #27ae60;
  font-weight: 600;
}

.badge {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
}

.badge.main {
  background: #3498db;
  color: white;
}

.badge.bonus {
  background: #f39c12;
  color: white;
}

.badge.powerball {
  background: #e74c3c;
  color: white;
}

.badge.exact {
  background: #27ae60;
  color: white;
}

.badge.partial {
  background: #f39c12;
  color: white;
}

.badge.hot {
  background: #e74c3c;
  color: white;
}

.badge.cold {
  background: #3498db;
  color: white;
}

.badge.normal {
  background: #95a5a6;
  color: white;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 8px;
  padding: 30px;
  max-width: 500px;
  width: 90%;
  max-height: 80vh;
  overflow-y: auto;
}

.modal-content h3 {
  margin-top: 0;
  margin-bottom: 20px;
  color: #2c3e50;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 600;
  color: #2c3e50;
}

.form-group input,
.form-group textarea {
  width: 100%;
  padding: 10px;
  border: 2px solid #ecf0f1;
  border-radius: 6px;
  font-size: 14px;
}

.form-group input:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #3498db;
}

.form-group textarea {
  resize: vertical;
  min-height: 80px;
}

.form-actions {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
}

@media (max-width: 768px) {
  .advanced-search {
    padding: 10px;
  }
  
  .search-actions {
    flex-direction: column;
  }
  
  .btn {
    width: 100%;
  }
  
  .results-header {
    flex-direction: column;
    gap: 10px;
    text-align: center;
  }
  
  .results-table {
    font-size: 12px;
  }
  
  .results-table th,
  .results-table td {
    padding: 8px;
  }
}
</style>