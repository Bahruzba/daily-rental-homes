export const authRoles = ['Admin', 'Broker', 'Customer'] as const

export type AuthRole = typeof authRoles[number]

export type AuthUser = {
  id: number
  fullName: string
  phone: string
  role: AuthRole
}

export type AuthSession = {
  accessToken: string
  expiresAt: string
  user: AuthUser
  demo: boolean
}

export function dashboardPath(role: AuthRole) {
  if (role === 'Admin') return '/admin'
  if (role === 'Broker') return '/broker'
  return '/account'
}
