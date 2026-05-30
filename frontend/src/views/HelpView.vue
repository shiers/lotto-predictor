<template>
  <main>
    <div class="help">
      <h1>❓ Help Guide</h1>
      <p>How to use LottoPredictor NZ</p>

      <div class="help-section">
        <h2>Overview</h2>
        <p>LottoPredictor is an AI-powered tool that analyses historical NZ Lotto Powerball data to generate statistically-informed number predictions. It uses gap analysis, sum constraints, pair co-occurrence, and multi-line coverage optimisation to produce coordinated ticket lines.</p>
        <div class="disclaimer">
          <strong>⚠️ Disclaimer:</strong> NZ Lotto draws are mechanically random (air-mix machines, audited). No system can guarantee wins. This tool optimises number selection based on statistical patterns, but each draw is an independent event.
        </div>
      </div>

      <div class="help-section">
        <h2>🎯 Predictions</h2>
        <p>Generate AI-powered number predictions for your next ticket.</p>
        <ul>
          <li><strong>Generate New</strong> — Creates predictions using the GroqCloud LLM with full statistical context (number gaps, sum ranges, pair analysis)</li>
          <li><strong>Multi-line optimisation</strong> — When generating 4+ lines, the system spreads numbers across lines to maximise coverage while allowing strategic overlap for overdue numbers</li>
          <li><strong>Confidence scores</strong> — Each prediction includes a confidence score (0-1) based on statistical evidence</li>
          <li><strong>Reasoning</strong> — Each line explains why those numbers were chosen</li>
        </ul>
        <div class="tip">
          💡 <strong>Tip:</strong> Generate 4 lines for a standard ticket, or 10 lines for maximum coverage.
        </div>
      </div>

      <div class="help-section">
        <h2>🎫 Ticket Tracker</h2>
        <p>Track your purchased tickets and measure performance over time.</p>
        <ul>
          <li><strong>Add tickets</strong> — Enter your draw number and line numbers. The system auto-checks against draw results when available.</li>
          <li><strong>Number highlighting</strong> — Blue circles = matched main numbers, Purple = bonus match, Gold = Powerball match</li>
          <li><strong>Source badges</strong> — 🤖 Purple border = AI Predicted numbers, ✋ Green border = manually chosen numbers</li>
          <li><strong>Performance summary</strong> — Tracks total spent, won, net return, ROI, and average matches per line</li>
          <li><strong>Override winnings</strong> — If the calculated prize differs from your actual payout, the system supports manual correction</li>
        </ul>
      </div>

      <div class="help-section">
        <h2>📈 Backtest</h2>
        <p>Compare the enhanced prediction strategy against random selection using historical data.</p>
        <ul>
          <li><strong>Enhanced strategy</strong> — Uses gap analysis, sum constraints (P10-P90 range), odd/even balance, and coverage optimisation</li>
          <li><strong>Random baseline</strong> — Pure random number generation for comparison</li>
          <li><strong>Metrics tracked</strong> — Average matches/line, winning lines, total winnings, ROI, unique coverage, powerball spread</li>
          <li><strong>Match distribution</strong> — Visual bar chart showing how many lines hit 0, 1, 2, 3, 4+ matches</li>
        </ul>
        <div class="tip">
          💡 <strong>Tip:</strong> Run with 100 draws for statistically meaningful results. The enhanced strategy shows +8-9% more matches per line on average.
        </div>
      </div>

      <div class="help-section">
        <h2>🎲 Draws</h2>
        <p>Browse historical NZ Lotto Powerball draw results. The database contains 1500+ draws from 2008 to present.</p>
      </div>

      <div class="help-section">
        <h2>📁 Upload Data</h2>
        <p>Import draw data from CSV files (Powerball_NZ.csv format). This updates the historical database that feeds the prediction engine.</p>
        <p>Latest CSVs can be downloaded from: <a href="https://en.lottolyzer.com/history/new-zealand/powerball-nz/page/1/per-page/50/summary-view" target="_blank" rel="noopener">Lottolyzer - Powerball NZ History</a> (click the CSV icon in the toolbar).</p>
      </div>

      <div class="help-section">
        <h2>🔍 Number Lookup</h2>
        <p>Search for specific numbers to see their frequency, last appearance, average gap, and co-occurrence with other numbers.</p>
      </div>

      <div class="help-section">
        <h2>How the Predictor Works</h2>
        <div class="how-it-works">
          <div class="step">
            <span class="step-num">1</span>
            <div>
              <strong>Statistical Analysis</strong>
              <p>Computes number gaps (draws since last seen), sum distributions, pair co-occurrence, odd/even patterns, and powerball frequencies from all historical draws.</p>
            </div>
          </div>
          <div class="step">
            <span class="step-num">2</span>
            <div>
              <strong>LLM Prompt Construction</strong>
              <p>Builds a detailed prompt with overdue numbers, sum constraints, distribution patterns, and multi-line optimisation rules.</p>
            </div>
          </div>
          <div class="step">
            <span class="step-num">3</span>
            <div>
              <strong>AI Generation</strong>
              <p>GroqCloud (Llama 3.3 70B) generates predictions following the statistical constraints and coverage rules.</p>
            </div>
          </div>
          <div class="step">
            <span class="step-num">4</span>
            <div>
              <strong>Validation</strong>
              <p>Each prediction is validated: sum must be within P10-P90 range, numbers must be unique and in valid ranges, coverage is logged.</p>
            </div>
          </div>
        </div>
      </div>

      <div class="help-section">
        <h2>NZ Lotto Prize Divisions</h2>
        <table class="prize-table">
          <thead>
            <tr><th>Division</th><th>Match</th><th>Typical Prize</th></tr>
          </thead>
          <tbody>
            <tr><td>Div 1</td><td>6 numbers</td><td>$200K - $1M+</td></tr>
            <tr><td>Div 2</td><td>5 + bonus</td><td>$15,000 - $47,000</td></tr>
            <tr><td>Div 3</td><td>5 numbers</td><td>$600 - $1,900</td></tr>
            <tr><td>Div 4</td><td>4 + bonus</td><td>$99 - $117</td></tr>
            <tr><td>Div 5</td><td>4 numbers</td><td>$49 - $61</td></tr>
            <tr><td>Div 6</td><td>3 + bonus</td><td>$33 - $40</td></tr>
            <tr><td>Div 7</td><td>3 numbers</td><td>$16.50</td></tr>
          </tbody>
        </table>
        <p class="note">Powerball multiplies prizes when matched. Actual prizes vary per draw based on ticket sales and winner count.</p>
      </div>
    </div>
  </main>
