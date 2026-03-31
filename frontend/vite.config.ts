/// <reference types="vitest" />
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    {
      name: 'log-hmr',
      handleHotUpdate({ file }) {
        console.log('[HMR] File changed:', file)
      }
    }
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 8080,
    host: '0.0.0.0',
    hmr: {
      clientPort: 3001
    },
    watch: {
      usePolling: false,
      ignored: ['**/node_modules/**', '**/dist/**', '**/.idea/**', '**/.git/**']
    }
  },
  test: {
    globals: true,
    environment: 'jsdom'
  }
})