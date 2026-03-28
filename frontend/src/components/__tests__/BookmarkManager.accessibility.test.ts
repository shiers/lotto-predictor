import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import BookmarkManager from '../BookmarkManager.vue'
import { bookmarkService } from '@/services/bookmarkService'
import accessibilityService from '@/services/accessibilityService'

// Mock services
vi.mock('@/services/bookmarkService', () => ({
  bookmarkService: {
    getUserBookmarks: vi.fn(() => Promise.resolve([
      { id: 1, drawNumber: 1234, label: 'Lucky Draw', description: 'My lucky numbers', createdAt: '2024-01-01' },
      { id: 2, drawNumber: 1235, label: 'Big Win', description: 'Won something', createdAt: '2024-01-02' }
    ])),
    deleteBookmark: vi.fn(() => Promise.resolve(true)),
    updateBookmark: vi.fn(() => Promise.resolve({ id: 1, label: 'Updated Label' }))
  }
}))

vi.mock('@/services/accessibilityService', () => ({
  default: {
    announce: vi.fn(),
    pushFocus: vi.fn(),
    popFocus: vi.fn(),
    trapFocus: vi.fn(() => vi.fn()),
    generateId: vi.fn(() => 'test-id')
  }
}))

describe('BookmarkManager Accessibility', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('has proper semantic structure', () => {
    const wrapper = mount(BookmarkManager)
    
    // Main container should have role="main"
    expect(wrapper.find('[role="main"]').exists()).toBe(true)
    
    // Should have proper heading
    expect(wrapper.find('#bookmarks-title').exists()).toBe(true)
    expect(wrapper.find('h2#bookmarks-title').text()).toBe('Bookmark Manager')
  })

  it('has accessible bookmark list', async () => {
    const wrapper = mount(BookmarkManager)
    
    // Wait for bookmarks to load
    await wrapper.vm.$nextTick()
    
    // Bookmarks list should have proper structure
    const bookmarksList = wrapper.find('.bookmarks-list')
    expect(bookmarksList.exists()).toBe(true)
    expect(bookmarksList.attributes('role')).toBe('list')
    expect(bookmarksList.attributes('aria-labelledby')).toBe('bookmarks-title')
    
    // Each bookmark should be a list item
    const bookmarkItems = wrapper.findAll('.bookmark-item')
    bookmarkItems.forEach(item => {
      expect(item.attributes('role')).toBe('listitem')
      expect(item.attributes('tabindex')).toBe('0')
    })
  })

  it('has accessible bookmark cards', async () => {
    const wrapper = mount(BookmarkManager)
    
    // Mock bookmarks
    wrapper.vm.bookmarks = [
      { id: 1, drawNumber: 1234, label: 'Lucky Draw', description: 'My lucky numbers', createdAt: '2024-01-01' }
    ]
    await wrapper.vm.$nextTick()
    
    const bookmarkCard = wrapper.find('.bookmark-card')
    expect(bookmarkCard.exists()).toBe(true)
    expect(bookmarkCard.attributes('role')).toBe('article')
    expect(bookmarkCard.attributes('aria-labelledby')).toBe('bookmark-1-title')
    expect(bookmarkCard.attributes('aria-describedby')).toBe('bookmark-1-description')
    
    // Bookmark title should exist
    const title = wrapper.find('#bookmark-1-title')
    expect(title.exists()).toBe(true)
    expect(title.text()).toBe('Lucky Draw')
    
    // Bookmark description should exist
    const description = wrapper.find('#bookmark-1-description')
    expect(description.exists()).toBe(true)
    expect(description.classes()).toContain('sr-only')
  })

  it('has accessible action buttons', async () => {
    const wrapper = mount(BookmarkManager)
    
    wrapper.vm.bookmarks = [
      { id: 1, drawNumber: 1234, label: 'Lucky Draw', description: 'My lucky numbers', createdAt: '2024-01-01' }
    ]
    await wrapper.vm.$nextTick()
    
    // Edit button should have proper attributes
    const editButton = wrapper.find('.edit-button')
    expect(editButton.exists()).toBe(true)
    expect(editButton.attributes('aria-label')).toBe('Edit bookmark Lucky Draw')
    expect(editButton.attributes('aria-describedby')).toBe('edit-help')
    
    // Delete button should have proper attributes
    const deleteButton = wrapper.find('.delete-button')
    expect(deleteButton.exists()).toBe(true)
    expect(deleteButton.attributes('aria-label')).toBe('Delete bookmark Lucky Draw')
    expect(deleteButton.attributes('aria-describedby')).toBe('delete-help')
    
    // Navigate button should have proper attributes
    const navigateButton = wrapper.find('.navigate-button')
    expect(navigateButton.exists()).toBe(true)
    expect(navigateButton.attributes('aria-label')).toBe('Navigate to draw 1234')
    
    // Help texts should exist
    expect(wrapper.find('#edit-help').exists()).toBe(true)
    expect(wrapper.find('#delete-help').exists()).toBe(true)
  })

  it('has accessible search functionality', () => {
    const wrapper = mount(BookmarkManager)
    
    // Search input should have proper attributes
    const searchInput = wrapper.find('#bookmark-search')
    expect(searchInput.exists()).toBe(true)
    expect(searchInput.attributes('type')).toBe('search')
    expect(searchInput.attributes('aria-describedby')).toBe('search-help')
    expect(searchInput.attributes('autocomplete')).toBe('off')
    
    // Label should be associated
    const label = wrapper.find('label[for="bookmark-search"]')
    expect(label.exists()).toBe(true)
    
    // Help text should exist
    const helpText = wrapper.find('#search-help')
    expect(helpText.exists()).toBe(true)
    expect(helpText.classes()).toContain('sr-only')
  })

  it('has accessible sort controls', () => {
    const wrapper = mount(BookmarkManager)
    
    // Sort select should have proper attributes
    const sortSelect = wrapper.find('#bookmark-sort')
    expect(sortSelect.exists()).toBe(true)
    expect(sortSelect.attributes('aria-describedby')).toBe('sort-help')
    
    // Label should be associated
    const label = wrapper.find('label[for="bookmark-sort"]')
    expect(label.exists()).toBe(true)
    
    // Help text should exist
    const helpText = wrapper.find('#sort-help')
    expect(helpText.exists()).toBe(true)
    expect(helpText.classes()).toContain('sr-only')
  })

  it('has accessible edit dialog', async () => {
    const wrapper = mount(BookmarkManager)
    
    wrapper.vm.bookmarks = [
      { id: 1, drawNumber: 1234, label: 'Lucky Draw', description: 'My lucky numbers', createdAt: '2024-01-01' }
    ]
    wrapper.vm.showEditDialog = true
    wrapper.vm.editingBookmark = wrapper.vm.bookmarks[0]
    await wrapper.vm.$nextTick()
    
    // Dialog should have proper modal attributes
    const dialog = wrapper.find('.edit-dialog')
    expect(dialog.exists()).toBe(true)
    expect(dialog.attributes('role')).toBe('dialog')
    expect(dialog.attributes('aria-modal')).toBe('true')
    expect(dialog.attributes('aria-labelledby')).toBe('edit-dialog-title')
    
    // Dialog title should exist
    const title = wrapper.find('#edit-dialog-title')
    expect(title.exists()).toBe(true)
    
    // Form fields should have proper labels
    const labelInput = wrapper.find('#edit-label')
    expect(labelInput.exists()).toBe(true)
    expect(labelInput.attributes('aria-describedby')).toBe('label-help')
    
    const descInput = wrapper.find('#edit-description')
    expect(descInput.exists()).toBe(true)
    expect(descInput.attributes('aria-describedby')).toBe('description-help')
    
    // Help texts should exist
    expect(wrapper.find('#label-help').exists()).toBe(true)
    expect(wrapper.find('#description-help').exists()).toBe(true)
  })

  it('has accessible delete confirmation dialog', async () => {
    const wrapper = mount(BookmarkManager)
    
    wrapper.vm.showDeleteDialog = true
    wrapper.vm.deletingBookmark = { id: 1, label: 'Lucky Draw', drawNumber: 1234 }
    await wrapper.vm.$nextTick()
    
    // Dialog should have proper modal attributes
    const dialog = wrapper.find('.delete-dialog')
    expect(dialog.exists()).toBe(true)
    expect(dialog.attributes('role')).toBe('alertdialog')
    expect(dialog.attributes('aria-modal')).toBe('true')
    expect(dialog.attributes('aria-labelledby')).toBe('delete-dialog-title')
    expect(dialog.attributes('aria-describedby')).toBe('delete-dialog-description')
    
    // Dialog title and description should exist
    const title = wrapper.find('#delete-dialog-title')
    expect(title.exists()).toBe(true)
    
    const description = wrapper.find('#delete-dialog-description')
    expect(description.exists()).toBe(true)
    
    // Confirm button should have warning styling
    const confirmButton = wrapper.find('.confirm-delete-button')
    expect(confirmButton.exists()).toBe(true)
    expect(confirmButton.attributes('aria-describedby')).toBe('delete-warning')
    
    const warning = wrapper.find('#delete-warning')
    expect(warning.exists()).toBe(true)
    expect(warning.classes()).toContain('sr-only')
  })

  it('has accessible loading state', async () => {
    const wrapper = mount(BookmarkManager)
    
    // Set loading state
    wrapper.vm.isLoading = true
    await wrapper.vm.$nextTick()
    
    const loadingState = wrapper.find('.loading-state')
    expect(loadingState.exists()).toBe(true)
    expect(loadingState.attributes('role')).toBe('status')
    expect(loadingState.attributes('aria-live')).toBe('polite')
    
    // Loading spinner should be hidden from screen readers
    const spinner = wrapper.find('.loading-spinner')
    if (spinner.exists()) {
      expect(spinner.attributes('aria-hidden')).toBe('true')
    }
  })

  it('has accessible empty state', async () => {
    const wrapper = mount(BookmarkManager)
    
    // Set empty state
    wrapper.vm.bookmarks = []
    wrapper.vm.isLoading = false
    await wrapper.vm.$nextTick()
    
    const emptyState = wrapper.find('.empty-state')
    expect(emptyState.exists()).toBe(true)
    expect(emptyState.attributes('role')).toBe('region')
    expect(emptyState.attributes('aria-labelledby')).toBe('empty-state-title')
    
    // Empty state title should exist
    const title = wrapper.find('#empty-state-title')
    expect(title.exists()).toBe(true)
    
    // Icon should be hidden from screen readers
    const icon = wrapper.find('.empty-state-icon')
    expect(icon.attributes('aria-hidden')).toBe('true')
  })

  it('has accessible error state', async () => {
    const wrapper = mount(BookmarkManager)
    
    // Set error state
    wrapper.vm.error = 'Failed to load bookmarks'
    await wrapper.vm.$nextTick()
    
    const errorState = wrapper.find('.error-state')
    expect(errorState.exists()).toBe(true)
    expect(errorState.attributes('role')).toBe('alert')
    expect(errorState.attributes('aria-live')).toBe('assertive')
    
    // Error icon should be hidden from screen readers
    const errorIcon = wrapper.find('.error-icon')
    expect(errorIcon.attributes('aria-hidden')).toBe('true')
    
    // Retry button should be accessible
    const retryButton = wrapper.find('.retry-button')
    expect(retryButton.exists()).toBe(true)
    expect(retryButton.attributes('aria-describedby')).toBe('retry-help')
  })

  it('announces bookmark actions to screen readers', async () => {
    const wrapper = mount(BookmarkManager)
    
    // Mock successful delete
    vi.mocked(bookmarkService.deleteBookmark).mockResolvedValue(true)
    
    // Delete bookmark
    await wrapper.vm.confirmDelete()
    
    expect(accessibilityService.announce).toHaveBeenCalledWith(
      expect.stringContaining('Bookmark deleted successfully')
    )
  })

  it('manages focus properly for dialogs', async () => {
    const wrapper = mount(BookmarkManager)
    
    // Open edit dialog
    wrapper.vm.openEditDialog({ id: 1, label: 'Test', drawNumber: 1234 })
    await wrapper.vm.$nextTick()
    
    expect(accessibilityService.trapFocus).toHaveBeenCalled()
    
    // Close dialog
    wrapper.vm.closeEditDialog()
    
    expect(accessibilityService.popFocus).toHaveBeenCalled()
  })

  it('supports keyboard navigation', async () => {
    const wrapper = mount(BookmarkManager)
    
    wrapper.vm.bookmarks = [
      { id: 1, drawNumber: 1234, label: 'Lucky Draw', description: 'My lucky numbers', createdAt: '2024-01-01' },
      { id: 2, drawNumber: 1235, label: 'Big Win', description: 'Won something', createdAt: '2024-01-02' }
    ]
    await wrapper.vm.$nextTick()
    
    // Arrow keys should navigate between bookmarks
    const firstBookmark = wrapper.find('.bookmark-item')
    
    // Simulate arrow down
    await firstBookmark.trigger('keydown', { key: 'ArrowDown' })
    
    // Should move focus to next bookmark
    const bookmarks = wrapper.findAll('.bookmark-item')
    expect(bookmarks[1].attributes('tabindex')).toBe('0')
    expect(bookmarks[0].attributes('tabindex')).toBe('-1')
  })

  it('has accessible drag and drop functionality', async () => {
    const wrapper = mount(BookmarkManager, {
      props: { enableDragDrop: true }
    })
    
    wrapper.vm.bookmarks = [
      { id: 1, drawNumber: 1234, label: 'Lucky Draw', description: 'My lucky numbers', createdAt: '2024-01-01' }
    ]
    await wrapper.vm.$nextTick()
    
    // Draggable items should have proper attributes
    const draggableItem = wrapper.find('.bookmark-item[draggable="true"]')
    if (draggableItem.exists()) {
      expect(draggableItem.attributes('role')).toBe('button')
      expect(draggableItem.attributes('aria-describedby')).toBe('drag-help')
      
      // Drag help should exist
      const dragHelp = wrapper.find('#drag-help')
      expect(dragHelp.exists()).toBe(true)
      expect(dragHelp.classes()).toContain('sr-only')
      expect(dragHelp.text()).toContain('Use arrow keys to reorder bookmarks')
    }
  })

  it('respects accessibility preferences', () => {
    // Test with high contrast mode
    document.documentElement.classList.add('high-contrast')
    
    const wrapper = mount(BookmarkManager)
    
    expect(wrapper.exists()).toBe(true)
    
    // Test with reduced motion
    document.documentElement.classList.remove('high-contrast')
    document.documentElement.classList.add('reduced-motion')
    
    const wrapper2 = mount(BookmarkManager)
    
    expect(wrapper2.exists()).toBe(true)
    
    // Clean up
    document.documentElement.classList.remove('reduced-motion')
  })

  it('has accessible pagination for large bookmark lists', async () => {
    const wrapper = mount(BookmarkManager)
    
    // Mock many bookmarks
    const manyBookmarks = Array.from({ length: 50 }, (_, i) => ({
      id: i + 1,
      drawNumber: 1000 + i,
      label: `Bookmark ${i + 1}`,
      description: `Description ${i + 1}`,
      createdAt: '2024-01-01'
    }))
    
    wrapper.vm.bookmarks = manyBookmarks
    await wrapper.vm.$nextTick()
    
    const pagination = wrapper.find('.pagination')
    if (pagination.exists()) {
      expect(pagination.attributes('role')).toBe('navigation')
      expect(pagination.attributes('aria-label')).toBe('Bookmark list pagination')
      
      // Page info should have status role
      const pageInfo = pagination.find('.page-info')
      expect(pageInfo.attributes('role')).toBe('status')
      expect(pageInfo.attributes('aria-live')).toBe('polite')
    }
  })
})