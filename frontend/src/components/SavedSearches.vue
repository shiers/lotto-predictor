<template>
  <div class="modal-overlay" @click="$emit('close')">
    <div class="modal-content" @click.stop>
      <div class="modal-header">
        <h3>Saved Searches</h3>
        <button class="close-btn" @click="$emit('close')">&times;</button>
      </div>

      <div class="modal-body">
        <div v-if="isLoading" class="loading-state">
          <p>Loading saved searches...</p>
        </div>

        <div v-else-if="error" class="error-state">
          <p class="error-message">{{ error }}</p>
          <button class="btn btn-primary" @click="loadSavedSearches">Retry</button>
        </div>

        <div v-else-if="savedSearches.length === 0" class="empty-state">
          <p>No saved searches found.</p>
          <p>Create and save a search configuration to see it here.</p>
        </div>

        <div v-else class="searches-list">
          <div 
            v-for="search in savedSearches" 
            :key="search.id"
            class="search-item"
          >
            <div class="search-info">
              <h4 class="search-name">{{ search.name }}</h4>
              <p v-if="search.description" class="search-description">{{ search.description }}</p>
              <div class="search-meta">
                <span class="created-date">Created: {{ formatDate(search.createdAt) }}</span>
                <span v-if="search.lastUsed" class="last-used">Last used: {{ formatDate(search.lastUsed) }}</span>
                <span v-if="search.resultCount !== undefined" class="result-count">{{ search.resultCount }} results</span>
              </div>
            </div>
            
            <div class="search-actions">
              <button 
                class="btn btn-primary btn-small"
                @click="loadSearch(search.id)"
                :disabled="isLoadingSearch === search.id"
              >
                <span v-if="isLoadingSearch === search.id">Loading...</span>
                <span v-else>Load</span>
              </button>
              
              <button 
                class="btn btn-secondary btn-small"
                @click="showEditDialog(search)"
              >
                Edit
              </button>
              
              <button 
                class="btn btn-danger btn-small"
                @click="confirmDelete(search)"
                :disabled="isDeletingSearch === search.id"
              >
                <span v-if="isDeletingSearch === search.id">Deleting...</span>
                <span v-else>Delete</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Edit Search Dialog -->
      <div v-if="editingSearch" class="edit-overlay" @click="cancelEdit">
        <div class="edit-dialog" @click.stop>
          <h4>Edit Search</h4>
          <form @submit.prevent="saveEdit">
            <div class="form-group">
              <label for="editName">Name:</label>
              <input
                id="editName"
                v-model="editForm.name"
                type="text"
                required
                placeholder="Enter search name"
              />
            </div>
            <div class="form-group">
              <label for="editDescription">Description:</label>
              <textarea
                id="editDescription"
                v-model="editForm.description"
                placeholder="Enter description (optional)"
              ></textarea>
            </div>
            <div class="form-actions">
              <button type="submit" class="btn btn-primary" :disabled="isSavingEdit">
                <span v-if="isSavingEdit">Saving...</span>
                <span v-else>Save</span>
              </button>
              <button type="button" class="btn btn-secondary" @click="cancelEdit">Cancel</button>
            </div>
          </form>
        </div>
      </div>

      <!-- Delete Confirmation Dialog -->
      <div v-if="deletingSearch" class="delete-overlay" @click="cancelDelete">
        <div class="delete-dialog" @click.stop>
          <h4>Confirm Delete</h4>
          <p>Are you sure you want to delete the search "{{ deletingSearch.name }}"?</p>
          <p class="warning">This action cannot be undone.</p>
          <div class="form-actions">
            <button 
              class="btn btn-danger" 
              @click="executeDelete"
              :disabled="isDeletingSearch === deletingSearch.id"
            >
              <span v-if="isDeletingSearch === deletingSearch.id">Deleting...</span>
              <span v-else>Delete</span>
            </button>
            <button class="btn btn-secondary" @click="cancelDelete">Cancel</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { AdvancedSearchService } from '../services/advancedSearchService'
import type { SavedSearchSummary, SearchConfiguration } from '../types/advancedSearch'

// Emits
const emit = defineEmits<{
  'close': []
  'load': [config: SearchConfiguration]
  'delete': [id: string]
}>()

// Reactive state
const savedSearches = ref<SavedSearchSummary[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)
const isLoadingSearch = ref<string | null>(null)
const isDeletingSearch = ref<string | null>(null)

// Edit dialog state
const editingSearch = ref<SavedSearchSummary | null>(null)
const editForm = ref({
  name: '',
  description: ''
})
const isSavingEdit = ref(false)

// Delete confirmation state
const deletingSearch = ref<SavedSearchSummary | null>(null)

// Methods
const loadSavedSearches = async () => {
  isLoading.value = true
  error.value = null
  
  try {
    savedSearches.value = await AdvancedSearchService.getSavedSearches()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load saved searches'
  } finally {
    isLoading.value = false
  }
}

