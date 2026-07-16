import type { ApiResponse } from '../types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
const useLiveApi = import.meta.env.VITE_USE_LIVE_API === 'true'

export type AdminNotification = {
  id: number
  type: string
  channel: string
  status: string
  recipientUserId?: number | null
  recipientName?: string | null
  recipientPhone: string
  title: string
  message: string
  scheduledAt?: string | null
  sentAt?: string | null
  deliveryAttemptCount?: number
  lastAttemptAt?: string | null
  nextAttemptAt?: string | null
  providerMessageId?: string | null
  providerDeliveryStatus?: string | null
  providerStatusUpdatedAt?: string | null
  deliveredAt?: string | null
  readAt?: string | null
  errorMessage?: string | null
  relatedBookingId?: number | null
  relatedDepositId?: number | null
  createdAt: string
}

export type ProcessPendingNotificationsResult = {
  processed: number
  sent: number
  failed: number
  retried?: number
}

export type AdminNotificationFilters = {
  status?: string
  type?: string
  bookingId?: string
}

export class AdminRequestError extends Error {
  constructor(message: string, readonly technicalCause?: unknown) {
    super(message)
    this.name = 'AdminRequestError'
  }
}

let mockNotifications: AdminNotification[] = [
  {
    id: 9101,
    type: 'booking_created',
    channel: 'whatsapp',
    status: 'pending',
    recipientUserId: 2,
    recipientName: 'Demo Broker',
    recipientPhone: '+994501000010',
    title: 'Yeni rezervasiya sorğusu',
    message: 'Qəbələ hovuzlu ev üçün yeni rezervasiya sorğusu yaradıldı.',
    scheduledAt: '2020-01-01T10:20:00Z',
    sentAt: null,
    providerMessageId: null,
    errorMessage: null,
    relatedBookingId: 1001,
    relatedDepositId: null,
    createdAt: '2026-07-08T10:19:00Z',
  },
  {
    id: 9102,
    type: 'deposit_requested',
    channel: 'whatsapp',
    status: 'pending',
    recipientUserId: 3,
    recipientName: 'Aysel Məmmədova',
    recipientPhone: '+994505551212',
    title: 'Beh ödənişi tələb olunur',
    message: 'Rezervasiyanı təsdiqləmək üçün beh qəbzini yükləyin.',
    scheduledAt: '2020-01-01T11:00:00Z',
    sentAt: null,
    providerMessageId: null,
    errorMessage: null,
    relatedBookingId: 1001,
    relatedDepositId: 5001,
    createdAt: '2026-07-08T10:55:00Z',
  },
  {
    id: 9103,
    type: 'deposit_approved',
    channel: 'whatsapp',
    status: 'sent',
    recipientUserId: 3,
    recipientName: 'Murad Əliyev',
    recipientPhone: '+994704442211',
    title: 'Beh təsdiqləndi',
    message: 'Beh qəbziniz təsdiqləndi və rezervasiyanız aktivdir.',
    scheduledAt: '2026-07-07T15:00:00Z',
    sentAt: '2026-07-07T15:01:00Z',
    providerMessageId: 'fake-9103',
    errorMessage: null,
    relatedBookingId: 1002,
    relatedDepositId: 5002,
    createdAt: '2026-07-07T14:58:00Z',
  },
  {
    id: 9104,
    type: 'booking_status_changed',
    channel: 'sms',
    status: 'failed',
    recipientUserId: 3,
    recipientName: 'Leyla Həsənli',
    recipientPhone: '+994552223344',
    title: 'Rezervasiya statusu dəyişdi',
    message: 'Rezervasiya statusu yeniləndi, amma SMS provider cavab vermədi.',
    scheduledAt: '2026-07-06T09:30:00Z',
    sentAt: null,
    providerMessageId: null,
    errorMessage: 'Demo provider xətası.',
    relatedBookingId: 1003,
    relatedDepositId: null,
    createdAt: '2026-07-06T09:28:00Z',
  },
  {
    id: 9105,
    type: 'booking_cancellation_requested',
    channel: 'whatsapp',
    status: 'pending',
    recipientUserId: 2,
    recipientName: 'Demo Broker',
    recipientPhone: '+994501000010',
    title: 'FAIL_FAKE_PROVIDER',
    message: 'Fake provider uğursuzluğunu yoxlamaq üçün demo bildiriş.',
    scheduledAt: '2020-01-01T12:00:00Z',
    sentAt: null,
    providerMessageId: null,
    errorMessage: null,
    relatedBookingId: 1004,
    relatedDepositId: null,
    createdAt: '2026-07-08T11:59:00Z',
  },
  {
    id: 9106,
    type: 'deposit_deadline_reminder',
    channel: 'whatsapp',
    status: 'pending',
    recipientUserId: 3,
    recipientName: 'Gələcək Müştəri',
    recipientPhone: '+994509990000',
    title: 'Beh deadline xatırlatması',
    message: 'Bu future scheduled demo bildirişidir.',
    scheduledAt: '2099-01-01T10:00:00Z',
    sentAt: null,
    providerMessageId: null,
    errorMessage: null,
    relatedBookingId: 1005,
    relatedDepositId: 5005,
    createdAt: '2026-07-08T12:05:00Z',
  },
]

