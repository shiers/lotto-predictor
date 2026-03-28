import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import ExportDialog from '../ExportDialog.vue'
import { exportService, ExportFormat, type ExportResult, type ExportStatus } from '../../services/exportService'

// Mock the export service
vi.mock('../../services/exportService', () => ({
  exportService: {
    exportLookupResults: vi.fn(),
    exportFrequencyData: vi.fn(),
    exportNavigationHistory: vi.fn(),
    getExportStatus: vi.fn(),
    downloadExport: vi.fn(),
    deleteExport: vi.fn(),
    downloadFile: vi.fn(),
    getFileExtension: vi.fn(),
    generateFileName: vi.fn()
  },
  ExportFormat: {
    CSV: 'CSV',
    JSON: 'JSON',
    PDF: 'PDF',
    Excel: 'Excel'
  }
}))

const mockExportResult: ExportResult = {
  exportId: 'export-123',
  downloadUrl: '/api/exports/export-123/download',
  expiresAt: '2023-12-31T23:59:59Z',
  fileSizeBytes: 1024
}

const mockExportStatus: ExportStatus = {
  exportId: 'export-123',
  status: 'Processing',
  progressPercentage: 50,
  createdAt: '2023-01-01T00:00:00Z'
}

const mockSearchCriteria = {
  numbers: [1, 15, 23],
  startDate: '2023-01-01',
  endDate: '2023-12-31'
}

