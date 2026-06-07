<template>
  <main>
    <div class="tickets">
      <h1>🎫 Ticket Tracker</h1>
      <p>Track your purchased tickets and see how they performed</p>

      <!-- Add Ticket Form -->
      <div class="add-ticket-section">
        <h3>Add a Ticket</h3>
        <div class="ticket-form">
          <div class="form-row">
            <div class="form-group">
              <label>Draw Number:</label>
              <input type="number" v-model.number="newTicket.drawNumber" placeholder="e.g. 2589" />
            </div>
            <div class="form-group">
              <label>Ticket Number (optional):</label>
              <input type="text" v-model="newTicket.ticketNumber" placeholder="e.g. 5400033586693300" />
            </div>
            <div class="form-group">
              <label>Cost ($):</label>
              <input type="number" v-model.number="newTicket.cost" step="0.50" />
            </div>
          </div>

          <div class="lines-section">
            <div class="lines-header">
              <h4>Lines</h4>
              <button @click="addLine" :disabled="newTicket.lines.length >= 10" class="btn-small">+ Add Line</button>
            </div>
            <div v-for="(line, idx) in newTicket.lines" :key="idx" class="line-row">
              <span class="line-label">{{ String.fromCharCode(65 + idx) }}</span>
              <input v-for="n in 6" :key="n" type="number" v-model.number="line.numbers[n-1]"
                     :placeholder="'#' + n" min="1" max="40" class="number-input" />
              <span class="pb-label">PB:</span>
              <input type="number" v-model.number="line.powerball" placeholder="0=none" min="0" max="10" class="pb-input" />
              <button @click="removeLine(idx)" class="btn-remove" v-if="newTicket.lines.length > 1">×</button>
            </div>
          </div>

          <button @click="submitTicket" :disabled="isSubmitting" class="generate-button primary">
            <span v-if="isSubmitting">Adding...</span>
            <span v-else>Add Ticket</span>
          </button>
        </div>
      </div>

      <!-- Summary -->
      <div v-if="summary" class="summary-section">
        <h3>📊 Performance Summary</h3>
        <div class="summary-grid">
          <div class="stat-card">
            <div class="stat-value">{{ summary.totalTickets }}</div>
            <div class="stat-label">Tickets Tracked</div>
          </div>
          <div class="stat-card">
            <div class="stat-value">${{ summary.totalCost?.toFixed(2) }}</div>
            <div class="stat-label">Total Spent</div>
          </div>
          <div class="stat-card">
            <div class="stat-value">${{ summary.totalWinnings?.toFixed(2) }}</div>
            <div class="stat-label">Total Won</div>
          </div>
          <div class="stat-card" :class="{ positive: summary.netReturn > 0, negative: summary.netReturn < 0 }">
            <div class="stat-value">${{ summary.netReturn?.toFixed(2) }}</div>
            <div class="stat-label">Net Return</div>
          </div>
          <div class="stat-card">
            <div class="stat-value">{{ summary.roi?.toFixed(1) }}%</div>
            <div class="stat-label">ROI</div>
          </div>
          <div class="stat-card">
            <div class="stat-value">{{ summary.averageMatchesPerLine?.toFixed(2) }}</div>
            <div class="stat-label">Avg Matches/Line</div>
          </div>
        </div>
      </div>

      <!-- Ticket List -->
      <div v-if="tickets.length > 0" class="tickets-list">
        <h3>Your Tickets</h3>
        <div v-for="ticket in tickets" :key="ticket.id" class="ticket-card" :class="{ 'ai-ticket': ticket.source === 'AI Predicted', 'manual-ticket': ticket.source === 'Manual' }">
          <div class="ticket-header">
            <span class="draw-badge">Draw #{{ ticket.drawNumber }}</span>
            <span class="source-badge" :class="{ 'source-ai': ticket.source === 'AI Predicted', 'source-manual': ticket.source === 'Manual' }">
              {{ ticket.source === 'AI Predicted' ? '🤖 AI Predicted' : '✋ Manual' }}
            </span>
            <span class="ticket-date">{{ formatDate(ticket.drawDate) }}</span>
            <span v-if="ticket.winningNumbers" class="winning-numbers">
              Winning: <span v-for="n in ticket.winningNumbers" :key="n" class="win-num">{{ n }}</span>
              <span class="bonus-num">+{{ ticket.bonusNumber }}</span>
              <span class="pb-num">PB{{ ticket.drawPowerball }}</span>
            </span>
            <span class="ticket-winnings" :class="{ won: ticket.winnings > 0 }">
              {{ ticket.winnings > 0 ? `Won $${ticket.winnings.toFixed(2)}` : 'No win' }}
            </span>
          </div>
          <div class="ticket-lines">
            <div v-for="line in ticket.lines" :key="line.id" class="ticket-line" :class="{ 'line-won': line.prize > 0 }">
              <span class="line-label-display">{{ line.lineLabel }}</span>
              <span v-for="num in [line.number1, line.number2, line.number3, line.number4, line.number5, line.number6]"
                    :key="num" class="number-ball"
                    :class="{
                      'matched-main': ticket.winningNumbers && ticket.winningNumbers.includes(num),
                      'matched-bonus': ticket.bonusNumber === num && !(ticket.winningNumbers && ticket.winningNumbers.includes(num))
                    }">
                {{ num }}
              </span>
              <span class="powerball-ball" :class="{ 'matched-pb': line.powerballMatched }" v-if="line.powerball > 0">{{ line.powerball }}</span>
              <span class="powerball-na" v-else>-</span>
              <span v-if="line.division && line.division !== 'None'" class="division-badge">{{ line.division }} (${{ line.prize }})</span>
              <span v-if="line.mainMatches !== null" class="match-count">{{ line.mainMatches }}/6</span>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="!isLoading" class="empty-state">
        <p>No tickets tracked yet. Add your first ticket above!</p>
      </div>
    </div>
  </main>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '@/services/api'

