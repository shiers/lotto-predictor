<template>
  <nav class="breadcrumb-navigation" aria-label="Breadcrumb">
    <ol class="breadcrumb-list">
      <li 
        v-for="(item, index) in items" 
        :key="index"
        class="breadcrumb-item"
        :class="{ 'active': item.active }"
      >
        <RouterLink 
          v-if="!item.active && item.path"
          :to="item.path"
          class="breadcrumb-link"
        >
          {{ item.label }}
        </RouterLink>
        
        <span v-else class="breadcrumb-text">
          {{ item.label }}
        </span>
        
        <span 
          v-if="index < items.length - 1"
          class="breadcrumb-separator"
          aria-hidden="true"
        >
          /
        </span>
      </li>
    </ol>
  </nav>
</template>

<script setup lang="ts">
import { RouterLink } from 'vue-router'
import type { BreadcrumbItem } from '@/types/navigation'

interface Props {
  items: BreadcrumbItem[]
}

defineProps<Props>()
</script>

<style scoped>
.breadcrumb-navigation {
  margin-bottom: 1.5rem;
  padding: 0.75rem 1rem;
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 6px;
}

.breadcrumb-list {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  list-style: none;
  margin: 0;
  padding: 0;
}

.breadcrumb-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.breadcrumb-link {
  color: var(--color-primary);
  text-decoration: none;
  font-size: 0.9rem;
  transition: color 0.2s ease;
}

.breadcrumb-link:hover {
  color: var(--color-primary-dark);
  text-decoration: underline;
}

.breadcrumb-text {
  color: var(--color-text);
  font-size: 0.9rem;
}

.breadcrumb-item.active .breadcrumb-text {
  color: var(--color-heading);
  font-weight: 500;
}

.breadcrumb-separator {
  color: var(--color-text-light);
  font-size: 0.9rem;
  user-select: none;
}

@media (max-width: 768px) {
  .breadcrumb-navigation {
    padding: 0.5rem 0.75rem;
  }
  
  .breadcrumb-link,
  .breadcrumb-text {
    font-size: 0.85rem;
  }
  
  .breadcrumb-list {
    gap: 0.25rem;
  }
  
  .breadcrumb-item {
    gap: 0.25rem;
  }
}
</style>