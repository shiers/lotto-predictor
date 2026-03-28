import api from './api'

export interface LottoDrawDto {
  draw: number
  date: string
  winningNumbers: number[]
  winningNumber1: number
  winningNumber2: number
  winningNumber3: number
  winningNumber4: number
  winningNumber5: number
  winningNumber6: number
  bonusNumber: number
  powerball: number
  division1Prize?: number
  division2Prize?: number
  division3Prize?: number
  division4Prize?: number
  division5Prize?: number
  division6Prize?: number
  division7Prize?: number
}

export interface NavigationContext {
  currentDraw: LottoDrawDto
  currentPosition: number
  totalDraws: number
  hasPrevious: boolean
  hasNext: boolean
  earliestDate: string
  latestDate: string
  missingDrawNumbers: number[]
}

export interface JumpToRequest {
  drawNumber?: number
  date?: string
  findClosest?: boolean
}

export interface NavigationRequest {
  drawNumber?: number
  date?: string
  direction: NavigationDirection
}

export enum NavigationDirection {
  Previous = 'Previous',
  Next = 'Next',
  First = 'First',
  Last = 'Last',
  Specific = 'Specific'
}

export interface BookmarkDto {
  id: number
  userId: string
  drawNumber: number
  label: string
  description?: string
  createdAt: string
  updatedAt?: string
  draw?: LottoDrawDto
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

class NavigationService {
  /**
   * Get a specific lottery draw by draw number
   */
  async getDrawByNumber(drawNumber: number): Promise<LottoDrawDto> {
    const response = await api.get(`/navigation/draw/${drawNumber}`)
    return response.data
  }

  /**
   * Get a lottery draw by date
   */
  async getDrawByDate(date: string): Promise<LottoDrawDto> {
    const response = await api.get(`/navigation/draw/by-date/${date}`)
    return response.data
  }

  /**
   * Get the previous draw relative to the current draw number
   */
  async getPreviousDraw(currentDrawNumber: number): Promise<LottoDrawDto> {
    const response = await api.get(`/navigation/draw/${currentDrawNumber}/previous`)
    return response.data
  }

  /**
   * Get the next draw relative to the current draw number
   */
  async getNextDraw(currentDrawNumber: number): Promise<LottoDrawDto> {
    const response = await api.get(`/navigation/draw/${currentDrawNumber}/next`)
    return response.data
  }

  /**
   * Get navigation context for a specific draw
   */
  async getNavigationContext(drawNumber: number): Promise<NavigationContext> {
    const response = await api.get(`/navigation/context/${drawNumber}`)
    return response.data
  }

  /**
   * Get draws within a date range
   */
  async getDrawsInRange(startDate: string, endDate: string): Promise<LottoDrawDto[]> {
    const response = await api.get('/navigation/draws/range', {
      params: { startDate, endDate }
    })
    return response.data
  }

  /**
   * Jump to a specific draw by number or date
   */
  async jumpToDraw(request: JumpToRequest): Promise<LottoDrawDto> {
    const response = await api.post('/navigation/jump-to', request)
    return response.data
  }

  /**
   * Navigate using a navigation request
   */
  async navigate(request: NavigationRequest): Promise<LottoDrawDto> {
    const response = await api.post('/navigation/navigate', request)
    return response.data
  }

  /**
   * Create a bookmark for a specific draw
   */
  async createBookmark(request: CreateBookmarkRequest): Promise<BookmarkDto> {
    const response = await api.post('/navigation/bookmarks', request)
    return response.data
  }

  /**
   * Get user's bookmarks
   */
  async getBookmarks(): Promise<BookmarkDto[]> {
    const response = await api.get('/navigation/bookmarks')
    return response.data
  }

  /**
   * Get a specific bookmark
   */
  async getBookmark(bookmarkId: number): Promise<BookmarkDto> {
    const response = await api.get(`/navigation/bookmarks/${bookmarkId}`)
    return response.data
  }

  /**
   * Update a bookmark
   */
  async updateBookmark(bookmarkId: number, request: UpdateBookmarkRequest): Promise<BookmarkDto> {
    const response = await api.put(`/navigation/bookmarks/${bookmarkId}`, request)
    return response.data
  }

  /**
   * Delete a bookmark
   */
  async deleteBookmark(bookmarkId: number): Promise<void> {
    await api.delete(`/navigation/bookmarks/${bookmarkId}`)
  }
}

export default new NavigationService()