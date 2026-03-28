<template>
  <div v-if="show" class="modal-overlay" @click="closeDialog">
    <div class="modal-content" @click.stop>
      <div class="modal-header">
        <h3>Export {{ exportTypeLabel }}</h3>
        <button @click="closeDialog" class="close-btn">
          <i class="icon-x"></i>
        </button>
      </div>

      <div class="modal-body">
        <div v-if="currentStep === 'configure'" class="export-configuration">
          <!-- Export Type Selection -->
          <div class="form-group">
            <label>Export Type:</label>
            <div class="export-type-selector">
              <button
                v-for="type in exportTypes"
                :key="type.value"
                @click="selectedExportType = type.value"
                :class="['type-btn', { active: selectedExportType === type.value }]"
              >
                <i :class="type.icon"></i>
                <span>{{ type.label }}</span>
              </button>
            </div>
          </div>

          <!-- Format Selection -->
          <div class="form-group">
            <label for="format">Export Format:</label>
            <select id="format" v-model="exportConfig.format" class="form-select">
              <option value="CSV">CSV (Comma Separated Values)</option>
              <option value="JSON">JSON (JavaScript Object Notation)</option>
              <option value="PDF">PDF (Portable Document Format)</option>
              <option value="Excel">Excel (Microsoft Excel)</option>
            </select>
          </div>

          <!-- File Name -->
          <div class="form-group">
            <label for="fileName">File Name:</label>
            <input
              id="fileName"
              v-model="exportConfig.fileName"
              type="text"
              class="form-input"
              :placeholder="defaultFileName"
            />
            <small class="form-help">
              Leave empty to use default name: {{ defaultFileName }}
            </small>
          </div>

          <!-- Export Options -->
          <div class="form-group">
            <label>Export Options:</label>
            <div class="checkbox-group">
              <label class="checkbox-label">
                <input
                  v-model="exportConfig.includeMetadata"
                  type="checkbox"
                  class="checkbox-input"
                />
                <span class="checkbox-text">Include metadata and search criteria</span>
              </label>
              <label v-if="selectedExportType === 'frequency'" class="checkbox-label">
                <input
                  v-model="exportConfig.includeCharts"
                  type="checkbox"
                  class="checkbox-input"
                />
                <span class="checkbox-text">Include charts and visualizations (PDF only)</span>
              </label>
            </div>
          </div>

          <!-- Data Preview -->
          <div v-if="dataPreview" class="data-preview">
            <h4>Data Preview</h4>
            <div class="preview-stats">
              <div class="stat-item">
                <span class="stat-label">Records:</span>
                <span class="stat-value">{{ dataPreview.recordCount }}</span>
              </div>
              <div class="stat-item">
                <span class="stat-label">Date Range:</span>
                <span class="stat-value">{{ dataPreview.dateRange }}</span>
              </div>
              <div class="stat-item">
                <span class="stat-label">Estimated Size:</span>
                <span class="stat-value">{{ dataPreview.estimatedSize }}</span>
              </div>
            </div>
          </div>
        </div>

        <div v-else-if="currentStep === 'processing'" class="export-processing">
          <div class="processing-content">
            <div class="processing-icon">
              <div class="spinner"></div>
            </div>
            <h4>Generating Export</h4>
            <p>Please wait while we prepare your {{ exportTypeLabel.toLowerCase() }} export...</p>
            
            <div v-if="exportStatus" class="progress-info">
              <div class="progress-bar">
                <div 
                  class="progress-fill" 
                  :style="{ width: `${exportStatus.progressPercentage}%` }"
                ></div>
              </div>
              <div class="progress-text">
                {{ exportStatus.progressPercentage }}% complete
              </div>
              <div v-if="exportStatus.status" class="status-text">
                Status: {{ exportStatus.status }}
              </div>
            </div>
          </div>
        </div>

        <div v-else-if="currentStep === 'complete'" class="export-complete">
          <div class="success-content">
            <div class="success-icon">
              <i class="icon-check-circle"></i>
            </div>
            <h4>Export Complete!</h4>
            <p>Your {{ exportTypeLabel.toLowerCase() }} export has been generated successfully.</p>
            
            <div v-if="exportResult" class="export-info">
              <div class="info-item">
                <span class="info-label">File Size:</span>
                <span class="info-value">{{ formatFileSize(exportResult.fileSizeBytes) }}</span>
              </div>
              <div class="info-item">
                <span class="info-label">Expires:</span>
                <span class="info-value">{{ formatDate(exportResult.expiresAt) }}</span>
              </div>
            </div>

            <div class="download-actions">
              <button @click="downloadExport" class="btn btn-primary btn-lg">
                <i class="icon-download"></i>
                Download Export
              </button>
              <button @click="copyDownloadLink" class="btn btn-secondary">
                <i class="icon-link"></i>
                Copy Link
              </button>
            </div>
          </div>
        </div>

        <div v-else-if="currentStep === 'error'" class="export-error">
          <div class="error-content">
            <div class="error-icon">
              <i class="icon-alert-circle"></i>
            </div>
            <h4>Export Failed</h4>
            <p>{{ errorMessage || 'An error occurred while generating the export.' }}</p>
            
            <div class="error-actions">
              <button @click="retryExport" class="btn btn-primary">
                <i class="icon-refresh"></i>
                Try Again
              </button>
              <button @click="resetDialog" class="btn btn-secondary">
                <i class="icon-arrow-left"></i>
                Back to Configuration
              </button>
            </div>
          </div>
        </div>
      </div>

      <div v-if="currentStep === 'configure'" class="modal-footer">
        <button @click="closeDialog" class="btn btn-secondary">
          Cancel
        </button>
        <button 
          @click="startExport" 
          class="btn btn-primary"
          :disabled="!canStartExport"
        >
          <i class="icon-download"></i>
          Start Export
        </button>
      </div>

      <div v-else-if="currentStep === 'processing'" class="modal-footer">
        <button @click="cancelExport" class="btn btn-secondary">
          Cancel Export
        </button>
      </div>

      <div v-else-if="currentStep === 'complete'" class="modal-footer">
        <button @click="closeDialog" class="btn btn-secondary">
          Close
        </button>
        <button @click="startNewExport" class="btn btn-primary">
          <i class="icon-plus"></i>
          New Export
        </button>
      </div>

      <div v-else-if="currentStep === 'error'" class="modal-footer">
        <button @click="closeDialog" class="btn btn-secondary">
          Close
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onUnmounted } from 'vue'
import { 
  exportService, 
  type ExportResult, 
  type ExportStatus, 
  type LookupExportRequest,
  type FrequencyExportRequest,
  type NavigationExportRequest,
  ExportFormat 
} from '../services/exportService'

