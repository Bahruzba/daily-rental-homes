import { CalendarDays, MapPin, Search, SlidersHorizontal, UsersRound } from 'lucide-react'
import { type FormEvent, useEffect, useState } from 'react'
import type { RentalHomeFilters } from '../api/client'

type SearchPanelProps = {
  filters?: RentalHomeFilters
  onSearch?: (filters: RentalHomeFilters) => void
  onClear?: () => void
}

const cities = ['Qəbələ', 'İsmayıllı', 'Nabran', 'Şəki', 'Quba', 'Mərdəkan']

export function SearchPanel({ filters, onSearch, onClear }: SearchPanelProps) {
  const [values, setValues] = useState<RentalHomeFilters>(filters ?? {})
  const [more, setMore] = useState(false)

  useEffect(() => {
    setValues(filters ?? {})
  }, [filters])

  const set = (key: keyof RentalHomeFilters, value: string) => setValues((current) => ({ ...current, [key]: value }))
  const submit = (event: FormEvent) => {
    event.preventDefault()
    onSearch?.(values)
  }

  return (
    <form className="search-panel" onSubmit={submit}>
      <div className="search-fields">
        <label className="search-field">
          <span><Search size={16} /> Axtarış</span>
          <input value={values.q ?? ''} onChange={(event) => set('q', event.target.value)} placeholder="Ev, rayon, təsvir..." />
        </label>
        <label className="search-field">
          <span><MapPin size={16} /> Şəhər</span>
          <select value={values.city ?? ''} onChange={(event) => set('city', event.target.value)}>
            <option value="">Bütün bölgələr</option>
            {cities.map((city) => <option key={city} value={city}>{city}</option>)}
          </select>
        </label>
        <label className="search-field">
          <span><UsersRound size={16} /> Qonaqlar</span>
          <input type="number" min="1" value={values.guests ?? ''} onChange={(event) => set('guests', event.target.value)} placeholder="4" />
        </label>
        <button className="button button-primary search-button" type="submit"><Search size={18} /> Axtar</button>
      </div>

      <div className="quick-filters">
        <button type="button" className={`filter-chip ${more ? 'active' : ''}`} onClick={() => setMore((value) => !value)}>
          <SlidersHorizontal size={15} /> Daha çox filtr
        </button>
        <button type="button" className="filter-chip" onClick={() => set('city', 'Qəbələ')}>Qəbələ</button>
        <button type="button" className="filter-chip" onClick={() => set('city', 'İsmayıllı')}>İsmayıllı</button>
        <button type="button" className="filter-chip" onClick={() => set('guests', '8')}>8+ qonaq</button>
        <button type="button" className="filter-chip" onClick={onClear}>Təmizlə</button>
      </div>

      {more && (
        <div className="more-filters">
          <label>Rayon / qəsəbə<input value={values.district ?? ''} onChange={(event) => set('district', event.target.value)} placeholder="Vəndam" /></label>
          <label>Minimum qiymət<input type="number" min="0" value={values.minPrice ?? ''} onChange={(event) => set('minPrice', event.target.value)} placeholder="50 ₼" /></label>
          <label>Maksimum qiymət<input type="number" min="0" value={values.maxPrice ?? ''} onChange={(event) => set('maxPrice', event.target.value)} placeholder="300 ₼" /></label>
          <label><span><CalendarDays size={14} /> Başlanğıc</span><input type="date" value={values.startDate ?? ''} onChange={(event) => set('startDate', event.target.value)} /></label>
          <label><span><CalendarDays size={14} /> Bitiş</span><input type="date" value={values.endDate ?? ''} onChange={(event) => set('endDate', event.target.value)} /></label>
          <button className="button button-dark" type="submit">Filtrləri tətbiq et</button>
        </div>
      )}
    </form>
  )
}
