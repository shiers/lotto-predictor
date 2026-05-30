<template>
  <main>
    <div class="backtest">
      <h1>📈 Backtest Dashboard</h1>
      <p>Compare prediction strategies against historical draws</p>

      <!-- Controls -->
      <div class="controls-section">
        <div class="control-row">
          <div class="control-group">
            <label>Draws to test:</label>
            <select v-model.number="drawCount">
              <option :value="20">20 draws</option>
              <option :value="50">50 draws</option>
              <option :value="100">100 draws</option>
            </select>
          </div>
          <div class="control-group">
            <label>Lines per draw:</label>
            <select v-model.number="linesPerDraw">
              <option :value="4">4 lines (standard ticket)</option>
              <option :value="6">6 lines</option>
              <option :value="8">8 lines</option>
            </select>
          </div>
          <button @click="runComparison" :disabled="isLoading" class="generate-button primary">
            <span v-if="isLoading">Running... (this may take a moment)</span>
            <span v-else>Run Comparison</span>
          </button>
        </div>
      </div>

      <!-- Results -->
      <div v-if="results" class="results-section">
        <h3>Results: {{ results.enhanced.strategy }} vs {{ results.random.strategy }}</h3>
        <p class="results-subtitle">{{ drawCount }} draws × {{ linesPerDraw }} lines = {{ drawCount * linesPerDraw }} total lines tested</p>

        <!-- Comparison Table -->
        <div class="comparison-table">
          <table>
            <thead>
              <tr>
                <th>Metric</th>
                <th class="enhanced-col">Enhanced</th>
                <th class="random-col">Random</th>
                <th>Winner</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Avg matches/line</td>
                <td class="enhanced-col">{{ results.enhanced.matchSummary.averageMatchesPerLine.toFixed(3) }}</td>
                <td class="random-col">{{ results.random.matchSummary.averageMatchesPerLine.toFixed(3) }}</td>
                <td>{{ getWinner(results.enhanced.matchSummary.averageMatchesPerLine, results.random.matchSummary.averageMatchesPerLine) }}</td>
              </tr>
              <tr>
                <td>Winning lines</td>
                <td class="enhanced-col">{{ results.enhanced.financialSummary.winningLines }}</td>
                <td class="random-col">{{ results.random.financialSummary.winningLines }}</td>
                <td>{{ getWinner(results.enhanced.financialSummary.winningLines, results.random.financialSummary.winningLines) }}</td>
              </tr>
              <tr>
                <td>Total winnings</td>
                <td class="enhanced-col">${{ results.enhanced.financialSummary.totalWinnings.toFixed(2) }}</td>
                <td class="random-col">${{ results.random.financialSummary.totalWinnings.toFixed(2) }}</td>
                <td>{{ getWinner(results.enhanced.financialSummary.totalWinnings, results.random.financialSummary.totalWinnings) }}</td>
              </tr>
              <tr>
                <td>ROI</td>
                <td class="enhanced-col">{{ results.enhanced.financialSummary.returnOnInvestment.toFixed(1) }}%</td>
                <td class="random-col">{{ results.random.financialSummary.returnOnInvestment.toFixed(1) }}%</td>
                <td>{{ getWinner(results.enhanced.financialSummary.returnOnInvestment, results.random.financialSummary.returnOnInvestment) }}</td>
              </tr>
              <tr>
                <td>Unique coverage</td>
                <td class="enhanced-col">{{ (results.enhanced.coverageStats.averageUniqueCoverage * 100).toFixed(1) }}%</td>
                <td class="random-col">{{ (results.random.coverageStats.averageUniqueCoverage * 100).toFixed(1) }}%</td>
                <td>{{ getWinner(results.enhanced.coverageStats.averageUniqueCoverage, results.random.coverageStats.averageUniqueCoverage) }}</td>
              </tr>
              <tr>
                <td>PB spread</td>
                <td class="enhanced-col">{{ (results.enhanced.coverageStats.averagePowerballSpread * 100).toFixed(1) }}%</td>
                <td class="random-col">{{ (results.random.coverageStats.averagePowerballSpread * 100).toFixed(1) }}%</td>
                <td>{{ getWinner(results.enhanced.coverageStats.averagePowerballSpread, results.random.coverageStats.averagePowerballSpread) }}</td>
              </tr>
              <tr>
                <td>Sum in range</td>
                <td class="enhanced-col">{{ (results.enhanced.coverageStats.averageSumInRange * 100).toFixed(1) }}%</td>
                <td class="random-col">{{ (results.random.coverageStats.averageSumInRange * 100).toFixed(1) }}%</td>
                <td>{{ getWinner(results.enhanced.coverageStats.averageSumInRange, results.random.coverageStats.averageSumInRange) }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Match Distribution -->
        <div class="distribution-section">
          <h4>Match Distribution</h4>
          <div class="distribution-grid">
            <div class="dist-column">
              <h5>Enhanced</h5>
              <div class="dist-bars">
                <div v-for="(count, label) in getMatchDist(results.enhanced.matchSummary)" :key="label" class="dist-row">
                  <span class="dist-label">{{ label }}</span>
                  <div class="dist-bar-container">
                    <div class="dist-bar enhanced-bar" :style="{ width: getBarWidth(count, drawCount * linesPerDraw) }"></div>
                  </div>
                  <span class="dist-count">{{ count }}</span>
                </div>
              </div>
            </div>
            <div class="dist-column">
              <h5>Random</h5>
              <div class="dist-bars">
                <div v-for="(count, label) in getMatchDist(results.random.matchSummary)" :key="label" class="dist-row">
                  <span class="dist-label">{{ label }}</span>
                  <div class="dist-bar-container">
                    <div class="dist-bar random-bar" :style="{ width: getBarWidth(count, drawCount * linesPerDraw) }"></div>
                  </div>
                  <span class="dist-count">{{ count }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="!isLoading" class="empty-state">
        <p>Configure parameters above and click "Run Comparison" to see how the enhanced predictor performs against random selection.</p>
      </div>
    </div>
  </main>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import api from '@/services/api'

const drawCount = ref(50)
const linesPerDraw = ref(4)
const isLoading = ref(false)
const results = ref<any>(null)

const runComparison = async () => {
  isLoading.value = true
  try {
    const res = await api.get('/backtest/compare', {
      params: { drawCount: drawCount.value, linesPerDraw: linesPerDraw.value },
      timeout: 120000
    })
    results.value = res.data
  } catch (err: any) {
    alert('Backtest failed: ' + (err.response?.data || err.message))
  } finally {
    isLoading.value = false
  }
}

const getWinner = (enhanced: number, random: number): string => {
  if (enhanced > random) return '✅ Enhanced'
  if (random > enhanced) return '🎲 Random'
  return '🤝 Tie'
}

const getMatchDist = (summary: any) => ({
  '0 match': summary.totalMatch0,
  '1 match': summary.totalMatch1,
  '2 match': summary.totalMatch2,
  '3 match': summary.totalMatch3,
  '3+bonus': summary.totalMatch3PlusBonus,
  '4 match': summary.totalMatch4,
  '4+bonus': summary.totalMatch4PlusBonus,
  '5+': summary.totalMatch5 + summary.totalMatch5PlusBonus + summary.totalMatch6
})

const getBarWidth = (count: number, total: number): string => {
  return Math.max(2, (count / total) * 100) + '%'
}
</script>

<style scoped>
.backtest {
  max-width: 900px;
  margin: 0 auto;
  padding: 2rem 1rem;
}

.controls-section {
  margin-top: 1.5rem;
  padding: 1.5rem;
  background: var(--color-background-soft);
  border-radius: 12px;
  border: 1px solid var(--color-border);
}

.control-row { display: flex; gap: 1rem; align-items: flex-end; flex-wrap: wrap; }
.control-group { display: flex; flex-direction: column; gap: 0.25rem; }
.control-group select { padding: 0.5rem; border: 1px solid var(--color-border); border-radius: 6px; background: var(--color-background); color: var(--color-text); }

.results-section {
  margin-top: 2rem;
  padding: 1.5rem;
  background: var(--color-background-soft);
  border-radius: 12px;
  border: 1px solid var(--color-border);
}

.results-subtitle { font-size: 0.85rem; color: var(--color-text); opacity: 0.7; margin-top: 0.25rem; }

.comparison-table { margin-top: 1.5rem; overflow-x: auto; }
.comparison-table table { width: 100%; border-collapse: collapse; }
.comparison-table th, .comparison-table td { padding: 0.6rem 1rem; text-align: left; border-bottom: 1px solid var(--color-border); }
.comparison-table th { background: var(--color-background); font-weight: 600; font-size: 0.85rem; }
.enhanced-col { color: #42b883; font-weight: 500; }
.random-col { color: #6c757d; }

.distribution-section { margin-top: 2rem; }
.distribution-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; margin-top: 1rem; }
.dist-column h5 { margin-bottom: 0.5rem; }
.dist-row { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.3rem; }
.dist-label { width: 4.5rem; font-size: 0.8rem; }
.dist-bar-container { flex: 1; height: 1rem; background: var(--color-background); border-radius: 4px; overflow: hidden; }
.dist-bar { height: 100%; border-radius: 4px; transition: width 0.5s ease; }
.enhanced-bar { background: #42b883; }
.random-bar { background: #6c757d; }
.dist-count { width: 2rem; text-align: right; font-size: 0.8rem; font-weight: 500; }

.empty-state { text-align: center; padding: 3rem; color: var(--color-text); opacity: 0.6; }

.generate-button { padding: 0.75rem 1.5rem; border: none; border-radius: 8px; cursor: pointer; font-size: 0.9rem; font-weight: 500; transition: all 0.2s; }
.generate-button.primary { background: #42b883; color: white; }
.generate-button.primary:hover { background: #38a373; }
.generate-button:disabled { opacity: 0.5; cursor: not-allowed; }

@media (max-width: 768px) {
  .control-row { flex-direction: column; align-items: stretch; }
  .distribution-grid { grid-template-columns: 1fr; }
}
</style>
