<template>
  <div class="frequency-view">
    <BreadcrumbNavigation :items="breadcrumbItems" />
    
    <div class="view-header">
      <h1>Frequency Analysis</h1>
      <p class="view-description">
        Analyze number frequency patterns and trends across historical lottery draws
      </p>
    </div>

    <div class="frequency-content">
      <div class="analysis-section">
        <FrequencyAnalysis />
      </div>
      
      <div class="visualization-section">
        <div class="chart-container">
          <FrequencyChart 
            v-if="frequencyData.length > 0"
            :data="frequencyData"
            :chart-type="selectedChartType"
            :interactive="true"
            :export-enabled="true"
          />
        </div>
        
        <div class="heatmap-container">
          <NumberHeatmap 
            v-if="frequencyData.length > 0"
            :frequency-data="frequencyData"
            :color-scheme="'hot-cold'"
            :show-labels="true"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import BreadcrumbNavigation from '@/components/BreadcrumbNavigation.vue'
import FrequencyAnalysis from '@/components/FrequencyAnalysis.vue'
import FrequencyChart from '@/components/FrequencyChart.vue'
import NumberHeatmap from '@/components/NumberHeatmap.vue'
import { useFrequencyStore } from '@/stores/frequency'
import type { BreadcrumbItem } from '@/types/navigation'

const route = useRoute()
const frequencyStore = useFrequencyStore()

const selectedChartType = ref<'bar' | 'line' | 'heatmap'>('bar')
const frequencyData = computed(() => frequencyStore.frequencyData)

const breadcrumbItems = computed<BreadcrumbItem[]>(() => [
  { label: 'Home', path: '/' },
  { label: 'Frequency Analysis', path: '/frequency', active: true }
])

onMounted(async () => {
  // Load initial frequency data if not already loaded
  if (frequencyData.value.length === 0) {
    await frequencyStore.loadFrequencyData()
  }
})
</script>

<style scoped>
.frequency-view {
  padding: 1rem;
  max-width: 1200px;
  margin: 0 auto;
}

.view-header {
  margin-bottom: 2rem;
}

.view-header h1 {
  color: var(--color-heading);
  margin-bottom: 0.5rem;
}

.view-description {
  color: var(--color-text);
  font-size: 1.1rem;
  margin: 0;
}

.frequency-content {
  display: grid;
  grid-template-columns: 1fr;
  gap: 2rem;
}

.analysis-section {
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: 1.5rem;
}

.visualization-section {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1.5rem;
}

.chart-container,
.heatmap-container {
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: 1.5rem;
}

@media (min-width: 768px) {
  .frequency-content {
    grid-template-columns: 1fr 1fr;
  }
  
  .visualization-section {
    grid-column: 1 / -1;
    grid-template-columns: 1fr 1fr;
  }
}

@media (min-width: 1024px) {
  .frequency-view {
    padding: 2rem;
  }
}
</style>