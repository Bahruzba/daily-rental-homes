import type { RentalHome } from '../types'

export function ImageGallery({ home }: { home: RentalHome }) {
  const images = home.images.length >= 4 ? home.images : [...home.images, ...home.images, ...home.images].slice(0, 4)
  return <div className="gallery-grid"><div className="gallery-main"><img src={images[0]} alt={home.imageAlt} /></div><div className="gallery-side"><img src={images[1]} alt={`${home.title} — həyət`} /><img src={images[2]} alt={`${home.title} — əlavə görünüş`} /><div className="gallery-more"><img src={images[3]} alt={`${home.title} — qalereya`} /><span>+8 şəkil</span></div></div></div>
}
