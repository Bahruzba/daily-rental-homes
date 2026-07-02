import { CalendarDays, CreditCard, RefreshCw } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getAccountBookings, type AccountBooking } from '../api/account'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'
import { EmptyState } from '../components/EmptyState'

const money = new Intl.NumberFormat('az-AZ', { style: 'currency', currency: 'AZN' })
const depositLabels: Record<string, string> = { requested: 'Qəbz gözlənilir', receipt_uploaded: 'Qəbz yoxlanılır', approved: 'Beh təsdiqlənib', rejected: 'Yenidən yükləyin' }

export function AccountDashboardPage() {
  const { session } = useAuth(); const [bookings, setBookings] = useState<AccountBooking[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('')
  const load = async () => { if (!session) return; setLoading(true); setError(''); try { setBookings(await getAccountBookings(session.accessToken)) } catch (cause) { console.error('Account bookings load failed', cause); setError(cause instanceof Error ? cause.message : 'Rezervasiyalar yüklənmədi.') } finally { setLoading(false) } }
  useEffect(() => { void load() }, [session?.accessToken])
  return <AppLayout><section className="account-page"><div className="container"><div className="broker-live-heading"><div><span className="eyebrow">MÜŞTƏRİ HESABI</span><h1>Rezervasiyalarım</h1><p>Beh təlimatları və qəbz vəziyyətini buradan izləyin.</p></div><button className="button button-ghost" onClick={() => void load()}><RefreshCw size={16} /> Yenilə</button></div>{error && <div className="broker-error" role="alert">{error}</div>}{loading ? <div className="broker-loading">Rezervasiyalar yüklənir…</div> : bookings.length ? <div className="account-bookings-grid">{bookings.map((booking) => <Link to={`/account/bookings/${booking.bookingId}`} key={booking.bookingId}><div><span>Rezervasiya #{booking.bookingId}</span><h2>{booking.rentalHomeTitle}</h2><p>{booking.city}{booking.district ? ` · ${booking.district}` : ''}</p></div><div className="account-booking-meta"><span><CalendarDays /> {booking.dates.length} gecə</span><strong>{money.format(booking.totalAmount)}</strong>{booking.deposit ? <em><CreditCard /> {depositLabels[booking.deposit.statusCode] ?? booking.deposit.statusCode}</em> : <small>Brokerin beh sorğusu gözlənilir</small>}</div></Link>)}</div> : <EmptyState title="Rezervasiya yoxdur" description="Telefon nömrənizə bağlı rezervasiyalar burada görünəcək." />}</div></section></AppLayout>
}
