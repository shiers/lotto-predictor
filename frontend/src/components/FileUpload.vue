<template>
  <div class="file-upload">
    <div class="upload-area" :class="{ 'drag-over': isDragOver }" @drop="handleDrop" @dragover.prevent="handleDragOver" @dragleave="handleDragLeave">
      <div v-if="!isUploading" class="upload-content">
        <div class="upload-icon">📁</div>
        <p>Drag and drop your CSV, TXT, or PDF files here, or click to browse</p>
        <input ref="fileInput" type="file" multiple accept=".csv,.txt,.pdf" @change="handleFileSelect" class="file-input" />
        <button @click="triggerFileSelect" class="browse-button">Browse Files</button>
      </div>
      
      <div v-if="isUploading || isCheckingDuplicates" class="upload-progress">
        <div class="progress-info">
          <h3 v-if="isCheckingDuplicates">Checking {{ currentFile?.name }} for duplicates...</h3>
          <h3 v-else>Uploading {{ currentFile?.name }}</h3>
          <p v-if="!isCheckingDuplicates">{{ uploadProgress }}% complete</p>
          <p v-else>Analyzing file content...</p>
        </div>
        <div v-if="!isCheckingDuplicates" class="progress-bar">
          <div class="progress-fill" :style="{ width: uploadProgress + '%' }"></div>
        </div>
        <div v-else class="checking-spinner">
          <div class="spinner"></div>
        </div>
        <button @click="cancelUpload" class="cancel-button" :disabled="isCheckingDuplicates">
          {{ isCheckingDuplicates ? 'Checking...' : 'Cancel' }}
        </button>
      </div>
    </div>
    
    <div v-if="uploadResults.length > 0" class="upload-results">
      <h4>Upload Results</h4>
      <div v-for="result in uploadResults" :key="result.fileName" class="result-item" :class="result.success ? 'success' : 'error'">
        <div class="result-info">
          <strong>{{ result.fileName }}</strong>
          <span v-if="result.success" class="success-message">
            ✅ {{ result.recordsAdded }} records added, {{ result.recordsSkipped }} skipped
            <span v-if="result.recordsAdded + result.recordsSkipped > 0" class="success-details">
              ({{ Math.round((result.recordsAdded / (result.recordsAdded + result.recordsSkipped)) * 100) }}% success rate)
            </span>
          </span>
          <span v-else class="error-message">
            ❌ {{ result.error }}
          </span>
          <button v-if="!result.success" @click="retryUpload(result.fileName)" class="retry-button">
            Retry
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useAppStore } from '@/stores/app'
import { uploadService, type UploadResult, type UploadProgress } from '@/services/uploadService'

const appStore = useAppStore()

// Reactive state
const isDragOver = ref(false)
const isUploading = ref(false)
const uploadProgress = ref(0)
const currentFile = ref<File | null>(null)
const uploadResults = ref<UploadResult[]>([])
const fileInput = ref<HTMLInputElement>()
const currentUploadId = ref<string | null>(null)
const isCheckingDuplicates = ref(false)

// File selection and drag/drop handlers
const triggerFileSelect = () => {
  fileInput.value?.click()
}

const handleFileSelect = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files) {
    handleFiles(Array.from(target.files))
  }
}

const handleDrop = (event: DragEvent) => {
  event.preventDefault()
  isDragOver.value = false
  
  if (event.dataTransfer?.files) {
    handleFiles(Array.from(event.dataTransfer.files))
  }
}

const handleDragOver = (event: DragEvent) => {
  event.preventDefault()
  isDragOver.value = true
}

const handleDragLeave = () => {
  isDragOver.value = false
}

// File validation using upload service
const validateFile = (file: File): string | null => {
  return uploadService.validateFile(file)
}

// File upload handling
const handleFiles = async (files: File[]) => {
  uploadResults.value = []
  
  for (const file of files) {
    const validationError = validateFile(file)
    if (validationError) {
      uploadResults.value.push({
        fileName: file.name,
        success: false,
        error: validationError
      })
      continue
    }
    
    await uploadFile(file)
  }
}

