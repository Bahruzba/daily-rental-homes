import { createContext, type ReactNode, useContext, useMemo, useState } from 'react'
import type { AuthSession } from './types'

const storageKey = 'daily-homes-auth-session'

type AuthContextValue = {
  session?: AuthSession
  setSession: (session: AuthSession) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function readStoredSession(): AuthSession | undefined {
  try {
    const stored = window.localStorage.getItem(storageKey)
    if (!stored) return undefined
    const session = JSON.parse(stored) as AuthSession
    if (!session.accessToken || !session.user || new Date(session.expiresAt).getTime() <= Date.now()) {
      window.localStorage.removeItem(storageKey)
      return undefined
    }
    return session
  } catch {
    window.localStorage.removeItem(storageKey)
    return undefined
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, updateSession] = useState<AuthSession | undefined>(readStoredSession)

  const value = useMemo<AuthContextValue>(() => ({
    session,
    setSession(nextSession) {
      window.localStorage.setItem(storageKey, JSON.stringify(nextSession))
      updateSession(nextSession)
    },
    logout() {
      window.localStorage.removeItem(storageKey)
      updateSession(undefined)
    },
  }), [session])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
