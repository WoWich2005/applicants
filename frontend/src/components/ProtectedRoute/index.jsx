import { Navigate } from 'react-router'
import { useAuth } from '../../contexts/AuthContext'
import { ROUTES } from '../../constants/routes'

function ProtectedRoute({ children, roles }) {
  const { auth } = useAuth()

  if (!auth) {
    return <Navigate to={ROUTES.LOGIN} replace />
  }

  if (roles && !roles.includes(auth.role)) {
    return <Navigate to={ROUTES.RESULTS} replace />
  }

  return children
}

export default ProtectedRoute
