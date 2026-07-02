import { getMockBrokerBookings, mockDeposits, persistMockDeposits, type DepositInfo } from './broker'
import type { ApiResponse } from '../types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
const useLiveApi = import.meta.env.VITE_USE_LIVE_API === 'true'

export type AccountBooking = {
  bookingId: number
  rentalHomeTitle: string
  city: string
  district?: string | null
  statusCode: string
  statusName: string
  totalAmount: number
  dates: string[]
  deposit?: DepositInfo | null
  createdAt: string
}

export type AccountBookingDetail = AccountBooking & {
  rentalHomeId: number
  dailyPrice: number
  guests: number
  note?: string | null
}

export class AccountRequestError extends Error {
  constructor(message: string, readonly technicalCause?: unknown) { super(message); this.name = 'AccountRequestError' }
}

async function request<T>(path: string, token: string, init?: RequestInit): Promise<T> {
  let response: Response
  try { response = await fetch(`${baseUrl}${path}`, { ...init, headers: { Authorization: `Bearer ${token}`, ...init?.headers } }) }
  catch (technicalCause) { throw new AccountRequestError('Serverlə əlaqə qurmaq mümkün olmadı.', technicalCause) }
  let payload: ApiResponse<T>
  try { payload = await response.json() as ApiResponse<T> }
  catch (technicalCause) { throw new AccountRequestError('Serverdən düzgün cavab alınmadı.', technicalCause) }
  if (!response.ok || !payload.success || payload.data === undefined) throw new AccountRequestError(payload.error || 'Hesab məlumatları yüklənmədi.', new Error(payload.error))
  return payload.data
}

function mockBooking(id: number): AccountBookingDetail | undefined {
  const booking = getMockBrokerBookings().find((item) => item.bookingId === id)
  if (!booking) return undefined
  const dates = booking.datesCount === 3 && booking.firstDate && booking.lastDate ? [booking.firstDate, '2026-07-13', booking.lastDate] : [booking.firstDate, booking.lastDate].filter(Boolean) as string[]
  const deposit = mockDeposits[id]
  const statusCode = deposit?.statusCode === 'approved' ? 'confirmed' : deposit ? 'waiting_deposit' : booking.statusCode
  return { bookingId: booking.bookingId, rentalHomeId: booking.rentalHomeId, rentalHomeTitle: booking.rentalHomeTitle, city: booking.rentalHomeId === 1 ? 'Qəbələ' : 'İsmayıllı', statusCode, statusName: statusCode === 'confirmed' ? 'Confirmed' : statusCode === 'waiting_deposit' ? 'WaitingDeposit' : booking.statusName, totalAmount: booking.totalAmount, dailyPrice: booking.totalAmount / booking.datesCount, dates, guests: 4, note: booking.note, deposit: deposit ?? null, createdAt: booking.createdAt }
}

export async function getAccountBookings(token: string): Promise<AccountBooking[]> {
  if (!useLiveApi) return getMockBrokerBookings().map((item) => mockBooking(item.bookingId)!).filter(Boolean)
  return request('/api/account/bookings', token)
}

export async function getAccountBooking(id: number, token: string): Promise<AccountBookingDetail> {
  if (!useLiveApi) {
    const booking = mockBooking(id)
    if (!booking) throw new AccountRequestError('Rezervasiya tapılmadı.')
    return booking
  }
  return request(`/api/account/bookings/${id}`, token)
}

export async function uploadDepositReceipt(id: number, file: File, token: string): Promise<DepositInfo> {
  if (!useLiveApi) {
    const deposit = mockDeposits[id]
    if (!deposit) throw new AccountRequestError('Beh sorğusu tapılmadı.')
    if (deposit.statusCode !== 'requested' && !(deposit.statusCode === 'rejected' && deposit.allowReupload)) throw new AccountRequestError('Bu status üçün qəbz yükləmək mümkün deyil.')
    deposit.statusCode = 'receipt_uploaded'; deposit.uploadedAt = new Date().toISOString(); deposit.receipt = { id: 9000 + id, fileName: file.name, fileUrl: `${import.meta.env.BASE_URL}images/ismayilli-cottage.webp`, contentType: file.type, sizeBytes: file.size }
    persistMockDeposits()
    return deposit
  }
  const body = new FormData(); body.append('file', file)
  return request(`/api/account/bookings/${id}/deposit/receipt`, token, { method: 'POST', body })
}