interface TicketLine {
  numbers: number[]
  powerball: number
}

const newTicket = ref({
  drawNumber: 0,
  ticketNumber: '',
  cost: 6.00,
  lines: [{ numbers: [0, 0, 0, 0, 0, 0], powerball: 0 }] as TicketLine[]
})

const tickets = ref<any[]>([])
const summary = ref<any>(null)
const isLoading = ref(false)
const isSubmitting = ref(false)

const addLine = () => {
  if (newTicket.value.lines.length < 10) {
    newTicket.value.lines.push({ numbers: [0, 0, 0, 0, 0, 0], powerball: 0 })
  }
}

const removeLine = (idx: number) => {
  newTicket.value.lines.splice(idx, 1)
}

const submitTicket = async () => {
  isSubmitting.value = true
  try {
    const payload = {
      drawNumber: newTicket.value.drawNumber,
      ticketNumber: newTicket.value.ticketNumber || undefined,
      cost: newTicket.value.cost,
      source: 'Manual',
      lines: newTicket.value.lines.map(l => ({
        numbers: l.numbers.filter(n => n > 0),
        powerball: l.powerball
      }))
    }
    await api.post('/ticket', payload)
    await loadTickets()
    await loadSummary()
    // Reset form
    newTicket.value = { drawNumber: 0, ticketNumber: '', cost: 6.00, lines: [{ numbers: [0,0,0,0,0,0], powerball: 0 }] }
  } catch (err: any) {
    alert(err.response?.data || 'Failed to add ticket')
  } finally {
    isSubmitting.value = false
  }
}

const loadTickets = async () => {
  isLoading.value = true
  try {
    const res = await api.get('/ticket')
    tickets.value = res.data
  } catch (err) {
    console.error('Failed to load tickets', err)
  } finally {
    isLoading.value = false
  }
}

const loadSummary = async () => {
  try {
    const res = await api.get('/ticket/summary')
    summary.value = res.data
  } catch (err) {
    // No summary yet
  }
}

const formatDate = (date: string) => {
  if (!date) return ''
  return new Date(date).toLocaleDateString('en-NZ', { day: 'numeric', month: 'short', year: 'numeric' })
}

onMounted(() => {
  loadTickets()
  loadSummary()
})
</script>

<style scoped>
.tickets {
  max-width: 900px;
  margin: 0 auto;
  padding: 2rem 1rem;
}

.add-ticket-section, .summary-section, .tickets-list {
  margin-top: 2rem;
  padding: 1.5rem;
  background: var(--color-background-soft);
  border-radius: 12px;
  border: 1px solid var(--color-border);
}

.ticket-form { display: flex; flex-direction: column; gap: 1rem; }
.form-row { display: flex; gap: 1rem; flex-wrap: wrap; }
.form-group { display: flex; flex-direction: column; gap: 0.25rem; }
.form-group input { padding: 0.5rem; border: 1px solid var(--color-border); border-radius: 6px; background: var(--color-background); color: var(--color-text); }

