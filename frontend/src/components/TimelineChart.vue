<template>
  <div class="timeline-chart">
    <div class="chart-header">
      <h3>{{ title }}</h3>
      <div class="chart-controls">
        <div class="time-scale-selector">
          <label>Time Scale:</label>
          <select v-model="timeScale" @change="updateTimeScale">
            <option value="daily">Daily</option>
            <option value="weekly">Weekly</option>
            <option value="monthly">Monthly</option>
          </select>
        </div>
        
        <div class="date-range-selector">
          <label>Date Range:</label>
          <input 
            type="date" 
            v-model="startDate" 
            @change="updateDateRange"
            class="date-input"
          >
          <span>to</span>
          <input 
            type="date" 
            v-model="endDate" 
            @change="updateDateRange"
            class="date-input"
          >
        </div>
        
        <button @click="exportChart" class="export-btn" :disabled="!canExport">
          Export
        </button>
      </div>
    </div>
    
    <div class="chart-container" ref="chartContainer">
      <canvas ref="chartCanvas"></canvas>
    </div>
    
    <div class="chart-legend" v-if="showLegend">
      <div class="legend-item" v-for="period in hotColdPeriods" :key="period.id">
        <span 
          class="legend-color" 
          :style="{ backgroundColor: period.color }"
        ></span>
        <span class="legend-label">{{ period.label }}</span>
      </div>
    </div>
    
    <div v-if="loading" class="loading-overlay">
      <div class="spinner"></div>
      <p>Loading timeline data...</p>
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
  TimeScale,
  LineElement,
  PointElement,
  Title,
  Tooltip,
  Legend,
  ChartConfiguration,
  ChartData,
  ChartOptions,
  ScatterController
} from 'chart.js'
import 'chartjs-adapter-date-fns'

// Register Chart.js components
Chart.register(
  CategoryScale,
  LinearScale,
  TimeScale,
  LineElement,
  PointElement,
  Title,
  Tooltip,
  Legend,
  ScatterController
)

interface TimelineDataPoint {
  date: string
  value: number
  movingAverage?: number
  isHotPeriod?: boolean
  isColdPeriod?: boolean
  annotation?: string
}

interface HotColdPeriod {
  id: string
  startDate: string
  endDate: string
  type: 'hot' | 'cold'
  label: string
  color: string
}

interface Props {
  data: TimelineDataPoint[]
  title?: string
  showMovingAverage?: boolean
  showHotColdPeriods?: boolean
  hotColdPeriods?: HotColdPeriod[]
  interactive?: boolean
  zoomEnabled?: boolean
  height?: number
  initialTimeScale?: 'daily' | 'weekly' | 'monthly'
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Historical Pattern Timeline',
  showMovingAverage: true,
  showHotColdPeriods: true,
  hotColdPeriods: () => [],
  interactive: true,
  zoomEnabled: true,
  height: 400,
  initialTimeScale: 'daily'
})

const emit = defineEmits<{
  dataPointClick: [dataPoint: TimelineDataPoint, index: number]
  dateRangeChange: [startDate: string, endDate: string]
  timeScaleChange: [scale: string]
  exportComplete: [format: string, url: string]
}>()

// Reactive state
const chartCanvas = ref<HTMLCanvasElement>()
const chartContainer = ref<HTMLDivElement>()
const timeScale = ref(props.initialTimeScale)
const startDate = ref('')
const endDate = ref('')
const loading = ref(false)
const error = ref('')
const chart = ref<Chart | null>(null)

// Computed properties
const canExport = computed(() => chart.value && !loading.value)
const showLegend = computed(() => props.showHotColdPeriods && props.hotColdPeriods.length > 0)

const processedData = computed(() => {
  if (!props.data || props.data.length === 0) return []
  
  let data = [...props.data]
  
  // Filter by date range if specified
  if (startDate.value && endDate.value) {
    const start = new Date(startDate.value)
    const end = new Date(endDate.value)
    data = data.filter(d => {
      const date = new Date(d.date)
      return date >= start && date <= end
    })
  }
  
  // Aggregate by time scale
  if (timeScale.value === 'weekly') {
    data = aggregateByWeek(data)
  } else if (timeScale.value === 'monthly') {
    data = aggregateByMonth(data)
  }
  
  return data.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())
})

