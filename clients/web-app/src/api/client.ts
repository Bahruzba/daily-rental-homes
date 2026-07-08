import { demoHomes } from '../data/homes'
import type { ApiResponse, BookingPayload, BookingResult, RentalHome } from '../types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
const useLiveApi = import.meta.env.VITE_USE_LIVE_API === 'true'

type RentalHomeApiModel = Pick<RentalHome, 'id' | 'title' | 'city' | 'dailyPrice' | 'roomCount' | 'guestCount' | 'isPublished'> & {
  description?: string
  district?: string | null
  address?: string | null
  mainImageUrl?: string | null
  mediaFiles?: Array<{ fileUrl: string; sortOrder: number }>
  contacts?: Array<{ fullName: string; value: string; contactType: number }>
  unavailableRanges?: Array<{ startDate: string; endDate: string }>
}

type BookingCreatedApiModel = {
  bookingId: number
  rentalHomeId: number
  statusCode: string
  statusName: string
  dailyPrice: number
  totalAmount: number
  dates: string[]
  customerName: string
  phone: string
  createdAt: string
}

export type RentalHomeFilters = {
  q?: string
  city?: string
  district?: string
  guests?: string
  minPrice?: string
  maxPrice?: string
  startDate?: string
  endDate?: string
}

export class BookingRequestError extends Error {
  constructor(message: string, readonly technicalCause?: unknown) {
    super(message)
    this.name = 'BookingRequestError'
  }
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
    .map((media) => resolveAssetUrl(media.fileUrl))
  const listImage = home.mainImageUrl ? [resolveAssetUrl(home.mainImageUrl)] : []

  return {
    ...fallback,
    ...home,
    description: home.description || fallback.description,
    images: apiImages?.length ? apiImages : listImage.length ? listImage : fallback.images,
    contact: {
      name: phone?.fullName || whatsapp?.fullName || fallback.contact.name,
      phone: phone?.value || fallback.contact.phone,
      whatsapp: getWhatsAppUrl(whatsapp?.value, fallback.contact.whatsapp),
    },
  }
}

export class RentalHomesRequestError extends Error {
  constructor(message: string, readonly technicalCause?: unknown) {
    super(message)
    this.name = 'RentalHomesRequestError'
  }
}

function resolveAssetUrl(url: string) {
  return /^https?:|^blob:/i.test(url) ? url : `${baseUrl}${url}`
}

function normalize(value: string | undefined) {
  return (value ?? '').trim().toLocaleLowerCase('az-AZ')
}

function dateRangeOverlaps(range: { startDate: string; endDate: string }, startDate: string, endDate: string) {
  return range.startDate <= endDate && range.endDate >= startDate
}

function applyMockFilters(homes: RentalHome[], filters: RentalHomeFilters = {}) {
  const q = normalize(filters.q)
  const city = normalize(filters.city)
  const district = normalize(filters.district)
  const guests = Number(filters.guests || 0)
  const minPrice = Number(filters.minPrice || 0)
  const maxPrice = Number(filters.maxPrice || 0)
  return homes.filter((home) => {
    if (city && normalize(home.city) !== city) return false
    if (district && normalize(home.district ?? '') !== district) return false
    if (guests > 0 && home.guestCount < guests) return false
    if (minPrice > 0 && home.dailyPrice < minPrice) return false
    if (maxPrice > 0 && home.dailyPrice > maxPrice) return false
    if (q) {
      const haystack = [home.title, home.city, home.district ?? '', home.description].map(normalize).join(' ')
      if (!haystack.includes(q)) return false
    }
    if (filters.startDate && filters.endDate && home.unavailableRanges?.some((range) => dateRangeOverlaps(range, filters.startDate!, filters.endDate!))) return false
    return true
  })
}

function queryString(filters: RentalHomeFilters = {}) {
  const params = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== null && `${value}`.trim()) params.set(key, `${value}`.trim())
  })
  const query = params.toString()
  return query ? `?${query}` : ''
}

export async function getRentalHomes(filters: RentalHomeFilters = {}): Promise<RentalHome[]> {
  if (!useLiveApi) return applyMockFilters(demoHomes, filters)

  try {
    const response = await fetch(`${baseUrl}/api/rental-homes${queryString(filters)}`)
    const payload = (await response.json()) as ApiResponse<RentalHomeApiModel[]>
    if (!response.ok || !payload.success || !payload.data) {
      throw new RentalHomesRequestError(payload.error || 'Ev siyahısı yüklənmədi.')
    }
    return payload.data.map(withMockPresentation)
  } catch (technicalCause) {
    if (technicalCause instanceof RentalHomesRequestError) throw technicalCause
    throw new RentalHomesRequestError('Serverlə əlaqə qurmaq mümkün olmadı. Bir qədər sonra yenidən yoxlayın.', technicalCause)
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

  let response: Response
  try {
    response = await fetch(`${baseUrl}/api/bookings`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    })
  } catch (technicalCause) {
    throw new BookingRequestError('Serverlə əlaqə qurmaq mümkün olmadı. Bir qədər sonra yenidən yoxlayın.', technicalCause)
  }

  let result: ApiResponse<BookingCreatedApiModel>
  try {
    result = (await response.json()) as ApiResponse<BookingCreatedApiModel>
  } catch (technicalCause) {
    throw new BookingRequestError('Serverdən düzgün cavab alınmadı.', technicalCause)
  }
  if (!response.ok || !result.success || !result.data) {
    const apiError = result.error || 'Rezervasiya sorğusu göndərilmədi.'
    const message = apiError.toLowerCase().includes('date conflict')
      ? 'Seçilmiş tarixlərdən biri artıq rezervasiya olunub. Başqa tarix seçin.'
      : apiError
    throw new BookingRequestError(message, new Error(apiError))
  }

  return {
    id: result.data.bookingId,
    demo: false,
    rentalHomeId: result.data.rentalHomeId,
    statusCode: result.data.statusCode,
    statusName: result.data.statusName,
    dailyPrice: result.data.dailyPrice,
    totalAmount: result.data.totalAmount,
    dates: result.data.dates,
    customerName: result.data.customerName,
    phone: result.data.phone,
    createdAt: result.data.createdAt,
  }
}
