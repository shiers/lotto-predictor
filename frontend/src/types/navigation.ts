export interface BreadcrumbItem {
  label: string
  path?: string
  active?: boolean
}

export interface NavigationContext {
  currentDraw?: LottoDraw
  currentPosition: number
  totalDraws: number
  hasPrevious: boolean
  hasNext: boolean
  earliestDate: Date
  latestDate: Date
  missingDrawNumbers?: number[]
}

export interface LottoDraw {
  draw: number
  date: Date | string
  winningNumbers: number[]
  bonusNumber?: number
  powerball?: number
}