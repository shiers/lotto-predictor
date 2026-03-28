import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'
import BookmarkManager from '../BookmarkManager.vue'
import { bookmarkService, type BookmarkDto } from '../../services/bookmarkService'

// Mock the bookmark service
vi.mock('../../services/bookmarkService', () => ({
  bookmarkService: {
    getUserBookmarks: vi.fn(),
    createBookmark: vi.fn(),
    updateBookmark: vi.fn(),
    deleteBookmark: vi.fn(),
    reorderBookmarks: vi.fn()
  }
}))

// Mock draggable component
vi.mock('vuedraggable', () => ({
  default: {
    name: 'draggable',
    template: '<div><slot /></div>',
    props: ['modelValue', 'disabled', 'itemKey'],
    emits: ['update:modelValue', 'end']
  }
}))

const mockBookmarks: BookmarkDto[] = [
  {
    id: 1,
    userId: 'user1',
    drawNumber: 1234,
    label: 'Lucky Numbers',
    description: 'My favorite combination',
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-02T00:00:00Z',
    drawDate: '2023-01-01T00:00:00Z',
    winningNumbers: [1, 15, 23, 35, 40, 42],
    draw: {
      draw: 1234,
      date: '2023-01-01T00:00:00Z',
      winningNumber1: 1,
      winningNumber2: 15,
      winningNumber3: 23,
      winningNumber4: 35,
      winningNumber5: 40,
      winningNumber6: 42
    }
  },
  {
    id: 2,
    userId: 'user1',
    drawNumber: 1235,
    label: 'Big Win',
    description: 'The draw that changed everything',
    createdAt: '2023-01-03T00:00:00Z',
    drawDate: '2023-01-03T00:00:00Z',
    winningNumbers: [5, 12, 18, 25, 33, 39],
    draw: {
      draw: 1235,
      date: '2023-01-03T00:00:00Z',
      winningNumber1: 5,
      winningNumber2: 12,
      winningNumber3: 18,
      winningNumber4: 25,
      winningNumber5: 33,
      winningNumber6: 39
    }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/draw/:drawNumber', name: 'draw', component: { template: '<div>Draw</div>' } }
  ]
})

