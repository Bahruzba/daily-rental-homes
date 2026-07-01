import { BedDouble, MapPin, Star, UsersRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { RentalHome } from '../types'
import { AmenityList } from './AmenityList'
import { ContactBox } from './ContactBox'

export function RentalHomeDetail({ home }: { home: RentalHome }) {
  return <div className="detail-layout">
    <div className="detail-content">
      <section className="detail-block"><div className="fact-row"><div><BedDouble /><strong>{home.roomCount} otaq</strong><span>rahat plan</span></div><div><UsersRound /><strong>{home.guestCount} qonaq</strong><span>maksimum tutum</span></div><div><MapPin /><strong>{home.city}</strong><span>{home.district || 'mərkəz'}</span></div></div></section>
      <section className="detail-block"><span className="eyebrow">EV HAQQINDA</span><h2>Rahatlığınız üçün düşünülmüş məkan</h2><p className="detail-description">{home.description}</p></section>
      <section className="detail-block"><span className="eyebrow">RAHATLIQLAR</span><h2>Evdə nələr var?</h2><AmenityList amenities={home.amenities} /></section>
      <section className="detail-block"><span className="eyebrow">MƏKAN</span><h2>{home.city}{home.district ? `, ${home.district}` : ''}</h2><div className="map-placeholder"><MapPin size={28} /><strong>Dəqiq məkan rezervasiya təsdiqindən sonra</strong><span>Yaxın ərazi və yol məlumatı broker tərəfindən paylaşılacaq.</span></div></section>
      <ContactBox contact={home.contact} />
    </div>
    <aside className="booking-card">
      <div className="booking-price"><strong>{home.dailyPrice} ₼</strong><span>/ gecə</span></div><div className="booking-rating"><Star size={14} fill="currentColor" /> {home.rating} <span>· {home.reviews} rəy</span></div>
      <div className="booking-fields"><label><span>Tarixlər</span><input value="12 – 14 iyul" readOnly /></label><label><span>Qonaqlar</span><select defaultValue="4"><option value="2">2 qonaq</option><option value="4">4 qonaq</option><option value="6">6 qonaq</option></select></label></div>
      <div className="price-line"><span>{home.dailyPrice} ₼ × 3 gecə</span><span>{home.dailyPrice * 3} ₼</span></div><div className="price-line total"><span>Cəmi</span><span>{home.dailyPrice * 3} ₼</span></div>
      <Link className="button button-primary button-full" to={`/booking/${home.id}`}>Rezervasiya sorğusu göndər</Link><p className="booking-note">İndi ödəniş etmirsiniz. Broker əvvəlcə tarixləri təsdiqləyəcək.</p>
    </aside>
  </div>
}