const uploadFile = async (file: File) => {
  currentFile.value = file
  isUploading.value = true
  uploadProgress.value = 0
  
  try {
    // Check for duplicates first (for CSV files)
    if (file.name.toLowerCase().endsWith('.csv')) {
      isCheckingDuplicates.value = true
      appStore.addNotification({
        type: 'info',
        title: 'Checking for Duplicates',
        message: `Analyzing ${file.name} for duplicate records...`
      })
      
      const duplicateCheck = await uploadService.checkForDuplicates(file)
      isCheckingDuplicates.value = false
      
      if (duplicateCheck.isDuplicate) {
        uploadResults.value.push({
          fileName: file.name,
          success: false,
          error: duplicateCheck.message || 'File contains duplicate data'
        })
        
        appStore.addNotification({
          type: 'warning',
          title: 'Duplicate Data Detected',
          message: `${duplicateCheck.message || 'File contains duplicate data'}. Upload cancelled to prevent data conflicts.`
        })
        
        emit('uploadError', { 
          fileName: file.name, 
          error: duplicateCheck.message || 'Duplicate data detected',
          errorType: 'duplicate',
          timestamp: new Date().toISOString()
        })
        return
      }
    }
    
    // Start upload using upload service
    currentUploadId.value = uploadService.uploadFile(
      file,
      (progress: UploadProgress) => {
        uploadProgress.value = progress.percentage
      },
      (result: UploadResult) => {
        uploadResults.value.push(result)
        
        const totalRecords = (result.recordsAdded || 0) + (result.recordsSkipped || 0)
        const successRate = totalRecords > 0 ? Math.round(((result.recordsAdded || 0) / totalRecords) * 100) : 100
        
        appStore.addNotification({
          type: 'success',
          title: 'Upload Successful',
          message: `${file.name} processed successfully. ${result.recordsAdded || 0} new records added, ${result.recordsSkipped || 0} duplicates skipped (${successRate}% success rate).`
        })
        
        // Emit success event for parent components with detailed statistics
        emit('uploadSuccess', {
          fileName: result.fileName,
          recordsAdded: result.recordsAdded,
          recordsSkipped: result.recordsSkipped,
          totalRecords: totalRecords,
          successRate: successRate,
          timestamp: new Date().toISOString()
        })
      },
      (error: string) => {
        uploadResults.value.push({
          fileName: file.name,
          success: false,
          error: error
        })
        
        // Categorize error types for better user feedback
        let errorType = 'unknown'
        let userFriendlyMessage = error
        
        if (error.toLowerCase().includes('network')) {
          errorType = 'network'
          userFriendlyMessage = 'Network connection failed. Please check your internet connection and try again.'
        } else if (error.toLowerCase().includes('timeout')) {
          errorType = 'timeout'
          userFriendlyMessage = 'Upload timed out. The file may be too large or the server is busy. Please try again.'
        } else if (error.toLowerCase().includes('invalid') || error.toLowerCase().includes('format')) {
          errorType = 'format'
          userFriendlyMessage = 'File format is invalid or corrupted. Please check your file and try again.'
        } else if (error.toLowerCase().includes('size') || error.toLowerCase().includes('large')) {
          errorType = 'size'
          userFriendlyMessage = 'File is too large. Please reduce the file size and try again.'
        }
        
        appStore.addNotification({
          type: 'error',
          title: 'Upload Failed',
          message: `${file.name}: ${userFriendlyMessage}`
        })
        
        // Emit error event for parent components with categorized error info
        emit('uploadError', { 
          fileName: file.name, 
          error: error,
          errorType: errorType,
          userFriendlyMessage: userFriendlyMessage,
          timestamp: new Date().toISOString()
        })
      }
    )
    
  } catch (error: any) {
    const errorMessage = error.message || 'Upload failed'
    
    uploadResults.value.push({
      fileName: file.name,
      success: false,
      error: errorMessage
    })
    
    appStore.addNotification({
      type: 'error',
      title: 'Upload Failed',
      message: `Failed to upload ${file.name}: ${errorMessage}`
    })
    
    // Emit error event for parent components
    emit('uploadError', { 
      fileName: file.name, 
      error: errorMessage,
      errorType: 'general',
      timestamp: new Date().toISOString()
    })
    
  } finally {
    isUploading.value = false
    isCheckingDuplicates.value = false
    currentFile.value = null
    uploadProgress.value = 0
    currentUploadId.value = null
  }
}

