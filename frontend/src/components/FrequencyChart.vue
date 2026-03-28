<template>
  <div class="frequency-chart">
    <div class="chart-header">
      <h3>{{ title }}</h3>
      <div class="chart-controls">
        <select v-model="chartType" @change="updateChart" class="chart-type-selector">
          <option value="bar">Bar Chart</option>
          <option value="line">Line Chart</option>
          <option value="doughnut">Doughnut Chart</option>
        </select>
        <button @click="exportChart" class="export-btn" :disabled="!canExport">
          Export
        </button>
      </div>
    </div>
    
    <div class="chart-container" ref="chartContainer">
      <canvas ref="chartCanvas"></canvas>
    </div>
    
    <div v-if="loading" class="loading-overlay">
      <div class="spinner"></div>
      <p>Loading chart data...</p>
    </div>
    
    <div v-if="error" class="error-message">
      <p>{{ error }}</p>
      <button @click="retryLoad" class="retry-btn">Retry</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, computed, nextTick } from 'vue'
import {
  Chart,
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  ChartConfiguration,
  ChartData,
  ChartOptions
} from 'chart.js'

// Register Chart.js components
Chart.register(
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  ArcElement,
  Title,
  Tooltip,
  Legend
)

interface FrequencyDataPoint {
  number: number
  totalOccurrences: number
  percentage: number
  isHot: boolean
  isCold: boolean
  lastAppearance: string
}

interface Props {
  data: FrequencyDataPoint[]
  title?: string
  interactive?: boolean
  exportEnabled?: boolean
  height?: number
  width?: number
  initialChartType?: 'bar' | 'line' | 'doughnut'
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Frequency Analysis',
  interactive: true,
  exportEnabled: true,
  height: 400,
  width: 800,
  initialChartType: 'bar'
})

const emit = defineEmits<{
  chartClick: [dataPoint: FrequencyDataPoint, index: number]
  chartHover: [dataPoint: FrequencyDataPoint | null, index: number]
  exportComplete: [format: string, url: string]
}>()

// Reactive state
const chartCanvas = ref<HTMLCanvasElement>()
const chartContainer = ref<HTMLDivElement>()
const chartType = ref(props.initialChartType)
const loading = ref(false)
const error = ref('')
const chart = ref<Chart | null>(null)

// Computed properties
const canExport = computed(() => props.exportEnabled && chart.value && !loading.value)

const chartData = computed((): ChartData => {
  if (!props.data || props.data.length === 0) {
    return {
      labels: [],
      datasets: []
    }
  }

  const sortedData = [...props.data].sort((a, b) => a.number - b.number)
  const labels = sortedData.map(d => d.number.toString())
  
  if (chartType.value === 'doughnut') {
    // For doughnut chart, show top 10 most frequent numbers
    const topNumbers = [...sortedData]
      .sort((a, b) => b.totalOccurrences - a.totalOccurrences)
      .slice(0, 10)
    
    return {
      labels: topNumbers.map(d => `Number ${d.number}`),
      datasets: [{
        label: 'Occurrences',
        data: topNumbers.map(d => d.totalOccurrences),
        backgroundColor: topNumbers.map(d => getNumberColor(d)),
        borderColor: topNumbers.map(d => getNumberColor(d, 0.8)),
        borderWidth: 2
      }]
    }
  }

  return {
    labels,
    datasets: [{
      label: 'Total Occurrences',
      data: sortedData.map(d => d.totalOccurrences),
      backgroundColor: sortedData.map(d => getNumberColor(d)),
      borderColor: sortedData.map(d => getNumberColor(d, 0.8)),
      borderWidth: chartType.value === 'line' ? 3 : 1,
      fill: chartType.value === 'line' ? false : true,
      tension: chartType.value === 'line' ? 0.4 : 0,
      pointRadius: chartType.value === 'line' ? 4 : 0,
      pointHoverRadius: chartType.value === 'line' ? 6 : 0
    }]
  }
})

