import { Heart, MapPin, Star, UsersRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { RentalHome } from '../types'

type Props = {
  home: RentalHome
  isFavorite?: boolean
  onToggleFavorite?: (id: number) => void
  isCompared?: boolean
  onToggleCompare?: (id: number) => void
}

export function RentalHomeCard({ home, isFavorite = false, onToggleFavorite, isCompared = false, onToggleCompare }: Props) {
  const favoriteLabel = isFavorite ? 'Seçilmişlərdən çıxar' : 'Seçilmişlərə əlavə et'

  return (
    <article className="home-card">
      <Link className="card-image-wrap" to={`/homes/${home.id}`}>
        <img className="card-image" src={home.images[0]} alt={home.imageAlt} />
        {home.badge && <span className="card-badge">{home.badge}</span>}
      </Link>
      <button type="button" className={`card-favorite${isFavorite ? ' is-favorite' : ''}`} aria-label={favoriteLabel} title={favoriteLabel} onClick={() => onToggleFavorite?.(home.id)}><Heart size={18} fill={isFavorite ? 'currentColor' : 'none'} /></button>
      <div className="home-card-body">
        <div className="card-topline"><span className="card-location"><MapPin size={14} /> {home.city}{home.district ? `, ${home.district}` : ''}</span><span className="rating"><Star size={14} fill="currentColor" /> {home.rating}</span></div>
        <Link to={`/homes/${home.id}`}><h3>{home.title}</h3></Link>
        <div className="card-meta"><span>{home.roomCount} otaq</span><span><UsersRound size={15} /> {home.guestCount} qonaq</span></div>
        <div className="card-footer"><span className="card-price">{home.dailyPrice} ₼ <small>/ gecə</small></span><span className="review-count">{home.reviews} rəy</span></div>
        <button type="button" className={`button button-outline compare-card-button${isCompared ? ' is-active' : ''}`} onClick={() => onToggleCompare?.(home.id)}>{isCompared ? 'Müqayisədən çıxar' : 'Müqayisə et'}</button>
      </div>
    </article>
  )
}
