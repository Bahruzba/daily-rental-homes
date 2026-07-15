import { ArrowLeft, CalendarDays, CheckCircle2, CreditCard, Home, Send, UploadCloud } from 'lucide-react'
import { type ChangeEvent, type FormEvent, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getAccountBooking, requestBookingCancellation, uploadDepositReceipt, type AccountBookingDetail } from '../api/account'
import { resolveApiAssetUrl } from '../api/broker'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'

const money = new Intl.NumberFormat('az-AZ', { style: 'currency', currency: 'AZN' })
const dateTime = new Intl.DateTimeFormat('az-AZ', { dateStyle: 'medium', timeStyle: 'short' })

const bookingLabels: Record<string, string> = {
  pending: 'Broker təsdiqi gözlənilir',
  confirmed: 'Broker təsdiqlədi',
  waiting_deposit: 'Beh qəbzi lazımdır',
  rejected: 'Rezervasiya rədd edilib',
  cancelled: 'Rezervasiya ləğv edilib',
  completed: 'Tamamlanıb',
}

const bookingDescriptions: Record<string, string> = {
  pending: 'Sorğunuz brokerə göndərilib. Broker tarixləri yoxladıqdan sonra təsdiq və ya rədd edəcək.',
  confirmed: 'Broker rezervasiyanı təsdiqləyib. Əgər beh təlimatı varsa, növbəti addımı aşağıda izləyin.',
  waiting_deposit: 'Broker beh təlimatını göndərib. Qəbzi yükləmək sizin növbəti addımınızdır.',
  rejected: 'Bu rezervasiya artıq aktiv deyil. Ödəniş etməyə ehtiyac yoxdur.',
  cancelled: 'Bu rezervasiya ləğv edilib. Əlavə əməliyyat tələb olunmur.',
  completed: 'Rezervasiya tamamlanıb.',
}

const depositLabels: Record<string, string> = {
  requested: 'Qəbz gözlənilir',
  receipt_uploaded: 'Qəbz broker tərəfindən yoxlanılır',
  approved: 'Beh təsdiqlənib',
  rejected: 'Qəbz rədd edilib',
  expired: 'Son tarix keçib',
  cancelled: 'Ləğv edilib',
}

const cancellableStatuses = ['pending', 'waiting_deposit', 'confirmed', 'paid']

function depositMessage(booking: AccountBookingDetail) {
  if (booking.statusCode === 'rejected' || booking.statusCode === 'cancelled') {
    return 'Rezervasiya aktiv olmadığı üçün beh üzrə əlavə addım yoxdur.'
  }
  if (!booking.deposit) return 'Broker beh təlimatını göndərdikdə burada görünəcək. Hazırda ödəniş etməyin.'
  if (booking.deposit.statusCode === 'requested') return 'Məbləği göstərilən karta köçürün və qəbz şəklini yükləyin.'
  if (booking.deposit.statusCode === 'receipt_uploaded') return 'Qəbz yüklənib. Broker yoxlamanı tamamlayana qədər gözləyin.'
  if (booking.deposit.statusCode === 'approved') return 'Beh təsdiqləndi. Rezervasiya üzrə növbəti məlumatı brokerlə dəqiqləşdirə bilərsiniz.'
  if (booking.deposit.statusCode === 'rejected') return booking.deposit.allowReupload ? 'Qəbz rədd edilib. Broker qeydinə baxın və yeni qəbz yükləyin.' : 'Qəbz rədd edilib və yenidən yükləmə bağlıdır.'
  return 'Beh statusunu izləyin.'
}

