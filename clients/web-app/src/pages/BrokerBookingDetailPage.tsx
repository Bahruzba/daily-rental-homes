import { ArrowLeft, CalendarDays, CheckCircle2, Phone, UserRound, XCircle } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { changeBrokerBookingStatus, getBrokerBooking, type BrokerBookingDetail } from '../api/broker'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'

const money = new Intl.NumberFormat('az-AZ', { style: 'currency', currency: 'AZN' })
const dateTime = new Intl.DateTimeFormat('az-AZ', { dateStyle: 'medium', timeStyle: 'short' })

const actions: Record<string, Array<{ code: string; label: string }>> = {
  pending: [{ code: 'waiting_deposit', label: 'Beh gözləyir' }, { code: 'cancelled', label: 'Ləğv et' }],
  waiting_deposit: [{ code: 'confirmed', label: 'Təsdiqlə' }, { code: 'cancelled', label: 'Ləğv et' }],
}

export function BrokerBookingDetailPage() {
  const { id } = useParams()
  const { session } = useAuth()
  const bookingId = Number(id)
  const [booking, setBooking] = useState<BrokerBookingDetail>()
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const load = async () => {
    if (!session) return
    if (!Number.isInteger(bookingId) || bookingId <= 0) {
      setError('Rezervasiya nömrəsi düzgün deyil.')
      setLoading(false)
      return
    }
    setLoading(true); setError('')
    try { setBooking(await getBrokerBooking(bookingId, session.accessToken)) }
    catch (cause) { console.error('Broker booking detail load failed', cause); setError(cause instanceof Error ? cause.message : 'Rezervasiya yüklənmədi.') }
    finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [bookingId, session?.accessToken])

  const changeStatus = async (statusCode: string) => {
    if (!session) return
    setSaving(true); setError('')
    try { await changeBrokerBookingStatus(bookingId, statusCode, session.accessToken); await load() }
    catch (cause) { console.error('Broker booking status change failed', cause); setError(cause instanceof Error ? cause.message : 'Status dəyişdirilmədi.') }
    finally { setSaving(false) }
  }

  return <AppLayout><section className="broker-detail-page"><div className="container">
    <Link className="back-link" to="/broker"><ArrowLeft size={16} /> Broker panelinə qayıt</Link>
    {loading ? <div className="broker-loading">Rezervasiya yüklənir…</div> : error && !booking ? <div className="broker-error" role="alert">{error}</div> : booking && <>
      <div className="broker-detail-heading"><div><span className="eyebrow">REZERVASİYA #{booking.bookingId}</span><h1>{booking.rentalHome.title}</h1><p>{booking.rentalHome.city}{booking.rentalHome.district ? ` · ${booking.rentalHome.district}` : ''}</p></div><div><em className={`broker-status status-${booking.status.code}`}>{booking.status.name}</em><strong>{money.format(booking.totalAmount)}</strong></div></div>
      {error && <div className="broker-error" role="alert">{error}</div>}
      <div className="broker-detail-layout"><div className="broker-detail-main">
        <article><h2>Müştəri məlumatları</h2><p><UserRound /> <span><strong>{booking.customer.fullName}</strong><small>{booking.guests} qonaq</small></span></p><p><Phone /> <a href={`tel:${booking.customer.phone}`}>{booking.customer.phone}</a></p></article>
        <article><h2>Seçilmiş tarixlər</h2><div className="broker-date-chips">{booking.dates.map((date) => <span key={date}><CalendarDays /> {date}</span>)}</div></article>
        <article><h2>Qeyd</h2><p className="broker-note">{booking.note || 'Müştəri qeyd əlavə etməyib.'}</p></article>
      </div><aside className="broker-action-card"><span>Gecəlik qiymət</span><strong>{money.format(booking.dailyPrice)}</strong><span>Yaradılma vaxtı</span><strong>{dateTime.format(new Date(booking.createdAt))}</strong><hr /><h2>Status əməliyyatları</h2>{actions[booking.status.code]?.length ? actions[booking.status.code].map((action) => <button className={`button button-full ${action.code === 'cancelled' ? 'button-ghost' : 'button-primary'}`} disabled={saving} onClick={() => void changeStatus(action.code)} key={action.code}>{action.code === 'cancelled' ? <XCircle size={16} /> : <CheckCircle2 size={16} />}{action.label}</button>) : <p>Bu status üçün əlavə əməliyyat yoxdur.</p>}</aside></div>
    </>}
  </div></section></AppLayout>
}
