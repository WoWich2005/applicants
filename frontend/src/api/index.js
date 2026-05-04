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
    const stored = localStorage.getItem('bntu_auth')
    const auth = stored ? JSON.parse(stored) : null
    if (auth?.token) {
      config.headers.Authorization = `Bearer ${auth.token}`
    }
  } catch {
    // ignore
  }
  return config
})
