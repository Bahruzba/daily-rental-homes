import { ArrowLeft, Bell, RefreshCw } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { getAdminNotifications, processPendingNotifications, retryAdminNotification, type AdminNotification } from '../api/admin'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'
import { EmptyState } from '../components/EmptyState'

const dateTime = new Intl.DateTimeFormat('az-AZ', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

const statusOptions = ['pending', 'sent', 'failed', 'cancelled', 'skipped']
const typeOptions = ['booking_created', 'booking_cancellation_requested', 'deposit_requested', 'deposit_approved', 'deposit_rejected', 'booking_status_changed', 'deposit_deadline_reminder', 'deposit_deadline_extended']

export function AdminNotificationsPage() {
  const { session } = useAuth()
  const [notifications, setNotifications] = useState<AdminNotification[]>([])
  const [status, setStatus] = useState('')
  const [type, setType] = useState('')
  const [bookingId, setBookingId] = useState('')
  const [loading, setLoading] = useState(true)
  const [processing, setProcessing] = useState(false)
  const [retryingId, setRetryingId] = useState<number | null>(null)
  const [batchSize, setBatchSize] = useState('20')
  const [error, setError] = useState('')
  const [processMessage, setProcessMessage] = useState('')

  const load = async (next = { status, type, bookingId }) => {
    if (!session) return
    const validation = validateBookingId(next.bookingId)
    if (validation) {
      setError(validation)
      return
    }

    setLoading(true)
    setError('')
    try {
      setNotifications(await getAdminNotifications(session.accessToken, next))
    } catch (cause) {
      console.error('Admin notifications load failed', cause)
      setError(cause instanceof Error ? cause.message : 'Bildirişlər yüklənmədi.')
    } finally {
      setLoading(false)
    }
  }

  const clearFilters = () => {
    setStatus('')
    setType('')
    setBookingId('')
    void load({ status: '', type: '', bookingId: '' })
  }

  const processPending = async () => {
    if (!session) return
    const value = Number(batchSize)
    if (!Number.isInteger(value) || value < 1 || value > 100) {
      setError('Batch sayı 1 və 100 arasında olmalıdır.')
      return
    }

    setProcessing(true)
    setError('')
    setProcessMessage('')
    try {
      const result = await processPendingNotifications(session.accessToken, value)
      setProcessMessage(`Emal edildi: ${result.processed}, göndərildi: ${result.sent}, uğursuz: ${result.failed}`)
      await load()
    } catch (cause) {
      console.error('Admin notification processing failed', cause)
      setError(cause instanceof Error ? cause.message : 'Pending bildirişlər emal edilmədi.')
    } finally {
      setProcessing(false)
    }
  }

  const retryNotification = async (id: number) => {
    if (!session) return
    setRetryingId(id)
    setError('')
    setProcessMessage('')
    try {
      const result = await retryAdminNotification(session.accessToken, id)
      setProcessMessage(`Yenidən emal edildi: ${result.processed}, göndərildi: ${result.sent}, uğursuz: ${result.failed}, retry: ${result.retried ?? 0}`)
      await load()
    } catch (cause) {
      console.error('Admin notification retry failed', cause)
      setError(cause instanceof Error ? cause.message : 'Bildiriş yenidən emal edilmədi.')
    } finally {
      setRetryingId(null)
    }
  }

  useEffect(() => { void load({ status: '', type: '', bookingId: '' }) }, [session?.accessToken])

  const counts = useMemo(() => ({
    pending: notifications.filter((item) => item.status === 'pending').length,
    failed: notifications.filter((item) => item.status === 'failed').length,
    sent: notifications.filter((item) => item.status === 'sent').length,
  }), [notifications])

  return (
    <AppLayout>
      <section className="admin-notifications-page">
        <div className="container">
          <div className="role-dashboard-heading">
            <div>
              <span className="eyebrow">ADMİN PANELİ</span>
              <h1>Bildirişlər</h1>
              <p>WhatsApp/SMS üçün növbəyə alınmış notification outbox mesajları.</p>
            </div>
            <Link className="button button-ghost" to="/admin"><ArrowLeft size={17} /> Admin panelə qayıt</Link>
          </div>

          <div className="admin-notification-summary">
            <article><Bell /><span>Cəmi bildiriş</span><strong>{notifications.length}</strong></article>
            <article><span className="status-dot pending" /><span>Növbədə</span><strong>{counts.pending}</strong></article>
            <article><span className="status-dot sent" /><span>Göndərilib</span><strong>{counts.sent}</strong></article>
            <article><span className="status-dot failed" /><span>Uğursuz</span><strong>{counts.failed}</strong></article>
          </div>

          <div className="admin-delivery-panel">
            <div>
              <h2>Bildirişləri göndər</h2>
              <p>Vaxtı çatmış pending bildirişləri fake provider ilə emal edir.</p>
            </div>
            <label>
              <span>Batch sayı</span>
              <input type="number" min="1" max="100" value={batchSize} onChange={(event) => setBatchSize(event.target.value)} />
            </label>
            <button className="button button-primary" onClick={() => void processPending()} disabled={processing || loading}>
              <RefreshCw size={16} /> {processing ? 'Emal edilir…' : 'Pending bildirişləri emal et'}
            </button>
          </div>

          {processMessage && <div className="account-success admin-process-success" role="status">{processMessage}</div>}

          <div className="admin-notification-filters">
            <label>
              <span>Status</span>
              <select value={status} onChange={(event) => setStatus(event.target.value)}>
                <option value="">Hamısı</option>
                {statusOptions.map((item) => <option value={item} key={item}>{statusLabel(item)}</option>)}
              </select>
            </label>
            <label>
              <span>Tip</span>
              <select value={type} onChange={(event) => setType(event.target.value)}>
                <option value="">Hamısı</option>
                {typeOptions.map((item) => <option value={item} key={item}>{typeLabel(item)}</option>)}
              </select>
            </label>
            <label>
              <span>Booking ID</span>
              <input value={bookingId} onChange={(event) => setBookingId(event.target.value)} placeholder="Məs: 1001" inputMode="numeric" />
            </label>
            <button className="button button-primary" onClick={() => void load()} disabled={loading}>{loading ? 'Yüklənir…' : 'Tətbiq et'}</button>
            <button className="button button-ghost" onClick={clearFilters} disabled={loading}>Təmizlə</button>
          </div>

          {error && (
            <div className="broker-error admin-notification-error" role="alert">
              <span>{error}</span>
              <button className="button button-ghost" onClick={() => void load()} disabled={loading}>Yenidən yoxla</button>
            </div>
          )}

          {loading ? (
            <div className="broker-loading">Bildirişlər yüklənir…</div>
          ) : notifications.length ? (
            <div className="admin-notification-list">
              {notifications.map((item) => (
                <article key={item.id}>
                  <div className="admin-notification-topline">
                    <div>
                      <span className={`notification-status status-${item.status}`}>{statusLabel(item.status)}</span>
                      <span className="notification-channel">{channelLabel(item.channel)}</span>
                      <small>#{item.id}</small>
                    </div>
                    <time>{formatDateTime(item.createdAt)}</time>
                  </div>
                  <div className="admin-notification-body">
                    <div>
                      <span>{typeLabel(item.type)}</span>
                      <h2>{item.title}</h2>
                      <p>{item.message}</p>
                    </div>
                    <div className="admin-notification-meta">
                      <span>Qəbul edən</span>
                      <strong>{item.recipientName || 'Ad yoxdur'}</strong>
                      <small>{item.recipientPhone}</small>
                    </div>
                  </div>
                  <div className="admin-notification-foot">
                    {item.scheduledAt && <span>Plan: {formatDateTime(item.scheduledAt)}</span>}
                    <span>Cəhd: {item.deliveryAttemptCount ?? 0}</span>
                    {item.lastAttemptAt && <span>Son cəhd: {formatDateTime(item.lastAttemptAt)}</span>}
                    {item.nextAttemptAt && <span>Növbəti cəhd: {formatDateTime(item.nextAttemptAt)}</span>}
                    {item.providerMessageId && <span>Provider ID: {item.providerMessageId}</span>}
                    {item.providerDeliveryStatus && <span>Provider status: {item.providerDeliveryStatus}</span>}
                    {item.providerStatusUpdatedAt && <span>Provider status vaxtı: {formatDateTime(item.providerStatusUpdatedAt)}</span>}
                    {item.deliveredAt && <span>Çatdırılma vaxtı: {formatDateTime(item.deliveredAt)}</span>}
                    {item.readAt && <span>Oxunma vaxtı: {formatDateTime(item.readAt)}</span>}
                    {item.errorMessage && <span className="notification-error-text">Xəta: {item.errorMessage}</span>}
                    {item.sentAt && <span>Göndərilmə vaxtı: {formatDateTime(item.sentAt)}</span>}
                    {item.relatedBookingId && <span>Booking #{item.relatedBookingId}</span>}
                    {item.relatedDepositId && <span>Deposit #{item.relatedDepositId}</span>}
                    {item.status === 'failed' && (
                      <button className="button button-ghost admin-notification-retry" onClick={() => void retryNotification(item.id)} disabled={retryingId === item.id || processing || loading}>
                        <RefreshCw size={14} /> {retryingId === item.id ? 'Yoxlanır…' : 'Yenidən göndər'}
                      </button>
                    )}
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <EmptyState title="Bildiriş yoxdur" description="Seçilmiş filterlərə uyğun outbox mesajı tapılmadı." />
          )}
        </div>
      </section>
    </AppLayout>
  )
}

function validateBookingId(value: string) {
  if (!value.trim()) return ''
  return /^\d+$/.test(value.trim()) ? '' : 'Booking ID yalnız rəqəmlərdən ibarət olmalıdır.'
}

function formatDateTime(value: string) {
  return dateTime.format(new Date(value))
}

function statusLabel(value: string) {
  const labels: Record<string, string> = {
    pending: 'Növbədə',
    sent: 'Göndərilib',
    failed: 'Uğursuz',
    cancelled: 'Ləğv edilib',
    skipped: 'Keçildi',
  }
  return labels[value] ?? value
}

function channelLabel(value: string) {
  const labels: Record<string, string> = { whatsapp: 'WhatsApp', sms: 'SMS', in_app: 'In-app' }
  return labels[value] ?? value
}

function typeLabel(value: string) {
  const labels: Record<string, string> = {
    booking_created: 'Yeni rezervasiya',
    booking_cancellation_requested: 'Ləğv sorğusu',
    deposit_requested: 'Beh istəyi',
    deposit_approved: 'Beh təsdiqi',
    deposit_rejected: 'Beh rəddi',
    booking_status_changed: 'Booking status dəyişikliyi',
    deposit_deadline_reminder: 'Beh deadline xatırlatması',
    deposit_deadline_extended: 'Beh deadline uzadıldı',
  }
  return labels[value] ?? value
}
