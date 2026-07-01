import { ArrowLeft } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getRentalHomeById } from '../api/client'
import { AppLayout } from '../components/AppLayout'
import { BookingForm } from '../components/BookingForm'
import type { RentalHome } from '../types'

export function BookingPage() {
  const { homeId } = useParams()
  const [home, setHome] = useState<RentalHome>()
  useEffect(() => { getRentalHomeById(Number(homeId)).then(setHome) }, [homeId])

  if (!home) return <AppLayout><div className="container page-loading">Rezervasiya formu yüklənir…</div></AppLayout>

  return <AppLayout><section className="booking-page container"><Link className="back-link" to={`/homes/${home.id}`}><ArrowLeft size={16} /> Evə qayıt</Link><div className="booking-page-heading"><span className="eyebrow">REZERVASİYA SORĞUSU</span><h1>Tarixləri seçin,<br />qalanını biz sadələşdirək.</h1><p>Bu mərhələdə ödəniş tələb olunmur.</p></div><BookingForm home={home} /></section></AppLayout>
}
