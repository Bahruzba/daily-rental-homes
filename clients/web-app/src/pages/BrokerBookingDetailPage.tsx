import { ArrowLeft, CalendarDays, CheckCircle2, CreditCard, PencilLine, Phone, PlusCircle, ReceiptText, Trash2, Upload, UserRound, XCircle } from 'lucide-react'
import { type FormEvent, useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  acceptBrokerBooking,
  approveBrokerDeposit,
  approveBrokerCancellationRequest,
  cancelBrokerBooking,
  createBrokerBookingExpense,
  deleteBrokerBookingExpense,
  extendBrokerDepositDeadline,
  getBrokerBooking,
  getBrokerBookingExpenses,
  rejectBrokerBooking,
  rejectBrokerCancellationRequest,
  rejectBrokerDeposit,
  requestBrokerDeposit,
  resolveApiAssetUrl,
  updateBrokerBookingExpense,
  type BrokerBookingDetail,
  type BrokerBookingExpense,
} from '../api/broker'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'

const money = new Intl.NumberFormat('az-AZ', { style: 'currency', currency: 'AZN' })
const dateTime = new Intl.DateTimeFormat('az-AZ', { dateStyle: 'medium', timeStyle: 'short' })
const tomorrow = () => {
  const value = new Date(Date.now() + 24 * 60 * 60_000)
  value.setMinutes(value.getMinutes() - value.getTimezoneOffset())
  return value.toISOString().slice(0, 16)
}
const toDateTimeLocalValue = (iso?: string | null) => {
  const value = iso ? new Date(iso) : new Date(Date.now() + 24 * 60 * 60_000)
  value.setMinutes(value.getMinutes() - value.getTimezoneOffset())
  return value.toISOString().slice(0, 16)
}
const blockedDepositExtensionBookingStatuses = ['cancelled', 'completed', 'rejected']

const depositLabels: Record<string, string> = {
  requested: 'Qəbz gözlənilir',
  receipt_uploaded: 'Qəbz yoxlanılır',
  approved: 'Təsdiqlənib',
  rejected: 'Qəbz rədd edilib',
  expired: 'Vaxtı bitib',
  cancelled: 'Ləğv edilib',
}

const expenseLabels: Record<string, string> = {
  cleaning: 'Təmizlik',
  owner_payout: 'Ev sahibinə ödəniş',
  utility: 'Kommunal',
  repair: 'Təmir',
  other: 'Digər',
}

const actions: Record<string, Array<{ action: 'accept' | 'reject' | 'cancel'; label: string; tone: 'primary' | 'ghost' }>> = {
  pending: [
    { action: 'accept', label: 'Təsdiqlə', tone: 'primary' },
    { action: 'reject', label: 'Rədd et', tone: 'ghost' },
    { action: 'cancel', label: 'Ləğv et', tone: 'ghost' },
  ],
  waiting_deposit: [{ action: 'cancel', label: 'Ləğv et', tone: 'ghost' }],
  confirmed: [{ action: 'cancel', label: 'Ləğv et', tone: 'ghost' }],
}

