import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import ExportDialog from '../ExportDialog.vue'
import { exportService } from '@/services/exportService'
import accessibilityService from '@/services/accessibilityService'

// Mock services
vi.mock('@/services/exportService', () => ({
  exportService: {
    exportData: vi.fn(() => Promise.resolve({
      exportId: 'test-export-123',
      downloadUrl: 'https://example.com/download',
      expiresAt: '2024-01-02T00:00:00Z',
      fileSizeBytes: 1024
    })),
    getExportStatus: vi.fn(() => Promise.resolve({
      status: 'completed',
      progress: 100
    }))
  }
}))

vi.mock('@/services/accessibilityService', () => ({
  default: {
    announce: vi.fn(),
    trapFocus: vi.fn(() => vi.fn()),
    pushFocus: vi.fn(),
    popFocus: vi.fn(),
    generateId: vi.fn(() => 'test-id')
  }
}))

describe('ExportDialog Accessibility', () => {
  const mockData = [
    { drawNumber: 1234, date: '2024-01-01', numbers: [1, 2, 3, 4, 5, 6] }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('has proper modal dialog structure', () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Dialog overlay should have proper modal attributes
    const overlay = wrapper.find('.export-dialog-overlay')
    expect(overlay.exists()).toBe(true)
    expect(overlay.attributes('role')).toBe('dialog')
    expect(overlay.attributes('aria-modal')).toBe('true')
    expect(overlay.attributes('aria-labelledby')).toBe('export-dialog-title')
    
    // Dialog title should exist
    const title = wrapper.find('#export-dialog-title')
    expect(title.exists()).toBe(true)
    expect(title.text()).toBe('Export Data')
  })

  it('has accessible close button', () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    const closeButton = wrapper.find('.close-button')
    expect(closeButton.exists()).toBe(true)
    expect(closeButton.attributes('aria-label')).toBe('Close export dialog')
    expect(closeButton.attributes('type')).toBe('button')
    
    // Close icon should be hidden from screen readers
    const icon = closeButton.find('.icon')
    expect(icon.attributes('aria-hidden')).toBe('true')
  })

  it('has accessible format selection', () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Format selection should have fieldset structure
    const formatFieldset = wrapper.find('.format-selection')
    expect(formatFieldset.exists()).toBe(true)
    expect(formatFieldset.attributes('role')).toBe('radiogroup')
    expect(formatFieldset.attributes('aria-labelledby')).toBe('format-legend')
    
    // Legend should exist
    const legend = wrapper.find('#format-legend')
    expect(legend.exists()).toBe(true)
    expect(legend.text()).toBe('Export Format')
    
    // Radio buttons should have proper attributes
    const radioButtons = wrapper.findAll('input[type="radio"][name="format"]')
    radioButtons.forEach((radio, index) => {
      expect(radio.attributes('id')).toBeDefined()
      expect(radio.attributes('aria-describedby')).toBeDefined()
    })
  })

  it('has accessible format descriptions', () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Each format should have a description
    const csvRadio = wrapper.find('#format-csv')
    expect(csvRadio.exists()).toBe(true)
    expect(csvRadio.attributes('aria-describedby')).toBe('csv-description')
    
    const csvDescription = wrapper.find('#csv-description')
    expect(csvDescription.exists()).toBe(true)
    expect(csvDescription.classes()).toContain('format-description')
    
    // Similar for other formats
    const jsonRadio = wrapper.find('#format-json')
    if (jsonRadio.exists()) {
      expect(jsonRadio.attributes('aria-describedby')).toBe('json-description')
      expect(wrapper.find('#json-description').exists()).toBe(true)
    }
  })

  it('has accessible filename input', () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Filename input should have proper attributes
    const filenameInput = wrapper.find('#export-filename')
    expect(filenameInput.exists()).toBe(true)
    expect(filenameInput.attributes('aria-describedby')).toBe('filename-help')
    expect(filenameInput.attributes('autocomplete')).toBe('off')
    
    // Label should be associated
    const label = wrapper.find('label[for="export-filename"]')
    expect(label.exists()).toBe(true)
    
    // Help text should exist
    const helpText = wrapper.find('#filename-help')
    expect(helpText.exists()).toBe(true)
    expect(helpText.classes()).toContain('input-help')
  })

  it('has accessible export options', () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Export options should have fieldset structure
    const optionsFieldset = wrapper.find('.export-options')
    expect(optionsFieldset.exists()).toBe(true)
    
    const legend = optionsFieldset.find('legend')
    expect(legend.exists()).toBe(true)
    expect(legend.text()).toBe('Export Options')
    
    // Include metadata checkbox
    const metadataCheckbox = wrapper.find('#include-metadata')
    expect(metadataCheckbox.exists()).toBe(true)
    expect(metadataCheckbox.attributes('aria-describedby')).toBe('metadata-help')
    
    const metadataHelp = wrapper.find('#metadata-help')
    expect(metadataHelp.exists()).toBe(true)
    expect(metadataHelp.classes()).toContain('sr-only')
  })

  it('has accessible data preview', () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Data preview should have proper structure
    const preview = wrapper.find('.data-preview')
    expect(preview.exists()).toBe(true)
    expect(preview.attributes('role')).toBe('region')
    expect(preview.attributes('aria-labelledby')).toBe('preview-title')
    
    // Preview title should exist
    const title = wrapper.find('#preview-title')
    expect(title.exists()).toBe(true)
    expect(title.text()).toBe('Data Preview')
    
    // Preview table should be accessible
    const table = wrapper.find('.preview-table')
    if (table.exists()) {
      expect(table.attributes('role')).toBe('table')
      expect(table.attributes('aria-labelledby')).toBe('preview-title')
      
      // Table should have caption
      const caption = table.find('caption')
      expect(caption.exists()).toBe(true)
      expect(caption.classes()).toContain('sr-only')
    }
  })

  it('has accessible action buttons', () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Cancel button should have proper attributes
    const cancelButton = wrapper.find('.cancel-button')
    expect(cancelButton.exists()).toBe(true)
    expect(cancelButton.attributes('type')).toBe('button')
    expect(cancelButton.attributes('aria-label')).toBe('Cancel export and close dialog')
    
    // Export button should have proper attributes
    const exportButton = wrapper.find('.export-button')
    expect(exportButton.exists()).toBe(true)
    expect(exportButton.attributes('type')).toBe('button')
    expect(exportButton.attributes('aria-describedby')).toBe('export-button-help')
    
    // Help text should exist
    const helpText = wrapper.find('#export-button-help')
    expect(helpText.exists()).toBe(true)
    expect(helpText.classes()).toContain('sr-only')
  })

  it('has accessible progress indicator', async () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Start export to show progress
    wrapper.vm.isExporting = true
    wrapper.vm.exportProgress = 50
    await wrapper.vm.$nextTick()
    
    // Progress container should have proper attributes
    const progressContainer = wrapper.find('.export-progress')
    expect(progressContainer.exists()).toBe(true)
    expect(progressContainer.attributes('role')).toBe('status')
    expect(progressContainer.attributes('aria-live')).toBe('polite')
    
    // Progress bar should have proper attributes
    const progressBar = wrapper.find('.progress-bar')
    expect(progressBar.exists()).toBe(true)
    expect(progressBar.attributes('role')).toBe('progressbar')
    expect(progressBar.attributes('aria-valuenow')).toBe('50')
    expect(progressBar.attributes('aria-valuemin')).toBe('0')
    expect(progressBar.attributes('aria-valuemax')).toBe('100')
    expect(progressBar.attributes('aria-labelledby')).toBe('progress-label')
    
    // Progress label should exist
    const progressLabel = wrapper.find('#progress-label')
    expect(progressLabel.exists()).toBe(true)
  })

  it('has accessible success state', async () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Set success state
    wrapper.vm.exportComplete = true
    wrapper.vm.downloadUrl = 'https://example.com/download'
    wrapper.vm.fileSize = 1024
    await wrapper.vm.$nextTick()
    
    // Success container should have proper attributes
    const successContainer = wrapper.find('.export-success')
    expect(successContainer.exists()).toBe(true)
    expect(successContainer.attributes('role')).toBe('alert')
    expect(successContainer.attributes('aria-live')).toBe('polite')
    
    // Success icon should be hidden from screen readers
    const successIcon = wrapper.find('.success-icon')
    expect(successIcon.attributes('aria-hidden')).toBe('true')
    
    // Download button should have proper attributes
    const downloadButton = wrapper.find('.download-button')
    expect(downloadButton.exists()).toBe(true)
    expect(downloadButton.attributes('aria-describedby')).toBe('download-help')
    
    const downloadHelp = wrapper.find('#download-help')
    expect(downloadHelp.exists()).toBe(true)
    expect(downloadHelp.classes()).toContain('sr-only')
  })

  it('has accessible error state', async () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Set error state
    wrapper.vm.exportError = 'Export failed due to server error'
    await wrapper.vm.$nextTick()
    
    // Error container should have proper attributes
    const errorContainer = wrapper.find('.export-error')
    expect(errorContainer.exists()).toBe(true)
    expect(errorContainer.attributes('role')).toBe('alert')
    expect(errorContainer.attributes('aria-live')).toBe('assertive')
    
    // Error icon should be hidden from screen readers
    const errorIcon = wrapper.find('.error-icon')
    expect(errorIcon.attributes('aria-hidden')).toBe('true')
    
    // Retry button should be accessible
    const retryButton = wrapper.find('.retry-button')
    expect(retryButton.exists()).toBe(true)
    expect(retryButton.attributes('aria-describedby')).toBe('retry-help')
  })

  it('manages focus properly', async () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Should trap focus on mount
    expect(accessibilityService.trapFocus).toHaveBeenCalled()
    
    // Should focus first interactive element
    await wrapper.vm.$nextTick()
    
    const firstRadio = wrapper.find('input[type="radio"]')
    expect(document.activeElement).toBe(firstRadio.element)
  })

  it('announces export progress to screen readers', async () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Start export
    await wrapper.vm.startExport()
    
    expect(accessibilityService.announce).toHaveBeenCalledWith('Starting data export')
    
    // Progress updates should be announced
    wrapper.vm.exportProgress = 50
    await wrapper.vm.$nextTick()
    
    expect(accessibilityService.announce).toHaveBeenCalledWith('Export progress: 50%')
  })

  it('announces export completion', async () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Mock successful export
    vi.mocked(exportService.exportData).mockResolvedValue({
      exportId: 'test-123',
      downloadUrl: 'https://example.com/download',
      expiresAt: '2024-01-02T00:00:00Z',
      fileSizeBytes: 1024
    })
    
    // Complete export
    await wrapper.vm.startExport()
    
    expect(accessibilityService.announce).toHaveBeenCalledWith(
      'Export completed successfully. Download is ready.'
    )
  })

  it('supports keyboard navigation', async () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Escape key should close dialog
    const overlay = wrapper.find('.export-dialog-overlay')
    await overlay.trigger('keydown', { key: 'Escape' })
    
    expect(wrapper.emitted('close')).toBeTruthy()
    
    // Enter key should start export when valid
    wrapper.vm.selectedFormat = 'csv'
    wrapper.vm.filename = 'test-export'
    await wrapper.vm.$nextTick()
    
    const exportButton = wrapper.find('.export-button')
    await exportButton.trigger('keydown', { key: 'Enter' })
    
    // Should start export process
    expect(wrapper.vm.isExporting).toBe(true)
  })

  it('has accessible file size display', async () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Set file size information
    wrapper.vm.estimatedSize = 2048
    await wrapper.vm.$nextTick()
    
    const sizeInfo = wrapper.find('.size-info')
    expect(sizeInfo.exists()).toBe(true)
    expect(sizeInfo.attributes('role')).toBe('status')
    expect(sizeInfo.attributes('aria-label')).toBe('Estimated file size: 2.0 KB')
  })

  it('respects accessibility preferences', () => {
    // Test with high contrast mode
    document.documentElement.classList.add('high-contrast')
    
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    expect(wrapper.exists()).toBe(true)
    
    // Test with reduced motion
    document.documentElement.classList.remove('high-contrast')
    document.documentElement.classList.add('reduced-motion')
    
    const wrapper2 = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    expect(wrapper2.exists()).toBe(true)
    
    // Clean up
    document.documentElement.classList.remove('reduced-motion')
  })

  it('has accessible format validation', async () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Try to export without selecting format
    await wrapper.vm.startExport()
    
    // Should show validation error
    const validationError = wrapper.find('.validation-error')
    expect(validationError.exists()).toBe(true)
    expect(validationError.attributes('role')).toBe('alert')
    expect(validationError.attributes('aria-live')).toBe('polite')
    
    // Error should be announced
    expect(accessibilityService.announce).toHaveBeenCalledWith(
      'Please select an export format',
      'assertive'
    )
  })

  it('provides clear instructions for screen readers', () => {
    const wrapper = mount(ExportDialog, {
      props: { 
        isOpen: true,
        data: mockData,
        dataType: 'lookup-results'
      }
    })
    
    // Should have instructions for screen readers
    const instructions = wrapper.find('#dialog-instructions')
    expect(instructions.exists()).toBe(true)
    expect(instructions.classes()).toContain('sr-only')
    expect(instructions.text()).toContain('Select export format and options')
    expect(instructions.text()).toContain('Use Tab to navigate between options')
    expect(instructions.text()).toContain('Press Escape to close this dialog')
  })
})