export async function getAdminNotifications(token: string, filters: AdminNotificationFilters = {}): Promise<AdminNotification[]> {
  if (!useLiveApi) return applyMockNotificationFilters(filters)

  const search = new URLSearchParams()
  if (filters.status?.trim()) search.set('status', filters.status.trim())
  if (filters.type?.trim()) search.set('type', filters.type.trim())
  if (filters.bookingId?.trim()) search.set('bookingId', filters.bookingId.trim())
  const query = search.toString()

  let response: Response
  try {
    response = await fetch(`${baseUrl}/api/admin/notifications${query ? `?${query}` : ''}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
  } catch (technicalCause) {
    throw new AdminRequestError('Serverlə əlaqə qurmaq mümkün olmadı.', technicalCause)
  }

  let payload: ApiResponse<AdminNotification[]>
  try {
    payload = await response.json() as ApiResponse<AdminNotification[]>
  } catch (technicalCause) {
    throw new AdminRequestError('Serverdən düzgün cavab alınmadı.', technicalCause)
  }

  if (!response.ok || !payload.success || !payload.data) {
    const message = response.status === 401 || response.status === 403
      ? 'Bildirişlərə baxmaq üçün admin icazəsi tələb olunur.'
      : payload.error || 'Bildirişlər yüklənmədi.'
    throw new AdminRequestError(message, new Error(payload.error))
  }

  return payload.data
}

export async function processPendingNotifications(token: string, batchSize: number): Promise<ProcessPendingNotificationsResult> {
  if (!useLiveApi) return processMockPendingNotifications(batchSize)

  let response: Response
  try {
    response = await fetch(`${baseUrl}/api/admin/notifications/process-pending`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({ batchSize }),
    })
  } catch (technicalCause) {
    throw new AdminRequestError('Serverlə əlaqə qurmaq mümkün olmadı.', technicalCause)
  }

  let payload: ApiResponse<ProcessPendingNotificationsResult>
  try {
    payload = await response.json() as ApiResponse<ProcessPendingNotificationsResult>
  } catch (technicalCause) {
    throw new AdminRequestError('Serverdən düzgün cavab alınmadı.', technicalCause)
  }

  if (!response.ok || !payload.success || !payload.data) {
    const message = response.status === 401 || response.status === 403
      ? 'Bildirişləri emal etmək üçün admin icazəsi tələb olunur.'
      : payload.error || 'Pending bildirişlər emal edilmədi.'
    throw new AdminRequestError(message, new Error(payload.error))
  }

  return payload.data
}

export async function retryAdminNotification(token: string, id: number): Promise<ProcessPendingNotificationsResult> {
  if (!useLiveApi) return retryMockNotification(id)

  let response: Response
  try {
    response = await fetch(`${baseUrl}/api/admin/notifications/${id}/retry`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    })
  } catch (technicalCause) {
    throw new AdminRequestError('Serverlə əlaqə qurmaq mümkün olmadı.', technicalCause)
  }

  let payload: ApiResponse<ProcessPendingNotificationsResult>
  try {
    payload = await response.json() as ApiResponse<ProcessPendingNotificationsResult>
  } catch (technicalCause) {
    throw new AdminRequestError('Serverdən düzgün cavab alınmadı.', technicalCause)
  }

  if (!response.ok || !payload.success || !payload.data) {
    const message = response.status === 401 || response.status === 403
      ? 'Bildirişi yenidən emal etmək üçün admin icazəsi tələb olunur.'
      : payload.error || 'Bildiriş yenidən emal edilmədi.'
    throw new AdminRequestError(message, new Error(payload.error))
  }

  return payload.data
}

function applyMockNotificationFilters(filters: AdminNotificationFilters) {
  return mockNotifications.filter((item) => {
    if (filters.status?.trim() && item.status !== filters.status.trim()) return false
    if (filters.type?.trim() && item.type !== filters.type.trim()) return false
    if (filters.bookingId?.trim() && `${item.relatedBookingId ?? ''}` !== filters.bookingId.trim()) return false
    return true
  })
}

function processMockPendingNotifications(batchSize: number): ProcessPendingNotificationsResult {
  const now = Date.now()
  const due = mockNotifications
    .filter((item) => item.status === 'pending' && (!item.scheduledAt || new Date(item.scheduledAt).getTime() <= now))
    .sort((left, right) => (left.scheduledAt ?? left.createdAt).localeCompare(right.scheduledAt ?? right.createdAt))
    .slice(0, Math.min(Math.max(batchSize, 1), 100))

  let sent = 0
  let failed = 0
  const processedAt = new Date().toISOString()
  const dueIds = new Set(due.map((item) => item.id))
  mockNotifications = mockNotifications.map((item) => {
    if (!dueIds.has(item.id)) return item
    const content = `${item.title} ${item.message} ${item.recipientName ?? ''} ${item.recipientPhone}`
    if (content.toUpperCase().includes('FAIL_FAKE_PROVIDER')) {
      failed += 1
      return {
        ...item,
        status: 'failed',
        errorMessage: 'Fake provider failure marker was found.',
        providerMessageId: null,
        lastAttemptAt: processedAt,
        nextAttemptAt: null,
        deliveryAttemptCount: (item.deliveryAttemptCount ?? 0) + 1,
      }
    }
    sent += 1
    return {
      ...item,
      status: 'sent',
      sentAt: processedAt,
      providerMessageId: `fake-${item.id}`,
      providerDeliveryStatus: 'sent',
      errorMessage: null,
      lastAttemptAt: processedAt,
      nextAttemptAt: null,
      deliveryAttemptCount: (item.deliveryAttemptCount ?? 0) + 1,
    }
  })

  return { processed: due.length, sent, failed, retried: 0 }
}

function retryMockNotification(id: number): ProcessPendingNotificationsResult {
  const item = mockNotifications.find((notification) => notification.id === id)
  if (!item) throw new AdminRequestError('Bildiriş tapılmadı.')

  const processedAt = new Date().toISOString()
  const content = `${item.title} ${item.message} ${item.recipientName ?? ''} ${item.recipientPhone}`
  const failed = content.toUpperCase().includes('FAIL_FAKE_PROVIDER')

  mockNotifications = mockNotifications.map((notification) => {
    if (notification.id !== id) return notification
    return {
      ...notification,
      status: failed ? 'failed' : 'sent',
      sentAt: failed ? null : processedAt,
      providerMessageId: failed ? null : `fake-${notification.id}`,
      providerDeliveryStatus: failed ? null : 'sent',
      errorMessage: failed ? 'Fake provider failure marker was found.' : null,
      lastAttemptAt: processedAt,
      nextAttemptAt: null,
      deliveryAttemptCount: (notification.deliveryAttemptCount ?? 0) + 1,
    }
  })

  return { processed: 1, sent: failed ? 0 : 1, failed: failed ? 1 : 0, retried: 1 }
}
