import { Building2, CalendarDays, ClipboardList, Coins, RefreshCw } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getBrokerBookings, getBrokerRentalHomes, getBrokerSummary, type BrokerBooking, type BrokerRentalHome, type BrokerSummary } from '../api/broker'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'
import { EmptyState } from '../components/EmptyState'

const money = new Intl.NumberFormat('az-AZ', { style: 'currency', currency: 'AZN', maximumFractionDigits: 0 })
const date = new Intl.DateTimeFormat('az-AZ', { day: '2-digit', month: 'short', year: 'numeric' })

export function BrokerDashboardPage() {
  const { session } = useAuth()
  const [summary, setSummary] = useState<BrokerSummary>()
  const [homes, setHomes] = useState<BrokerRentalHome[]>([])
  const [bookings, setBookings] = useState<BrokerBooking[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = async () => {
    if (!session) return
    setLoading(true)
    setError('')
    try {
      const [nextSummary, nextHomes, nextBookings] = await Promise.all([
        getBrokerSummary(session.accessToken), getBrokerRentalHomes(session.accessToken), getBrokerBookings(session.accessToken),
      ])
      setSummary(nextSummary); setHomes(nextHomes); setBookings(nextBookings)
    } catch (cause) {
      console.error('Broker dashboard load failed', cause)
      setError(cause instanceof Error ? cause.message : 'Broker paneli yüklənmədi.')
    } finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [session?.accessToken])

  return <AppLayout><section className="broker-live-page"><div className="container">
    <div className="broker-live-heading"><div><span className="eyebrow">BROKER PANELİ</span><h1>Salam, {session?.user.fullName}</h1><p>Evləriniz və rezervasiya sorğularınızın real xülasəsi.</p></div><button className="button button-ghost" onClick={() => void load()} disabled={loading}><RefreshCw size={16} /> Yenilə</button></div>
    {error && <div className="broker-error" role="alert"><span>{error}</span><button className="button button-ghost" onClick={() => void load()}>Yenidən yoxla</button></div>}
    {loading ? <div className="broker-loading">Broker məlumatları yüklənir…</div> : <>
      {summary && <div className="broker-summary-grid">
        <article><Building2 /><span>Aktiv evlər</span><strong>{summary.activeHomes} / {summary.totalHomes}</strong></article>
        <article><ClipboardList /><span>Rezervasiyalar</span><strong>{summary.totalBookings}</strong><small>{summary.pendingBookings} yeni sorğu</small></article>
        <article><CalendarDays /><span>Yaxın rezervasiyalar</span><strong>{summary.upcomingBookings}</strong><small>{summary.pendingDepositBookings} beh gözləyir</small></article>
        <article><Coins /><span>Gözlənilən məbləğ</span><strong>{money.format(summary.totalExpectedAmount)}</strong></article>
      </div>}
      <div className="broker-section-heading"><div><h2>Evlərim</h2><p>Broker hesabınıza bağlı elanlar.</p></div></div>
      {homes.length ? <div className="broker-homes-grid">{homes.map((home) => <article key={home.id}>{home.mainImageUrl ? <img src={home.mainImageUrl} alt={home.title} /> : <div className="broker-home-placeholder"><Building2 /></div>}<div><span>{home.city}{home.district ? ` · ${home.district}` : ''}</span><h3>{home.title}</h3><p>{home.guestCount} qonaq · {home.bookingCount} rezervasiya</p><strong>{money.format(home.dailyPrice)} / gecə</strong></div><em className={home.isPublished ? 'is-active' : ''}>{home.isPublished ? 'Aktiv' : 'Gizli'}</em></article>)}</div> : <EmptyState title="Brokerə bağlı ev yoxdur" description="Ev əlavə edildikdə burada görünəcək." />}
      <div className="broker-section-heading"><div><h2>Son rezervasiyalar</h2><p>Ən yeni sorğular əvvəl göstərilir.</p></div></div>
      {bookings.length ? <div className="broker-bookings-list">{bookings.map((booking) => <Link to={`/broker/bookings/${booking.bookingId}`} key={booking.bookingId}><div><span>#{booking.bookingId} · {booking.rentalHomeTitle}</span><strong>{booking.customerName}</strong><small>{booking.firstDate ? date.format(new Date(`${booking.firstDate}T00:00:00`)) : 'Tarix yoxdur'} · {booking.datesCount} gecə</small></div><div><em className={`broker-status status-${booking.statusCode}`}>{booking.statusName}</em><strong>{money.format(booking.totalAmount)}</strong></div></Link>)}</div> : <EmptyState title="Rezervasiya yoxdur" description="Yeni sorğular burada görünəcək." />}
    </>}
  </div></section></AppLayout>
}
