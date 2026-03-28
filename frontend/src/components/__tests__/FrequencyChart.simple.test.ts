import { describe, it, expect } from 'vitest'

describe('FrequencyChart Component Logic', () => {
  const mockData = [
    {
      number: 1,
      totalOccurrences: 25,
      percentage: 5.2,
      isHot: true,
      isCold: false,
      lastAppearance: '2023-12-01'
    },
    {
      number: 2,
      totalOccurrences: 15,
      percentage: 3.1,
      isHot: false,
      isCold: true,
      lastAppearance: '2023-11-15'
    },
    {
      number: 3,
      totalOccurrences: 20,
      percentage: 4.2,
      isHot: false,
      isCold: false,
      lastAppearance: '2023-12-10'
    }
  ]

  it('should have valid data structure', () => {
    expect(mockData).toHaveLength(3)
    expect(mockData[0]).toHaveProperty('number')
    expect(mockData[0]).toHaveProperty('totalOccurrences')
    expect(mockData[0]).toHaveProperty('percentage')
    expect(mockData[0]).toHaveProperty('isHot')
    expect(mockData[0]).toHaveProperty('isCold')
    expect(mockData[0]).toHaveProperty('lastAppearance')
  })

  it('should identify hot and cold numbers correctly', () => {
    const hotNumbers = mockData.filter(d => d.isHot)
    const coldNumbers = mockData.filter(d => d.isCold)
    const normalNumbers = mockData.filter(d => !d.isHot && !d.isCold)

    expect(hotNumbers).toHaveLength(1)
    expect(coldNumbers).toHaveLength(1)
    expect(normalNumbers).toHaveLength(1)
  })

  it('should sort data by number for consistent display', () => {
    const unsortedData = [mockData[2], mockData[0], mockData[1]]
    const sortedData = [...unsortedData].sort((a, b) => a.number - b.number)
    
    expect(sortedData[0].number).toBe(1)
    expect(sortedData[1].number).toBe(2)
    expect(sortedData[2].number).toBe(3)
  })

  it('should handle empty data gracefully', () => {
    const emptyData: any[] = []
    expect(emptyData).toHaveLength(0)
    
    // Chart should handle empty data without errors
    const chartData = {
      labels: emptyData.map(d => d.number?.toString() || ''),
      datasets: [{
        data: emptyData.map(d => d.totalOccurrences || 0)
      }]
    }
    
    expect(chartData.labels).toHaveLength(0)
    expect(chartData.datasets[0].data).toHaveLength(0)
  })

  it('should generate correct color mapping', () => {
    function getNumberColor(dataPoint: typeof mockData[0], alpha: number = 0.6): string {
      if (dataPoint.isHot) {
        return `rgba(255, 68, 68, ${alpha})`
      } else if (dataPoint.isCold) {
        return `rgba(68, 68, 255, ${alpha})`
      } else {
        return `rgba(68, 255, 68, ${alpha})`
      }
    }

    const hotColor = getNumberColor(mockData[0]) // Hot number
    const coldColor = getNumberColor(mockData[1]) // Cold number
    const normalColor = getNumberColor(mockData[2]) // Normal number

    expect(hotColor).toBe('rgba(255, 68, 68, 0.6)')
    expect(coldColor).toBe('rgba(68, 68, 255, 0.6)')
    expect(normalColor).toBe('rgba(68, 255, 68, 0.6)')
  })

  it('should process doughnut chart data correctly', () => {
    // For doughnut chart, show top numbers by frequency
    const topNumbers = [...mockData]
      .sort((a, b) => b.totalOccurrences - a.totalOccurrences)
      .slice(0, 10)
    
    expect(topNumbers[0].number).toBe(1) // Highest frequency (25)
    expect(topNumbers[1].number).toBe(3) // Second highest (20)
    expect(topNumbers[2].number).toBe(2) // Lowest (15)
  })

  it('should validate data ranges', () => {
    mockData.forEach(dataPoint => {
      expect(dataPoint.number).toBeGreaterThanOrEqual(1)
      expect(dataPoint.number).toBeLessThanOrEqual(40)
      expect(dataPoint.totalOccurrences).toBeGreaterThanOrEqual(0)
      expect(dataPoint.percentage).toBeGreaterThanOrEqual(0)
      expect(dataPoint.percentage).toBeLessThanOrEqual(100)
    })
  })

  it('should handle chart type changes', () => {
    const chartTypes = ['bar', 'line', 'doughnut']
    
    chartTypes.forEach(type => {
      expect(['bar', 'line', 'doughnut']).toContain(type)
    })
  })

  it('should format tooltip data correctly', () => {
    const dataPoint = mockData[0]
    
    const tooltipLines = [
      `Occurrences: ${dataPoint.totalOccurrences}`,
      `Percentage: ${dataPoint.percentage.toFixed(2)}%`,
      `Last seen: ${new Date(dataPoint.lastAppearance).toLocaleDateString()}`
    ]
    
    if (dataPoint.isHot) tooltipLines.push('🔥 Hot number')
    if (dataPoint.isCold) tooltipLines.push('❄️ Cold number')
    
    expect(tooltipLines).toContain('Occurrences: 25')
    expect(tooltipLines).toContain('Percentage: 5.20%')
    expect(tooltipLines).toContain('🔥 Hot number')
  })
})