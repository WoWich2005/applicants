import { useEffect, useState } from "react"

export const useLocalStorage = (localStorageKey, initialValue) => {
  const [state, setState] = useState(() => {
    const savedState = localStorage.getItem(localStorageKey)
    return JSON.parse(savedState) ?? initialValue
  })
    
  useEffect(() => {
    localStorage.setItem(localStorageKey, JSON.stringify(state))
  }, [state, localStorageKey])

  return [state, setState]
}