<template>
  <main class="lookup-view">
    <div class="view-header">
      <h1>Number Lookup & Search</h1>
      <p>Search for specific numbers and combinations in historical lottery data</p>
    </div>

    <div class="lookup-tabs">
      <button
        @click="activeTab = 'numbers'"
        :class="['tab-button', { active: activeTab === 'numbers' }]"
      >
        <span class="tab-icon">🔢</span>
        Number Lookup
      </button>
      <button
        @click="activeTab = 'combinations'"
        :class="['tab-button', { active: activeTab === 'combinations' }]"
      >
        <span class="tab-icon">🎯</span>
        Combination Search
      </button>
    </div>

    <div class="tab-content">
      <div v-if="activeTab === 'numbers'" class="tab-panel">
        <NumberLookup
          :initial-numbers="initialNumbers"
          :max-numbers="6"
          :show-frequency-data="true"
        />
      </div>
      
      <div v-if="activeTab === 'combinations'" class="tab-panel">
        <CombinationSearch
          :max-combination-size="6"
          :highlight-matches="true"
        />
      </div>
    </div>

    <!-- Quick Actions -->
    <div class="quick-actions">
      <div class="quick-actions-header">
        <h3>Quick Actions</h3>
        <p>Common searches and useful tools</p>
      </div>
      
      <div class="action-cards">
        <div class="action-card" @click="searchPopularNumbers">
          <div class="action-icon">🔥</div>
          <h4>Popular Numbers</h4>
          <p>Search the most frequently drawn numbers</p>
        </div>
        
        <div class="action-card" @click="searchRecentWinners">
          <div class="action-icon">🏆</div>
          <h4>Recent Winners</h4>
          <p>Check recent winning combinations</p>
        </div>
        
        <div class="action-card" @click="searchColdNumbers">
          <div class="action-icon">❄️</div>
          <h4>Cold Numbers</h4>
          <p>Find numbers that haven't appeared recently</p>
        </div>
        
        <div class="action-card" @click="searchMyNumbers">
          <div class="action-icon">⭐</div>
          <h4>My Numbers</h4>
          <p>Search your favorite number combinations</p>
        </div>
      </div>
    </div>

    <!-- Search Tips -->
    <div class="search-tips">
      <div class="tips-header">
        <h3>Search Tips</h3>
      </div>
      
      <div class="tips-grid">
        <div class="tip-card">
          <div class="tip-icon">💡</div>
          <h4>Number Format</h4>
          <p>Enter numbers separated by commas or spaces. Example: 1, 15, 23, 35</p>
        </div>
        
        <div class="tip-card">
          <div class="tip-icon">📅</div>
          <h4>Date Ranges</h4>
          <p>Use date filters to search within specific time periods for more targeted results</p>
        </div>
        
        <div class="tip-card">
          <div class="tip-icon">🎲</div>
          <h4>Partial Matches</h4>
          <p>Enable partial matches to find draws where some of your numbers appeared together</p>
        </div>
        
        <div class="tip-card">
          <div class="tip-icon">🔍</div>
          <h4>Search History</h4>
          <p>Your recent searches are saved automatically for quick access</p>
        </div>
      </div>
    </div>
  </main>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import NumberLookup from '@/components/NumberLookup.vue'
import CombinationSearch from '@/components/CombinationSearch.vue'
import { useAppStore } from '@/stores/app'

// Router
const route = useRoute()
const router = useRouter()

// Store
const appStore = useAppStore()

// Reactive state
const activeTab = ref<'numbers' | 'combinations'>('combinations')
const initialNumbers = ref<number[]>([])

// Initialize from route params
onMounted(() => {
  // Check if there are initial numbers from route query
  if (route.query.numbers) {
    try {
      const numbers = String(route.query.numbers).split(',').map(n => parseInt(n.trim(), 10))
      if (numbers.every(n => !isNaN(n) && n >= 1 && n <= 40)) {
        initialNumbers.value = numbers
        activeTab.value = numbers.length > 1 ? 'combinations' : 'numbers'
      }
    } catch (error) {
      console.warn('Invalid numbers in route query:', error)
    }
  }
  
  // Check if tab is specified in route
  if (route.query.tab === 'combinations') {
    activeTab.value = 'combinations'
  }
})

// Quick action methods
const searchPopularNumbers = () => {
  // Most popular numbers based on historical data
  const popularNumbers = [1, 2, 3, 4, 5, 6] // This would come from API in real implementation
  initialNumbers.value = popularNumbers
  activeTab.value = 'numbers'
  
  // Update route to reflect the search
  router.push({
    query: {
      ...route.query,
      numbers: popularNumbers.join(','),
      tab: 'numbers'
    }
  })
  
  appStore.addNotification({
    type: 'info',
    title: 'Popular Numbers Loaded',
    message: 'Searching for the most frequently drawn numbers'
  })
}

