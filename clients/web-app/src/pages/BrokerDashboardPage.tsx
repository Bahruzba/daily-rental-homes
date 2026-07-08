import { Building2, CalendarDays, ClipboardList, Coins, RefreshCw, WalletCards } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  getBrokerBookings,
  getBrokerRentalHomes,
  getBrokerReportSummary,
  getBrokerSummary,
  type BrokerBooking,
  type BrokerRentalHome,
  type BrokerReportSummary,
  type BrokerSummary,
} from '../api/broker'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'
import { EmptyState } from '../components/EmptyState'

const money = new Intl.NumberFormat('az-AZ', { style: 'currency', currency: 'AZN', maximumFractionDigits: 0 })
const date = new Intl.DateTimeFormat('az-AZ', { day: '2-digit', month: 'short', year: 'numeric' })

export function BrokerDashboardPage() {
  const { session } = useAuth()
  const [summary, setSummary] = useState<BrokerSummary>()
  const [reportSummary, setReportSummary] = useState<BrokerReportSummary>()
  const [homes, setHomes] = useState<BrokerRentalHome[]>([])
  const [bookings, setBookings] = useState<BrokerBooking[]>([])
  const [reportFrom, setReportFrom] = useState('')
  const [reportTo, setReportTo] = useState('')
  const [reportLoading, setReportLoading] = useState(false)
  const [reportError, setReportError] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = async () => {
    if (!session) return
    setLoading(true)
    setError('')
    setReportError('')
    try {
      const [nextSummary, nextReportSummary, nextHomes, nextBookings] = await Promise.all([
        getBrokerSummary(session.accessToken),
        getBrokerReportSummary(session.accessToken),
        getBrokerRentalHomes(session.accessToken),
        getBrokerBookings(session.accessToken),
      ])
      setSummary(nextSummary)
      setReportSummary(nextReportSummary)
      setHomes(nextHomes)
      setBookings(nextBookings)
    } catch (cause) {
      console.error('Broker dashboard load failed', cause)
      setError(cause instanceof Error ? cause.message : 'Broker paneli yüklənmədi.')
    } finally {
      setLoading(false)
    }
  }

  const loadReportSummary = async (from = reportFrom, to = reportTo) => {
    if (!session) return
    const validation = validateReportRange(from, to)
    if (validation) {
      setReportError(validation)
      return
    }

    setReportLoading(true)
    setReportError('')
    try {
      setReportSummary(await getBrokerReportSummary(session.accessToken, from && to ? { from, to } : {}))
    } catch (cause) {
      console.error('Broker report summary load failed', cause)
      setReportError(cause instanceof Error ? cause.message : 'Hesabat xülasəsi yüklənmədi.')
    } finally {
      setReportLoading(false)
    }
  }

  const clearReportFilters = () => {
    setReportFrom('')
    setReportTo('')
    void loadReportSummary('', '')
  }

  useEffect(() => { void load() }, [session?.accessToken])

  const reportIsEmpty = reportSummary
    ? reportSummary.bookingCount === 0 &&
      reportSummary.revenueBookingCount === 0 &&
      reportSummary.totalBookingAmount === 0 &&
      reportSummary.totalExpenses === 0 &&
      reportSummary.estimatedProfit === 0
    : false

  return (
    <AppLayout>
      <section className="broker-live-page">
        <div className="container">
          <div className="broker-live-heading">
            <div>
              <span className="eyebrow">BROKER PANELİ</span>
              <h1>Salam, {session?.user.fullName}</h1>
              <p>Evləriniz və rezervasiya sorğularınızın real xülasəsi.</p>
            </div>
            <button className="button button-ghost" onClick={() => void load()} disabled={loading}>
              <RefreshCw size={16} /> Yenilə
            </button>
          </div>

          {error && (
            <div className="broker-error" role="alert">
              <span>{error}</span>
              <button className="button button-ghost" onClick={() => void load()}>Yenidən yoxla</button>
            </div>
          )}

          {loading ? (
            <div className="broker-loading">Broker məlumatları yüklənir…</div>
          ) : (
            <>
              {summary && (
                <div className="broker-summary-grid">
                  <article><Building2 /><span>Aktiv evlər</span><strong>{summary.activeHomes} / {summary.totalHomes}</strong></article>
                  <article><ClipboardList /><span>Rezervasiyalar</span><strong>{summary.totalBookings}</strong><small>{summary.pendingBookings} yeni sorğu</small></article>
                  <article><CalendarDays /><span>Yaxın rezervasiyalar</span><strong>{summary.upcomingBookings}</strong><small>{summary.pendingDepositBookings} beh gözləyir</small></article>
                  <article><Coins /><span>Gözlənilən məbləğ</span><strong>{money.format(summary.totalExpectedAmount)}</strong></article>
                </div>
              )}

              <div className="broker-section-heading">
                <div>
                  <h2>Hesabat xülasəsi</h2>
                  <p>Rezervasiya gəliri, xərclər və təxmini mənfəət.</p>
                </div>
              </div>

              <div className="broker-report-panel">
                <div className="broker-report-filter">
                  <label>
                    <span>Başlanğıc</span>
                    <input type="date" value={reportFrom} onChange={(event) => setReportFrom(event.target.value)} />
                  </label>
                  <label>
                    <span>Bitiş</span>
                    <input type="date" value={reportTo} onChange={(event) => setReportTo(event.target.value)} />
                  </label>
                  <button className="button button-primary" onClick={() => void loadReportSummary()} disabled={reportLoading}>
                    {reportLoading ? 'Yüklənir…' : 'Tətbiq et'}
                  </button>
                  <button className="button button-ghost" onClick={clearReportFilters} disabled={reportLoading}>
                    Təmizlə
                  </button>
                </div>

                {reportError && (
                  <div className="broker-error" role="alert">
                    <span>{reportError}</span>
                    <button className="button button-ghost" onClick={() => void loadReportSummary()} disabled={reportLoading}>Yenidən yoxla</button>
                  </div>
                )}

                {reportLoading ? (
                  <div className="broker-loading broker-report-loading">Hesabat xülasəsi yüklənir…</div>
                ) : reportSummary ? (
                  <>
                    {reportIsEmpty && <div className="broker-report-empty">Bu seçim üçün hesabat məlumatı yoxdur.</div>}
                    <div className="broker-report-grid">
                      <article><Coins /><span>Ümumi bron məbləği</span><strong>{money.format(reportSummary.totalBookingAmount)}</strong></article>
                      <article><WalletCards /><span>Ümumi xərclər</span><strong>{money.format(reportSummary.totalExpenses)}</strong></article>
                      <article className={reportSummary.estimatedProfit >= 0 ? 'is-profit' : 'is-loss'}><Coins /><span>Təxmini mənfəət</span><strong>{money.format(reportSummary.estimatedProfit)}</strong></article>
                      <article><ClipboardList /><span>Bron sayı</span><strong>{reportSummary.bookingCount}</strong></article>
                      <article><CalendarDays /><span>Gəlirə daxil bronlar</span><strong>{reportSummary.revenueBookingCount}</strong></article>
                      <article><WalletCards /><span>Təmizlik xərci</span><strong>{money.format(reportSummary.totalCleaningCost)}</strong></article>
                      <article><WalletCards /><span>Ev sahibinə ödəniş</span><strong>{money.format(reportSummary.totalOwnerPayout)}</strong></article>
                      <article><WalletCards /><span>Digər xərclər</span><strong>{money.format(reportSummary.totalOtherExpenses)}</strong></article>
                    </div>
                  </>
                ) : (
                  <EmptyState title="Hesabat xülasəsi yoxdur" description="Məlumat yükləndikdə burada görünəcək." />
                )}
              </div>

              <div className="broker-section-heading">
                <div>
                  <h2>Evlərim</h2>
                  <p>Broker hesabınıza bağlı elanlar.</p>
                </div>
                <Link className="button button-primary" to="/broker/rental-homes/new">Ev əlavə et</Link>
              </div>
              {homes.length ? (
                <div className="broker-homes-grid">
                  {homes.map((home) => (
                    <Link to={`/broker/rental-homes/${home.id}/edit`} key={home.id}>
                      {home.mainImageUrl ? <img src={home.mainImageUrl} alt={home.title} /> : <div className="broker-home-placeholder"><Building2 /></div>}
                      <div>
                        <span>{home.city}{home.district ? ` · ${home.district}` : ''}</span>
                        <h3>{home.title}</h3>
                        <p>{home.guestCount} qonaq · {home.bookingCount} rezervasiya</p>
                        <strong>{money.format(home.dailyPrice)} / gecə</strong>
                      </div>
                      <em className={home.isPublished ? 'is-active' : ''}>{home.isPublished ? 'Aktiv' : 'Gizli'}</em>
                    </Link>
                  ))}
                </div>
              ) : (
                <EmptyState title="Brokerə bağlı ev yoxdur" description="Ev əlavə edildikdə burada görünəcək." />
              )}

              <div className="broker-section-heading">
                <div>
                  <h2>Son rezervasiyalar</h2>
                  <p>Ən yeni sorğular əvvəl göstərilir.</p>
                </div>
              </div>
              {bookings.length ? (
                <div className="broker-bookings-list">
                  {bookings.map((booking) => (
                    <Link to={`/broker/bookings/${booking.bookingId}`} key={booking.bookingId}>
                      <div>
                        <span>#{booking.bookingId} · {booking.rentalHomeTitle}</span>
                        <strong>{booking.customerName}</strong>
                        <small>{booking.firstDate ? date.format(new Date(`${booking.firstDate}T00:00:00`)) : 'Tarix yoxdur'} · {booking.datesCount} gecə</small>
                      </div>
                      <div>
                        <em className={`broker-status status-${booking.statusCode}`}>{booking.statusName}</em>
                        <strong>{money.format(booking.totalAmount)}</strong>
                      </div>
                    </Link>
                  ))}
                </div>
              ) : (
                <EmptyState title="Rezervasiya yoxdur" description="Yeni sorğular burada görünəcək." />
              )}
            </>
          )}
        </div>
      </section>
    </AppLayout>
  )
}

function validateReportRange(from: string, to: string) {
  if ((from && !to) || (!from && to)) return 'Tarix aralığı üçün həm başlanğıc, həm də bitiş tarixi seçilməlidir.'
  if (from && to && from > to) return 'Başlanğıc tarixi bitiş tarixindən sonra ola bilməz.'
  return ''
}