interface Props {
  show: boolean
  exportType: 'lookup' | 'frequency' | 'navigation'
  searchCriteria?: any
  userId?: string
}

interface Emits {
  (e: 'close'): void
  (e: 'export-complete', result: ExportResult): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Export types configuration
const exportTypes = [
  { value: 'lookup', label: 'Number Lookup', icon: 'icon-search' },
  { value: 'frequency', label: 'Frequency Analysis', icon: 'icon-bar-chart' },
  { value: 'navigation', label: 'Navigation History', icon: 'icon-navigation' }
]

// Reactive state
const currentStep = ref<'configure' | 'processing' | 'complete' | 'error'>('configure')
const selectedExportType = ref(props.exportType)
const exportConfig = ref({
  format: ExportFormat.CSV,
  fileName: '',
  includeMetadata: true,
  includeCharts: false
})

const exportResult = ref<ExportResult | null>(null)
const exportStatus = ref<ExportStatus | null>(null)
const errorMessage = ref<string | null>(null)
const dataPreview = ref<any>(null)

// Polling for export status
let statusPollingInterval: number | null = null

// Computed properties
const exportTypeLabel = computed(() => {
  const type = exportTypes.find(t => t.value === selectedExportType.value)
  return type?.label || 'Data'
})

const defaultFileName = computed(() => {
  const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, '-')
  const extension = exportService.getFileExtension(exportConfig.value.format)
  return `${selectedExportType.value}-export-${timestamp}.${extension}`
})

const canStartExport = computed(() => {
  return selectedExportType.value && exportConfig.value.format
})

// Watchers
watch(() => props.show, (newShow) => {
  if (newShow) {
    resetDialog()
    selectedExportType.value = props.exportType
    generateDataPreview()
  } else {
    stopStatusPolling()
  }
})

watch(() => props.searchCriteria, () => {
  if (props.show) {
    generateDataPreview()
  }
}, { deep: true })

// Methods
const closeDialog = () => {
  stopStatusPolling()
  emit('close')
}

