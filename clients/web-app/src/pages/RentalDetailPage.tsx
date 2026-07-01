import { ArrowLeft, Heart, MapPin, Share2, Star } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { AppLayout } from '../components/AppLayout'
import { ImageGallery } from '../components/ImageGallery'
import { RentalHomeDetail } from '../components/RentalHomeDetail'
import { getRentalHomeById } from '../api/client'
import type { RentalHome } from '../types'

export function RentalDetailPage() {
  const { id } = useParams()
  const [home, setHome] = useState<RentalHome>()
  useEffect(() => { getRentalHomeById(Number(id)).then(setHome) }, [id])

  if (!home) return <AppLayout><div className="container page-loading">Ev məlumatları yüklənir…</div></AppLayout>

  return (
    <AppLayout>
      <section className="detail-page container">
        <Link className="back-link" to="/"><ArrowLeft size={16} /> Bütün evlər</Link>
        <div className="detail-title-row"><div><div className="detail-location"><MapPin size={15} /> {home.city}{home.district ? `, ${home.district}` : ''}</div><h1>{home.title}</h1><div className="detail-submeta"><span><Star size={15} fill="currentColor" /> {home.rating} · {home.reviews} rəy</span><span>Elan #{1000 + home.id}</span></div></div><div className="detail-actions"><button className="button button-ghost"><Share2 size={17} /> Paylaş</button><button className="button button-ghost"><Heart size={17} /> Saxla</button></div></div>

        <ImageGallery home={home} />
        <RentalHomeDetail home={home} />
      </section>
      <div className="mobile-booking-bar"><div><strong>{home.dailyPrice} ₼</strong><span>/ gecə</span></div><Link className="button button-primary" to={`/booking/${home.id}`}>Rezervasiya</Link></div>
    </AppLayout>
  )
}
