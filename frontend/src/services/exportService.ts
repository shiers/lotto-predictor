import api from './api'

export enum ExportFormat {
  CSV = 'CSV',
  JSON = 'JSON',
  PDF = 'PDF',
  Excel = 'Excel'
}

export interface ExportResult {
  exportId: string
  downloadUrl: string
  expiresAt: string
  fileSizeBytes: number
}

export interface ExportStatus {
  exportId: string
  status: string
  progressPercentage: number
  errorMessage?: string
  createdAt: string
  completedAt?: string
}

export interface LookupExportRequest {
  searchCriteria: {
    numbers: number[]
    startDate?: string
    endDate?: string
    includeBonus?: boolean
    includePowerball?: boolean
  }
  format: ExportFormat
  includeMetadata?: boolean
  fileName?: string
}

export interface FrequencyExportRequest {
  searchCriteria: {
    ranges?: Array<{
      startNumber: number
      endNumber: number
      label: string
    }>
    startDate?: string
    endDate?: string
    includeBonus?: boolean
    includePowerball?: boolean
  }
  format: ExportFormat
  includeCharts?: boolean
  fileName?: string
}

export interface NavigationExportRequest {
  startDate?: string
  endDate?: string
  format: ExportFormat
  includeMetadata?: boolean
  fileName?: string
  userId: string
}

export class ExportService {
  private readonly baseUrl = '/exports'

  async exportLookupResults(request: LookupExportRequest): Promise<ExportResult> {
    const response = await api.post<ExportResult>(`${this.baseUrl}/lookup`, request)
    return response.data
  }

  async exportFrequencyData(request: FrequencyExportRequest): Promise<ExportResult> {
    const response = await api.post<ExportResult>(`${this.baseUrl}/frequency`, request)
    return response.data
  }

  async exportNavigationHistory(request: NavigationExportRequest): Promise<ExportResult> {
    const response = await api.post<ExportResult>(`${this.baseUrl}/navigation`, request)
    return response.data
  }

  async getExportStatus(exportId: string): Promise<ExportStatus> {
    const response = await api.get<ExportStatus>(`${this.baseUrl}/${exportId}/status`)
    return response.data
  }

  async downloadExport(exportId: string): Promise<Blob> {
    const response = await api.get(`${this.baseUrl}/${exportId}/download`, {
      responseType: 'blob'
    })
    return response.data
  }

  async deleteExport(exportId: string): Promise<boolean> {
    try {
      await api.delete(`${this.baseUrl}/${exportId}`)
      return true
    } catch (error) {
      console.error('Error deleting export:', error)
      return false
    }
  }

  async getUserExports(): Promise<ExportStatus[]> {
    const response = await api.get<ExportStatus[]>(`${this.baseUrl}/user`)
    return response.data
  }

  // Helper method to trigger file download
  downloadFile(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = fileName
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  }

  // Helper method to get file extension from format
  getFileExtension(format: ExportFormat): string {
    switch (format) {
      case ExportFormat.CSV:
        return 'csv'
      case ExportFormat.JSON:
        return 'json'
      case ExportFormat.PDF:
        return 'pdf'
      case ExportFormat.Excel:
        return 'xlsx'
      default:
        return 'txt'
    }
  }

  // Helper method to generate default filename
  generateFileName(type: string, format: ExportFormat): string {
    const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, '-')
    const extension = this.getFileExtension(format)
    return `${type}-export-${timestamp}.${extension}`
  }
}

export const exportService = new ExportService()