import type { ApiResponse } from '../types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
const useLiveApi = import.meta.env.VITE_USE_LIVE_API === 'true'
export function resolveApiAssetUrl(url: string) { return /^https?:|^blob:/i.test(url) ? url : `${baseUrl}${url}` }

export type BrokerSummary = {
  totalHomes: number
  activeHomes: number
  totalBookings: number
  pendingBookings: number
  pendingDepositBookings: number
  upcomingBookings: number
  totalExpectedAmount: number
}

export type BrokerRentalHome = {
  id: number
  title: string
  city: string
  district?: string | null
  address?: string | null
  dailyPrice: number
  guestCount: number
  isPublished: boolean
  mainImageUrl?: string | null
  bookingCount: number
}

export type BrokerBooking = {
  bookingId: number
  rentalHomeId: number
  rentalHomeTitle: string
  customerName: string
  customerPhone: string
  statusCode: string
  statusName: string
  totalAmount: number
  datesCount: number
  firstDate?: string | null
  lastDate?: string | null
  createdAt: string
  note?: string | null
  isDepositPending: boolean
}

export type BrokerBookingDetail = {
  bookingId: number
  rentalHome: { id: number; title: string; city: string; district?: string | null }
  customer: { fullName: string; phone: string }
  status: { code: string; name: string }
  dailyPrice: number
  totalAmount: number
  dates: string[]
  guests: number
  note?: string | null
  createdAt: string
  statusHistory: Array<{ oldStatusCode?: string | null; newStatusCode: string; note?: string | null; changedAt: string }>
  deposit?: DepositInfo | null
}

export type DepositInfo = {
  id: number
  amount: number
  statusCode: string
  deadlineAt?: string | null
  cardHolderName?: string | null
  cardPanMasked?: string | null
  bankName?: string | null
  note?: string | null
  requestedAt: string
  uploadedAt?: string | null
  reviewedAt?: string | null
  reviewNote?: string | null
  allowReupload: boolean
  receipt?: { id: number; fileName: string; fileUrl: string; contentType?: string | null; sizeBytes?: number | null } | null
}

export type RequestDepositPayload = {
  amount: number
  deadlineAt: string
  cardHolderName?: string
  cardPanMasked: string
  bankName?: string
  note?: string
}

export class BrokerRequestError extends Error {
  constructor(message: string, readonly technicalCause?: unknown) {
    super(message)
    this.name = 'BrokerRequestError'
  }
}

const mockHomes: BrokerRentalHome[] = [
  { id: 1, title: 'Dağ mənzərəli hovuzlu bağ evi', city: 'Qəbələ', district: 'Vəndam', dailyPrice: 180, guestCount: 8, isPublished: true, mainImageUrl: `${import.meta.env.BASE_URL}images/qebele-villa.webp`, bookingCount: 3 },
  { id: 2, title: 'Meşə içində sakit kottec', city: 'İsmayıllı', district: 'Lahıc yolu', dailyPrice: 125, guestCount: 5, isPublished: true, mainImageUrl: `${import.meta.env.BASE_URL}images/ismayilli-cottage.webp`, bookingCount: 2 },
]

let mockBookings: BrokerBooking[] = [
  { bookingId: 1001, rentalHomeId: 1, rentalHomeTitle: mockHomes[0].title, customerName: 'Aysel Məmmədova', customerPhone: '+994 50 555 12 12', statusCode: 'pending', statusName: 'Pending', totalAmount: 540, datesCount: 3, firstDate: '2026-07-12', lastDate: '2026-07-14', createdAt: '2026-07-02T10:20:00Z', note: 'Saat 14:00-da gələcəyik.', isDepositPending: false },
  { bookingId: 1002, rentalHomeId: 2, rentalHomeTitle: mockHomes[1].title, customerName: 'Murad Əliyev', customerPhone: '+994 70 444 22 11', statusCode: 'waiting_deposit', statusName: 'WaitingDeposit', totalAmount: 250, datesCount: 2, firstDate: '2026-07-19', lastDate: '2026-07-20', createdAt: '2026-07-01T16:45:00Z', isDepositPending: true },
]

const mockDepositStorageKey = 'daily-homes-mock-deposits'
const defaultMockDeposits: Record<number, DepositInfo | undefined> = {
  1002: { id: 5002, amount: 75, statusCode: 'requested', deadlineAt: '2026-07-10T18:00:00Z', cardHolderName: 'Demo Broker', cardPanMasked: '**** **** **** 2026', bankName: 'Kapital Bank', note: 'Təyinatda booking nömrəsini qeyd edin.', requestedAt: '2026-07-02T12:00:00Z', allowReupload: true, receipt: null },
}

function readMockDeposits() {
  try { return { ...defaultMockDeposits, ...JSON.parse(window.localStorage.getItem(mockDepositStorageKey) ?? '{}') } }
  catch { return { ...defaultMockDeposits } }
}

export const mockDeposits: Record<number, DepositInfo | undefined> = readMockDeposits()
export function persistMockDeposits() { window.localStorage.setItem(mockDepositStorageKey, JSON.stringify(mockDeposits)) }

export function getMockBrokerBookings() { return mockBookings }

async function request<T>(path: string, token: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${baseUrl}${path}`, {
      ...init,
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}`, ...init?.headers },
    })
  } catch (technicalCause) {
    throw new BrokerRequestError('Serverlə əlaqə qurmaq mümkün olmadı.', technicalCause)
  }

  let payload: ApiResponse<T>
  try {
    payload = await response.json() as ApiResponse<T>
  } catch (technicalCause) {
    throw new BrokerRequestError('Serverdən düzgün cavab alınmadı.', technicalCause)
  }

  if (!response.ok || !payload.success || payload.data === undefined) {
    const message = response.status === 401 || response.status === 403
      ? 'Bu məlumatlara baxmaq üçün broker icazəsi tələb olunur.'
      : payload.error || 'Broker məlumatları yüklənmədi.'
    throw new BrokerRequestError(message, new Error(payload.error))
  }
  return payload.data
}