export function AccountBookingDetailPage() {
  const { id } = useParams()
  const bookingId = Number(id)
  const { session } = useAuth()
  const [booking, setBooking] = useState<AccountBookingDetail>()
  const [loading, setLoading] = useState(true)
  const [uploading, setUploading] = useState(false)
  const [cancelRequesting, setCancelRequesting] = useState(false)
  const [cancelReason, setCancelReason] = useState('')
  const [cancelRequestSent, setCancelRequestSent] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const load = async () => {
    if (!session) return
    setLoading(true)
    setError('')
    try {
      const nextBooking = await getAccountBooking(bookingId, session.accessToken)
      setBooking(nextBooking)
      setCancelRequestSent(Boolean(nextBooking.cancelRequestSent))
    } catch (cause) {
      console.error('Account booking load failed', cause)
      setError(cause instanceof Error ? cause.message : 'Rezervasiya yüklənmədi.')
    } finally {
      setLoading(false)
    }
  }

  const submitCancelRequest = async (event: FormEvent) => {
    event.preventDefault()
    if (!session || !booking) return
    setError('')
    setSuccess('')

    if (!cancellableStatuses.includes(booking.statusCode)) {
      setError('Bu rezervasiya statusu üçün ləğv sorğusu göndərmək mümkün deyil.')
      return
    }

    if (!window.confirm('Bu rezervasiya üçün ləğv sorğusu göndərmək istədiyinizə əminsiniz?')) return

    setCancelRequesting(true)
    try {
      await requestBookingCancellation(booking.bookingId, cancelReason, session.accessToken)
      setCancelRequestSent(true)
      setCancelReason('')
      setSuccess('Ləğv sorğunuz göndərildi. Broker sizinlə əlaqə saxlayacaq.')
      await load()
    } catch (cause) {
      console.error('Cancel request failed', cause)
      setError(cause instanceof Error ? cause.message : 'Ləğv sorğusu göndərilmədi.')
    } finally {
      setCancelRequesting(false)
    }
  }

  useEffect(() => {
    void load()
  }, [bookingId, session?.accessToken])

  const upload = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file || !session) return
    setUploading(true)
    setError('')
    setSuccess('')
    try {
      await uploadDepositReceipt(bookingId, file, session.accessToken)
      setSuccess('Qəbz uğurla yükləndi, broker bildirişi növbəyə alındı.')
      await load()
    } catch (cause) {
      console.error('Receipt upload failed', cause)
      setError(cause instanceof Error ? cause.message : 'Qəbz yüklənmədi.')
    } finally {
      setUploading(false)
      event.target.value = ''
    }
  }

  const copyBookingLink = async () => {
    setError('')
    setSuccess('')
    try {
      await window.navigator.clipboard.writeText(window.location.href)
      setSuccess('Link kopyalandı.')
    } catch (cause) {
      console.error('Booking link copy failed', cause)
      setError('Linki kopyalamaq mümkün olmadı. Brauzer icazələrini yoxlayın.')
    }
  }

  return (
    <AppLayout>
      <section className="account-detail-page">
        <div className="container">
          <Link className="back-link" to="/account"><ArrowLeft size={16} /> Rezervasiyalarıma qayıt</Link>
          {loading ? (
            <div className="broker-loading">Rezervasiya yüklənir…</div>
          ) : error && !booking ? (
            <div className="broker-error">{error}</div>
          ) : booking && (
            <>
              <div className="broker-detail-heading">
                <div>
                  <span className="eyebrow">REZERVASİYA #{booking.bookingId}</span>
                  <h1>{booking.rentalHomeTitle}</h1>
                  <p>{booking.city}{booking.district ? ` · ${booking.district}` : ''}</p>
                </div>
                <div>
                  <em className={`broker-status status-${booking.statusCode}`}>{bookingLabels[booking.statusCode] ?? booking.statusName}</em>
                  <strong>{money.format(booking.totalAmount)}</strong>
                  <button className="button button-ghost" type="button" onClick={() => void copyBookingLink()}>
                    Linki kopyala
                  </button>
                </div>
              </div>

              {error && <div className="broker-error" role="alert">{error}</div>}
              {success && <div className="account-success"><CheckCircle2 size={17} /> {success}</div>}

              <div className="account-status-panel">
                <strong>Növbəti addım</strong>
                <p>{bookingDescriptions[booking.statusCode] ?? 'Rezervasiya statusunu izləyin.'}</p>
              </div>

              <div className="account-detail-layout">
                <article>
                  <div className="account-home-summary">
                    {booking.mainImageUrl ? (
                      <img src={resolveApiAssetUrl(booking.mainImageUrl)} alt={booking.rentalHomeTitle} />
                    ) : (
                      <div><Home /></div>
                    )}
                    <div>
                      <h2>Ev məlumatı</h2>
                      <strong>{booking.rentalHomeTitle}</strong>
                      <p>{booking.city}{booking.district ? ` · ${booking.district}` : ''}</p>
                    </div>
                  </div>

                  <h2>Seçilmiş tarixlər</h2>
                  <div className="broker-date-chips">
                    {booking.dates.map((value) => <span key={value}><CalendarDays /> {value}</span>)}
                  </div>

                  <h2>Rezervasiya məlumatı</h2>
                  <div className="account-booking-facts">
                    <div><span>Qonaq sayı</span><strong>{booking.guests}</strong></div>
                    <div><span>Gecəlik qiymət</span><strong>{money.format(booking.dailyPrice)}</strong></div>
                    <div><span>Cəmi</span><strong>{money.format(booking.totalAmount)}</strong></div>
                  </div>
                  {booking.note && <p className="broker-note">Sizin qeydiniz: {booking.note}</p>}

                  {cancellableStatuses.includes(booking.statusCode) && (
                    <div className="cancel-request-panel">
                      <h2>Rezervasiyanı ləğv etmək istəyirsiniz?</h2>
                      <p>Bu sorğu rezervasiyanı avtomatik ləğv etmir. Broker müraciəti görüb sizinlə əlaqə saxlayacaq.</p>
                      <form onSubmit={submitCancelRequest}>
                        <label>
                          <span>Səbəb</span>
                          <textarea
                            value={cancelReason}
                            maxLength={1000}
                            onChange={(event) => setCancelReason(event.target.value)}
                            placeholder="İstəyə bağlı olaraq ləğv səbəbini yazın"
                            disabled={cancelRequesting || cancelRequestSent}
                          />
                        </label>
                        <button className="button button-ghost" disabled={cancelRequesting || cancelRequestSent}>
                          <Send size={16} /> {cancelRequestSent ? 'Ləğv sorğusu göndərilib' : 'Ləğv sorğusu göndər'}
                        </button>
                      </form>
                    </div>
                  )}
                </article>

                <article className="customer-deposit-card">
                  <CreditCard />
                  <h2>Beh məlumatları</h2>
                  <p className="account-next-action">{depositMessage(booking)}</p>
                  {!booking.deposit ? null : (
                    <>
                      <em>{depositLabels[booking.deposit.statusCode] ?? booking.deposit.statusCode}</em>
                      <div><span>Məbləğ</span><strong>{money.format(booking.deposit.amount)}</strong></div>
                      <div><span>Son tarix</span><strong>{booking.deposit.deadlineAt ? dateTime.format(new Date(booking.deposit.deadlineAt)) : '—'}</strong></div>
                      <div><span>Kart sahibi</span><strong>{booking.deposit.cardHolderName || '—'}</strong></div>
                      <div><span>Maskalanmış kart</span><strong>{booking.deposit.cardPanMasked || '—'}</strong></div>
                      <div><span>Bank</span><strong>{booking.deposit.bankName || '—'}</strong></div>
                      {booking.deposit.deadlineExtendedAt && (
                        <div><span>Uzadılma vaxtı</span><strong>{dateTime.format(new Date(booking.deposit.deadlineExtendedAt))}</strong></div>
                      )}
                      {booking.deposit.deadlineExtensionReason && (
                        <p className="deposit-instruction">Uzadılma səbəbi: {booking.deposit.deadlineExtensionReason}</p>
                      )}
                      {booking.deposit.note && <p className="deposit-instruction">{booking.deposit.note}</p>}
                      {booking.deposit.reviewNote && <p className="deposit-review-note">Broker qeydi: {booking.deposit.reviewNote}</p>}
                      {booking.deposit.receipt && (
                        <a className="receipt-link" href={resolveApiAssetUrl(booking.deposit.receipt.fileUrl)} target="_blank" rel="noreferrer">
                          Yüklənmiş qəbzi aç
                        </a>
                      )}
                      {(booking.deposit.statusCode === 'requested' || (booking.deposit.statusCode === 'rejected' && booking.deposit.allowReupload)) && (
                        <label className="receipt-upload button button-primary">
                          <UploadCloud size={17} /> {uploading ? 'Yüklənir…' : 'Qəbz şəkli yüklə'}
                          <input type="file" accept="image/jpeg,image/png,image/webp" disabled={uploading} onChange={(event) => void upload(event)} />
                        </label>
                      )}
                    </>
                  )}
                </article>
              </div>
            </>
          )}
        </div>
      </section>
    </AppLayout>
  )
}
