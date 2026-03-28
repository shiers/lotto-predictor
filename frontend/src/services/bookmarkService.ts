import api from './api'

export interface BookmarkDto {
  id: number
  userId: string
  drawNumber: number
  label: string
  description: string
  createdAt: string
  updatedAt?: string
  drawDate: string
  winningNumbers: number[]
  draw?: {
    draw: number
    date: string
    winningNumber1: number
    winningNumber2: number
    winningNumber3: number
    winningNumber4: number
    winningNumber5: number
    winningNumber6: number
    bonusNumber?: number
    powerballNumber?: number
  }
}

export interface CreateBookmarkRequest {
  drawNumber: number
  label: string
  description?: string
}

export interface UpdateBookmarkRequest {
  label: string
  description?: string
}

export class BookmarkService {
  private readonly baseUrl = '/bookmarks'

  async createBookmark(request: CreateBookmarkRequest): Promise<BookmarkDto> {
    const response = await api.post<BookmarkDto>(this.baseUrl, request)
    return response.data
  }

  async getUserBookmarks(): Promise<BookmarkDto[]> {
    const response = await api.get<BookmarkDto[]>(this.baseUrl)
    return response.data
  }

  async updateBookmark(bookmarkId: number, request: UpdateBookmarkRequest): Promise<BookmarkDto> {
    const response = await api.put<BookmarkDto>(`${this.baseUrl}/${bookmarkId}`, request)
    return response.data
  }

  async deleteBookmark(bookmarkId: number): Promise<boolean> {
    try {
      await api.delete(`${this.baseUrl}/${bookmarkId}`)
      return true
    } catch (error) {
      console.error('Error deleting bookmark:', error)
      return false
    }
  }

  async bookmarkExists(drawNumber: number): Promise<boolean> {
    try {
      const response = await api.get<boolean>(`${this.baseUrl}/exists/${drawNumber}`)
      return response.data
    } catch (error) {
      console.error('Error checking bookmark existence:', error)
      return false
    }
  }

  async reorderBookmarks(bookmarkIds: number[]): Promise<boolean> {
    try {
      await api.post(`${this.baseUrl}/reorder`, { bookmarkIds })
      return true
    } catch (error) {
      console.error('Error reordering bookmarks:', error)
      return false
    }
  }
}

export const bookmarkService = new BookmarkService()