export async function getBrokerSummary(token: string): Promise<BrokerSummary> {
  if (!useLiveApi) return { totalHomes: 2, activeHomes: 2, totalBookings: mockBookings.length, pendingBookings: mockBookings.filter((item) => item.statusCode === 'pending').length, pendingDepositBookings: mockBookings.filter((item) => item.statusCode === 'waiting_deposit').length, upcomingBookings: mockBookings.length, totalExpectedAmount: mockBookings.filter((item) => item.statusCode !== 'cancelled').reduce((sum, item) => sum + item.totalAmount, 0) }
  return request('/api/broker/summary', token)
}

export async function getBrokerRentalHomes(token: string): Promise<BrokerRentalHome[]> {
  if (!useLiveApi) return mockHomes
  return request('/api/broker/rental-homes', token)
}

export async function getBrokerBookings(token: string): Promise<BrokerBooking[]> {
  if (!useLiveApi) return mockBookings
  return request('/api/broker/bookings', token)
}

export async function getBrokerBooking(id: number, token: string): Promise<BrokerBookingDetail> {
  if (!useLiveApi) {
    const booking = mockBookings.find((item) => item.bookingId === id)
    if (!booking) throw new BrokerRequestError('Rezervasiya tapılmadı.')
    return {
      bookingId: booking.bookingId,
      rentalHome: { id: booking.rentalHomeId, title: booking.rentalHomeTitle, city: mockHomes.find((home) => home.id === booking.rentalHomeId)?.city ?? '' },
      customer: { fullName: booking.customerName, phone: booking.customerPhone },
      status: { code: booking.statusCode, name: booking.statusName },
      dailyPrice: booking.totalAmount / booking.datesCount,
      totalAmount: booking.totalAmount,
      dates: booking.firstDate && booking.lastDate && booking.datesCount === 3 ? [booking.firstDate, '2026-07-13', booking.lastDate] : [booking.firstDate, booking.lastDate].filter(Boolean) as string[],
      guests: 4,
      note: booking.note,
      createdAt: booking.createdAt,
      statusHistory: [],
      deposit: mockDeposits[id] ?? null,
    }
  }
  return request(`/api/broker/bookings/${id}`, token)
}

export async function requestBrokerDeposit(id: number, payload: RequestDepositPayload, token: string): Promise<DepositInfo> {
  if (!useLiveApi) {
    if (mockDeposits[id]) throw new BrokerRequestError('Bu rezervasiya üçün artıq beh sorğusu var.')
    const deposit: DepositInfo = { id: 5000 + id, ...payload, statusCode: 'requested', requestedAt: new Date().toISOString(), allowReupload: true, receipt: null }
    mockDeposits[id] = deposit
    persistMockDeposits()
    await changeBrokerBookingStatus(id, 'waiting_deposit', token)
    return deposit
  }
  return request(`/api/broker/bookings/${id}/deposit/request`, token, { method: 'POST', body: JSON.stringify(payload) })
}

export async function approveBrokerDeposit(id: number, token: string, note?: string): Promise<DepositInfo> {
  if (!useLiveApi) {
    const deposit = mockDeposits[id]
    if (!deposit?.receipt) throw new BrokerRequestError('Təsdiq üçün qəbz yüklənməlidir.')
    deposit.statusCode = 'approved'; deposit.reviewedAt = new Date().toISOString(); deposit.reviewNote = note; deposit.allowReupload = false
    const booking = mockBookings.find((item) => item.bookingId === id)
    if (booking) { booking.statusCode = 'confirmed'; booking.statusName = 'Confirmed' }
    persistMockDeposits()
    return deposit
  }
  return request(`/api/broker/bookings/${id}/deposit/approve`, token, { method: 'POST', body: JSON.stringify({ note }) })
}

export async function rejectBrokerDeposit(id: number, token: string, note?: string): Promise<DepositInfo> {
  if (!useLiveApi) {
    const deposit = mockDeposits[id]
    if (!deposit?.receipt) throw new BrokerRequestError('Rədd etmək üçün qəbz yüklənməlidir.')
    deposit.statusCode = 'rejected'; deposit.reviewedAt = new Date().toISOString(); deposit.reviewNote = note; deposit.allowReupload = true
    persistMockDeposits()
    return deposit
  }
  return request(`/api/broker/bookings/${id}/deposit/reject`, token, { method: 'POST', body: JSON.stringify({ note, allowReupload: true }) })
}

export async function changeBrokerBookingStatus(id: number, statusCode: string, token: string) {
  if (!useLiveApi) {
    const booking = mockBookings.find((item) => item.bookingId === id)
    if (!booking) throw new BrokerRequestError('Rezervasiya tapılmadı.')
    booking.statusCode = statusCode
    booking.statusName = statusCode === 'waiting_deposit' ? 'WaitingDeposit' : statusCode === 'confirmed' ? 'Confirmed' : 'Cancelled'
    booking.isDepositPending = statusCode === 'waiting_deposit'
    mockBookings = [...mockBookings]
    return { bookingId: id, statusCode, statusName: booking.statusName }
  }
  return request<{ bookingId: number; statusCode: string; statusName: string }>(`/api/broker/bookings/${id}/status`, token, { method: 'PATCH', body: JSON.stringify({ statusCode }) })
}
