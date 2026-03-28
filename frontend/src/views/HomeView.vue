<template>
  <main>
    <div class="home">
      <div class="hero-section">
        <h1>PredictLottoNZ</h1>
        <p>Upload historical lottery data and generate predictions using multiple algorithms.</p>
      </div>
      
      <div class="features">
        <div class="feature-card">
          <h3>Upload Data</h3>
          <p>Import CSV files with historical lottery draws</p>
        </div>
        
        <div class="feature-card">
          <h3>Generate Predictions</h3>
          <p>Use frequency analysis and ML models for predictions</p>
        </div>
        
        <div class="feature-card">
          <h3>View Results</h3>
          <p>See predictions with source information and scores</p>
        </div>
      </div>
      

      
      <section class="latest-section">
        <LatestDraw ref="latestDrawRef" @drawLoaded="handleDrawLoaded" />
      </section>
    </div>
  </main>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'

import LatestDraw from '@/components/LatestDraw.vue'
import { useAppStore } from '@/stores/app'

const route = useRoute()
const appStore = useAppStore()
const latestDrawRef = ref()



// Handle draw loaded
const handleDrawLoaded = (draw: any) => {
  console.log('Draw loaded:', draw)
}

// Handle navigation to specific draw from query parameter
const handleDrawNavigation = () => {
  const drawNumber = route.query.draw
  if (drawNumber && latestDrawRef.value) {
    const drawNum = parseInt(drawNumber as string)
    if (!isNaN(drawNum)) {
      // Scroll to the latest draw section
      setTimeout(() => {
        const latestElement = document.getElementById('latest')
        if (latestElement) {
          latestElement.scrollIntoView({ behavior: 'smooth' })
        }
        // Navigate to the specific draw
        latestDrawRef.value.goToDraw(drawNum)
      }, 100)
    }
  }
}

// Watch for route changes
watch(() => route.query.draw, handleDrawNavigation)

// Handle navigation on mount
onMounted(() => {
  handleDrawNavigation()
})
</script>

<style scoped>
.home {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.hero-section {
  text-align: center;
  margin-bottom: 4rem;
}

.hero-section h1 {
  font-size: 3rem;
  margin-bottom: 1rem;
  color: var(--color-heading);
}

.hero-section p {
  font-size: 1.25rem;
  color: var(--color-text);
  max-width: 600px;
  margin: 0 auto;
}

.features {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 2rem;
  margin-bottom: 4rem;
}

.feature-card {
  padding: 2rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background-soft);
  text-align: center;
}

.feature-card h3 {
  margin-bottom: 1rem;
  color: var(--color-heading);
}

.feature-card p {
  color: var(--color-text);
  line-height: 1.5;
}



.latest-section {
  margin-bottom: 2rem;
}

/* Responsive */
@media (max-width: 768px) {
  .home {
    padding: 1rem;
  }
  
  .hero-section h1 {
    font-size: 2rem;
  }
  
  .hero-section p {
    font-size: 1rem;
  }
  
  .features {
    grid-template-columns: 1fr;
    gap: 1rem;
  }
  
  .feature-card {
    padding: 1.5rem;
  }
}
</style>