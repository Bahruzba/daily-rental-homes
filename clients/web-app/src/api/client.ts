import { demoHomes } from '../data/homes'
import type { ApiResponse, BookingPayload, BookingResult, RentalHome } from '../types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
const useLiveApi = import.meta.env.VITE_USE_LIVE_API === 'true'

function withMockPresentation(home: RentalHome): RentalHome {
  const fallback = demoHomes[(Math.abs(home.id) - 1) % demoHomes.length]
  return {
    ...fallback,
    ...home,
    images: home.images?.length ? home.images : fallback.images,
    contact: home.contact ?? fallback.contact,
  }
}

export async function getRentalHomes(): Promise<RentalHome[]> {
  if (!useLiveApi) return demoHomes

  try {
    const response = await fetch(`${baseUrl}/api/rental-homes`)
    if (!response.ok) throw new Error('Rental homes request failed')
    const payload = (await response.json()) as ApiResponse<RentalHome[]>
    return payload.success && payload.data?.length
      ? payload.data.map(withMockPresentation)
      : demoHomes
  } catch {
    return demoHomes
  }
}

export async function getRentalHomeById(id: number): Promise<RentalHome | undefined> {
  const homes = await getRentalHomes()
  return homes.find((home) => home.id === id)
}

export async function createBooking(payload: BookingPayload): Promise<BookingResult> {
  if (!useLiveApi) {
    await new Promise((resolve) => window.setTimeout(resolve, 350))
    return { id: Math.floor(1000 + Math.random() * 9000), demo: true }
  }

  const response = await fetch(`${baseUrl}/api/bookings`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  const result = (await response.json()) as ApiResponse<{ id: number }>
  if (!response.ok || !result.success || !result.data) {
    throw new Error(result.error || 'Rezervasiya sorğusu göndərilmədi')
  }
  return { id: result.data.id, demo: false }
}
