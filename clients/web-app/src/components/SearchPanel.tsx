import { CalendarDays, MapPin, Search, SlidersHorizontal, UsersRound } from 'lucide-react'
import { useState } from 'react'

type SearchPanelProps = {
  onSearch?: (city: string) => void
}

export function SearchPanel({ onSearch }: SearchPanelProps) {
  const [city, setCity] = useState('')
  const [more, setMore] = useState(false)

  return (
    <div className="search-panel">
      <div className="search-fields">
        <label className="search-field">
          <span><MapPin size={16} /> Məkan</span>
          <select value={city} onChange={(event) => setCity(event.target.value)}>
            <option value="">Bütün bölgələr</option><option>Qəbələ</option><option>İsmayıllı</option><option>Nabran</option><option>Şəki</option><option>Quba</option><option>Mərdəkan</option>
          </select>
        </label>
        <label className="search-field">
          <span><CalendarDays size={16} /> Tarixlər</span>
          <input value="12 – 14 iyul" readOnly aria-label="Tarixlər" />
        </label>
        <label className="search-field">
          <span><UsersRound size={16} /> Qonaqlar</span>
          <select defaultValue="4"><option value="2">2 qonaq</option><option value="4">4 qonaq</option><option value="6">6 qonaq</option><option value="8">8+ qonaq</option></select>
        </label>
        <button className="button button-primary search-button" onClick={() => onSearch?.(city)}><Search size={18} /> Axtar</button>
      </div>
      <div className="quick-filters">
        <button className={`filter-chip ${more ? 'active' : ''}`} onClick={() => setMore((value) => !value)}><SlidersHorizontal size={15} /> Daha çox filtr</button>
        <button className="filter-chip">Hovuz</button><button className="filter-chip">Wi-Fi</button><button className="filter-chip">Manqal</button><button className="filter-chip">Parking</button>
      </div>
      {more && <div className="more-filters"><label>Minimum qiymət<input type="number" placeholder="50 ₼" /></label><label>Maksimum qiymət<input type="number" placeholder="300 ₼" /></label><label>Otaq sayı<select><option>Fərqi yoxdur</option><option>2+</option><option>3+</option><option>4+</option></select></label><button className="button button-dark">Filtrləri tətbiq et</button></div>}
    </div>
  )
}