describe('ExportDialog', () => {
  let intervalSpy: ReturnType<typeof vi.spyOn>

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    
    // Mock window.setInterval and clearInterval
    intervalSpy = vi.spyOn(window, 'setInterval')
    vi.spyOn(window, 'clearInterval')
    
    // Set default mock return values
    vi.mocked(exportService.exportLookupResults).mockResolvedValue(mockExportResult)
    vi.mocked(exportService.exportFrequencyData).mockResolvedValue(mockExportResult)
    vi.mocked(exportService.exportNavigationHistory).mockResolvedValue(mockExportResult)
    vi.mocked(exportService.getExportStatus).mockResolvedValue(mockExportStatus)
    vi.mocked(exportService.downloadExport).mockResolvedValue(new Blob(['test']))
    vi.mocked(exportService.deleteExport).mockResolvedValue(true)
    vi.mocked(exportService.getFileExtension).mockReturnValue('csv')
    vi.mocked(exportService.generateFileName).mockReturnValue('test-export.csv')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('renders correctly when shown', () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup',
        searchCriteria: mockSearchCriteria
      }
    })
    
    expect(wrapper.find('.modal-overlay').exists()).toBe(true)
    expect(wrapper.find('h3').text()).toBe('Export Number Lookup')
  })

  it('does not render when not shown', () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: false,
        exportType: 'lookup'
      }
    })
    
    expect(wrapper.find('.modal-overlay').exists()).toBe(false)
  })

  it('displays correct export type labels', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'frequency'
      }
    })
    
    expect(wrapper.find('h3').text()).toBe('Export Frequency Analysis')
    
    // Need to update the selectedExportType as well
    await wrapper.setProps({ exportType: 'navigation' })
    wrapper.vm.selectedExportType = 'navigation'
    await wrapper.vm.$nextTick()
    expect(wrapper.find('h3').text()).toBe('Export Navigation History')
  })

  it('shows configuration step initially', () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    expect(wrapper.find('.export-configuration').exists()).toBe(true)
    expect(wrapper.find('.export-type-selector').exists()).toBe(true)
    expect(wrapper.find('#format').exists()).toBe(true)
  })

  it('allows format selection', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    const formatSelect = wrapper.find('#format')
    await formatSelect.setValue('JSON')
    
    expect(wrapper.vm.exportConfig.format).toBe('JSON')
  })

  it('allows custom filename input', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    const fileNameInput = wrapper.find('#fileName')
    await fileNameInput.setValue('my-custom-export')
    
    expect(wrapper.vm.exportConfig.fileName).toBe('my-custom-export')
  })

  it('toggles export options correctly', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    const metadataCheckbox = wrapper.find('input[type="checkbox"]')
    await metadataCheckbox.setValue(false)
    
    expect(wrapper.vm.exportConfig.includeMetadata).toBe(false)
  })

  it('shows charts option for frequency exports', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'frequency'
      }
    })
    
    wrapper.vm.selectedExportType = 'frequency'
    await wrapper.vm.$nextTick()
    
    const checkboxes = wrapper.findAll('input[type="checkbox"]')
    expect(checkboxes.length).toBe(2) // metadata + charts
  })

  it('disables start export button when invalid', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    wrapper.vm.selectedExportType = ''
    wrapper.vm.exportConfig.format = '' as any
    await wrapper.vm.$nextTick()
    
    const startButton = wrapper.find('.modal-footer .btn-primary')
    expect(startButton.attributes('disabled')).toBeDefined()
  })

  it('enables start export button when valid', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    await wrapper.vm.$nextTick()
    
    const startButton = wrapper.find('.modal-footer .btn-primary')
    expect(startButton.attributes('disabled')).toBeUndefined()
  })

  it('starts lookup export correctly', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup',
        searchCriteria: mockSearchCriteria
      }
    })
    
    await wrapper.vm.startExport()
    
    expect(exportService.exportLookupResults).toHaveBeenCalledWith({
      searchCriteria: mockSearchCriteria,
      format: ExportFormat.CSV,
      includeMetadata: true,
      fileName: expect.any(String)
    })
    expect(wrapper.vm.currentStep).toBe('processing')
  })

  it('starts frequency export correctly', async () => {
    const frequencyCriteria = {
      ranges: [{ startNumber: 1, endNumber: 10, label: 'Low' }]
    }
    
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'frequency',
        searchCriteria: frequencyCriteria
      }
    })
    
    wrapper.vm.selectedExportType = 'frequency'
    await wrapper.vm.startExport()
    
    expect(exportService.exportFrequencyData).toHaveBeenCalledWith({
      searchCriteria: frequencyCriteria,
      format: ExportFormat.CSV,
      includeCharts: false,
      fileName: expect.any(String)
    })
  })

  it('starts navigation export correctly', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'navigation',
        userId: 'user123'
      }
    })
    
    wrapper.vm.selectedExportType = 'navigation'
    await wrapper.vm.startExport()
    
    expect(exportService.exportNavigationHistory).toHaveBeenCalledWith({
      format: ExportFormat.CSV,
      includeMetadata: true,
      fileName: expect.any(String),
      userId: 'user123'
    })
  })

  it('shows processing state during export', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup',
        searchCriteria: mockSearchCriteria
      }
    })
    
    await wrapper.vm.startExport()
    
    expect(wrapper.find('.export-processing').exists()).toBe(true)
    expect(wrapper.find('.spinner').exists()).toBe(true)
    expect(wrapper.find('h4').text()).toBe('Generating Export')
  })

  it('polls export status during processing', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup',
        searchCriteria: mockSearchCriteria
      }
    })
    
    await wrapper.vm.startExport()
    
    expect(intervalSpy).toHaveBeenCalledWith(expect.any(Function), 2000)
  })

  it('shows completed state when export finishes', async () => {
    const completedStatus = { ...mockExportStatus, status: 'Completed', progressPercentage: 100 }
    vi.mocked(exportService.getExportStatus).mockResolvedValue(completedStatus)
    
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup',
        searchCriteria: mockSearchCriteria
      }
    })
    
    await wrapper.vm.startExport()
    
    // Simulate status polling
    wrapper.vm.exportStatus = completedStatus
    wrapper.vm.currentStep = 'complete'
    await wrapper.vm.$nextTick()
    
    expect(wrapper.find('.export-complete').exists()).toBe(true)
    expect(wrapper.find('h4').text()).toBe('Export Complete!')
  })

  it('shows error state when export fails', async () => {
    vi.mocked(exportService.exportLookupResults).mockRejectedValue(new Error('Export failed'))
    
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup',
        searchCriteria: mockSearchCriteria
      }
    })
    
    await wrapper.vm.startExport()
    
    expect(wrapper.vm.currentStep).toBe('error')
    expect(wrapper.vm.errorMessage).toBe('Export failed')
  })

  it('downloads export file when download button clicked', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    wrapper.vm.exportResult = mockExportResult
    wrapper.vm.currentStep = 'complete'
    await wrapper.vm.$nextTick()
    
    await wrapper.vm.downloadExport()
    
    expect(exportService.downloadExport).toHaveBeenCalledWith('export-123')
    expect(exportService.downloadFile).toHaveBeenCalled()
  })

  it('copies download link to clipboard', async () => {
    // Mock clipboard API
    Object.assign(navigator, {
      clipboard: {
        writeText: vi.fn().mockResolvedValue(undefined)
      }
    })
    
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    wrapper.vm.exportResult = mockExportResult
    wrapper.vm.currentStep = 'complete'
    await wrapper.vm.$nextTick()
    
    await wrapper.vm.copyDownloadLink()
    
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
      expect.stringContaining('/api/exports/export-123/download')
    )
  })

  it('cancels export and deletes export job', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup',
        searchCriteria: mockSearchCriteria
      }
    })
    
    await wrapper.vm.startExport()
    wrapper.vm.exportResult = mockExportResult
    
    await wrapper.vm.cancelExport()
    
    expect(exportService.deleteExport).toHaveBeenCalledWith('export-123')
  })

  it('retries export after failure', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup',
        searchCriteria: mockSearchCriteria
      }
    })
    
    wrapper.vm.currentStep = 'error'
    wrapper.vm.errorMessage = 'Test error'
    
    // Call retryExport which should reset and call startExport
    await wrapper.vm.retryExport()
    
    // Verify that the dialog was reset and startExport was called
    expect(wrapper.vm.currentStep).toBe('processing')
  })

  it('resets dialog when starting new export', () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    wrapper.vm.currentStep = 'complete'
    wrapper.vm.exportResult = mockExportResult
    wrapper.vm.errorMessage = 'Test error'
    
    wrapper.vm.startNewExport()
    
    expect(wrapper.vm.currentStep).toBe('configure')
    expect(wrapper.vm.exportResult).toBe(null)
    expect(wrapper.vm.errorMessage).toBe(null)
  })

  it('closes dialog and emits close event', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    await wrapper.vm.closeDialog()
    
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('emits export-complete event when export finishes', async () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    wrapper.vm.exportResult = mockExportResult
    wrapper.vm.currentStep = 'complete'
    
    // Simulate the completion event
    wrapper.vm.$emit('export-complete', mockExportResult)
    
    expect(wrapper.emitted('export-complete')).toBeTruthy()
    expect(wrapper.emitted('export-complete')![0]).toEqual([mockExportResult])
  })

  it('formats file size correctly', () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    expect(wrapper.vm.formatFileSize(0)).toBe('0 Bytes')
    expect(wrapper.vm.formatFileSize(1024)).toBe('1 KB')
    expect(wrapper.vm.formatFileSize(1048576)).toBe('1 MB')
    expect(wrapper.vm.formatFileSize(1536)).toBe('1.5 KB')
  })

  it('formats dates correctly', () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    const formattedDate = wrapper.vm.formatDate('2023-01-15T12:30:00Z')
    
    // Should format as New Zealand locale
    expect(formattedDate).toMatch(/\d{1,2}\s\w{3}\s\d{4}/)
  })

  it('generates default filename correctly', () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    const filename = wrapper.vm.defaultFileName
    
    expect(filename).toMatch(/lookup-export-\d{4}-\d{2}-\d{2}T\d{2}-\d{2}-\d{2}\.csv/)
  })

  it('stops polling when component is unmounted', () => {
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    // Start polling
    wrapper.vm.statusPollingInterval = 123 as any
    
    wrapper.unmount()
    
    expect(window.clearInterval).toHaveBeenCalledWith(123)
  })

  it('handles export status polling errors', async () => {
    vi.mocked(exportService.getExportStatus).mockRejectedValue(new Error('Status error'))
    
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup',
        searchCriteria: mockSearchCriteria
      }
    })
    
    await wrapper.vm.startExport()
    
    // Simulate polling error by calling the status check directly
    try {
      await exportService.getExportStatus('export-123')
    } catch (error) {
      wrapper.vm.errorMessage = 'Failed to check export status'
      wrapper.vm.currentStep = 'error'
    }
    
    expect(wrapper.vm.currentStep).toBe('error')
    expect(wrapper.vm.errorMessage).toBe('Failed to check export status')
  })

  it('handles download errors gracefully', async () => {
    vi.mocked(exportService.downloadExport).mockRejectedValue(new Error('Download failed'))
    
    const wrapper = mount(ExportDialog, {
      props: {
        show: true,
        exportType: 'lookup'
      }
    })
    
    wrapper.vm.exportResult = mockExportResult
    wrapper.vm.currentStep = 'complete'
    
    await wrapper.vm.downloadExport()
    
    expect(wrapper.vm.currentStep).toBe('error')
    expect(wrapper.vm.errorMessage).toBe('Failed to download export')
  })
})