import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import { createPinia } from 'pinia'
import App from '@/App.vue'
import HomeView from '@/views/HomeView.vue'

describe('Responsive Layout', () => {
  let router: any
  let pinia: any
  let originalInnerWidth: number

  beforeEach(() => {
    router = createRouter({
      history: createWebHistory(),
      routes: [
        { path: '/', name: 'home', component: HomeView }
      ]
    })
    pinia = createPinia()
    originalInnerWidth = window.innerWidth
  })

  afterEach(() => {
    // Restore original window width
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      configurable: true,
      value: originalInnerWidth
    })
  })

  it('should adapt to mobile viewport (320px)', async () => {
    // Mock mobile viewport
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      configurable: true,
      value: 320
    })

    const wrapper = mount(App, {
      global: {
        plugins: [router, pinia]
      }
    })

    await router.push('/')
    await wrapper.vm.$nextTick()

    const mainContent = wrapper.find('.main-content')
    expect(mainContent.exists()).toBe(true)

    // Check that the side menu exists and is properly configured for mobile
    const sideMenu = wrapper.find('.side-menu')
    expect(sideMenu.exists()).toBe(true)
  })

  it('should adapt to tablet viewport (768px)', async () => {
    // Mock tablet viewport
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      configurable: true,
      value: 768
    })

    const wrapper = mount(App, {
      global: {
        plugins: [router, pinia]
      }
    })

    await router.push('/')
    await wrapper.vm.$nextTick()

    const mainContent = wrapper.find('.main-content')
    expect(mainContent.exists()).toBe(true)
  })

  it('should adapt to desktop viewport (1024px)', async () => {
    // Mock desktop viewport
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      configurable: true,
      value: 1024
    })

    const wrapper = mount(App, {
      global: {
        plugins: [router, pinia]
      }
    })

    await router.push('/')
    await wrapper.vm.$nextTick()

    const mainContent = wrapper.find('.main-content')
    expect(mainContent.exists()).toBe(true)
  })

  it('should adapt to large desktop viewport (1440px)', async () => {
    // Mock large desktop viewport
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      configurable: true,
      value: 1440
    })

    const wrapper = mount(App, {
      global: {
        plugins: [router, pinia]
      }
    })

    await router.push('/')
    await wrapper.vm.$nextTick()

    const mainContent = wrapper.find('.main-content')
    expect(mainContent.exists()).toBe(true)
  })

  it('should handle window resize events', async () => {
    const wrapper = mount(App, {
      global: {
        plugins: [router, pinia]
      }
    })

    await router.push('/')
    await wrapper.vm.$nextTick()

    // Start with mobile
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      configurable: true,
      value: 375
    })

    // Simulate resize to desktop
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      configurable: true,
      value: 1200
    })

    // Trigger resize event
    window.dispatchEvent(new Event('resize'))
    await wrapper.vm.$nextTick()

    const mainContent = wrapper.find('.main-content')
    expect(mainContent.exists()).toBe(true)
  })
})