const searchRecentWinners = () => {
  // This would fetch recent winning combinations from API
  const recentWinners = [7, 14, 21, 28, 35, 42] // Example data
  initialNumbers.value = recentWinners
  activeTab.value = 'combinations'
  
  router.push({
    query: {
      ...route.query,
      numbers: recentWinners.join(','),
      tab: 'combinations'
    }
  })
  
  appStore.addNotification({
    type: 'info',
    title: 'Recent Winners Loaded',
    message: 'Searching for recent winning combinations'
  })
}

const searchColdNumbers = () => {
  // Numbers that haven't appeared recently
  const coldNumbers = [13, 17, 19, 23, 29, 31] // Example data
  initialNumbers.value = coldNumbers
  activeTab.value = 'numbers'
  
  router.push({
    query: {
      ...route.query,
      numbers: coldNumbers.join(','),
      tab: 'numbers'
    }
  })
  
  appStore.addNotification({
    type: 'info',
    title: 'Cold Numbers Loaded',
    message: 'Searching for numbers that haven\'t appeared recently'
  })
}

const searchMyNumbers = () => {
  // This would load user's saved favorite numbers
  appStore.addNotification({
    type: 'info',
    title: 'My Numbers',
    message: 'Feature coming soon! Save your favorite numbers for quick access.'
  })
}
</script>

<style scoped>
.lookup-view {
  max-width: 1400px;
  margin: 0 auto;
  padding: 2rem;
  min-height: 100vh;
}

.view-header {
  text-align: center;
  margin-bottom: 3rem;
}

.view-header h1 {
  font-size: 2.5rem;
  color: var(--color-heading);
  margin-bottom: 1rem;
}

.view-header p {
  font-size: 1.2rem;
  color: var(--color-text);
  max-width: 600px;
  margin: 0 auto;
}

.lookup-tabs {
  display: flex;
  justify-content: center;
  gap: 1rem;
  margin-bottom: 2rem;
}

.tab-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem 2rem;
  border: 1px solid var(--color-border);
  background: var(--color-background);
  color: var(--color-text);
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s ease;
}

.tab-button:hover {
  background: var(--color-background-soft);
  border-color: var(--color-border-hover);
}

.tab-button.active {
  background: var(--color-border-hover);
  color: white;
  border-color: var(--color-border-hover);
}

.tab-icon {
  font-size: 1.2rem;
}

.tab-content {
  margin-bottom: 4rem;
}

.tab-panel {
  animation: fadeIn 0.3s ease-in-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.quick-actions {
  margin-bottom: 4rem;
}

.quick-actions-header {
  text-align: center;
  margin-bottom: 2rem;
}

.quick-actions-header h3 {
  color: var(--color-heading);
  margin-bottom: 0.5rem;
}

.quick-actions-header p {
  color: var(--color-text);
}

.action-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
}

.action-card {
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  padding: 2rem;
  text-align: center;
  cursor: pointer;
  transition: all 0.3s ease;
}

.action-card:hover {
  background: var(--color-background);
  border-color: var(--color-border-hover);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.action-icon {
  font-size: 2.5rem;
  margin-bottom: 1rem;
}

.action-card h4 {
  color: var(--color-heading);
  margin-bottom: 0.5rem;
}

.action-card p {
  color: var(--color-text);
  font-size: 0.9rem;
  line-height: 1.4;
}

.search-tips {
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  padding: 2rem;
}

.tips-header {
  text-align: center;
  margin-bottom: 2rem;
}

.tips-header h3 {
  color: var(--color-heading);
}

.tips-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
}

.tip-card {
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: 1.5rem;
  text-align: center;
}

.tip-icon {
  font-size: 2rem;
  margin-bottom: 1rem;
}

.tip-card h4 {
  color: var(--color-heading);
  margin-bottom: 0.5rem;
  font-size: 1rem;
}

.tip-card p {
  color: var(--color-text);
  font-size: 0.875rem;
  line-height: 1.4;
}

/* Responsive */
@media (max-width: 768px) {
  .lookup-view {
    padding: 1rem;
  }
  
  .view-header h1 {
    font-size: 2rem;
  }
  
  .view-header p {
    font-size: 1rem;
  }
  
  .lookup-tabs {
    flex-direction: column;
    align-items: center;
  }
  
  .tab-button {
    width: 100%;
    max-width: 300px;
    justify-content: center;
  }
  
  .action-cards {
    grid-template-columns: 1fr;
  }
  
  .tips-grid {
    grid-template-columns: 1fr;
  }
  
  .action-card,
  .tip-card {
    padding: 1.5rem;
  }
}

@media (max-width: 480px) {
  .view-header h1 {
    font-size: 1.75rem;
  }
  
  .action-card,
  .tip-card {
    padding: 1rem;
  }
  
  .action-icon,
  .tip-icon {
    font-size: 2rem;
  }
}
</style>