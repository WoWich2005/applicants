import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import AppRouter from './AppRouter'
import 'normalize.css'
import './styles/common.scss'
import GlobalProvider from './providers/GlobalProvider'
import { AuthProvider } from './contexts/AuthContext'

createRoot(document.getElementById('root')).render(
  <BrowserRouter>
    <GlobalProvider>
      <AuthProvider>
        <AppRouter />
      </AuthProvider>
    </GlobalProvider>
  </BrowserRouter>
)