const chartData = computed((): ChartData => {
  const data = processedData.value
  
  if (data.length === 0) {
    return {
      labels: [],
      datasets: []
    }
  }

  const datasets: any[] = [
    {
      label: 'Frequency',
      data: data.map(d => ({
        x: d.date,
        y: d.value
      })),
      borderColor: '#007bff',
      backgroundColor: 'rgba(0, 123, 255, 0.1)',
      borderWidth: 2,
      fill: true,
      tension: 0.4,
      pointRadius: 3,
      pointHoverRadius: 5,
      pointBackgroundColor: data.map(d => {
        if (d.isHotPeriod) return '#ff4444'
        if (d.isColdPeriod) return '#4444ff'
        return '#007bff'
      })
    }
  ]

  // Add moving average if enabled
  if (props.showMovingAverage && data.some(d => d.movingAverage !== undefined)) {
    datasets.push({
      label: 'Moving Average',
      data: data.map(d => ({
        x: d.date,
        y: d.movingAverage || 0
      })),
      borderColor: '#ff6b35',
      backgroundColor: 'transparent',
      borderWidth: 2,
      fill: false,
      tension: 0.4,
      pointRadius: 2,
      pointHoverRadius: 4,
      borderDash: [5, 5]
    })
  }

  return {
    datasets
  }
})

const chartOptions = computed((): ChartOptions => {
  return {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      intersect: false,
      mode: 'index'
    },
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
        display: true,
        position: 'top'
      },
      tooltip: {
        callbacks: {
          title: (context) => {
            const date = new Date(context[0].parsed.x)
            return date.toLocaleDateString()
          },
          label: (context) => {
            const dataIndex = context.dataIndex
            const dataPoint = processedData.value[dataIndex]
            
            const lines = [
              `${context.dataset.label}: ${context.parsed.y}`,
              `Date: ${new Date(context.parsed.x).toLocaleDateString()}`
            ]
            
            if (dataPoint?.annotation) {
              lines.push(`Note: ${dataPoint.annotation}`)
            }
            
            if (dataPoint?.isHotPeriod) lines.push('🔥 Hot period')
            if (dataPoint?.isColdPeriod) lines.push('❄️ Cold period')
            
            return lines
          }
        }
      }
    },
    scales: {
      x: {
        type: 'time',
        time: {
          unit: timeScale.value === 'daily' ? 'day' : 
                timeScale.value === 'weekly' ? 'week' : 'month',
          displayFormats: {
            day: 'MMM dd',
            week: 'MMM dd',
            month: 'MMM yyyy'
          }
        },
        title: {
          display: true,
          text: 'Date'
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
    },
    onClick: (event, elements) => {
      if (!props.interactive || elements.length === 0) return
      
      const element = elements[0]
      const index = element.index
      const dataPoint = processedData.value[index]
      
      if (dataPoint) {
        emit('dataPointClick', dataPoint, index)
      }
    }
  }
})

// Helper functions
function aggregateByWeek(data: TimelineDataPoint[]): TimelineDataPoint[] {
  const weekMap = new Map<string, TimelineDataPoint[]>()
  
  data.forEach(point => {
    const date = new Date(point.date)
    const weekStart = new Date(date.getFullYear(), date.getMonth(), date.getDate() - date.getDay())
    const weekKey = weekStart.toISOString().split('T')[0]
    
    if (!weekMap.has(weekKey)) {
      weekMap.set(weekKey, [])
    }
    weekMap.get(weekKey)!.push(point)
  })
  
  return Array.from(weekMap.entries()).map(([weekStart, points]) => ({
    date: weekStart,
    value: Math.round(points.reduce((sum, p) => sum + p.value, 0) / points.length),
    movingAverage: points.reduce((sum, p) => sum + (p.movingAverage || 0), 0) / points.length,
    isHotPeriod: points.some(p => p.isHotPeriod),
    isColdPeriod: points.some(p => p.isColdPeriod)
  }))
}

