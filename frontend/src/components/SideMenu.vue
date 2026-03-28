<template>
  <div class="side-menu" :class="{ 'menu-open': isMenuOpen }">
    <div class="menu-toggle" @click="toggleMenu">
      <span class="hamburger"></span>
      <span class="hamburger"></span>
      <span class="hamburger"></span>
    </div>
    
    <nav class="menu-nav" :class="{ 'nav-open': isMenuOpen }">
      <div class="menu-header">
        <h2>PredictLottoNZ</h2>
        <button @click="closeMenu" class="close-button">×</button>
      </div>
      
      <ul class="menu-items">
        <li>
          <RouterLink to="/" @click="closeMenu" class="menu-link">
            <span class="menu-icon">🏠</span>
            Home
          </RouterLink>
        </li>
        <li>
          <RouterLink to="/predictions" @click="closeMenu" class="menu-link">
            <span class="menu-icon">🎯</span>
            Predictions
          </RouterLink>
        </li>
        <li>
          <RouterLink to="/draws" @click="closeMenu" class="menu-link">
            <span class="menu-icon">🎲</span>
            Draws
          </RouterLink>
        </li>
        <li>
          <a @click="openUploadDialog" class="menu-link">
            <span class="menu-icon">📁</span>
            Upload Data
          </a>
        </li>
        <li>
          <RouterLink to="/lookup" @click="closeMenu" class="menu-link">
            <span class="menu-icon">🔍</span>
            Number Lookup
          </RouterLink>
        </li>
      </ul>
      
      <div class="menu-footer">
        <p class="app-version">v1.0.0</p>
      </div>
    </nav>
    
    <div v-if="isMenuOpen" class="menu-overlay" @click="closeMenu"></div>
  </div>
  
  <!-- Upload Dialog -->
  <div v-if="showUploadDialog" class="dialog-overlay" @click="showUploadDialog = false">
    <div class="dialog-content" @click.stop>
      <div class="dialog-header">
        <h3>Upload Data</h3>
        <button @click="showUploadDialog = false" class="dialog-close">×</button>
      </div>
      <div class="dialog-body">
        <FileUpload @uploadSuccess="handleUploadSuccess" @uploadError="handleUploadError" />
      </div>
    </div>
  </div>
  
  <!-- Toast Notifications -->
  <div class="toast-container">
    <TransitionGroup name="toast" tag="div">
      <div
        v-for="notification in appStore.notifications"
        :key="notification.id"
        class="toast"
        :class="[`toast-${notification.type}`]"
      >
        <div class="toast-content">
          <div class="toast-header">
            <span class="toast-icon">{{ getToastIcon(notification.type) }}</span>
            <strong class="toast-title">{{ notification.title }}</strong>
            <button @click="appStore.removeNotification(notification.id)" class="toast-close">×</button>
          </div>
          <p class="toast-message">{{ notification.message }}</p>
        </div>
      </div>
    </TransitionGroup>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink } from 'vue-router'
import { useAppStore } from '@/stores/app'
import FileUpload from '@/components/FileUpload.vue'

const appStore = useAppStore()

// Menu state
const isMenuOpen = ref(false)

// Menu controls
const toggleMenu = () => {
  isMenuOpen.value = !isMenuOpen.value
}

const closeMenu = () => {
  isMenuOpen.value = false
}

// Dialog state
const showUploadDialog = ref(false)

// Navigation helpers
const openUploadDialog = () => {
  closeMenu()
  showUploadDialog.value = true
}

const scrollToLatest = () => {
  closeMenu()
  const latestElement = document.getElementById('latest')
  if (latestElement) {
    latestElement.scrollIntoView({ behavior: 'smooth' })
  }
}

// Upload dialog handlers
const handleUploadSuccess = (data: any) => {
  showUploadDialog.value = false
  appStore.addNotification({
    type: 'success',
    title: 'Data Uploaded Successfully',
    message: `${data.fileName} uploaded with ${data.recordsAdded} new records`
  })
}

const handleUploadError = (error: any) => {
  console.error('Upload error:', error)
  // Error notification is already handled by FileUpload component
}

// Toast notification helpers
const getToastIcon = (type: string): string => {
  switch (type) {
    case 'success': return '✅'
    case 'error': return '❌'
    case 'warning': return '⚠️'
    case 'info': return 'ℹ️'
    default: return 'ℹ️'
  }
}
</script>

<style scoped>
.side-menu {
  position: relative;
  z-index: 1000;
}

