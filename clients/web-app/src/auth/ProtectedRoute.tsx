import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'
import type { AuthRole } from './types'

export function ProtectedRoute({ roles, children }: { roles: AuthRole[]; children: ReactNode }) {
  const { session } = useAuth()
  const location = useLocation()

  if (!session) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  if (!roles.includes(session.user.role)) {
    return <Navigate to="/unauthorized" replace />
  }

  return children
}
