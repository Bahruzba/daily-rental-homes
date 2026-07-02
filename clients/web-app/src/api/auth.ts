import type { ApiResponse } from '../types'
import { authRoles, type AuthRole, type AuthSession } from '../auth/types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
export const isLiveApiEnabled = import.meta.env.VITE_USE_LIVE_API === 'true'

type OtpApiResponse = {
  message: string
  expiresAt: string
  devPin?: string | null
}

type AuthSessionApiResponse = {
  accessToken: string
  expiresAt: string
  user: {
    id: number
    fullName: string
    phone: string
    role: string
  }
}

export class AuthRequestError extends Error {
  constructor(message: string, readonly technicalCause?: unknown) {
    super(message)
    this.name = 'AuthRequestError'
  }
}

function normalizeRole(role: string): AuthRole {
  return authRoles.includes(role as AuthRole) ? role as AuthRole : 'Customer'
}

async function post<T>(path: string, body: unknown): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${baseUrl}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
  } catch (technicalCause) {
    throw new AuthRequestError('Serverlə əlaqə qurmaq mümkün olmadı.', technicalCause)
  }

  let payload: ApiResponse<T>
  try {
    payload = await response.json() as ApiResponse<T>
  } catch (technicalCause) {
    throw new AuthRequestError('Serverdən düzgün cavab alınmadı.', technicalCause)
  }

  if (!response.ok || !payload.success || !payload.data) {
    throw new AuthRequestError(payload.error || 'Əməliyyatı tamamlamaq mümkün olmadı.', new Error(payload.error))
  }

  return payload.data
}

export async function requestOtp(phone: string) {
  if (!isLiveApiEnabled) {
    await new Promise((resolve) => window.setTimeout(resolve, 250))
    return {
      message: 'Demo OTP yaradıldı.',
      expiresAt: new Date(Date.now() + 5 * 60_000).toISOString(),
      devPin: '123456',
    }
  }

  return post<OtpApiResponse>('/api/auth/send', { phone })
}

export async function verifyOtp(input: {
  phone: string
  pin: string
  fullName?: string
  mockRole: AuthRole
}): Promise<AuthSession> {
  if (!isLiveApiEnabled) {
    await new Promise((resolve) => window.setTimeout(resolve, 250))
    if (input.pin !== '123456') {
      throw new AuthRequestError('Demo kod yanlışdır. 123456 istifadə edin.')
    }

    return {
      accessToken: `demo-${input.mockRole.toLowerCase()}-token`,
      expiresAt: new Date(Date.now() + 24 * 60 * 60_000).toISOString(),
      user: {
        id: input.mockRole === 'Admin' ? 1 : input.mockRole === 'Broker' ? 2 : 3,
        fullName: input.fullName?.trim() || `Demo ${input.mockRole}`,
        phone: input.phone.trim(),
        role: input.mockRole,
      },
      demo: true,
    }
  }

  const session = await post<AuthSessionApiResponse>('/api/auth/confirm', {
    phone: input.phone,
    pin: input.pin,
    fullName: input.fullName,
  })

  return {
    accessToken: session.accessToken,
    expiresAt: session.expiresAt,
    user: { ...session.user, role: normalizeRole(session.user.role) },
    demo: false,
  }
}
