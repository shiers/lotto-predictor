import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { LottoDraw, NavigationContext } from '@/types/navigation'
import { navigationService } from '@/services/navigationService'

export const useNavigationStore = defineStore('navigation', () => {
  // State
  const currentDraw = ref<LottoDraw | null>(null)
  const navigationContext = ref<NavigationContext | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Getters
  const canNavigatePrevious = computed(() => 
    navigationContext.value?.hasPrevious ?? false
  )
  
  const canNavigateNext = computed(() => 
    navigationContext.value?.hasNext ?? false
  )
  
  const currentPosition = computed(() => 
    navigationContext.value?.currentPosition ?? 0
  )
  
  const totalDraws = computed(() => 
    navigationContext.value?.totalDraws ?? 0
  )

  // Actions
  const navigate = async (direction: 'previous' | 'next' | 'first' | 'last') => {
    if (!currentDraw.value && direction !== 'first' && direction !== 'last') {
      throw new Error('No current draw to navigate from')
    }

    isLoading.value = true
    error.value = null
    
    try {
      let newDraw: LottoDraw
      
      switch (direction) {
        case 'previous':
          newDraw = await navigationService.getPreviousDraw(currentDraw.value!.draw)
          break
        case 'next':
          newDraw = await navigationService.getNextDraw(currentDraw.value!.draw)
          break
        case 'first':
          newDraw = await navigationService.getFirstDraw()
          break
        case 'last':
          newDraw = await navigationService.getLatestDraw()
          break
        default:
          throw new Error(`Invalid navigation direction: ${direction}`)
      }
      
      currentDraw.value = newDraw
      await loadNavigationContext()
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Navigation failed'
    } finally {
      isLoading.value = false
    }
  }

  const jumpTo = async (target: { drawNumber?: number; date?: Date }) => {
    isLoading.value = true
    error.value = null
    
    try {
      let newDraw: LottoDraw
      
      if (target.drawNumber) {
        newDraw = await navigationService.getDrawByNumber(target.drawNumber)
      } else if (target.date) {
        newDraw = await navigationService.getDrawByDate(target.date)
      } else {
        throw new Error('Must specify either draw number or date')
      }
      
      currentDraw.value = newDraw
      await loadNavigationContext()
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Jump to draw failed'
    } finally {
      isLoading.value = false
    }
  }

  const loadNavigationContext = async () => {
    if (!currentDraw.value) return
    
    try {
      navigationContext.value = await navigationService.getNavigationContext(currentDraw.value.draw)
    } catch (err) {
      console.error('Failed to load navigation context:', err)
    }
  }

  const clearError = () => {
    error.value = null
  }

  return {
    // State
    currentDraw,
    navigationContext,
    isLoading,
    error,
    
    // Getters
    canNavigatePrevious,
    canNavigateNext,
    currentPosition,
    totalDraws,
    
    // Actions
    navigate,
    jumpTo,
    loadNavigationContext,
    clearError
  }
})