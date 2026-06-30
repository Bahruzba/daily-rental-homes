import { Check } from 'lucide-react'

export function AmenityList({ amenities }: { amenities: string[] }) {
  return <div className="amenities-grid">{amenities.map((amenity) => <span key={amenity}><Check size={17} /> {amenity}</span>)}</div>
}
