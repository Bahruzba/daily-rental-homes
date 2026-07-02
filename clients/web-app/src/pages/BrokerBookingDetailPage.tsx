import { ArrowLeft, CalendarDays, CheckCircle2, CreditCard, Phone, Upload, UserRound, XCircle } from 'lucide-react'
import { type FormEvent, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { approveBrokerDeposit, changeBrokerBookingStatus, getBrokerBooking, rejectBrokerDeposit, requestBrokerDeposit, resolveApiAssetUrl, type BrokerBookingDetail } from '../api/broker'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'

const money = new Intl.NumberFormat('az-AZ', { style: 'currency', currency: 'AZN' })
const dateTime = new Intl.DateTimeFormat('az-AZ', { dateStyle: 'medium', timeStyle: 'short' })
const tomorrow = () => { const value = new Date(Date.now() + 24 * 60 * 60_000); value.setMinutes(value.getMinutes() - value.getTimezoneOffset()); return value.toISOString().slice(0, 16) }
const depositLabels: Record<string, string> = { requested: 'Qəbz gözlənilir', receipt_uploaded: 'Qəbz yoxlanılır', approved: 'Təsdiqlənib', rejected: 'Qəbz rədd edilib', expired: 'Vaxtı bitib', cancelled: 'Ləğv edilib' }
const actions: Record<string, Array<{ code: string; label: string }>> = { pending: [{ code: 'cancelled', label: 'Ləğv et' }], waiting_deposit: [{ code: 'cancelled', label: 'Ləğv et' }] }

export function BrokerBookingDetailPage() {
  const { id } = useParams(); const { session } = useAuth(); const bookingId = Number(id)
  const [booking, setBooking] = useState<BrokerBookingDetail>(); const [loading, setLoading] = useState(true); const [saving, setSaving] = useState(false); const [error, setError] = useState('')
  const [amount, setAmount] = useState(''); const [deadline, setDeadline] = useState(tomorrow); const [holder, setHolder] = useState(''); const [pan, setPan] = useState('**** **** **** '); const [bank, setBank] = useState(''); const [depositNote, setDepositNote] = useState('')

  const load = async () => {
    if (!session) return
    if (!Number.isInteger(bookingId) || bookingId <= 0) { setError('Rezervasiya nömrəsi düzgün deyil.'); setLoading(false); return }
    setLoading(true); setError('')
    try { setBooking(await getBrokerBooking(bookingId, session.accessToken)) }
    catch (cause) { console.error('Broker booking detail load failed', cause); setError(cause instanceof Error ? cause.message : 'Rezervasiya yüklənmədi.') }
    finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [bookingId, session?.accessToken])

  const runAction = async (action: () => Promise<unknown>) => { setSaving(true); setError(''); try { await action(); await load() } catch (cause) { console.error('Broker deposit action failed', cause); setError(cause instanceof Error ? cause.message : 'Əməliyyat tamamlanmadı.') } finally { setSaving(false) } }
  const submitDeposit = (event: FormEvent) => { event.preventDefault(); if (!session) return; void runAction(() => requestBrokerDeposit(bookingId, { amount: Number(amount), deadlineAt: new Date(deadline).toISOString(), cardHolderName: holder, cardPanMasked: pan, bankName: bank, note: depositNote }, session.accessToken)) }

  return <AppLayout><section className="broker-detail-page"><div className="container">
    <Link className="back-link" to="/broker"><ArrowLeft size={16} /> Broker panelinə qayıt</Link>
    {loading ? <div className="broker-loading">Rezervasiya yüklənir…</div> : error && !booking ? <div className="broker-error" role="alert">{error}</div> : booking && <>
      <div className="broker-detail-heading"><div><span className="eyebrow">REZERVASİYA #{booking.bookingId}</span><h1>{booking.rentalHome.title}</h1><p>{booking.rentalHome.city}{booking.rentalHome.district ? ` · ${booking.rentalHome.district}` : ''}</p></div><div><em className={`broker-status status-${booking.status.code}`}>{booking.status.name}</em><strong>{money.format(booking.totalAmount)}</strong></div></div>
      {error && <div className="broker-error" role="alert">{error}</div>}
      <div className="broker-detail-layout"><div className="broker-detail-main">
        <article><h2>Müştəri məlumatları</h2><p><UserRound /> <span><strong>{booking.customer.fullName}</strong><small>{booking.guests} qonaq</small></span></p><p><Phone /> <a href={`tel:${booking.customer.phone}`}>{booking.customer.phone}</a></p></article>
        <article><h2>Seçilmiş tarixlər</h2><div className="broker-date-chips">{booking.dates.map((value) => <span key={value}><CalendarDays /> {value}</span>)}</div></article>
        <article><h2>Beh məlumatları</h2>{!booking.deposit ? <form className="deposit-request-form" onSubmit={submitDeposit}><p>Müştəriyə yalnız maskalanmış kart məlumatı göstərilir.</p><div className="input-grid"><label><span>Beh məbləği</span><input type="number" min="1" step="0.01" required value={amount} onChange={(event) => setAmount(event.target.value)} /></label><label><span>Son tarix</span><input type="datetime-local" required value={deadline} onChange={(event) => setDeadline(event.target.value)} /></label><label><span>Kart sahibinin adı</span><input value={holder} maxLength={150} onChange={(event) => setHolder(event.target.value)} /></label><label><span>Maskalanmış kart</span><input required value={pan} maxLength={30} onChange={(event) => setPan(event.target.value)} placeholder="**** **** **** 1234" /></label><label><span>Bank</span><input value={bank} maxLength={100} onChange={(event) => setBank(event.target.value)} /></label><label className="full"><span>Qeyd</span><textarea value={depositNote} maxLength={1000} onChange={(event) => setDepositNote(event.target.value)} /></label></div><button className="button button-primary" disabled={saving}><CreditCard size={16} /> Beh istə</button></form> : <div className="deposit-details"><div><span>Status</span><strong>{depositLabels[booking.deposit.statusCode] ?? booking.deposit.statusCode}</strong></div><div><span>Məbləğ</span><strong>{money.format(booking.deposit.amount)}</strong></div><div><span>Son tarix</span><strong>{booking.deposit.deadlineAt ? dateTime.format(new Date(booking.deposit.deadlineAt)) : '—'}</strong></div><div><span>Kart</span><strong>{booking.deposit.cardPanMasked || '—'}</strong></div><div><span>Bank</span><strong>{booking.deposit.bankName || '—'}</strong></div>{booking.deposit.receipt && <div className="deposit-receipt"><span>Yüklənmiş qəbz</span><a href={resolveApiAssetUrl(booking.deposit.receipt.fileUrl)} target="_blank" rel="noreferrer"><img src={resolveApiAssetUrl(booking.deposit.receipt.fileUrl)} alt="Beh qəbzi" /><Upload size={15} /> Qəbzi aç</a></div>}{booking.deposit.reviewNote && <p className="deposit-review-note">Yoxlama qeydi: {booking.deposit.reviewNote}</p>}{booking.deposit.statusCode === 'receipt_uploaded' && <div className="deposit-review-actions"><button className="button button-primary" disabled={saving} onClick={() => session && void runAction(() => approveBrokerDeposit(bookingId, session.accessToken))}><CheckCircle2 size={16} /> Təsdiqlə</button><button className="button button-ghost" disabled={saving} onClick={() => session && void runAction(() => rejectBrokerDeposit(bookingId, session.accessToken, 'Qəbz aydın deyil, yenidən yükləyin.'))}><XCircle size={16} /> Rədd et</button></div>}</div>}</article>
        <article><h2>Müştəri qeydi</h2><p className="broker-note">{booking.note || 'Müştəri qeyd əlavə etməyib.'}</p></article>
      </div><aside className="broker-action-card"><span>Gecəlik qiymət</span><strong>{money.format(booking.dailyPrice)}</strong><span>Yaradılma vaxtı</span><strong>{dateTime.format(new Date(booking.createdAt))}</strong><hr /><h2>Status əməliyyatları</h2>{actions[booking.status.code]?.length ? actions[booking.status.code].map((action) => <button className="button button-full button-ghost" disabled={saving} onClick={() => session && void runAction(() => changeBrokerBookingStatus(bookingId, action.code, session.accessToken))} key={action.code}><XCircle size={16} />{action.label}</button>) : <p>Bu status üçün əlavə əməliyyat yoxdur.</p>}</aside></div>
    </>}
  </div></section></AppLayout>
}
