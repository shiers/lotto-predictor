import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView
    },
    {
      path: '/predictions',
      name: 'predictions',
      component: () => import('../views/PredictionsView.vue')
    },
    {
      path: '/draws',
      name: 'draws',
      component: () => import('../views/DrawsView.vue')
    },
    {
      path: '/lookup',
      name: 'lookup',
      component: () => import('../views/LookupView.vue')
    },
    {
      path: '/history',
      name: 'history',
      component: () => import('../views/HistoryView.vue')
    }
  ]
})

export default router