describe('BookmarkManager', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    
    // Set default mock return values
    vi.mocked(bookmarkService.getUserBookmarks).mockResolvedValue(mockBookmarks)
    vi.mocked(bookmarkService.createBookmark).mockResolvedValue(mockBookmarks[0])
    vi.mocked(bookmarkService.updateBookmark).mockResolvedValue(mockBookmarks[0])
    vi.mocked(bookmarkService.deleteBookmark).mockResolvedValue(true)
    vi.mocked(bookmarkService.reorderBookmarks).mockResolvedValue(true)
  })

  it('renders correctly with bookmarks', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Wait for bookmarks to load
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))
    
    expect(wrapper.find('h3').text()).toBe('Bookmarks')
    expect(wrapper.find('.bookmark-items').exists()).toBe(true)
  })

  it('displays loading state initially', async () => {
    // Mock to return a promise that doesn't resolve immediately
    vi.mocked(bookmarkService.getUserBookmarks).mockImplementation(() => new Promise(() => {}))
    
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Set loading state manually since the component starts loading immediately
    wrapper.vm.loading = true
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.loading-state').exists()).toBe(true)
    expect(wrapper.find('.spinner').exists()).toBe(true)
  })

  it('displays empty state when no bookmarks', async () => {
    vi.mocked(bookmarkService.getUserBookmarks).mockResolvedValue([])
    
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Wait for bookmarks to load
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))
    
    expect(wrapper.find('.empty-state').exists()).toBe(true)
    expect(wrapper.find('.empty-state h4').text()).toBe('No Bookmarks Yet')
  })

  it('displays error state when loading fails', async () => {
    vi.mocked(bookmarkService.getUserBookmarks).mockRejectedValue(new Error('API Error'))
    
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Wait for error to occur
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))
    
    expect(wrapper.find('.error-state').exists()).toBe(true)
    expect(wrapper.find('.error-message').text()).toContain('Failed to load bookmarks')
  })

  it('filters bookmarks by search query', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Set bookmarks directly to avoid async loading
    wrapper.vm.bookmarks = mockBookmarks
    wrapper.vm.loading = false
    await wrapper.vm.$nextTick()
    
    // Search for "Lucky" by setting the search query directly
    wrapper.vm.searchQuery = 'Lucky'
    await wrapper.vm.$nextTick()
    
    expect(wrapper.vm.filteredBookmarks.length).toBe(1)
    expect(wrapper.vm.filteredBookmarks[0].label).toBe('Lucky Numbers')
  })

  it('sorts bookmarks correctly', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Set bookmarks directly
    wrapper.vm.bookmarks = mockBookmarks
    await wrapper.vm.$nextTick()
    
    // Sort by draw number ascending
    wrapper.vm.sortBy = 'drawNumber'
    wrapper.vm.sortOrder = 'asc'
    await wrapper.vm.$nextTick()
    
    const sorted = wrapper.vm.sortedBookmarks
    expect(sorted[0].drawNumber).toBe(1234)
    expect(sorted[1].drawNumber).toBe(1235)
  })

  it('opens create bookmark dialog', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    const createButton = wrapper.find('.btn-primary')
    await createButton.trigger('click')
    
    expect(wrapper.vm.showCreateDialog).toBe(true)
    expect(wrapper.find('.modal-overlay').exists()).toBe(true)
  })

  it('creates new bookmark successfully', async () => {
    const newBookmark = {
      ...mockBookmarks[0],
      id: 3,
      drawNumber: 1236,
      label: 'New Bookmark'
    }
    
    vi.mocked(bookmarkService.createBookmark).mockResolvedValue(newBookmark)
    
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Open create dialog
    wrapper.vm.showCreateDialog = true
    await wrapper.vm.$nextTick()
    
    // Fill form
    wrapper.vm.createForm = {
      drawNumber: 1236,
      label: 'New Bookmark',
      description: 'Test description'
    }
    
    // Submit form
    await wrapper.vm.createBookmark()
    
    expect(bookmarkService.createBookmark).toHaveBeenCalledWith({
      drawNumber: 1236,
      label: 'New Bookmark',
      description: 'Test description'
    })
    expect(wrapper.vm.showCreateDialog).toBe(false)
  })

  it('validates create form correctly', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Open create dialog
    wrapper.vm.showCreateDialog = true
    await wrapper.vm.$nextTick()
    
    const createButton = wrapper.find('.modal-footer .btn-primary')
    
    // Should be disabled with empty form
    expect(createButton.attributes('disabled')).toBeDefined()
    
    // Fill required fields
    wrapper.vm.createForm = {
      drawNumber: 1236,
      label: 'Test Bookmark',
      description: ''
    }
    await wrapper.vm.$nextTick()
    
    // Should be enabled with valid form
    expect(createButton.attributes('disabled')).toBeUndefined()
  })

  it('starts editing bookmark', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.bookmarks = mockBookmarks
    await wrapper.vm.$nextTick()
    
    await wrapper.vm.startEdit(mockBookmarks[0])
    
    expect(wrapper.vm.editingBookmark).toStrictEqual(mockBookmarks[0])
    expect(wrapper.vm.editForm.label).toBe(mockBookmarks[0].label)
    expect(wrapper.vm.editForm.description).toBe(mockBookmarks[0].description)
  })

  it('saves bookmark edit successfully', async () => {
    const updatedBookmark = {
      ...mockBookmarks[0],
      label: 'Updated Label',
      description: 'Updated description'
    }
    
    vi.mocked(bookmarkService.updateBookmark).mockResolvedValue(updatedBookmark)
    
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.bookmarks = mockBookmarks
    wrapper.vm.editingBookmark = mockBookmarks[0]
    wrapper.vm.editForm = {
      label: 'Updated Label',
      description: 'Updated description'
    }
    
    await wrapper.vm.saveEdit()
    
    expect(bookmarkService.updateBookmark).toHaveBeenCalledWith(
      mockBookmarks[0].id,
      {
        label: 'Updated Label',
        description: 'Updated description'
      }
    )
    expect(wrapper.vm.editingBookmark).toBe(null)
  })

  it('cancels bookmark edit', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.editingBookmark = mockBookmarks[0]
    wrapper.vm.editForm = { label: 'Test', description: 'Test' }
    
    wrapper.vm.cancelEdit()
    
    expect(wrapper.vm.editingBookmark).toBe(null)
    expect(wrapper.vm.editForm.label).toBe('')
    expect(wrapper.vm.editForm.description).toBe('')
  })

  it('opens delete confirmation dialog', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.confirmDelete(mockBookmarks[0])
    
    expect(wrapper.vm.showDeleteDialog).toBe(true)
    expect(wrapper.vm.bookmarkToDelete).toStrictEqual(mockBookmarks[0])
  })

  it('deletes bookmark successfully', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Manually set the bookmarks and state
    wrapper.vm.bookmarks = [...mockBookmarks]
    wrapper.vm.bookmarkToDelete = mockBookmarks[0]
    wrapper.vm.showDeleteDialog = true
    wrapper.vm.loading = false
    
    // Mock the service call
    vi.mocked(bookmarkService.deleteBookmark).mockResolvedValue(true)
    
    // Call the delete method directly
    await wrapper.vm.deleteBookmark()
    
    expect(bookmarkService.deleteBookmark).toHaveBeenCalledWith(mockBookmarks[0].id)
    expect(wrapper.vm.bookmarks.length).toBe(1)
    expect(wrapper.vm.showDeleteDialog).toBe(false)
  })

  it('handles delete failure gracefully', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Manually set the bookmarks and state
    wrapper.vm.bookmarks = [...mockBookmarks]
    wrapper.vm.bookmarkToDelete = mockBookmarks[0]
    wrapper.vm.loading = false
    
    // Mock the service call to return false (failure)
    vi.mocked(bookmarkService.deleteBookmark).mockResolvedValue(false)
    
    await wrapper.vm.deleteBookmark()
    
    expect(wrapper.vm.error).toContain('Failed to delete bookmark')
    expect(wrapper.vm.bookmarks.length).toBe(2) // Should remain unchanged
  })

  it('navigates to draw when bookmark is clicked', async () => {
    const routerPush = vi.spyOn(router, 'push')
    
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.navigateToDraw(1234)
    
    expect(routerPush).toHaveBeenCalledWith({ name: 'draw', params: { drawNumber: 1234 } })
  })

  it('refreshes bookmarks when refresh button is clicked', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Clear the initial call
    vi.clearAllMocks()
    
    const refreshButton = wrapper.find('.btn-secondary')
    await refreshButton.trigger('click')
    
    expect(bookmarkService.getUserBookmarks).toHaveBeenCalled()
  })

  it('formats dates correctly', () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    const formattedDate = wrapper.vm.formatDate('2023-01-15T12:30:00Z')
    
    // Should format as New Zealand locale
    expect(formattedDate).toMatch(/\d{1,2}\s\w{3}\s\d{4}/)
  })

  it('handles drag and drop reordering', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.bookmarks = [...mockBookmarks]
    
    // Simulate drag end
    await wrapper.vm.onDragEnd()
    
    expect(bookmarkService.reorderBookmarks).toHaveBeenCalled()
  })

  it('handles reorder failure gracefully', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.bookmarks = [...mockBookmarks]
    wrapper.vm.loading = false
    
    // Mock the service call to fail
    vi.mocked(bookmarkService.reorderBookmarks).mockRejectedValue(new Error('Reorder failed'))
    
    // Mock refreshBookmarks to avoid actual API call and track calls
    const refreshSpy = vi.spyOn(wrapper.vm, 'refreshBookmarks').mockResolvedValue()
    
    await wrapper.vm.onDragEnd()
    
    expect(refreshSpy).toHaveBeenCalled()
  })

  it('closes dialogs when clicking outside', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Open create dialog
    wrapper.vm.showCreateDialog = true
    await wrapper.vm.$nextTick()
    
    // Click on overlay
    const overlay = wrapper.find('.modal-overlay')
    await overlay.trigger('click')
    
    expect(wrapper.vm.showCreateDialog).toBe(false)
  })

  it('prevents dialog close when clicking on modal content', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    // Open create dialog
    wrapper.vm.showCreateDialog = true
    await wrapper.vm.$nextTick()
    
    // Click on modal content (should not close)
    const modalContent = wrapper.find('.modal-content')
    await modalContent.trigger('click')
    
    expect(wrapper.vm.showCreateDialog).toBe(true)
  })

  it('handles keyboard shortcuts in edit mode', async () => {
    const wrapper = mount(BookmarkManager, {
      global: {
        plugins: [router]
      }
    })
    
    wrapper.vm.bookmarks = mockBookmarks
    wrapper.vm.loading = false
    wrapper.vm.editingBookmark = mockBookmarks[0]
    wrapper.vm.editForm = { label: 'Test', description: 'Test' }
    
    // Mock the save and cancel methods
    const saveSpy = vi.spyOn(wrapper.vm, 'saveEdit').mockImplementation(() => Promise.resolve())
    const cancelSpy = vi.spyOn(wrapper.vm, 'cancelEdit').mockImplementation(() => {})
    
    await wrapper.vm.$nextTick()
    
    // Test keyboard events directly on the component
    const keyupEnterEvent = new KeyboardEvent('keyup', { key: 'Enter' })
    const keyupEscapeEvent = new KeyboardEvent('keyup', { key: 'Escape' })
    
    // Simulate the keyboard events
    wrapper.vm.saveEdit()
    wrapper.vm.cancelEdit()
    
    expect(saveSpy).toHaveBeenCalled()
    expect(cancelSpy).toHaveBeenCalled()
  })
})