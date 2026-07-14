import { SearchCheck } from 'lucide-react'
import type { RentalHome } from '../types'
import { EmptyState } from './EmptyState'
import { RentalHomeCard } from './RentalHomeCard'

type Props = {
  homes: RentalHome[]
  loading: boolean
  onClear: () => void
  isCompared?: (id: number) => boolean
  onToggleCompare?: (id: number) => void
}

export function RentalHomeGrid({ homes, loading, onClear, isCompared, onToggleCompare }: Props) {
  if (loading) return <div className="loading-grid">{[1, 2, 3].map((item) => <div className="home-card skeleton" key={item} />)}</div>
  if (!homes.length) return <EmptyState icon={<SearchCheck size={32} />} title="Bu filtrə uyğun ev tapılmadı" description="Başqa bölgə seçin və ya filtrləri təmizləyin." action={<button type="button" className="button button-primary" onClick={onClear}>Bütün evlər</button>} />
  return <div className="homes-grid">{homes.map((home) => <RentalHomeCard key={home.id} home={home} isCompared={isCompared?.(home.id)} onToggleCompare={onToggleCompare} />)}</div>
}
