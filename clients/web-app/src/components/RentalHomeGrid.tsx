import { SearchCheck } from 'lucide-react'
import type { ReactNode } from 'react'
import type { RentalHome } from '../types'
import { EmptyState } from './EmptyState'
import { RentalHomeCard } from './RentalHomeCard'

type Props = {
  homes: RentalHome[]
  loading: boolean
  onClear: () => void
  isFavorite?: (id: number) => boolean
  onToggleFavorite?: (id: number) => void
  isCompared?: (id: number) => boolean
  onToggleCompare?: (id: number) => void
  emptyTitle?: string
  emptyDescription?: string
  emptyAction?: ReactNode
}

export function RentalHomeGrid({ homes, loading, onClear, isFavorite, onToggleFavorite, isCompared, onToggleCompare, emptyTitle = 'Bu filtrə uyğun ev tapılmadı', emptyDescription = 'Başqa bölgə seçin və ya filtrləri təmizləyin.', emptyAction }: Props) {
  if (loading) return <div className="loading-grid">{[1, 2, 3].map((item) => <div className="home-card skeleton" key={item} />)}</div>
  if (!homes.length) return <EmptyState icon={<SearchCheck size={32} />} title={emptyTitle} description={emptyDescription} action={emptyAction ?? <button type="button" className="button button-primary" onClick={onClear}>Bütün evlər</button>} />
  return <div className="homes-grid">{homes.map((home) => <RentalHomeCard key={home.id} home={home} isFavorite={isFavorite?.(home.id)} onToggleFavorite={onToggleFavorite} isCompared={isCompared?.(home.id)} onToggleCompare={onToggleCompare} />)}</div>
}
