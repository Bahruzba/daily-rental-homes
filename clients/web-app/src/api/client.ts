import { demoHomes } from '../data/homes'
import type { ApiResponse, BookingPayload, BookingResult, RentalHome } from '../types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
const useLiveApi = import.meta.env.VITE_USE_LIVE_API === 'true'

type RentalHomeApiModel = Pick<RentalHome, 'id' | 'title' | 'city' | 'dailyPrice' | 'roomCount' | 'guestCount' | 'isPublished'> & {
  description?: string
  district?: string | null
  address?: string | null
  mediaFiles?: Array<{ fileUrl: string; sortOrder: number }>
  contacts?: Array<{ fullName: string; value: string; contactType: number }>
}

function getWhatsAppUrl(value: string | undefined, fallback: string) {
  if (!value) return fallback
  if (/^https?:\/\//i.test(value)) return value
  const digits = value.replace(/\D/g, '')
  return digits ? `https://wa.me/${digits}` : fallback
}

function withMockPresentation(home: RentalHomeApiModel): RentalHome {
  const fallback = demoHomes[(Math.abs(home.id) - 1) % demoHomes.length]
  const phone = home.contacts?.find((contact) => contact.contactType === 1)
  const whatsapp = home.contacts?.find((contact) => contact.contactType === 2)
  const apiImages = home.mediaFiles
    ?.filter((media) => media.fileUrl)
    .sort((left, right) => left.sortOrder - right.sortOrder)
    .map((media) => media.fileUrl)

  return {
    ...fallback,
    ...home,
    description: home.description || fallback.description,
    images: apiImages?.length ? apiImages : fallback.images,
    contact: {
      name: phone?.fullName || whatsapp?.fullName || fallback.contact.name,
      phone: phone?.value || fallback.contact.phone,
      whatsapp: getWhatsAppUrl(whatsapp?.value, fallback.contact.whatsapp),
    },
  }
}

export async function getRentalHomes(): Promise<RentalHome[]> {
  if (!useLiveApi) return demoHomes

  try {
    const response = await fetch(`${baseUrl}/api/rental-homes`)
    if (!response.ok) throw new Error('Rental homes request failed')
    const payload = (await response.json()) as ApiResponse<RentalHomeApiModel[]>
    return payload.success && payload.data?.length
      ? payload.data.map(withMockPresentation)
      : demoHomes
  } catch {
    return demoHomes
  }
}

export async function getRentalHomeById(id: number): Promise<RentalHome | undefined> {
  const fallback = demoHomes.find((home) => home.id === id)
  if (!useLiveApi) return fallback

  try {
    const response = await fetch(`${baseUrl}/api/rental-homes/${id}`)
    if (!response.ok) return fallback
    const payload = (await response.json()) as ApiResponse<RentalHomeApiModel>
    return payload.success && payload.data ? withMockPresentation(payload.data) : fallback
  } catch {
    return fallback
  }
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