const loadSearch = async (id: string) => {
  isLoadingSearch.value = id
  
  try {
    const config = await AdvancedSearchService.loadSearchConfiguration(id)
    emit('load', config)
    
    // Update last used timestamp
    const searchIndex = savedSearches.value.findIndex(s => s.id === id)
    if (searchIndex !== -1) {
      savedSearches.value[searchIndex].lastUsed = new Date().toISOString()
    }
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load search configuration'
  } finally {
    isLoadingSearch.value = null
  }
}

const showEditDialog = (search: SavedSearchSummary) => {
  editingSearch.value = search
  editForm.value = {
    name: search.name,
    description: search.description || ''
  }
}

const cancelEdit = () => {
  editingSearch.value = null
  editForm.value = { name: '', description: '' }
  isSavingEdit.value = false
}

const saveEdit = async () => {
  if (!editingSearch.value) return
  
  isSavingEdit.value = true
  
  try {
    await AdvancedSearchService.updateSearchConfiguration(editingSearch.value.id, {
      name: editForm.value.name,
      description: editForm.value.description
    })
    
    // Update local state
    const searchIndex = savedSearches.value.findIndex(s => s.id === editingSearch.value!.id)
    if (searchIndex !== -1) {
      savedSearches.value[searchIndex].name = editForm.value.name
      savedSearches.value[searchIndex].description = editForm.value.description
    }
    
    cancelEdit()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to update search'
  } finally {
    isSavingEdit.value = false
  }
}

const confirmDelete = (search: SavedSearchSummary) => {
  deletingSearch.value = search
}

const cancelDelete = () => {
  deletingSearch.value = null
}

const executeDelete = async () => {
  if (!deletingSearch.value) return
  
  const searchId = deletingSearch.value.id
  isDeletingSearch.value = searchId
  
  try {
    await AdvancedSearchService.deleteSearchConfiguration(searchId)
    
    // Remove from local state
    savedSearches.value = savedSearches.value.filter(s => s.id !== searchId)
    
    emit('delete', searchId)
    cancelDelete()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to delete search'
  } finally {
    isDeletingSearch.value = null
  }
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

// Lifecycle
onMounted(() => {
  loadSavedSearches()
})
</script>

<style scoped>
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
  max-width: 800px;
  width: 90%;
  max-height: 80vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.modal-header {
  background: #34495e;
  color: white;
  padding: 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  margin: 0;
}

.close-btn {
  background: none;
  border: none;
  color: white;
  font-size: 24px;
  cursor: pointer;
  padding: 0;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  transition: background-color 0.2s;
}

.close-btn:hover {
  background: rgba(255,255,255,0.2);
}

.modal-body {
  padding: 20px;
  flex: 1;
  overflow-y: auto;
}

.loading-state,
.error-state,
.empty-state {
  text-align: center;
  padding: 40px;
  color: #7f8c8d;
}

.error-message {
  color: #e74c3c;
  margin-bottom: 15px;
}

.searches-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.search-item {
  background: #f8f9fa;
  border: 2px solid #ecf0f1;
  border-radius: 8px;
  padding: 20px;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 20px;
}

.search-info {
  flex: 1;
}

.search-name {
  margin: 0 0 8px 0;
  color: #2c3e50;
  font-size: 16px;
}

.search-description {
  margin: 0 0 10px 0;
  color: #7f8c8d;
  font-size: 14px;
  line-height: 1.4;
}

.search-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 15px;
  font-size: 12px;
  color: #95a5a6;
}

.search-actions {
  display: flex;
  gap: 8px;
  flex-shrink: 0;
}

.btn {
  padding: 8px 16px;
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

.btn-small {
  padding: 6px 12px;
  font-size: 12px;
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

.btn-danger {
  background: #e74c3c;
  color: white;
}

.btn-danger:hover:not(:disabled) {
  background: #c0392b;
}

.edit-overlay,
.delete-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.3);
  display: flex;
  align-items: center;
  justify-content: center;
}

.edit-dialog,
.delete-dialog {
  background: white;
  border-radius: 8px;
  padding: 30px;
  max-width: 400px;
  width: 90%;
}

.edit-dialog h4,
.delete-dialog h4 {
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

.warning {
  color: #e74c3c;
  font-size: 14px;
  margin-bottom: 20px;
}

@media (max-width: 768px) {
  .modal-content {
    width: 95%;
    max-height: 90vh;
  }
  
  .search-item {
    flex-direction: column;
    align-items: stretch;
  }
  
  .search-actions {
    justify-content: flex-end;
  }
  
  .search-meta {
    flex-direction: column;
    gap: 5px;
  }
  
  .form-actions {
    flex-direction: column;
  }
}
</style>