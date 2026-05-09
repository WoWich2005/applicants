import axios from "axios"

export const instance = axios.create({
  baseURL: 'http://localhost:5059/api/v1',
  headers: {
    "Content-Type": "application/json",
    "Accept": "application/json",
  }
})

instance.interceptors.request.use((config) => {
  try {
    const stored = localStorage.getItem('auth')
    const auth = stored ? JSON.parse(stored) : null
    if (auth?.token) {
      config.headers.Authorization = `Bearer ${auth.token}`
    }
  } catch {
    // ignore
  }
  config.headers['Accept-Language'] = localStorage.getItem('language') ?? 'ru'
  return config
})

instance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && !window.location.pathname.includes('/login')) {
      localStorage.removeItem('auth')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)