.lines-section { margin-top: 1rem; }
.lines-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem; }
.line-row { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.5rem; flex-wrap: wrap; }
.line-label { font-weight: bold; width: 1.5rem; }
.number-input { width: 3rem; padding: 0.4rem; text-align: center; border: 1px solid var(--color-border); border-radius: 4px; background: var(--color-background); color: var(--color-text); }
.pb-label { font-size: 0.8rem; color: var(--color-text); opacity: 0.7; }
.pb-input { width: 3rem; padding: 0.4rem; text-align: center; border: 1px solid #f0ad4e; border-radius: 4px; background: var(--color-background); color: var(--color-text); }
.btn-small { padding: 0.3rem 0.75rem; border: 1px solid var(--color-border); border-radius: 4px; background: var(--color-background); color: var(--color-text); cursor: pointer; }
.btn-remove { background: none; border: none; color: #dc3545; font-size: 1.2rem; cursor: pointer; padding: 0 0.5rem; }

.summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(130px, 1fr)); gap: 1rem; margin-top: 1rem; }
.stat-card { text-align: center; padding: 1rem; background: var(--color-background); border-radius: 8px; border: 1px solid var(--color-border); }
.stat-value { font-size: 1.5rem; font-weight: bold; color: var(--color-heading); }
.stat-label { font-size: 0.8rem; color: var(--color-text); opacity: 0.7; margin-top: 0.25rem; }
.stat-card.positive .stat-value { color: #28a745; }
.stat-card.negative .stat-value { color: #dc3545; }

.ticket-card { background: var(--color-background); border: 1px solid var(--color-border); border-radius: 8px; padding: 1rem; margin-bottom: 1rem; }
.ticket-card.ai-ticket { border-left: 3px solid #9C27B0; }
.ticket-card.manual-ticket { border-left: 3px solid #42b883; }
.ticket-header { display: flex; align-items: center; gap: 1rem; margin-bottom: 0.75rem; flex-wrap: wrap; }
.draw-badge { background: var(--color-background-soft); padding: 0.25rem 0.75rem; border-radius: 12px; font-weight: bold; font-size: 0.85rem; }
.source-badge { padding: 0.2rem 0.6rem; border-radius: 10px; font-size: 0.75rem; font-weight: 500; }
.source-ai { background: rgba(156, 39, 176, 0.15); color: #CE93D8; border: 1px solid rgba(156, 39, 176, 0.3); }
.source-manual { background: rgba(66, 184, 131, 0.15); color: #42b883; border: 1px solid rgba(66, 184, 131, 0.3); }
.ticket-date { font-size: 0.85rem; color: var(--color-text); opacity: 0.7; }
.ticket-winnings { margin-left: auto; font-weight: bold; }
.ticket-winnings.won { color: #28a745; }
.winning-numbers { font-size: 0.8rem; display: flex; align-items: center; gap: 0.2rem; }
.win-num { display: inline-flex; align-items: center; justify-content: center; width: 1.5rem; height: 1.5rem; border-radius: 50%; background: #2196F3; color: white; font-size: 0.7rem; font-weight: 500; }
.bonus-num { display: inline-flex; align-items: center; justify-content: center; min-width: 1.5rem; height: 1.5rem; border-radius: 50%; background: #9C27B0; color: white; font-size: 0.65rem; font-weight: 500; padding: 0 0.2rem; }
.pb-num { display: inline-flex; align-items: center; justify-content: center; min-width: 1.5rem; height: 1.5rem; border-radius: 10px; background: #f0ad4e; color: white; font-size: 0.65rem; font-weight: 500; padding: 0 0.3rem; }

.ticket-lines { display: flex; flex-direction: column; gap: 0.5rem; }
.ticket-line { display: flex; align-items: center; gap: 0.4rem; padding: 0.4rem 0.5rem; border-radius: 6px; }
.ticket-line.line-won { background: rgba(40, 167, 69, 0.1); border: 1px solid rgba(40, 167, 69, 0.3); }
.line-label-display { font-weight: bold; width: 1.5rem; font-size: 0.85rem; }

.number-ball { display: inline-flex; align-items: center; justify-content: center; width: 2rem; height: 2rem; border-radius: 50%; background: var(--color-background-soft); border: 1px solid var(--color-border); font-size: 0.8rem; font-weight: 500; }
.number-ball.matched-main { background: #2196F3; color: white; border-color: #1976D2; }
.number-ball.matched-bonus { background: #9C27B0; color: white; border-color: #7B1FA2; }
.powerball-ball { display: inline-flex; align-items: center; justify-content: center; width: 2rem; height: 2rem; border-radius: 50%; background: #fff3cd; border: 1px solid #f0ad4e; font-size: 0.8rem; font-weight: 500; color: #856404; margin-left: 0.5rem; }
.powerball-ball.matched-pb { background: #f0ad4e; color: white; border-color: #e09900; }
.powerball-na { display: inline-flex; align-items: center; justify-content: center; width: 2rem; height: 2rem; font-size: 0.75rem; color: var(--color-text); opacity: 0.4; margin-left: 0.5rem; }
.division-badge { background: #28a745; color: white; padding: 0.15rem 0.5rem; border-radius: 10px; font-size: 0.75rem; margin-left: 0.5rem; }
.match-count { font-size: 0.75rem; color: var(--color-text); opacity: 0.6; margin-left: auto; }

.empty-state { text-align: center; padding: 2rem; color: var(--color-text); opacity: 0.6; }

.generate-button { padding: 0.75rem 1.5rem; border: none; border-radius: 8px; cursor: pointer; font-size: 0.9rem; font-weight: 500; transition: all 0.2s; }
.generate-button.primary { background: #42b883; color: white; }
.generate-button.primary:hover { background: #38a373; }
.generate-button:disabled { opacity: 0.5; cursor: not-allowed; }

@media (max-width: 768px) {
  .form-row { flex-direction: column; }
  .summary-grid { grid-template-columns: repeat(2, 1fr); }
}
</style>
