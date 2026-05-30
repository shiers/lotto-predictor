<template>
  <div id="app">
    <SideMenu />
    <main class="main-content">
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
import { RouterView } from 'vue-router'
import SideMenu from '@/components/SideMenu.vue'
</script>

<style scoped>
#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.main-content {
  flex: 1;
  padding-left: 0;
  padding-top: 4rem;
  transition: padding-left 0.3s ease;
}

@media (min-width: 1024px) {
  .main-content {
    padding-left: 0;
  }
}
</style>