import { createContext, useContext, useState, useEffect } from 'react'
import i18n from '../i18n/index.js'

const LanguageContext = createContext({ language: 'ru', setLanguage: () => {} })

const STORAGE_KEY = 'language'

export function LanguageProvider({ children }) {
  const [language, setLanguageState] = useState(
    () => localStorage.getItem(STORAGE_KEY) ?? 'ru'
  )

  useEffect(() => {
    i18n.changeLanguage(language)
  }, [])

  const setLanguage = (lang) => {
    localStorage.setItem(STORAGE_KEY, lang)
    i18n.changeLanguage(lang)
    setLanguageState(lang)
  }

  return (
    <LanguageContext.Provider value={{ language, setLanguage }}>
      {children}
    </LanguageContext.Provider>
  )
}

export const useLanguage = () => useContext(LanguageContext)