export function BrokerBookingDetailPage() {
  const { id } = useParams()
  const { session } = useAuth()
  const bookingId = Number(id)
  const [booking, setBooking] = useState<BrokerBookingDetail>()
  const [expenses, setExpenses] = useState<BrokerBookingExpense[]>([])
  const [loading, setLoading] = useState(true)
  const [expensesLoading, setExpensesLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [expenseSaving, setExpenseSaving] = useState(false)
  const [error, setError] = useState('')
  const [expenseError, setExpenseError] = useState('')
  const [success, setSuccess] = useState('')
  const [amount, setAmount] = useState('')
  const [deadline, setDeadline] = useState(tomorrow)
  const [extensionDeadline, setExtensionDeadline] = useState(tomorrow)
  const [extensionReason, setExtensionReason] = useState('')
  const [holder, setHolder] = useState('')
  const [pan, setPan] = useState('**** **** **** ')
  const [bank, setBank] = useState('')
  const [depositNote, setDepositNote] = useState('')
  const [expenseType, setExpenseType] = useState('cleaning')
  const [expenseTitle, setExpenseTitle] = useState('')
  const [expenseAmount, setExpenseAmount] = useState('')
  const [expenseNote, setExpenseNote] = useState('')
  const [editingExpenseId, setEditingExpenseId] = useState<number | null>(null)
  const [cancellationDecisionNote, setCancellationDecisionNote] = useState('')

  const totalExpenses = useMemo(() => expenses.reduce((sum, item) => sum + item.amount, 0), [expenses])
  const estimatedProfit = (booking?.totalAmount ?? 0) - totalExpenses
  const canExtendDepositDeadline = Boolean(
    booking?.deposit &&
      booking.deposit.statusCode !== 'approved' &&
      !blockedDepositExtensionBookingStatuses.includes(booking.status.code),
  )

  const loadExpenses = async () => {
    if (!session || !Number.isInteger(bookingId) || bookingId <= 0) return
    setExpensesLoading(true)
    setExpenseError('')
    try {
      setExpenses(await getBrokerBookingExpenses(bookingId, session.accessToken))
    } catch (cause) {
      console.error('Broker booking expenses load failed', cause)
      setExpenseError(cause instanceof Error ? cause.message : 'Xərclər yüklənmədi.')
    } finally {
      setExpensesLoading(false)
    }
  }

  const load = async () => {
    if (!session) return
    if (!Number.isInteger(bookingId) || bookingId <= 0) {
      setError('Rezervasiya nömrəsi düzgün deyil.')
      setLoading(false)
      return
    }

    setLoading(true)
    setError('')
    try {
      const [bookingData, expenseData] = await Promise.all([
        getBrokerBooking(bookingId, session.accessToken),
        getBrokerBookingExpenses(bookingId, session.accessToken),
      ])
      setBooking(bookingData)
      if (bookingData.deposit?.deadlineAt) {
        const nextDeadline = new Date(bookingData.deposit.deadlineAt)
        nextDeadline.setDate(nextDeadline.getDate() + 1)
        setExtensionDeadline(toDateTimeLocalValue(nextDeadline.toISOString()))
      }
      setExpenses(expenseData)
    } catch (cause) {
      console.error('Broker booking detail load failed', cause)
      setError(cause instanceof Error ? cause.message : 'Rezervasiya yüklənmədi.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [bookingId, session?.accessToken])

  const runAction = async (action: () => Promise<unknown>, successMessage: string) => {
    setSaving(true)
    setError('')
    setSuccess('')
    try {
      await action()
      setSuccess(successMessage)
      await load()
    } catch (cause) {
      console.error('Broker action failed', cause)
      setError(cause instanceof Error ? cause.message : 'Əməliyyat tamamlanmadı.')
    } finally {
      setSaving(false)
    }
  }

  const runStatusAction = (action: 'accept' | 'reject' | 'cancel') => {
    if (!session) return
    const handlers = {
      accept: () => acceptBrokerBooking(bookingId, session.accessToken, 'Broker confirmed booking request.'),
      reject: () => rejectBrokerBooking(bookingId, session.accessToken, 'Broker rejected booking request.'),
      cancel: () => cancelBrokerBooking(bookingId, session.accessToken, 'Broker cancelled booking.'),
    }
    const messages = {
      accept: 'Rezervasiya təsdiqləndi və müştəri bildirişi növbəyə alındı.',
      reject: 'Rezervasiya rədd edildi və müştəri bildirişi növbəyə alındı.',
      cancel: 'Rezervasiya ləğv edildi və müştəri bildirişi növbəyə alındı.',
    }
    void runAction(handlers[action], messages[action])
  }

  const submitDeposit = (event: FormEvent) => {
    event.preventDefault()
    if (!session) return
    void runAction(
      () =>
        requestBrokerDeposit(
          bookingId,
          {
            amount: Number(amount),
            deadlineAt: new Date(deadline).toISOString(),
            cardHolderName: holder,
            cardPanMasked: pan,
            bankName: bank,
            note: depositNote,
          },
          session.accessToken,
        ),
      'Beh tələbi yaradıldı, müştəri bildirişi və reminder növbəyə alındı.',
    )
  }

  const submitDeadlineExtension = (event: FormEvent) => {
    event.preventDefault()
    if (!session || !booking?.deposit) return
    setError('')
    setSuccess('')

    if (extensionReason.length > 500) {
      setError('Səbəb 500 simvoldan çox olmamalıdır.')
      return
    }

    const nextDeadline = new Date(extensionDeadline)
    const currentDeadline = booking.deposit.deadlineAt ? new Date(booking.deposit.deadlineAt) : null
    if (Number.isNaN(nextDeadline.getTime()) || nextDeadline <= new Date()) {
      setError('Yeni son tarix gələcəkdə olmalıdır.')
      return
    }
    if (currentDeadline && nextDeadline <= currentDeadline) {
      setError('Yeni son tarix mövcud son tarixdən gec olmalıdır.')
      return
    }

    if (!window.confirm('Beh müddətini uzatmaq istədiyinizə əminsiniz?')) return

    void runAction(
      () =>
        extendBrokerDepositDeadline(
          bookingId,
          {
            deadlineAt: nextDeadline.toISOString(),
            reason: extensionReason.trim() || undefined,
          },
          session.accessToken,
        ),
      'Beh müddəti uzadıldı və müştəri bildirişi növbəyə alındı.',
    )
  }

  const resetExpenseForm = () => {
    setExpenseType('cleaning')
    setExpenseTitle('')
    setExpenseAmount('')
    setExpenseNote('')
    setEditingExpenseId(null)
  }

  const startEditExpense = (expense: BrokerBookingExpense) => {
    setExpenseType(expense.typeCode)
    setExpenseTitle(expense.title)
    setExpenseAmount(String(expense.amount))
    setExpenseNote(expense.note ?? '')
    setEditingExpenseId(expense.id)
    setExpenseError('')
  }

  const cancelEditExpense = () => {
    resetExpenseForm()
    setExpenseError('')
  }

  const submitExpense = async (event: FormEvent) => {
    event.preventDefault()
    if (!session) return
    const amountValue = Number(expenseAmount)
    if (!expenseType.trim()) { setExpenseError('Xərc növü seçilməlidir.'); return }
    if (!expenseTitle.trim()) { setExpenseError('Xərc başlığı yazılmalıdır.'); return }
    if (!Number.isFinite(amountValue) || amountValue <= 0) { setExpenseError('Məbləğ 0-dan böyük olmalıdır.'); return }

    setExpenseSaving(true)
    setExpenseError('')
    setSuccess('')
    try {
      const payload = {
        typeCode: expenseType,
        title: expenseTitle.trim(),
        amount: amountValue,
        note: expenseNote.trim() || undefined,
      }
      if (editingExpenseId) {
        await updateBrokerBookingExpense(bookingId, editingExpenseId, payload, session.accessToken)
        setSuccess('Xərc yeniləndi.')
      } else {
        await createBrokerBookingExpense(bookingId, payload, session.accessToken)
        setSuccess('Xərc əlavə edildi.')
      }
      resetExpenseForm()
      await loadExpenses()
    } catch (cause) {
      console.error('Broker expense save failed', cause)
      setExpenseError(cause instanceof Error ? cause.message : editingExpenseId ? 'Xərc yenilənmədi.' : 'Xərc əlavə edilmədi.')
    } finally {
      setExpenseSaving(false)
    }
  }

  const deleteExpense = async (expenseId: number) => {
    if (!session) return
    setExpenseSaving(true)
    setExpenseError('')
    setSuccess('')
    try {
      await deleteBrokerBookingExpense(bookingId, expenseId, session.accessToken)
      setSuccess('Xərc silindi.')
      if (editingExpenseId === expenseId) resetExpenseForm()
      await loadExpenses()
    } catch (cause) {
      console.error('Broker expense delete failed', cause)
      setExpenseError(cause instanceof Error ? cause.message : 'Xərc silinmədi.')
    } finally {
      setExpenseSaving(false)
    }
  }

  const runCancellationDecision = (decision: 'approve' | 'reject') => {
    if (!session || !booking?.cancellationRequest) return
    if (cancellationDecisionNote.length > 1000) {
      setError('Qərar qeydi 1000 simvoldan çox olmamalıdır.')
      return
    }

    const confirmed = window.confirm(decision === 'approve'
      ? 'Ləğv sorğusunu təsdiqləmək və rezervasiyanı ləğv etmək istəyirsiniz?'
      : 'Ləğv sorğusunu rədd etmək istəyirsiniz?')
    if (!confirmed) return

    const action = decision === 'approve'
      ? () => approveBrokerCancellationRequest(bookingId, booking.cancellationRequest!.id, session.accessToken, cancellationDecisionNote.trim() || undefined)
      : () => rejectBrokerCancellationRequest(bookingId, booking.cancellationRequest!.id, session.accessToken, cancellationDecisionNote.trim() || undefined)
    const message = decision === 'approve'
      ? 'Ləğv sorğusu təsdiqləndi və rezervasiya ləğv edildi.'
      : 'Ləğv sorğusu rədd edildi.'

    void runAction(async () => {
      await action()
      setCancellationDecisionNote('')
    }, message)
  }

  return (
    <AppLayout>
      <section className="broker-detail-page">
        <div className="container">
          <Link className="back-link" to="/broker">
            <ArrowLeft size={16} /> Broker panelinə qayıt
          </Link>
          {loading ? (
            <div className="broker-loading">Rezervasiya yüklənir…</div>
          ) : error && !booking ? (
            <div className="broker-error" role="alert">{error}</div>
          ) : booking && (
            <>
              <div className="broker-detail-heading">
                <div>
                  <span className="eyebrow">REZERVASİYA #{booking.bookingId}</span>
                  <h1>{booking.rentalHome.title}</h1>
                  <p>{booking.rentalHome.city}{booking.rentalHome.district ? ` · ${booking.rentalHome.district}` : ''}</p>
                </div>
                <div>
                  <em className={`broker-status status-${booking.status.code}`}>{booking.status.name}</em>
                  <strong>{money.format(booking.totalAmount)}</strong>
                </div>
              </div>

              {error && <div className="broker-error" role="alert">{error}</div>}
              {success && <div className="account-success" role="status">{success}</div>}

              {booking.cancellationRequest && (
                <div className="broker-cancellation-panel">
                  <div>
                    <span className="eyebrow">LƏĞV SORĞUSU</span>
                    <h2>Müştəri ləğv sorğusu göndərib</h2>
                    <p>Bu sorğu rezervasiyanı avtomatik ləğv etmir. Qərarı broker ayrıca verməlidir.</p>
                  </div>
                  <div className="broker-cancellation-meta">
                    <span>Status</span>
                    <strong>{booking.cancellationRequest.statusCode}</strong>
                    <span>Göndərilmə vaxtı</span>
                    <strong>{dateTime.format(new Date(booking.cancellationRequest.createdAt))}</strong>
                  </div>
                  {booking.cancellationRequest.reason && (
                    <p className="broker-cancellation-reason">Səbəb: {booking.cancellationRequest.reason}</p>
                  )}
                  <label className="broker-cancellation-note">
                    <span>Qərar qeydi</span>
                    <textarea
                      maxLength={1000}
                      value={cancellationDecisionNote}
                      onChange={(event) => setCancellationDecisionNote(event.target.value)}
                      placeholder="İstəyə bağlı qeyd yazın"
                    />
                    <small>{cancellationDecisionNote.length}/1000</small>
                  </label>
                  <div className="broker-cancellation-actions">
                    <button className="button button-primary" disabled={saving} onClick={() => runCancellationDecision('approve')}>Təsdiqlə</button>
                    <button className="button button-ghost" disabled={saving} onClick={() => runCancellationDecision('reject')}>Rədd et</button>
                    <small>Təsdiq rezervasiyanı ləğv edir. Rədd etmə booking statusunu dəyişmir.</small>
                  </div>
                </div>
              )}

              <div className="broker-detail-layout">
                <div className="broker-detail-main">
                  <article>
                    <h2>Müştəri məlumatları</h2>
                    <p><UserRound /> <span><strong>{booking.customer.fullName}</strong><small>{booking.guests} qonaq</small></span></p>
                    <p><Phone /> <a href={`tel:${booking.customer.phone}`}>{booking.customer.phone}</a></p>
                  </article>

                  <article>
                    <h2>Seçilmiş tarixlər</h2>
                    <div className="broker-date-chips">
                      {booking.dates.map((value) => <span key={value}><CalendarDays /> {value}</span>)}
                    </div>
                  </article>

                  <article>
                    <h2>Beh məlumatları</h2>
                    {!booking.deposit ? (
                      <form className="deposit-request-form" onSubmit={submitDeposit}>
                        <p>Müştəriyə yalnız maskalanmış kart məlumatı göstərilir.</p>
                        <div className="input-grid">
                          <label><span>Beh məbləği</span><input type="number" min="1" step="0.01" required value={amount} onChange={(event) => setAmount(event.target.value)} /></label>
                          <label><span>Son tarix</span><input type="datetime-local" required value={deadline} onChange={(event) => setDeadline(event.target.value)} /></label>
                          <label><span>Kart sahibinin adı</span><input value={holder} maxLength={150} onChange={(event) => setHolder(event.target.value)} /></label>
                          <label><span>Maskalanmış kart</span><input required value={pan} maxLength={30} onChange={(event) => setPan(event.target.value)} placeholder="**** **** **** 1234" /></label>
                          <label><span>Bank</span><input value={bank} maxLength={100} onChange={(event) => setBank(event.target.value)} /></label>
                          <label className="full"><span>Qeyd</span><textarea value={depositNote} maxLength={1000} onChange={(event) => setDepositNote(event.target.value)} /></label>
                        </div>
                        <button className="button button-primary" disabled={saving}><CreditCard size={16} /> Beh istə</button>
                      </form>
                    ) : (
                      <div className="deposit-details">
                        <div><span>Status</span><strong>{depositLabels[booking.deposit.statusCode] ?? booking.deposit.statusCode}</strong></div>
                        <div><span>Məbləğ</span><strong>{money.format(booking.deposit.amount)}</strong></div>
                        <div><span>Son tarix</span><strong>{booking.deposit.deadlineAt ? dateTime.format(new Date(booking.deposit.deadlineAt)) : '—'}</strong></div>
                        <div><span>Kart</span><strong>{booking.deposit.cardPanMasked || '—'}</strong></div>
                        <div><span>Bank</span><strong>{booking.deposit.bankName || '—'}</strong></div>
                        <div><span>Uzadılıb</span><strong>{booking.deposit.deadlineExtendedAt ? 'Bəli' : 'Xeyr'}</strong></div>
                        {booking.deposit.deadlineExtendedAt && (
                          <div><span>Uzadılma vaxtı</span><strong>{dateTime.format(new Date(booking.deposit.deadlineExtendedAt))}</strong></div>
                        )}
                        {booking.deposit.deadlineExtensionReason && (
                          <p className="deposit-instruction">Uzadılma səbəbi: {booking.deposit.deadlineExtensionReason}</p>
                        )}
                        {booking.deposit.isDeadlineExpired && (
                          <p className="deposit-expired-warning">Beh üçün ayrılmış vaxt bitib.</p>
                        )}
                        {booking.deposit.receipt && (
                          <div className="deposit-receipt">
                            <span>Yüklənmiş qəbz</span>
                            <a href={resolveApiAssetUrl(booking.deposit.receipt.fileUrl)} target="_blank" rel="noreferrer">
                              <img src={resolveApiAssetUrl(booking.deposit.receipt.fileUrl)} alt="Beh qəbzi" /><Upload size={15} /> Qəbzi aç
                            </a>
                          </div>
                        )}
                        {booking.deposit.reviewNote && <p className="deposit-review-note">Yoxlama qeydi: {booking.deposit.reviewNote}</p>}
                        {canExtendDepositDeadline && (
                          <form className="deposit-extension-form input-grid" onSubmit={submitDeadlineExtension}>
                            <div className="full">
                              <strong>Beh müddətini uzat</strong>
                              <p>Yeni son tarix mövcud son tarixdən gec və gələcəkdə olmalıdır.</p>
                            </div>
                            <label><span>Yeni son tarix</span><input type="datetime-local" required value={extensionDeadline} onChange={(event) => setExtensionDeadline(event.target.value)} /></label>
                            <label className="full">
                              <span>Səbəb</span>
                              <textarea value={extensionReason} maxLength={500} onChange={(event) => setExtensionReason(event.target.value)} placeholder="İstəyə bağlı səbəb" />
                              <small>{extensionReason.length}/500</small>
                            </label>
                            <button className="button button-primary" disabled={saving}><CreditCard size={16} /> Müddəti uzat</button>
                          </form>
                        )}
                        {booking.deposit.statusCode === 'receipt_uploaded' && (
                          <div className="deposit-review-actions">
                            <button className="button button-primary" disabled={saving} onClick={() => session && void runAction(() => approveBrokerDeposit(bookingId, session.accessToken), 'Beh təsdiqləndi və müştəri bildirişi növbəyə alındı.')}><CheckCircle2 size={16} /> Təsdiqlə</button>
                            <button className="button button-ghost" disabled={saving} onClick={() => session && void runAction(() => rejectBrokerDeposit(bookingId, session.accessToken, 'Qəbz aydın deyil, yenidən yükləyin.'), 'Qəbz rədd edildi və müştəri bildirişi növbəyə alındı.')}><XCircle size={16} /> Rədd et</button>
                          </div>
                        )}
                      </div>
                    )}
                  </article>

                  <article className="expense-section">
                    <div className="expense-heading">
                      <div>
                        <h2>Xərclər</h2>
                        <p>Bu rezervasiya üzrə broker daxili xərclərini qeyd edin.</p>
                      </div>
                      <ReceiptText />
                    </div>

                    <div className="expense-summary-grid">
                      <div><span>Rezervasiya məbləği</span><strong>{money.format(booking.totalAmount)}</strong></div>
                      <div><span>Cəmi xərclər</span><strong>{money.format(totalExpenses)}</strong></div>
                      <div><span>Təxmini mənfəət</span><strong>{money.format(estimatedProfit)}</strong></div>
                    </div>

                    {expenseError && <div className="broker-error" role="alert">{expenseError}</div>}

                    <form className={`expense-form input-grid${editingExpenseId ? ' is-editing' : ''}`} onSubmit={submitExpense}>
                      <div className="expense-form-heading full">
                        <div>
                          <strong>{editingExpenseId ? 'Xərci redaktə et' : 'Yeni xərc əlavə et'}</strong>
                          {editingExpenseId && <span>Seçilmiş xərc formaya yüklənib.</span>}
                        </div>
                        {editingExpenseId && <button type="button" className="button button-ghost" onClick={cancelEditExpense} disabled={expenseSaving}>Ləğv et</button>}
                      </div>
                      <label><span>Xərc növü</span><select value={expenseType} onChange={(event) => setExpenseType(event.target.value)}>{Object.entries(expenseLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
                      <label><span>Başlıq</span><input value={expenseTitle} maxLength={150} onChange={(event) => setExpenseTitle(event.target.value)} placeholder="Məs: Təmizlik" /></label>
                      <label><span>Məbləğ</span><input type="number" min="0.01" step="0.01" value={expenseAmount} onChange={(event) => setExpenseAmount(event.target.value)} placeholder="0.00" /></label>
                      <label className="full"><span>Qeyd</span><textarea value={expenseNote} maxLength={1000} onChange={(event) => setExpenseNote(event.target.value)} placeholder="İstəyə bağlı qeyd" /></label>
                      <button className="button button-primary" disabled={expenseSaving}>
                        {editingExpenseId ? <CheckCircle2 size={16} /> : <PlusCircle size={16} />}
                        {editingExpenseId ? 'Yadda saxla' : 'Xərc əlavə et'}
                      </button>
                    </form>

                    {expensesLoading ? (
                      <div className="broker-loading">Xərclər yüklənir…</div>
                    ) : expenses.length ? (
                      <div className="expense-list">
                        {expenses.map((expense) => (
                          <div className={`expense-row${editingExpenseId === expense.id ? ' is-editing' : ''}`} key={expense.id}>
                            <div>
                              <em>{expenseLabels[expense.typeCode] ?? expense.typeCode}</em>
                              <strong>{expense.title}</strong>
                              {expense.note && <p>{expense.note}</p>}
                              <small>{dateTime.format(new Date(expense.createdAt))}</small>
                            </div>
                            <div>
                              <strong>{money.format(expense.amount)}</strong>
                              <button className="button button-ghost expense-edit-button" disabled={expenseSaving} onClick={() => startEditExpense(expense)} aria-label="Xərci redaktə et"><PencilLine size={15} /> Düzəliş et</button>
                              <button className="icon-button" disabled={expenseSaving} onClick={() => void deleteExpense(expense.id)} aria-label="Xərci sil"><Trash2 size={16} /></button>
                            </div>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <p className="broker-note">Bu rezervasiya üçün xərc əlavə edilməyib.</p>
                    )}
                  </article>

                  <article>
                    <h2>Müştəri qeydi</h2>
                    <p className="broker-note">{booking.note || 'Müştəri qeyd əlavə etməyib.'}</p>
                  </article>
                </div>

                <aside className="broker-action-card">
                  <span>Gecəlik qiymət</span>
                  <strong>{money.format(booking.dailyPrice)}</strong>
                  <span>Yaradılma vaxtı</span>
                  <strong>{dateTime.format(new Date(booking.createdAt))}</strong>
                  <hr />
                  <h2>Maliyyə xülasəsi</h2>
                  <span>Rezervasiya məbləği</span>
                  <strong>{money.format(booking.totalAmount)}</strong>
                  <span>Cəmi xərclər</span>
                  <strong>{money.format(totalExpenses)}</strong>
                  <span>Təxmini mənfəət</span>
                  <strong>{money.format(estimatedProfit)}</strong>
                  <hr />
                  <h2>Status əməliyyatları</h2>
                  {actions[booking.status.code]?.length ? (
                    actions[booking.status.code].map((action) => (
                      <button className={`button button-full button-${action.tone}`} disabled={saving} onClick={() => runStatusAction(action.action)} key={action.action}>
                        {action.action === 'accept' ? <CheckCircle2 size={16} /> : <XCircle size={16} />}
                        {action.label}
                      </button>
                    ))
                  ) : (
                    <p>Bu status üçün əlavə əməliyyat yoxdur.</p>
                  )}
                </aside>
              </div>
            </>
          )}
        </div>
      </section>
    </AppLayout>
  )
}