</template>

<script setup lang="ts">
</script>

<style scoped>
.help {
  max-width: 800px;
  margin: 0 auto;
  padding: 2rem 1rem;
}

.help-section {
  margin-top: 2rem;
  padding: 1.5rem;
  background: var(--color-background-soft);
  border-radius: 12px;
  border: 1px solid var(--color-border);
}

.help-section h2 {
  margin: 0 0 0.75rem;
  color: var(--color-heading);
  font-size: 1.2rem;
}

.help-section p {
  margin: 0.5rem 0;
  line-height: 1.6;
}

.help-section ul {
  margin: 0.75rem 0;
  padding-left: 1.25rem;
}

.help-section li {
  margin-bottom: 0.5rem;
  line-height: 1.5;
}

.disclaimer {
  margin-top: 1rem;
  padding: 0.75rem 1rem;
  background: rgba(255, 193, 7, 0.1);
  border: 1px solid rgba(255, 193, 7, 0.3);
  border-radius: 8px;
  font-size: 0.9rem;
  line-height: 1.5;
}

.tip {
  margin-top: 0.75rem;
  padding: 0.5rem 0.75rem;
  background: rgba(66, 184, 131, 0.1);
  border: 1px solid rgba(66, 184, 131, 0.3);
  border-radius: 6px;
  font-size: 0.85rem;
}

.how-it-works {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-top: 1rem;
}

.step {
  display: flex;
  gap: 1rem;
  align-items: flex-start;
}

.step-num {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 2rem;
  height: 2rem;
  border-radius: 50%;
  background: #42b883;
  color: white;
  font-weight: bold;
  font-size: 0.9rem;
}

.step p {
  margin: 0.25rem 0 0;
  font-size: 0.9rem;
  opacity: 0.85;
}

.prize-table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 0.75rem;
}

.prize-table th, .prize-table td {
  padding: 0.5rem 0.75rem;
  text-align: left;
  border-bottom: 1px solid var(--color-border);
}

.prize-table th {
  font-weight: 600;
  font-size: 0.85rem;
  background: var(--color-background);
}

.prize-table td {
  font-size: 0.9rem;
}

.note {
  font-size: 0.8rem;
  opacity: 0.7;
  margin-top: 0.5rem;
}

@media (max-width: 768px) {
  .help-section { padding: 1rem; }
}
</style>