const chartOptions = computed((): ChartOptions => {
  const baseOptions: ChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      title: {
        display: true,
        text: props.title,
        font: {
          size: 16,
          weight: 'bold'
        }
      },
      legend: {
        display: chartType.value !== 'doughnut',
        position: 'top'
      },
      tooltip: {
        callbacks: {
          title: (context) => {
            const index = context[0].dataIndex
            const dataPoint = props.data[index]
            return `Number ${dataPoint?.number || context[0].label}`
          },
          label: (context) => {
            const index = context.dataIndex
            const dataPoint = props.data[index]
            if (!dataPoint) return context.formattedValue
            
            const lines = [
              `Occurrences: ${dataPoint.totalOccurrences}`,
              `Percentage: ${dataPoint.percentage.toFixed(2)}%`,
              `Last seen: ${new Date(dataPoint.lastAppearance).toLocaleDateString()}`
            ]
            
            if (dataPoint.isHot) lines.push('🔥 Hot number')
            if (dataPoint.isCold) lines.push('❄️ Cold number')
            
            return lines
          }
        }
      }
    },
    onClick: (event, elements) => {
      if (!props.interactive || elements.length === 0) return
      
      const element = elements[0]
      const index = element.index
      const dataPoint = props.data[index]
      
      if (dataPoint) {
        emit('chartClick', dataPoint, index)
      }
    },
    onHover: (event, elements) => {
      if (!props.interactive) return
      
      if (elements.length > 0) {
        const element = elements[0]
        const index = element.index
        const dataPoint = props.data[index]
        emit('chartHover', dataPoint, index)
      } else {
        emit('chartHover', null, -1)
      }
    }
  }

  if (chartType.value === 'doughnut') {
    return {
      ...baseOptions,
      plugins: {
        ...baseOptions.plugins,
        legend: {
          display: true,
          position: 'right'
        }
      }
    }
  }

  return {
    ...baseOptions,
    scales: {
      x: {
        title: {
          display: true,
          text: 'Lottery Numbers'
        },
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.1)'
        }
      },
      y: {
        title: {
          display: true,
          text: 'Frequency'
        },
        beginAtZero: true,
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.1)'
        }
      }
    }
  }
})

// Helper functions
function getNumberColor(dataPoint: FrequencyDataPoint, alpha: number = 0.6): string {
  if (dataPoint.isHot) {
    return `rgba(255, 68, 68, ${alpha})` // Red for hot numbers
  } else if (dataPoint.isCold) {
    return `rgba(68, 68, 255, ${alpha})` // Blue for cold numbers
  } else {
    return `rgba(68, 255, 68, ${alpha})` // Green for normal numbers
  }
}

function createChart() {
  if (!chartCanvas.value) return

  destroyChart()

  const config: ChartConfiguration = {
    type: chartType.value,
    data: chartData.value,
    options: chartOptions.value
  }

  chart.value = new Chart(chartCanvas.value, config)
}

function destroyChart() {
  if (chart.value) {
    chart.value.destroy()
    chart.value = null
  }
}

function updateChart() {
  if (!chart.value) {
    createChart()
    return
  }

  // Update chart type if needed
  if (chart.value.config.type !== chartType.value) {
    createChart()
    return
  }

  // Update data and options
  chart.value.data = chartData.value
  chart.value.options = chartOptions.value
  chart.value.update('active')
}

async function exportChart() {
  if (!chart.value || !canExport.value) return

  try {
    loading.value = true
    
    // Export as PNG by default
    const url = chart.value.toBase64Image('image/png', 1.0)
    
    // Create download link
    const link = document.createElement('a')
    link.download = `frequency-chart-${Date.now()}.png`
    link.href = url
    link.click()
    
    emit('exportComplete', 'png', url)
  } catch (err) {
    error.value = 'Failed to export chart'
    console.error('Chart export error:', err)
  } finally {
    loading.value = false
  }
}

function retryLoad() {
  error.value = ''
  nextTick(() => {
    createChart()
  })
}

// Lifecycle hooks
onMounted(() => {
  nextTick(() => {
    createChart()
  })
})

onUnmounted(() => {
  destroyChart()
})

// Watchers
watch(() => props.data, () => {
  updateChart()
}, { deep: true })

watch(chartType, () => {
  updateChart()
})

// Expose methods for parent components
defineExpose({
  exportChart,
  updateChart,
  getChartInstance: () => chart.value
})
</script>

<style scoped>
.frequency-chart {
  position: relative;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  padding: 20px;
}

.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.chart-header h3 {
  margin: 0;
  color: #333;
  font-size: 1.2rem;
}

.chart-controls {
  display: flex;
  gap: 10px;
  align-items: center;
}

.chart-type-selector {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  background: white;
  font-size: 14px;
}

.export-btn {
  padding: 8px 16px;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.2s;
}

.export-btn:hover:not(:disabled) {
  background: #0056b3;
}

.export-btn:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.chart-container {
  position: relative;
  height: 400px;
  width: 100%;
}

.loading-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(255, 255, 255, 0.9);
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  z-index: 10;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #007bff;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 10px;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.error-message {
  text-align: center;
  color: #dc3545;
  padding: 20px;
}

.retry-btn {
  padding: 8px 16px;
  background: #dc3545;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  margin-top: 10px;
}

.retry-btn:hover {
  background: #c82333;
}

@media (max-width: 768px) {
  .chart-header {
    flex-direction: column;
    gap: 10px;
    align-items: stretch;
  }
  
  .chart-controls {
    justify-content: center;
  }
  
  .chart-container {
    height: 300px;
  }
}
</style>