const cancelUpload = () => {
  if (currentUploadId.value) {
    const cancelled = uploadService.cancelUpload(currentUploadId.value)
    if (cancelled) {
      appStore.addNotification({
        type: 'info',
        title: 'Upload Cancelled',
        message: 'File upload was cancelled'
      })
    }
  }
}

const retryUpload = (fileName: string) => {
  // Find the failed file in results and retry
  const failedResult = uploadResults.value.find(r => r.fileName === fileName && !r.success)
  if (failedResult && currentFile.value?.name === fileName) {
    uploadFile(currentFile.value)
  }
}

// Component events
const emit = defineEmits<{
  uploadSuccess: [data: any]
  uploadError: [error: { fileName: string; error: string }]
}>()

// Cleanup on unmount
onUnmounted(() => {
  uploadService.cancelAllUploads()
})
</script>

<style scoped>
.file-upload {
  max-width: 600px;
  margin: 0 auto;
}

.upload-area {
  border: 2px dashed var(--color-border);
  border-radius: 12px;
  padding: 3rem;
  text-align: center;
  transition: all 0.3s ease;
  background: var(--color-background-soft);
}

.upload-area.drag-over {
  border-color: #42b883;
  background: rgba(66, 184, 131, 0.1);
}

.upload-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.upload-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}



.upload-content p {
  color: var(--color-text);
  margin: 0;
}

.file-input {
  display: none;
}

.browse-button {
  background: #42b883;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 1rem;
  transition: background-color 0.3s ease;
}

.browse-button:hover {
  background: #369870;
}

.upload-progress {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.progress-info h3 {
  color: var(--color-heading);
  margin: 0;
}

.progress-info p {
  color: var(--color-text);
  margin: 0;
}

.progress-bar {
  width: 100%;
  height: 8px;
  background: var(--color-background-mute);
  border-radius: 4px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: #42b883;
  transition: width 0.3s ease;
}

.cancel-button {
  background: #dc3545;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
}

.cancel-button:hover:not(:disabled) {
  background: #c82333;
}

.cancel-button:disabled {
  background: #6c757d;
  cursor: not-allowed;
}

.checking-spinner {
  display: flex;
  justify-content: center;
  margin: 1rem 0;
}

.spinner {
  width: 24px;
  height: 24px;
  border: 3px solid var(--color-background-mute);
  border-top: 3px solid #42b883;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.retry-button {
  background: #ffc107;
  color: #212529;
  border: none;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.8rem;
  margin-left: 0.5rem;
  transition: background-color 0.3s ease;
}

.retry-button:hover {
  background: #e0a800;
}

.upload-results {
  margin-top: 2rem;
  padding: 1.5rem;
  background: var(--color-background-soft);
  border-radius: 8px;
}

.upload-results h4 {
  color: var(--color-heading);
  margin: 0 0 1rem 0;
}

.result-item {
  padding: 1rem;
  margin-bottom: 0.5rem;
  border-radius: 6px;
  border-left: 4px solid;
}

.result-item.success {
  background: rgba(40, 167, 69, 0.1);
  border-left-color: #28a745;
}

.result-item.error {
  background: rgba(220, 53, 69, 0.1);
  border-left-color: #dc3545;
}

.result-info strong {
  display: block;
  margin-bottom: 0.25rem;
  color: var(--color-heading);
}

.success-message {
  color: #28a745;
  font-size: 0.9rem;
}

.success-details {
  color: #6c757d;
  font-size: 0.8rem;
  margin-left: 0.5rem;
}

.error-message {
  color: #dc3545;
  font-size: 0.9rem;
}
</style>