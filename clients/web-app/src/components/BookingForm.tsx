import { CalendarDays, CheckCircle2, Minus, Plus, ShieldCheck } from 'lucide-react'
import { type FormEvent, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { BookingRequestError, createBooking } from '../api/client'
import type { BookingResult, RentalHome } from '../types'

const availableDates = Array.from({ length: 14 }, (_, index) => ({ value: `2026-07-${String(index + 8).padStart(2, '0')}`, label: index + 8 }))

export function BookingForm({ home }: { home: RentalHome }) {
  const [dates, setDates] = useState<string[]>(['2026-07-12', '2026-07-13', '2026-07-14'])
  const [guests, setGuests] = useState(Math.min(4, home.guestCount))
  const [submitting, setSubmitting] = useState(false)
  const [confirmation, setConfirmation] = useState<BookingResult>()
  const [error, setError] = useState('')
  const total = useMemo(() => home.dailyPrice * dates.length, [dates.length, home.dailyPrice])
  const unavailableDates = useMemo(() => new Set((home.unavailableRanges ?? []).flatMap((range) => {
    const values: string[] = []
    const cursor = new Date(`${range.startDate}T00:00:00`)
    const end = new Date(`${range.endDate}T00:00:00`)
    while (cursor <= end) {
      values.push(cursor.toISOString().slice(0, 10))
      cursor.setDate(cursor.getDate() + 1)
    }
    return values
  })), [home.unavailableRanges])

  useEffect(() => {
    setDates((current) => current.filter((date) => !unavailableDates.has(date)))
  }, [unavailableDates])

  function toggleDate(value: string) {
    if (unavailableDates.has(value)) return
    setDates((current) => current.includes(value) ? current.filter((item) => item !== value) : [...current, value].sort())
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!dates.length) return
    const form = new FormData(event.currentTarget)
    setSubmitting(true)
    setError('')
    try {
      const result = await createBooking({ rentalHomeId: home.id, name: String(form.get('name')), phone: String(form.get('phone')), guests, dates, note: String(form.get('note') || '') })
      setConfirmation(result)
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (technicalError) {
      const errorMessage = technicalError instanceof BookingRequestError
        ? technicalError.message
        : 'Sorğunu göndərmək mümkün olmadı. Bir qədər sonra yenidən yoxlayın.'
      const consoleError = technicalError instanceof BookingRequestError
        ? technicalError.technicalCause ?? technicalError
        : technicalError
      console.error('Booking submit failed', consoleError)
      setError(errorMessage)
    } finally {
      setSubmitting(false)
    }
  }

  if (confirmation) return <section className="confirmation-page"><div className="confirmation-icon"><CheckCircle2 /></div><span className="eyebrow">SORĞU GÖNDƏRİLDİ</span><h1>İndi növbə brokerdədir.</h1><p>Sorğunuz <strong>#{confirmation.id}</strong> nömrəsi ilə qeydə alındı. Broker tarixləri yoxladıqdan sonra sizinlə əlaqə saxlayacaq.</p>{confirmation.demo ? <div className="demo-notice">Demo rejimi: sorğu ekranda uğurla simulyasiya edildi.</div> : <p>Status: <strong>{confirmation.statusName}</strong> · {confirmation.dates?.length ?? dates.length} gün · Cəmi <strong>{confirmation.totalAmount} ₼</strong></p>}<div className="confirmation-actions"><Link className="button button-primary" to={`/homes/${home.id}`}>Evə qayıt</Link><Link className="button button-ghost" to="/">Başqa evlərə bax</Link></div></section>

  return <form className="booking-form-layout" onSubmit={submit}>
    <div className="booking-form-main">
      <section className="form-section"><div className="form-step">1</div><div className="form-section-content"><h2>Rezervasiya tarixləri</h2><p>Uyğun günləri ayrıca seçin. Boz tarixlər artıq tutulub və ya broker tərəfindən bağlanıb.</p><div className="calendar-card"><div className="calendar-head"><button type="button">‹</button><strong>İyul 2026</strong><button type="button">›</button></div><div className="week-row">{['B.e', 'Ç.a', 'Ç', 'C.a', 'C', 'Ş', 'B'].map((day) => <span key={day}>{day}</span>)}</div><div className="date-grid">{availableDates.map((date) => <button type="button" disabled={unavailableDates.has(date.value)} className={`${dates.includes(date.value) ? 'selected' : ''} ${unavailableDates.has(date.value) ? 'unavailable' : ''}`} onClick={() => toggleDate(date.value)} key={date.value}>{date.label}</button>)}</div></div><div className="selected-dates" aria-label="Seçilmiş tarixlər">{dates.map((date) => <button type="button" key={date} onClick={() => toggleDate(date)}>{new Date(`${date}T00:00:00`).toLocaleDateString('az-AZ', { day: 'numeric', month: 'long' })} ×</button>)}</div>{!dates.length && <p className="field-error">Ən azı bir tarix seçin.</p>}</div></section>
      <section className="form-section"><div className="form-step">2</div><div className="form-section-content"><h2>Qonaqlar</h2><p>Bu evdə maksimum {home.guestCount} qonaq qala bilər.</p><div className="guest-stepper"><span>Qonaq sayı</span><div><button type="button" onClick={() => setGuests(Math.max(1, guests - 1))} aria-label="Qonaq sayını azalt"><Minus size={16} /></button><strong>{guests}</strong><button type="button" onClick={() => setGuests(Math.min(home.guestCount, guests + 1))} aria-label="Qonaq sayını artır"><Plus size={16} /></button></div></div></div></section>
      <section className="form-section"><div className="form-step">3</div><div className="form-section-content"><h2>Əlaqə məlumatları</h2><p>Broker təsdiq üçün bu məlumatlarla əlaqə saxlayacaq.</p><div className="input-grid"><label><span>Ad və soyad</span><input name="name" required placeholder="Məsələn, Aysel Məmmədova" /></label><label><span>Telefon nömrəsi</span><input name="phone" type="tel" required placeholder="+994 50 000 00 00" /></label><label className="full"><span>Broker üçün qeyd <em>istəyə bağlı</em></span><textarea name="note" placeholder="Gəliş vaxtı və ya əlavə sualınız" /></label></div></div></section>
    </div>
    <aside className="booking-summary"><div className="summary-home"><img src={home.images[0]} alt={home.imageAlt} /><div><strong>{home.title}</strong><span>{home.city}, {home.district}</span></div></div><div className="summary-divider" /><div className="summary-line"><span><CalendarDays size={16} /> Seçilmiş günlər</span><strong>{dates.length}</strong></div><div className="summary-line"><span>Gecəlik qiymət</span><strong>{home.dailyPrice} ₼</strong></div><div className="summary-line summary-total"><span>Cəmi</span><strong>{total} ₼</strong></div><div className="summary-notice"><ShieldCheck size={18} /><span>Beh tələb olunarsa məbləğ və son tarix broker təsdiqindən sonra göstəriləcək.</span></div><label className="consent"><input type="checkbox" required /><span>Məlumatlarımın rezervasiya üçün brokerə göndərilməsini qəbul edirəm.</span></label>{error && <p className="field-error">{error}</p>}<button className="button button-primary button-full" disabled={!dates.length || submitting}>{submitting ? 'Göndərilir…' : 'Sorğunu təsdiqlə'}</button></aside>
  </form>
}
