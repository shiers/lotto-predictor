import axios from 'axios'

// Create axios instance with base configuration
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Retry configuration
const MAX_RETRIES = 2
const RETRY_DELAY = 2000

// Request interceptor
api.interceptors.request.use(
  (config) => {
    // Track retry count
    config.headers['x-retry-count'] = config.headers['x-retry-count'] || '0'
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor with retry on network errors
api.interceptors.response.use(
  (response) => {
    return response
  },
  async (error) => {
    const config = error.config
    const retryCount = parseInt(config?.headers?.['x-retry-count'] || '0')

    // Retry on network errors or 502/503/504 (backend restarting)
    const isRetryable = !error.response || [502, 503, 504].includes(error.response?.status)

    if (isRetryable && retryCount < MAX_RETRIES && config) {
      config.headers['x-retry-count'] = String(retryCount + 1)
      console.warn(`API request failed, retrying (${retryCount + 1}/${MAX_RETRIES})...`)
      await new Promise(resolve => setTimeout(resolve, RETRY_DELAY))
      return api(config)
    }

    console.error('API Error:', error.response?.data || error.message)
    return Promise.reject(error)
  }
)

export default api