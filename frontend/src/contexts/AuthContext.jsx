import { createContext, useContext, useState } from 'react'

/**
 * @typedef {{ token: string, username: string, role: string }} AuthData
 */

const defaultContext = {
  /** @type {AuthData | null} */
  auth: null,
  /** @param {AuthData} _data */
  login: (_data) => {},
  logout: () => {},
}

const AuthContext = createContext(defaultContext)

const STORAGE_KEY = 'auth'

const isTokenExpired = (token) => {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    return payload.exp * 1000 < Date.now()
  } catch {
    return true
  }
}

export function AuthProvider({ children }) {
  const [auth, setAuth] = useState(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      const parsed = stored ? JSON.parse(stored) : null
      if (parsed?.token && isTokenExpired(parsed.token)) {
        localStorage.removeItem(STORAGE_KEY)
        return null
      }
      return parsed
    } catch {
      return null
    }
  })

  const login = (data) => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
    setAuth(data)
  }

  const logout = () => {
    localStorage.removeItem(STORAGE_KEY)
    setAuth(null)
  }

  return (
    <AuthContext.Provider value={{ auth, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = () => useContext(AuthContext)