const resetDialog = () => {
  currentStep.value = 'configure'
  exportResult.value = null
  exportStatus.value = null
  errorMessage.value = null
  exportConfig.value = {
    format: ExportFormat.CSV,
    fileName: '',
    includeMetadata: true,
    includeCharts: false
  }
}

const generateDataPreview = () => {
  // Generate preview based on search criteria
  if (!props.searchCriteria) {
    dataPreview.value = null
    return
  }

  // Mock data preview - in real implementation, this would call an API
  const recordCount = Math.floor(Math.random() * 1000) + 100
  const estimatedSizeKB = Math.floor(recordCount * 0.5) // Rough estimate
  
  dataPreview.value = {
    recordCount,
    dateRange: '2020-01-01 to 2024-12-31',
    estimatedSize: estimatedSizeKB > 1024 
      ? `${(estimatedSizeKB / 1024).toFixed(1)} MB`
      : `${estimatedSizeKB} KB`
  }
}

const startExport = async () => {
  currentStep.value = 'processing'
  errorMessage.value = null

  try {
    let result: ExportResult

    const fileName = exportConfig.value.fileName || defaultFileName.value

    switch (selectedExportType.value) {
      case 'lookup':
        const lookupRequest: LookupExportRequest = {
          searchCriteria: props.searchCriteria || { numbers: [] },
          format: exportConfig.value.format,
          includeMetadata: exportConfig.value.includeMetadata,
          fileName
        }
        result = await exportService.exportLookupResults(lookupRequest)
        break

      case 'frequency':
        const frequencyRequest: FrequencyExportRequest = {
          searchCriteria: props.searchCriteria || { ranges: [] },
          format: exportConfig.value.format,
          includeCharts: exportConfig.value.includeCharts,
          fileName
        }
        result = await exportService.exportFrequencyData(frequencyRequest)
        break

      case 'navigation':
        const navigationRequest: NavigationExportRequest = {
          format: exportConfig.value.format,
          includeMetadata: exportConfig.value.includeMetadata,
          fileName,
          userId: props.userId || 'anonymous'
        }
        result = await exportService.exportNavigationHistory(navigationRequest)
        break

      default:
        throw new Error('Invalid export type')
    }

    exportResult.value = result
    startStatusPolling(result.exportId)

  } catch (error) {
    console.error('Export failed:', error)
    errorMessage.value = error instanceof Error ? error.message : 'Export failed'
    currentStep.value = 'error'
  }
}

const startStatusPolling = (exportId: string) => {
  statusPollingInterval = window.setInterval(async () => {
    try {
      const status = await exportService.getExportStatus(exportId)
      exportStatus.value = status

      if (status.status === 'Completed') {
        stopStatusPolling()
        currentStep.value = 'complete'
        emit('export-complete', exportResult.value!)
      } else if (status.status === 'Failed') {
        stopStatusPolling()
        errorMessage.value = status.errorMessage || 'Export failed'
        currentStep.value = 'error'
      }
    } catch (error) {
      console.error('Error polling export status:', error)
      stopStatusPolling()
      errorMessage.value = 'Failed to check export status'
      currentStep.value = 'error'
    }
  }, 2000) // Poll every 2 seconds
}

const stopStatusPolling = () => {
  if (statusPollingInterval) {
    clearInterval(statusPollingInterval)
    statusPollingInterval = null
  }
}

const cancelExport = async () => {
  if (exportResult.value) {
    try {
      await exportService.deleteExport(exportResult.value.exportId)
    } catch (error) {
      console.error('Error cancelling export:', error)
    }
  }
  stopStatusPolling()
  closeDialog()
}

const downloadExport = async () => {
  if (!exportResult.value) return

  try {
    const blob = await exportService.downloadExport(exportResult.value.exportId)
    const fileName = exportConfig.value.fileName || defaultFileName.value
    exportService.downloadFile(blob, fileName)
  } catch (error) {
    console.error('Error downloading export:', error)
    errorMessage.value = 'Failed to download export'
    currentStep.value = 'error'
  }
}

const copyDownloadLink = async () => {
  if (!exportResult.value) return

  try {
    const baseUrl = window.location.origin
    const downloadUrl = `${baseUrl}${exportResult.value.downloadUrl}`
    await navigator.clipboard.writeText(downloadUrl)
    
    // Show temporary success message
    const button = event?.target as HTMLButtonElement
    if (button) {
      const originalText = button.innerHTML
      button.innerHTML = '<i class="icon-check"></i> Copied!'
      setTimeout(() => {
        button.innerHTML = originalText
      }, 2000)
    }
  } catch (error) {
    console.error('Error copying link:', error)
  }
}

