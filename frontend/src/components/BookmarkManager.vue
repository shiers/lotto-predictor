<template>
  <div class="bookmark-manager">
    <div class="bookmark-header">
      <h3>Bookmarks</h3>
      <div class="bookmark-actions">
        <button 
          @click="refreshBookmarks" 
          :disabled="loading"
          class="btn btn-secondary"
        >
          <i class="icon-refresh"></i>
          Refresh
        </button>
        <button 
          @click="showCreateDialog = true"
          class="btn btn-primary"
        >
          <i class="icon-plus"></i>
          Add Bookmark
        </button>
      </div>
    </div>

    <div v-if="loading" class="loading-state">
      <div class="spinner"></div>
      <p>Loading bookmarks...</p>
    </div>

    <div v-else-if="error" class="error-state">
      <p class="error-message">{{ error }}</p>
      <button @click="refreshBookmarks" class="btn btn-secondary">
        Try Again
      </button>
    </div>

    <div v-else-if="bookmarks.length === 0" class="empty-state">
      <div class="empty-icon">📚</div>
      <h4>No Bookmarks Yet</h4>
      <p>Save interesting draws to quickly access them later.</p>
      <button @click="showCreateDialog = true" class="btn btn-primary">
        Create Your First Bookmark
      </button>
    </div>

    <div v-else class="bookmark-list">
      <div class="bookmark-controls">
        <div class="search-box">
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search bookmarks..."
            class="search-input"
          />
          <i class="icon-search"></i>
        </div>
        <div class="sort-controls">
          <label>Sort by:</label>
          <select v-model="sortBy" class="sort-select">
            <option value="createdAt">Date Created</option>
            <option value="drawNumber">Draw Number</option>
            <option value="label">Label</option>
            <option value="drawDate">Draw Date</option>
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

      <draggable
        v-model="sortedBookmarks"
        @end="onDragEnd"
        :disabled="!dragEnabled"
        class="bookmark-items"
        item-key="id"
      >
        <template #item="{ element: bookmark }">
          <div 
            class="bookmark-item"
            :class="{ 'editing': editingBookmark?.id === bookmark.id }"
          >
            <div class="bookmark-content">
              <div class="bookmark-main">
                <div class="bookmark-info">
                  <div class="bookmark-header-row">
                    <h4 v-if="editingBookmark?.id !== bookmark.id" class="bookmark-label">
                      {{ bookmark.label }}
                    </h4>
                    <input
                      v-else
                      v-model="editForm.label"
                      type="text"
                      class="edit-input"
                      @keyup.enter="saveEdit"
                      @keyup.escape="cancelEdit"
                      ref="editInput"
                    />
                    <div class="bookmark-meta">
                      <span class="draw-number">Draw #{{ bookmark.drawNumber }}</span>
                      <span class="draw-date">{{ formatDate(bookmark.drawDate) }}</span>
                    </div>
                  </div>
                  
                  <div v-if="bookmark.description && editingBookmark?.id !== bookmark.id" class="bookmark-description">
                    {{ bookmark.description }}
                  </div>
                  <textarea
                    v-else-if="editingBookmark?.id === bookmark.id"
                    v-model="editForm.description"
                    class="edit-textarea"
                    placeholder="Add description..."
                    rows="2"
                  ></textarea>

                  <div class="winning-numbers">
                    <span class="numbers-label">Numbers:</span>
                    <div class="number-balls">
                      <span 
                        v-for="number in bookmark.winningNumbers" 
                        :key="number"
                        class="number-ball"
                      >
                        {{ number }}
                      </span>
                    </div>
                  </div>
                </div>

                <div class="bookmark-actions">
                  <div v-if="editingBookmark?.id !== bookmark.id" class="action-buttons">
                    <button 
                      @click="navigateToDraw(bookmark.drawNumber)"
                      class="btn btn-sm btn-secondary"
                      title="Go to Draw"
                    >
                      <i class="icon-external-link"></i>
                    </button>
                    <button 
                      @click="startEdit(bookmark)"
                      class="btn btn-sm btn-secondary"
                      title="Edit Bookmark"
                    >
                      <i class="icon-edit"></i>
                    </button>
                    <button 
                      @click="confirmDelete(bookmark)"
                      class="btn btn-sm btn-danger"
                      title="Delete Bookmark"
                    >
                      <i class="icon-trash"></i>
                    </button>
                  </div>
                  <div v-else class="edit-actions">
                    <button 
                      @click="saveEdit"
                      class="btn btn-sm btn-primary"
                      :disabled="!editForm.label.trim()"
                    >
                      <i class="icon-check"></i>
                      Save
                    </button>
                    <button 
                      @click="cancelEdit"
                      class="btn btn-sm btn-secondary"
                    >
                      <i class="icon-x"></i>
                      Cancel
                    </button>
                  </div>
                </div>
              </div>

              <div class="bookmark-footer">
                <span class="created-date">
                  Created: {{ formatDate(bookmark.createdAt) }}
                </span>
                <span v-if="bookmark.updatedAt" class="updated-date">
                  Updated: {{ formatDate(bookmark.updatedAt) }}
                </span>
              </div>
            </div>
          </div>
        </template>
      </draggable>
    </div>

    <!-- Create Bookmark Dialog -->
    <div v-if="showCreateDialog" class="modal-overlay" @click="closeCreateDialog">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>Create Bookmark</h3>
          <button @click="closeCreateDialog" class="close-btn">
            <i class="icon-x"></i>
          </button>
        </div>
        <div class="modal-body">
          <form @submit.prevent="createBookmark">
            <div class="form-group">
              <label for="drawNumber">Draw Number:</label>
              <input
                id="drawNumber"
                v-model.number="createForm.drawNumber"
                type="number"
                min="1"
                required
                class="form-input"
                placeholder="Enter draw number"
              />
            </div>
            <div class="form-group">
              <label for="label">Label:</label>
              <input
                id="label"
                v-model="createForm.label"
                type="text"
                required
                maxlength="100"
                class="form-input"
                placeholder="Enter bookmark label"
              />
            </div>
            <div class="form-group">
              <label for="description">Description (optional):</label>
              <textarea
                id="description"
                v-model="createForm.description"
                maxlength="500"
                rows="3"
                class="form-textarea"
                placeholder="Add description..."
              ></textarea>
            </div>
          </form>
        </div>
        <div class="modal-footer">
          <button 
            @click="closeCreateDialog" 
            type="button" 
            class="btn btn-secondary"
          >
            Cancel
          </button>
          <button 
            @click="createBookmark" 
            type="button" 
            class="btn btn-primary"
            :disabled="!createForm.drawNumber || !createForm.label.trim() || creating"
          >
            <span v-if="creating">Creating...</span>
            <span v-else>Create Bookmark</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Dialog -->
    <div v-if="showDeleteDialog" class="modal-overlay" @click="closeDeleteDialog">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>Confirm Delete</h3>
          <button @click="closeDeleteDialog" class="close-btn">
            <i class="icon-x"></i>
          </button>
        </div>
        <div class="modal-body">
          <p>Are you sure you want to delete the bookmark "{{ bookmarkToDelete?.label }}"?</p>
          <p class="warning-text">This action cannot be undone.</p>
        </div>
        <div class="modal-footer">
          <button 
            @click="closeDeleteDialog" 
            type="button" 
            class="btn btn-secondary"
          >
            Cancel
          </button>
          <button 
            @click="deleteBookmark" 
            type="button" 
            class="btn btn-danger"
            :disabled="deleting"
          >
            <span v-if="deleting">Deleting...</span>
            <span v-else>Delete</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import draggable from 'vuedraggable'
