export interface LottoDraw {
  draw: number
  date: Date | string
  winningNumbers: number[]
  bonusNumber?: number
  powerball?: number
}

export interface NumberOccurrence {
  drawNumber: number
  drawDate: Date | string
  number: number
  position: number
  isBonus: boolean
  isPowerball: boolean
  fullCombination: number[]
}

export interface NumberFrequency {
  number: number
  totalOccurrences: number
  lastAppearance: Date | string
  firstAppearance: Date | string
  longestGap: number
  currentGap: number
  percentage: number
  isHot: boolean
  isCold: boolean
}

export interface RangeFrequency {
  range: NumberRange
  totalOccurrences: number
  percentage: number
  averagePerDraw: number
  individualNumbers: NumberFrequency[]
}

export interface NumberRange {
  startNumber: number
  endNumber: number
  label: string
}

export interface CombinationSearchResult {
  searchedCombination: number[]
  exactMatches: CombinationMatch[]
  partialMatches: CombinationMatch[]
  totalExactMatches: number
  totalPartialMatches: number
}

export interface CombinationMatch {
  drawNumber: number
  drawDate: Date | string
  winningCombination: number[]
  matchedNumbers: number[]
  matchCount: number
  isExactMatch: boolean
}

export interface FrequencyAnalysisRequest {
  ranges: NumberRange[]
  startDate?: Date
  endDate?: Date
  includeBonus?: boolean
  includePowerball?: boolean
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

export interface Bookmark {
  id: number
  userId: string
  drawNumber: number
  label: string
  description?: string
  createdAt: Date | string
  updatedAt?: Date | string
  draw?: LottoDraw
}