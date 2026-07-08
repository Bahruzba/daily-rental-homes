import { CalendarDays, CreditCard, RefreshCw } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getAccountBookings, type AccountBooking } from '../api/account'
import { resolveApiAssetUrl } from '../api/broker'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'
import { EmptyState } from '../components/EmptyState'

const money = new Intl.NumberFormat('az-AZ', { style: 'currency', currency: 'AZN' })

const bookingLabels: Record<string, string> = {
  pending: 'Broker təsdiqi gözlənilir',
  confirmed: 'Broker təsdiqlədi',
  waiting_deposit: 'Beh qəbzi lazımdır',
  rejected: 'Rezervasiya rədd edilib',
  cancelled: 'Rezervasiya ləğv edilib',
  completed: 'Tamamlanıb',
}

const depositLabels: Record<string, string> = {
  requested: 'Qəbz yükləyin',
  receipt_uploaded: 'Broker yoxlayır',
  approved: 'Beh təsdiqlənib',
  rejected: 'Qəbz rədd edilib',
  expired: 'Son tarix keçib',
  cancelled: 'Beh ləğv edilib',
}

function nextAction(booking: AccountBooking) {
  if (booking.statusCode === 'rejected' || booking.statusCode === 'cancelled') return 'Əlavə əməliyyat tələb olunmur.'
  if (!booking.deposit) return 'Brokerdən beh təlimatı gözlənilir.'
  if (booking.deposit.statusCode === 'requested') return 'Beh qəbzini yükləyin.'
  if (booking.deposit.statusCode === 'receipt_uploaded') return 'Qəbz broker tərəfindən yoxlanılır.'
  if (booking.deposit.statusCode === 'approved') return 'Beh qəbul edildi.'
  if (booking.deposit.statusCode === 'rejected') return booking.deposit.allowReupload ? 'Qəbzi yenidən yükləyin.' : 'Broker qeydinə baxın.'
  return 'Detallara baxın.'
}

export function AccountDashboardPage() {
  const { session } = useAuth()
  const [bookings, setBookings] = useState<AccountBooking[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = async () => {
    if (!session) return
    setLoading(true)
    setError('')
    try {
      setBookings(await getAccountBookings(session.accessToken))
    } catch (cause) {
      console.error('Account bookings load failed', cause)
      setError(cause instanceof Error ? cause.message : 'Rezervasiyalar yüklənmədi.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [session?.accessToken])

  return (
    <AppLayout>
      <section className="account-page">
        <div className="container">
          <div className="broker-live-heading">
            <div>
              <span className="eyebrow">MÜŞTƏRİ HESABI</span>
              <h1>Rezervasiyalarım</h1>
              <p>Status, seçilmiş tarixlər və beh/qəbz addımlarını buradan izləyin.</p>
            </div>
            <button className="button button-ghost" onClick={() => void load()}>
              <RefreshCw size={16} /> Yenilə
            </button>
          </div>

          {error && <div className="broker-error" role="alert">{error}</div>}

          {loading ? (
            <div className="broker-loading">Rezervasiyalar yüklənir…</div>
          ) : bookings.length ? (
            <div className="account-bookings-grid">
              {bookings.map((booking) => (
                <Link to={`/account/bookings/${booking.bookingId}`} key={booking.bookingId}>
                  {booking.mainImageUrl && <img className="account-booking-thumb" src={resolveApiAssetUrl(booking.mainImageUrl)} alt={booking.rentalHomeTitle} />}
                  <div>
                    <span>Rezervasiya #{booking.bookingId}</span>
                    <h2>{booking.rentalHomeTitle}</h2>
                    <p>{booking.city}{booking.district ? ` · ${booking.district}` : ''}</p>
                    <small className="account-next-action">{nextAction(booking)}</small>
                  </div>
                  <div className="account-booking-meta">
                    <em className={`broker-status status-${booking.statusCode}`}>{bookingLabels[booking.statusCode] ?? booking.statusName}</em>
                    <span><CalendarDays /> {booking.dates.length} gecə</span>
                    <strong>{money.format(booking.totalAmount)}</strong>
                    {booking.deposit ? <small><CreditCard /> {depositLabels[booking.deposit.statusCode] ?? booking.deposit.statusCode}</small> : <small>Beh sorğusu yoxdur</small>}
                  </div>
                </Link>
              ))}
            </div>
          ) : (
            <EmptyState title="Rezervasiya yoxdur" description="Telefon nömrənizə bağlı rezervasiyalar burada görünəcək." />
          )}
        </div>
      </section>
    </AppLayout>
  )
}