const retryExport = () => {
  resetDialog()
  startExport()
}

const startNewExport = () => {
  resetDialog()
}

const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 Bytes'
  
  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
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

// Cleanup
onUnmounted(() => {
  stopStatusPolling()
})
</script>

<style scoped>
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
  max-width: 600px;
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
  color: #333;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #666;
  padding: 0.25rem;
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

/* Export Configuration */
.export-configuration {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.export-type-selector {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 0.5rem;
}

.type-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem;
  border: 2px solid #e0e0e0;
  border-radius: 8px;
  background: white;
  cursor: pointer;
  transition: all 0.2s ease;
}

.type-btn:hover {
  border-color: #007bff;
}

.type-btn.active {
  border-color: #007bff;
  background: #f8f9fa;
}

.type-btn i {
  font-size: 1.5rem;
  color: #666;
}

.type-btn.active i {
  color: #007bff;
}

.checkbox-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
}

.checkbox-input {
  margin: 0;
}

.checkbox-text {
  font-size: 0.9rem;
}

/* Data Preview */
.data-preview {
  background: #f8f9fa;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  padding: 1rem;
}

.data-preview h4 {
  margin: 0 0 0.75rem 0;
  color: #333;
  font-size: 1rem;
}

.preview-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 0.75rem;
}

.stat-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.stat-label {
  font-size: 0.8rem;
  color: #666;
  font-weight: 500;
}

.stat-value {
  font-size: 0.9rem;
  color: #333;
  font-weight: 600;
}

/* Processing State */
.export-processing {
  text-align: center;
  padding: 2rem 1rem;
}

.processing-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.processing-icon {
  margin-bottom: 0.5rem;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #007bff;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.progress-info {
  width: 100%;
  max-width: 300px;
}

.progress-bar {
  width: 100%;
  height: 8px;
  background: #e0e0e0;
  border-radius: 4px;
  overflow: hidden;
  margin-bottom: 0.5rem;
}

.progress-fill {
  height: 100%;
  background: #007bff;
  transition: width 0.3s ease;
}

.progress-text {
  font-size: 0.9rem;
  color: #666;
  margin-bottom: 0.25rem;
}

.status-text {
  font-size: 0.8rem;
  color: #888;
}

/* Complete State */
.export-complete {
  text-align: center;
  padding: 2rem 1rem;
}

.success-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.success-icon {
  color: #28a745;
  font-size: 3rem;
}

.export-info {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin: 1rem 0;
}

.info-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.5rem 0;
  border-bottom: 1px solid #f0f0f0;
}

.info-label {
  font-size: 0.9rem;
  color: #666;
}

.info-value {
  font-size: 0.9rem;
  color: #333;
  font-weight: 500;
}

.download-actions {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  width: 100%;
  max-width: 250px;
}

/* Error State */
.export-error {
  text-align: center;
  padding: 2rem 1rem;
}

.error-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.error-icon {
  color: #dc3545;
  font-size: 3rem;
}

.error-actions {
  display: flex;
  gap: 0.5rem;
  justify-content: center;
}

/* Form Elements */
.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-weight: 500;
  color: #333;
  font-size: 0.9rem;
}

.form-input, .form-select {
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
}

.form-help {
  font-size: 0.8rem;
  color: #666;
  margin-top: 0.25rem;
}

/* Button Styles */
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
  justify-content: center;
  gap: 0.5rem;
  transition: all 0.2s ease;
  min-width: 100px;
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

.btn-lg {
  padding: 0.75rem 1.5rem;
  font-size: 1rem;
}

/* Responsive Design */
@media (max-width: 768px) {
  .modal-content {
    width: 95%;
    margin: 1rem;
  }
  
  .export-type-selector {
    grid-template-columns: 1fr;
  }
  
  .preview-stats {
    grid-template-columns: 1fr;
  }
  
  .download-actions {
    max-width: none;
  }
  
  .error-actions {
    flex-direction: column;
  }
  
  .modal-footer {
    flex-direction: column-reverse;
    gap: 0.5rem;
  }
  
  .modal-footer .btn {
    width: 100%;
  }
}
</style>