.menu-toggle {
  position: fixed;
  top: 1rem;
  left: 1rem;
  width: 3rem;
  height: 3rem;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  gap: 0.25rem;
  z-index: 1001;
  transition: all 0.3s ease;
}

.menu-toggle:hover {
  background: var(--color-background-soft);
}

.hamburger {
  width: 1.5rem;
  height: 2px;
  background: var(--color-text);
  transition: all 0.3s ease;
}

.menu-open .hamburger:nth-child(1) {
  transform: rotate(45deg) translate(0.5rem, 0.5rem);
}

.menu-open .hamburger:nth-child(2) {
  opacity: 0;
}

.menu-open .hamburger:nth-child(3) {
  transform: rotate(-45deg) translate(0.5rem, -0.5rem);
}

.menu-nav {
  position: fixed;
  top: 0;
  left: -300px;
  width: 300px;
  height: 100vh;
  background: var(--color-background);
  border-right: 1px solid var(--color-border);
  transition: left 0.3s ease;
  z-index: 1000;
  display: flex;
  flex-direction: column;
}

.nav-open {
  left: 0;
}

.menu-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid var(--color-border);
}

.menu-header h2 {
  color: var(--color-heading);
  margin: 0;
  font-size: 1.25rem;
}

.close-button {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: var(--color-text);
  padding: 0;
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.close-button:hover {
  background: var(--color-background-soft);
  border-radius: 4px;
}

.menu-items {
  list-style: none;
  padding: 0;
  margin: 0;
  flex: 1;
}

.menu-items li {
  border-bottom: 1px solid var(--color-border);
}

.menu-link {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1.5rem;
  color: var(--color-text);
  text-decoration: none;
  transition: background-color 0.3s ease;
}

.menu-link:hover {
  background: var(--color-background-soft);
}

.menu-link.router-link-active {
  background: var(--color-background-soft);
  color: var(--color-heading);
  font-weight: 500;
}

.menu-icon {
  font-size: 1.25rem;
  width: 1.5rem;
  text-align: center;
}

.menu-footer {
  padding: 1.5rem;
  border-top: 1px solid var(--color-border);
  text-align: center;
}

.app-version {
  color: var(--color-text);
  font-size: 0.875rem;
  margin: 0;
  opacity: 0.7;
}

.menu-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.5);
  z-index: 999;
}

/* Toast Notifications */
.toast-container {
  position: fixed;
  top: 1rem;
  right: 1rem;
  z-index: 2000;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  max-width: 400px;
}

.toast {
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  overflow: hidden;
  min-width: 300px;
}

.toast-success {
  border-left: 4px solid #28a745;
}

.toast-error {
  border-left: 4px solid #dc3545;
}

.toast-warning {
  border-left: 4px solid #ffc107;
}

.toast-info {
  border-left: 4px solid #17a2b8;
}

.toast-content {
  padding: 1rem;
}

.toast-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.toast-icon {
  font-size: 1rem;
}

.toast-title {
  flex: 1;
  color: var(--color-heading);
  font-size: 0.9rem;
  margin: 0;
}

.toast-close {
  background: none;
  border: none;
  font-size: 1.25rem;
  cursor: pointer;
  color: var(--color-text);
  padding: 0;
  width: 1.5rem;
  height: 1.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
}

.toast-close:hover {
  background: var(--color-background-soft);
}

.toast-message {
  color: var(--color-text);
  font-size: 0.875rem;
  margin: 0;
  line-height: 1.4;
}

/* Toast Transitions */
.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s ease;
}

.toast-enter-from {
  opacity: 0;
  transform: translateX(100%);
}

.toast-leave-to {
  opacity: 0;
  transform: translateX(100%);
}

/* Upload Dialog */
.dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.5);
  z-index: 2000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.dialog-content {
  background: var(--color-background);
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
}

.dialog-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid var(--color-border);
}

.dialog-header h3 {
  color: var(--color-heading);
  margin: 0;
  font-size: 1.25rem;
}

.dialog-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: var(--color-text);
  padding: 0;
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
}

.dialog-close:hover {
  background: var(--color-background-soft);
}

.dialog-body {
  padding: 1.5rem;
}

/* Responsive */
@media (max-width: 768px) {
  .menu-nav {
    width: 100vw;
    left: -100vw;
  }
  
  .toast-container {
    left: 1rem;
    right: 1rem;
    max-width: none;
  }
  
  .toast {
    min-width: auto;
  }
  
  .dialog-overlay {
    padding: 0.5rem;
  }
  
  .dialog-header {
    padding: 1rem;
  }
  
  .dialog-body {
    padding: 1rem;
  }
}
</style>