import { bookmarkService, type BookmarkDto, type CreateBookmarkRequest, type UpdateBookmarkRequest } from '../services/bookmarkService'

const router = useRouter()

// Reactive state
const bookmarks = ref<BookmarkDto[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const searchQuery = ref('')
const sortBy = ref<'createdAt' | 'drawNumber' | 'label' | 'drawDate'>('createdAt')
const sortOrder = ref<'asc' | 'desc'>('desc')
const dragEnabled = ref(true)

// Edit state
const editingBookmark = ref<BookmarkDto | null>(null)
const editForm = ref<UpdateBookmarkRequest>({ label: '', description: '' })
const editInput = ref<HTMLInputElement>()

// Create dialog state
const showCreateDialog = ref(false)
const createForm = ref<CreateBookmarkRequest>({ drawNumber: 0, label: '', description: '' })
const creating = ref(false)

// Delete dialog state
const showDeleteDialog = ref(false)
const bookmarkToDelete = ref<BookmarkDto | null>(null)
const deleting = ref(false)

// Computed properties
const filteredBookmarks = computed(() => {
  if (!searchQuery.value) return bookmarks.value
  
  const query = searchQuery.value.toLowerCase()
  return bookmarks.value.filter(bookmark => 
    bookmark.label.toLowerCase().includes(query) ||
    bookmark.description.toLowerCase().includes(query) ||
    bookmark.drawNumber.toString().includes(query)
  )
})

const sortedBookmarks = computed({
  get: () => {
    const sorted = [...filteredBookmarks.value].sort((a, b) => {
      let aValue: any
      let bValue: any
      
      switch (sortBy.value) {
        case 'createdAt':
          aValue = new Date(a.createdAt)
          bValue = new Date(b.createdAt)
          break
        case 'drawNumber':
          aValue = a.drawNumber
          bValue = b.drawNumber
          break
        case 'label':
          aValue = a.label.toLowerCase()
          bValue = b.label.toLowerCase()
          break
        case 'drawDate':
          aValue = new Date(a.drawDate)
          bValue = new Date(b.drawDate)
          break
        default:
          return 0
      }
      
      if (aValue < bValue) return sortOrder.value === 'asc' ? -1 : 1
      if (aValue > bValue) return sortOrder.value === 'asc' ? 1 : -1
      return 0
    })
    
    return sorted
  },
  set: (value) => {
    // Handle drag and drop reordering
    bookmarks.value = value
  }
})

// Methods
const refreshBookmarks = async () => {
  loading.value = true
  error.value = null
  
  try {
    bookmarks.value = await bookmarkService.getUserBookmarks()
  } catch (err) {
    error.value = 'Failed to load bookmarks. Please try again.'
    console.error('Error loading bookmarks:', err)
  } finally {
    loading.value = false
  }
}

const navigateToDraw = (drawNumber: number) => {
  router.push({ name: 'draw', params: { drawNumber } })
}

const startEdit = async (bookmark: BookmarkDto) => {
  editingBookmark.value = bookmark
  editForm.value = {
    label: bookmark.label,
    description: bookmark.description || ''
  }
  
  await nextTick()
  editInput.value?.focus()
}

const saveEdit = async () => {
  if (!editingBookmark.value || !editForm.value.label.trim()) return
  
  try {
    const updated = await bookmarkService.updateBookmark(editingBookmark.value.id, editForm.value)
    if (updated) {
      const index = bookmarks.value.findIndex(b => b.id === editingBookmark.value!.id)
      if (index !== -1) {
        bookmarks.value[index] = updated
      }
    }
    cancelEdit()
  } catch (err) {
    error.value = 'Failed to update bookmark. Please try again.'
    console.error('Error updating bookmark:', err)
  }
}

const cancelEdit = () => {
  editingBookmark.value = null
  editForm.value = { label: '', description: '' }
}

const confirmDelete = (bookmark: BookmarkDto) => {
  bookmarkToDelete.value = bookmark
  showDeleteDialog.value = true
}

const deleteBookmark = async () => {
  if (!bookmarkToDelete.value) return
  
  deleting.value = true
  
  try {
    const success = await bookmarkService.deleteBookmark(bookmarkToDelete.value.id)
    if (success) {
      bookmarks.value = bookmarks.value.filter(b => b.id !== bookmarkToDelete.value!.id)
      closeDeleteDialog()
    } else {
      error.value = 'Failed to delete bookmark. Please try again.'
    }
  } catch (err) {
    error.value = 'Failed to delete bookmark. Please try again.'
    console.error('Error deleting bookmark:', err)
  } finally {
    deleting.value = false
  }
}

const closeDeleteDialog = () => {
  showDeleteDialog.value = false
  bookmarkToDelete.value = null
  deleting.value = false
}

const createBookmark = async () => {
  if (!createForm.value.drawNumber || !createForm.value.label.trim()) return
  
  creating.value = true
  
  try {
    const newBookmark = await bookmarkService.createBookmark(createForm.value)
    bookmarks.value.unshift(newBookmark)
    closeCreateDialog()
  } catch (err) {
    error.value = 'Failed to create bookmark. Please check the draw number and try again.'
    console.error('Error creating bookmark:', err)
  } finally {
    creating.value = false
  }
}

const closeCreateDialog = () => {
  showCreateDialog.value = false
  createForm.value = { drawNumber: 0, label: '', description: '' }
  creating.value = false
}

const onDragEnd = async () => {
  // Handle drag and drop reordering
  const bookmarkIds = sortedBookmarks.value.map(b => b.id)
  try {
    await bookmarkService.reorderBookmarks(bookmarkIds)
  } catch (err) {
    console.error('Error reordering bookmarks:', err)
    // Refresh bookmarks to restore original order
    await refreshBookmarks()
  }
}

const formatDate = (dateString: string): string => {
  return new Date(dateString).toLocaleDateString('en-NZ', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// Lifecycle
onMounted(() => {
  refreshBookmarks()
})
</script>

<style scoped>
.bookmark-manager {
  max-width: 800px;
  margin: 0 auto;
  padding: 1rem;
}

.bookmark-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #e0e0e0;
}

.bookmark-header h3 {
  margin: 0;
  color: #333;
}

.bookmark-actions {
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

.bookmark-controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  gap: 1rem;
}

.search-box {
  position: relative;
  flex: 1;
  max-width: 300px;
}

.search-input {
  width: 100%;
  padding: 0.5rem 2rem 0.5rem 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.9rem;
}

.search-box .icon-search {
  position: absolute;
  right: 0.75rem;
  top: 50%;
  transform: translateY(-50%);
  color: #666;
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

.bookmark-items {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.bookmark-item {
  background: white;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 1rem;
  transition: all 0.2s ease;
  cursor: move;
}

.bookmark-item:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.bookmark-item.editing {
  border-color: #007bff;
  box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.25);
}

.bookmark-main {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
}

.bookmark-info {
  flex: 1;
}

.bookmark-header-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 0.5rem;
}

.bookmark-label {
  margin: 0;
  color: #333;
  font-size: 1.1rem;
}

.bookmark-meta {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.25rem;
  font-size: 0.85rem;
  color: #666;
}

.draw-number {
  font-weight: 600;
  color: #007bff;
}

.bookmark-description {
  color: #666;
  font-size: 0.9rem;
  margin-bottom: 0.75rem;
  line-height: 1.4;
}

.winning-numbers {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.numbers-label {
  font-size: 0.85rem;
  color: #666;
  font-weight: 500;
}

.number-balls {
  display: flex;
  gap: 0.25rem;
}

.number-ball {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  background: #007bff;
  color: white;
  border-radius: 50%;
  font-size: 0.8rem;
  font-weight: 600;
}

.bookmark-actions {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.action-buttons, .edit-actions {
  display: flex;
  gap: 0.25rem;
}

.bookmark-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 0.75rem;
  padding-top: 0.75rem;
  border-top: 1px solid #f0f0f0;
  font-size: 0.8rem;
  color: #888;
}

.edit-input, .edit-textarea {
  width: 100%;
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
}

.edit-input {
  font-size: 1.1rem;
  font-weight: 600;
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

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #333;
}

.form-input, .form-textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
}

.form-textarea {
  resize: vertical;
  min-height: 80px;
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
  .bookmark-manager {
    padding: 0.5rem;
  }
  
  .bookmark-header {
    flex-direction: column;
    gap: 1rem;
    align-items: stretch;
  }
  
  .bookmark-controls {
    flex-direction: column;
    gap: 0.5rem;
  }
  
  .search-box {
    max-width: none;
  }
  
  .bookmark-main {
    flex-direction: column;
    gap: 0.75rem;
  }
  
  .bookmark-header-row {
    flex-direction: column;
    gap: 0.5rem;
    align-items: flex-start;
  }
  
  .bookmark-meta {
    align-items: flex-start;
  }
  
  .action-buttons, .edit-actions {
    justify-content: flex-start;
  }
  
  .modal-content {
    width: 95%;
    margin: 1rem;
  }
}
</style>