import { CalendarDays, ChevronLeft, ChevronRight, RefreshCw } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { getBrokerCalendarEvents, type BrokerCalendarEvent } from '../api/broker'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'

const monthFormatter = new Intl.DateTimeFormat('az-AZ', { month: 'long', year: 'numeric' })
const dayFormatter = new Intl.DateTimeFormat('az-AZ', { weekday: 'short' })
const isoDate = (date: Date) => {
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function BrokerCalendarPage() {
  const { session } = useAuth()
  const [cursor, setCursor] = useState(() => startOfMonth(new Date()))
  const [events, setEvents] = useState<BrokerCalendarEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const range = useMemo(() => {
    const from = startOfCalendar(cursor)
    const to = endOfCalendar(cursor)
    return { from: isoDate(from), to: isoDate(to) }
  }, [cursor])

  const days = useMemo(() => calendarDays(cursor), [cursor])

  const load = async () => {
    if (!session) return
    setLoading(true)
    setError('')
    try {
      setEvents(await getBrokerCalendarEvents(session.accessToken, range.from, range.to))
    } catch (cause) {
      console.error('Broker calendar load failed', cause)
      setError(cause instanceof Error ? cause.message : 'Təqvim yüklənmədi.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [session?.accessToken, range.from, range.to])

  const eventsByDay = useMemo(() => {
    const map = new Map<string, BrokerCalendarEvent[]>()
    for (const day of days) map.set(isoDate(day), [])
    for (const event of events) {
      for (const day of days) {
        const value = isoDate(day)
        if (value >= event.startDate && value <= event.endDate) {
          map.get(value)?.push(event)
        }
      }
    }
    return map
  }, [days, events])

  return (
    <AppLayout>
      <section className="broker-live-page">
        <div className="container">
          <div className="broker-live-heading">
            <div>
              <span className="eyebrow">BROKER TƏQVİMİ</span>
              <h1>Rezervasiya təqvimi</h1>
              <p>Rezervasiyaları və manual bloklanmış tarixləri ay görünüşündə izləyin.</p>
            </div>
            <button className="button button-ghost" onClick={() => void load()} disabled={loading}>
              <RefreshCw size={16} /> Yenilə
            </button>
          </div>

          <div className="broker-calendar-toolbar">
            <button className="button button-ghost" onClick={() => setCursor(addMonths(cursor, -1))}><ChevronLeft size={16} /> Əvvəlki ay</button>
            <strong>{monthFormatter.format(cursor)}</strong>
            <div>
              <button className="button button-ghost" onClick={() => setCursor(startOfMonth(new Date()))}>Bu gün</button>
              <button className="button button-ghost" onClick={() => setCursor(addMonths(cursor, 1))}>Növbəti ay <ChevronRight size={16} /></button>
            </div>
          </div>

          <div className="broker-calendar-legend">
            <span><i className="booking" /> Rezervasiya</span>
            <span><i className="manual-block" /> Manual blok</span>
          </div>

          {error && <div className="broker-error" role="alert">{error}</div>}

          {loading ? (
            <div className="broker-loading">Təqvim yüklənir…</div>
          ) : (
            <div className="broker-calendar-grid">
              {days.map((day) => {
                const value = isoDate(day)
                const isCurrentMonth = day.getMonth() === cursor.getMonth()
                const dayEvents = eventsByDay.get(value) ?? []
                return (
                  <article className={isCurrentMonth ? '' : 'is-muted'} key={value}>
                    <header>
                      <span>{dayFormatter.format(day)}</span>
                      <strong>{day.getDate()}</strong>
                    </header>
                    <div>
                      {dayEvents.slice(0, 3).map((event) => (
                        <Link
                          className={`broker-calendar-event ${event.eventType}`}
                          to={event.eventType === 'booking' && event.bookingId ? `/broker/bookings/${event.bookingId}` : `/broker/rental-homes/${event.rentalHomeId}/edit`}
                          key={`${event.eventType}-${event.bookingId ?? event.rentalHomeId}-${event.startDate}-${value}`}
                        >
                          <CalendarDays size={12} />
                          <span>{event.eventType === 'booking' ? event.customerName : 'Bloklanıb'}</span>
                          <small>{event.rentalHomeTitle}</small>
                        </Link>
                      ))}
                      {dayEvents.length > 3 && <em>+{dayEvents.length - 3} əlavə</em>}
                    </div>
                  </article>
                )
              })}
            </div>
          )}
        </div>
      </section>
    </AppLayout>
  )
}

function startOfMonth(value: Date) {
  return new Date(value.getFullYear(), value.getMonth(), 1)
}

function addMonths(value: Date, count: number) {
  return new Date(value.getFullYear(), value.getMonth() + count, 1)
}

function startOfCalendar(month: Date) {
  const start = startOfMonth(month)
  const day = (start.getDay() + 6) % 7
  const result = new Date(start)
  result.setDate(start.getDate() - day)
  return result
}

function endOfCalendar(month: Date) {
  const start = startOfCalendar(month)
  const result = new Date(start)
  result.setDate(start.getDate() + 41)
  return result
}

function calendarDays(month: Date) {
  const start = startOfCalendar(month)
  return Array.from({ length: 42 }, (_, index) => {
    const day = new Date(start)
    day.setDate(start.getDate() + index)
    return day
  })
}
