<template>
  <div class="navigation-view">
    <BreadcrumbNavigation :items="breadcrumbItems" />
    
    <div class="view-header">
      <h1>Draw Navigation</h1>
      <p class="view-description">
        Browse through historical lottery draws with intuitive navigation controls
      </p>
    </div>

    <div class="navigation-content">
      <div class="navigation-controls">
        <DrawNavigation 
          :current-draw="currentDraw"
          :show-position="true"
          :enable-jump-to="true"
          @navigate="handleNavigation"
          @jump-to="handleJumpTo"
        />
      </div>
      
      <div class="navigation-context">
        <NavigationContext 
          v-if="navigationContext"
          :context="navigationContext"
        />
      </div>
      
      <div class="draw-details" v-if="currentDraw">
        <div class="draw-card">
          <h3>Draw #{{ currentDraw.draw }}</h3>
          <p class="draw-date">{{ formatDate(currentDraw.date) }}</p>
          
          <div class="winning-numbers">
            <div class="main-numbers">
              <span 
                v-for="number in currentDraw.winningNumbers" 
                :key="number"
                class="number-ball"
              >
                {{ number }}
              </span>
            </div>
            
            <div class="bonus-numbers" v-if="currentDraw.bonusNumber || currentDraw.powerball">
              <span v-if="currentDraw.bonusNumber" class="bonus-ball">
                {{ currentDraw.bonusNumber }}
              </span>
              <span v-if="currentDraw.powerball" class="powerball">
                {{ currentDraw.powerball }}
              </span>
            </div>
          </div>
        </div>
      </div>
      
      <div class="bookmark-section">
        <BookmarkManager 
          v-if="currentDraw"
          :current-draw="currentDraw"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BreadcrumbNavigation from '@/components/BreadcrumbNavigation.vue'
import DrawNavigation from '@/components/DrawNavigation.vue'
import NavigationContext from '@/components/NavigationContext.vue'
import BookmarkManager from '@/components/BookmarkManager.vue'
import { useNavigationStore } from '@/stores/navigation'
import type { BreadcrumbItem } from '@/types/navigation'
import type { LottoDraw, NavigationContext as NavContext } from '@/types/lottery'

const route = useRoute()
const router = useRouter()
const navigationStore = useNavigationStore()

const currentDraw = computed(() => navigationStore.currentDraw)
const navigationContext = computed(() => navigationStore.navigationContext)

const breadcrumbItems = computed<BreadcrumbItem[]>(() => {
  const items: BreadcrumbItem[] = [
    { label: 'Home', path: '/' },
    { label: 'Draw Navigation', path: '/navigation' }
  ]
  
  if (currentDraw.value) {
    items.push({
      label: `Draw #${currentDraw.value.draw}`,
      path: `/navigation/${currentDraw.value.draw}`,
      active: true
    })
  } else {
    items[items.length - 1].active = true
  }
  
  return items
})

const handleNavigation = async (direction: 'previous' | 'next' | 'first' | 'last') => {
  await navigationStore.navigate(direction)
  
  // Update URL to reflect current draw
  if (currentDraw.value) {
    await router.push(`/navigation/${currentDraw.value.draw}`)
  }
}

const handleJumpTo = async (target: { drawNumber?: number; date?: Date }) => {
  await navigationStore.jumpTo(target)
  
  // Update URL to reflect current draw
  if (currentDraw.value) {
    await router.push(`/navigation/${currentDraw.value.draw}`)
  }
}

const formatDate = (date: Date | string): string => {
  const dateObj = typeof date === 'string' ? new Date(date) : date
  return dateObj.toLocaleDateString('en-NZ', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

onMounted(async () => {
  // Load draw from route parameter if provided
  const drawNumber = route.params.drawNumber as string
  
  if (drawNumber && !isNaN(Number(drawNumber))) {
    await navigationStore.jumpTo({ drawNumber: Number(drawNumber) })
  } else if (!currentDraw.value) {
    // Load latest draw if no current draw
    await navigationStore.navigate('last')
  }
  
  // Load navigation context
  await navigationStore.loadNavigationContext()
})
</script>

<style scoped>
.navigation-view {
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

.navigation-content {
  display: grid;
  grid-template-columns: 1fr;
  gap: 2rem;
}

.navigation-controls,
.navigation-context,
.draw-details,
.bookmark-section {
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: 1.5rem;
}

.draw-card h3 {
  color: var(--color-heading);
  margin-bottom: 0.5rem;
}

.draw-date {
  color: var(--color-text);
  font-size: 1.1rem;
  margin-bottom: 1.5rem;
}

.winning-numbers {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.main-numbers,
.bonus-numbers {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.number-ball,
.bonus-ball,
.powerball {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 3rem;
  height: 3rem;
  border-radius: 50%;
  font-weight: bold;
  font-size: 1.1rem;
}

.number-ball {
  background: var(--color-primary);
  color: white;
}

.bonus-ball {
  background: var(--color-secondary);
  color: white;
}

.powerball {
  background: var(--color-accent);
  color: white;
}

@media (min-width: 768px) {
  .navigation-content {
    grid-template-columns: 1fr 1fr;
  }
  
  .draw-details {
    grid-column: 1 / -1;
  }
}

@media (min-width: 1024px) {
  .navigation-view {
    padding: 2rem;
  }
  
  .navigation-content {
    grid-template-columns: 2fr 1fr;
  }
  
  .navigation-controls {
    grid-column: 1 / -1;
  }
  
  .draw-details {
    grid-column: 1;
  }
  
  .bookmark-section {
    grid-column: 2;
  }
}
</style>