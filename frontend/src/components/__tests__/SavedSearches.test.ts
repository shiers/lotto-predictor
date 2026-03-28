import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import SavedSearches from '../SavedSearches.vue'
import { AdvancedSearchService } from '@/services/advancedSearchService'
import type { SavedSearchSummary, SearchConfiguration } from '@/types/advancedSearch'

// Mock the advanced search service
vi.mock('@/services/advancedSearchService', () => ({
  AdvancedSearchService: {
    getSavedSearches: vi.fn(),
    loadSearchConfiguration: vi.fn(),
    deleteSearchConfiguration: vi.fn(),
    updateSearchConfiguration: vi.fn()
  }
}))

describe('SavedSearches', () => {
  let wrapper: any
  let pinia: any

  const mockSavedSearches: SavedSearchSummary[] = [
    {
      id: '1',
      name: 'Hot Numbers Search',
      description: 'Search for frequently appearing numbers',
      createdAt: '2023-01-01T10:00:00Z',
      lastUsed: '2023-01-15T14:30:00Z',
      resultCount: 25
    },
    {
      id: '2',
      name: 'Range Analysis',
      description: 'Analyze number ranges 1-20',
      createdAt: '2023-01-05T09:00:00Z',
      resultCount: 15
    }
  ]

  const mockSearchConfiguration: SearchConfiguration = {
    id: '1',
    name: 'Hot Numbers Search',
    description: 'Search for frequently appearing numbers',
    criteria: {
      conditions: [{
        id: 'test-1',
        type: 'frequency',
        value: { operator: 'greater', value: 10, timeframe: 'all' }
      }],
      logic: 'AND',
      dateRange: { startDate: null, endDate: null },
      frequencyFilters: { minOccurrences: null, maxOccurrences: null },
      includeBonus: true,
      includePowerball: true
    },
    createdAt: '2023-01-01T10:00:00Z'
  }

  beforeEach(() => {
    pinia = createPinia()
    setActivePinia(pinia)
    vi.clearAllMocks()
  })

  it('renders correctly with modal structure', () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue([])
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    expect(wrapper.find('.modal-overlay').exists()).toBe(true)
    expect(wrapper.find('.modal-content').exists()).toBe(true)
    expect(wrapper.find('h3').text()).toBe('Saved Searches')
    expect(wrapper.find('.close-btn').exists()).toBe(true)
  })

  it('loads saved searches on mount', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue(mockSavedSearches)
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    expect(AdvancedSearchService.getSavedSearches).toHaveBeenCalled()
    expect(wrapper.vm.savedSearches).toEqual(mockSavedSearches)
  })

  it('shows loading state initially', () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockImplementation(() => 
      new Promise(resolve => setTimeout(() => resolve([]), 100))
    )
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    expect(wrapper.find('.loading-state').exists()).toBe(true)
    expect(wrapper.find('.loading-state').text()).toContain('Loading saved searches...')
  })

  it('shows empty state when no searches exist', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue([])
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    expect(wrapper.find('.empty-state').exists()).toBe(true)
    expect(wrapper.find('.empty-state').text()).toContain('No saved searches found')
  })

  it('displays saved searches correctly', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue(mockSavedSearches)
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    const searchItems = wrapper.findAll('.search-item')
    expect(searchItems).toHaveLength(2)
    
    expect(wrapper.text()).toContain('Hot Numbers Search')
    expect(wrapper.text()).toContain('Range Analysis')
    expect(wrapper.text()).toContain('Search for frequently appearing numbers')
    expect(wrapper.text()).toContain('25 results')
  })

  it('loads a search configuration when Load button is clicked', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue(mockSavedSearches)
    vi.mocked(AdvancedSearchService.loadSearchConfiguration).mockResolvedValue(mockSearchConfiguration)
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    const loadButton = wrapper.find('.btn-primary')
    await loadButton.trigger('click')

    expect(AdvancedSearchService.loadSearchConfiguration).toHaveBeenCalledWith('1')
    expect(wrapper.emitted('load')).toBeTruthy()
    expect(wrapper.emitted('load')[0][0]).toEqual(mockSearchConfiguration)
  })

  it('shows loading state on Load button during loading', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue(mockSavedSearches)
    vi.mocked(AdvancedSearchService.loadSearchConfiguration).mockImplementation(() => 
      new Promise(resolve => setTimeout(() => resolve(mockSearchConfiguration), 100))
    )
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    const loadButton = wrapper.find('.btn-primary')
    await loadButton.trigger('click')

    expect(loadButton.text()).toContain('Loading...')
    expect(loadButton.attributes('disabled')).toBeDefined()
  })

  it('opens edit dialog when Edit button is clicked', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue(mockSavedSearches)
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    const editButton = wrapper.find('.btn-secondary')
    await editButton.trigger('click')

    expect(wrapper.vm.editingSearch).toEqual(mockSavedSearches[0])
    expect(wrapper.find('.edit-overlay').exists()).toBe(true)
    expect(wrapper.find('.edit-dialog').exists()).toBe(true)
  })

  it('saves edited search configuration', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue(mockSavedSearches)
    vi.mocked(AdvancedSearchService.updateSearchConfiguration).mockResolvedValue(mockSearchConfiguration)
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    // Open edit dialog
    wrapper.vm.editingSearch = mockSavedSearches[0]
    wrapper.vm.editForm = { name: 'Updated Name', description: 'Updated description' }
    await wrapper.vm.$nextTick()

    // Submit edit form
    await wrapper.vm.saveEdit()

    expect(AdvancedSearchService.updateSearchConfiguration).toHaveBeenCalledWith('1', {
      name: 'Updated Name',
      description: 'Updated description'
    })
    expect(wrapper.vm.editingSearch).toBeNull()
  })

  it('cancels edit dialog', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue(mockSavedSearches)
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    wrapper.vm.editingSearch = mockSavedSearches[0]
    wrapper.vm.editForm = { name: 'Test', description: 'Test' }
    await wrapper.vm.$nextTick()

    await wrapper.vm.cancelEdit()

    expect(wrapper.vm.editingSearch).toBeNull()
    expect(wrapper.vm.editForm).toEqual({ name: '', description: '' })
  })

  it('opens delete confirmation dialog when Delete button is clicked', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue(mockSavedSearches)
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    const deleteButton = wrapper.find('.btn-danger')
    await deleteButton.trigger('click')

    expect(wrapper.vm.deletingSearch).toEqual(mockSavedSearches[0])
    expect(wrapper.find('.delete-overlay').exists()).toBe(true)
    expect(wrapper.find('.delete-dialog').exists()).toBe(true)
  })

  it('executes delete when confirmed', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue(mockSavedSearches)
    vi.mocked(AdvancedSearchService.deleteSearchConfiguration).mockResolvedValue()
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    // Open delete confirmation
    wrapper.vm.deletingSearch = mockSavedSearches[0]
    await wrapper.vm.$nextTick()

    // Execute delete
    await wrapper.vm.executeDelete()

    expect(AdvancedSearchService.deleteSearchConfiguration).toHaveBeenCalledWith('1')
    expect(wrapper.emitted('delete')).toBeTruthy()
    expect(wrapper.emitted('delete')[0][0]).toBe('1')
    expect(wrapper.vm.savedSearches).toHaveLength(1)
  })

  it('cancels delete confirmation', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockResolvedValue(mockSavedSearches)
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    wrapper.vm.deletingSearch = mockSavedSearches[0]
    await wrapper.vm.$nextTick()

    await wrapper.vm.cancelDelete()

    expect(wrapper.vm.deletingSearch).toBeNull()
  })

  it('closes modal when close button is clicked', async () => {
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.close-btn').trigger('click')

    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('closes modal when overlay is clicked', async () => {
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.find('.modal-overlay').trigger('click')

    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('formats dates correctly', () => {
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    const dateString = '2023-01-15T10:30:00Z'
    const formatted = wrapper.vm.formatDate(dateString)
    
    expect(formatted).toMatch(/\d{1,2}\/\d{1,2}\/\d{4} \d{1,2}:\d{2}/)
  })

  it('handles loading errors gracefully', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches).mockRejectedValue(new Error('Load failed'))
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    expect(wrapper.find('.error-state').exists()).toBe(true)
    expect(wrapper.find('.error-message').text()).toContain('Load failed')
    expect(wrapper.find('.btn-primary').text()).toContain('Retry')
  })

  it('retries loading when retry button is clicked', async () => {
    vi.mocked(AdvancedSearchService.getSavedSearches)
      .mockRejectedValueOnce(new Error('Load failed'))
      .mockResolvedValueOnce(mockSavedSearches)
    
    wrapper = mount(SavedSearches, {
      global: {
        plugins: [pinia]
      }
    })

    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    // Should show error state
    expect(wrapper.find('.error-state').exists()).toBe(true)

    // Click retry
    await wrapper.find('.btn-primary').trigger('click')
    await wrapper.vm.$nextTick()
    await new Promise(resolve => setTimeout(resolve, 0))

    // Should now show searches
    expect(wrapper.find('.searches-list').exists()).toBe(true)
    expect(AdvancedSearchService.getSavedSearches).toHaveBeenCalledTimes(2)
  })
})