import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { NumberFrequency, RangeFrequency, FrequencyAnalysisRequest } from '@/types/lottery'
import { frequencyService } from '@/services/frequencyService'

export const useFrequencyStore = defineStore('frequency', () => {
  // State
  const frequencyData = ref<NumberFrequency[]>([])
  const rangeFrequencies = ref<RangeFrequency[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Getters
  const hotNumbers = computed(() => 
    frequencyData.value.filter(f => f.isHot).slice(0, 10)
  )
  
  const coldNumbers = computed(() => 
    frequencyData.value.filter(f => f.isCold).slice(0, 10)
  )

  // Actions
  const loadFrequencyData = async () => {
    isLoading.value = true
    error.value = null
    
    try {
      frequencyData.value = await frequencyService.getNumberFrequencies()
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load frequency data'
    } finally {
      isLoading.value = false
    }
  }

  const analyzeRanges = async (request: FrequencyAnalysisRequest) => {
    isLoading.value = true
    error.value = null
    
    try {
      rangeFrequencies.value = await frequencyService.getRangeFrequencies(request.ranges)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to analyze ranges'
    } finally {
      isLoading.value = false
    }
  }

  const clearError = () => {
    error.value = null
  }

  return {
    // State
    frequencyData,
    rangeFrequencies,
    isLoading,
    error,
    
    // Getters
    hotNumbers,
    coldNumbers,
    
    // Actions
    loadFrequencyData,
    analyzeRanges,
    clearError
  }
})