function aggregateByMonth(data: TimelineDataPoint[]): TimelineDataPoint[] {
  const monthMap = new Map<string, TimelineDataPoint[]>()
  
  data.forEach(point => {
    const date = new Date(point.date)
    const monthKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-01`
    
    if (!monthMap.has(monthKey)) {
      monthMap.set(monthKey, [])
    }
    monthMap.get(monthKey)!.push(point)
  })
  
  return Array.from(monthMap.entries()).map(([monthStart, points]) => ({
    date: monthStart,
    value: Math.round(points.reduce((sum, p) => sum + p.value, 0) / points.length),
    movingAverage: points.reduce((sum, p) => sum + (p.movingAverage || 0), 0) / points.length,
    isHotPeriod: points.some(p => p.isHotPeriod),
    isColdPeriod: points.some(p => p.isColdPeriod)
  }))
}

function createChart() {
  if (!chartCanvas.value) return

  destroyChart()

  const config: ChartConfiguration = {
    type: 'line',
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

  chart.value.data = chartData.value
  chart.value.options = chartOptions.value
  chart.value.update('active')
}

function updateTimeScale() {
  emit('timeScaleChange', timeScale.value)
  updateChart()
}

function updateDateRange() {
  if (startDate.value && endDate.value) {
    emit('dateRangeChange', startDate.value, endDate.value)
    updateChart()
  }
}

async function exportChart() {
  if (!chart.value || !canExport.value) return

  try {
    loading.value = true
    
    const url = chart.value.toBase64Image('image/png', 1.0)
    
    const link = document.createElement('a')
    link.download = `timeline-chart-${Date.now()}.png`
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

// Initialize date range from data
function initializeDateRange() {
  if (props.data && props.data.length > 0) {
    const dates = props.data.map(d => new Date(d.date)).sort((a, b) => a.getTime() - b.getTime())
    startDate.value = dates[0].toISOString().split('T')[0]
    endDate.value = dates[dates.length - 1].toISOString().split('T')[0]
  }
}

// Lifecycle hooks
onMounted(() => {
  initializeDateRange()
  nextTick(() => {
    createChart()
  })
})

onUnmounted(() => {
  destroyChart()
})

// Watchers
watch(() => props.data, () => {
  initializeDateRange()
  updateChart()
}, { deep: true })

// Expose methods for parent components
defineExpose({
  exportChart,
  updateChart,
  setDateRange: (start: string, end: string) => {
    startDate.value = start
    endDate.value = end
    updateDateRange()
  },
  getChartInstance: () => chart.value
})
</script>

<style scoped>
.timeline-chart {
  position: relative;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  padding: 20px;
}

.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 20px;
  flex-wrap: wrap;
  gap: 15px;
}

.chart-header h3 {
  margin: 0;
  color: #333;
  font-size: 1.2rem;
}

.chart-controls {
  display: flex;
  gap: 15px;
  align-items: center;
  flex-wrap: wrap;
}

.time-scale-selector,
.date-range-selector {
  display: flex;
  align-items: center;
  gap: 8px;
}

.time-scale-selector label,
.date-range-selector label {
  font-size: 14px;
  font-weight: 500;
  color: #555;
}

.time-scale-selector select,
.date-input {
  padding: 6px 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  background: white;
  font-size: 14px;
}

.date-range-selector span {
  font-size: 14px;
  color: #666;
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

.chart-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 15px;
  margin-top: 15px;
  padding-top: 15px;
  border-top: 1px solid #eee;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.legend-color {
  width: 16px;
  height: 16px;
  border-radius: 2px;
  border: 1px solid #ddd;
}

.legend-label {
  font-size: 14px;
  color: #555;
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
    align-items: stretch;
  }
  
  .chart-controls {
    flex-direction: column;
    align-items: stretch;
  }
  
  .time-scale-selector,
  .date-range-selector {
    justify-content: space-between;
  }
  
  .chart-container {
    height: 300px;
  }
  
  .chart-legend {
    justify-content: center;
  }
}
</style>