import { createContext, useContext, useState } from 'react'

/**
 * @typedef {{ token: string, username: string, role: string, facultyId?: number, specialtyIds?: string, facultyAccessIds?: string }} AuthData
 */

const defaultContext = {
  /** @type {AuthData | null} */
  auth: null,
  /** @param {AuthData} _data */
  login: (_data) => {},
  logout: () => {},
}

const AuthContext = createContext(defaultContext)

const STORAGE_KEY = 'bntu_auth'

export function AuthProvider({ children }) {
  const [auth, setAuth] = useState(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      return stored ? JSON.parse(stored) : null
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
