import { ArrowRight, BadgeCheck, Headphones, SearchCheck, ShieldCheck } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { AppLayout } from '../components/AppLayout'
import { RentalHomeGrid } from '../components/RentalHomeGrid'
import { SearchFilters } from '../components/SearchFilters'
import { getRentalHomes, type RentalHomeFilters } from '../api/client'
import { useCompareProperties } from '../hooks/useCompareProperties'
import type { RentalHome } from '../types'

const filterKeys: Array<keyof RentalHomeFilters> = ['q', 'city', 'district', 'guests', 'minPrice', 'maxPrice', 'startDate', 'endDate']
const sortStorageKey = 'daily-homes-public-property-sort'
const sortOptions = ['', 'newest', 'price-asc', 'price-desc', 'name-asc'] as const
type SortOption = typeof sortOptions[number]

function filtersFromParams(params: URLSearchParams): RentalHomeFilters {
  return Object.fromEntries(filterKeys.map((key) => [key, params.get(key) ?? '']).filter(([, value]) => value)) as RentalHomeFilters
}

function validateFilters(filters: RentalHomeFilters) {
  if ((filters.startDate && !filters.endDate) || (!filters.startDate && filters.endDate)) return 'Tarix filtrində həm başlanğıc, həm də bitiş tarixi seçilməlidir.'
  if (filters.startDate && filters.endDate && filters.startDate > filters.endDate) return 'Başlanğıc tarixi bitiş tarixindən sonra ola bilməz.'
  if (Number(filters.guests || 0) < 0) return 'Qonaq sayı mənfi ola bilməz.'
  if (Number(filters.minPrice || 0) < 0 || Number(filters.maxPrice || 0) < 0) return 'Qiymət filtrləri mənfi ola bilməz.'
  if (filters.minPrice && filters.maxPrice && Number(filters.minPrice) > Number(filters.maxPrice)) return 'Minimum qiymət maksimum qiymətdən böyük ola bilməz.'
  return ''
}

function hasFilters(filters: RentalHomeFilters) {
  return Object.values(filters).some((value) => `${value ?? ''}`.trim())
}

function readSavedSort(): SortOption {
  try {
    const saved = window.localStorage.getItem(sortStorageKey)
    return sortOptions.includes(saved as SortOption) ? saved as SortOption : ''
  } catch {
    return ''
  }
}

function sortHomes(homes: RentalHome[], sort: SortOption) {
  const nextHomes = [...homes]
  if (sort === 'newest') return nextHomes.sort((left, right) => right.id - left.id)
  if (sort === 'price-asc') return nextHomes.sort((left, right) => left.dailyPrice - right.dailyPrice)
  if (sort === 'price-desc') return nextHomes.sort((left, right) => right.dailyPrice - left.dailyPrice)
  if (sort === 'name-asc') return nextHomes.sort((left, right) => left.title.localeCompare(right.title, 'az'))
  return nextHomes
}

export function HomePage() {
  const [params, setParams] = useSearchParams()
  const filters = useMemo(() => filtersFromParams(params), [params])
  const [homes, setHomes] = useState<RentalHome[]>([])
  const [sort, setSort] = useState<SortOption>(readSavedSort)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [compareError, setCompareError] = useState('')
  const { compareIds, isCompared, setCompared } = useCompareProperties()
  const sortedHomes = useMemo(() => sortHomes(homes, sort), [homes, sort])

  useEffect(() => {
    const validation = validateFilters(filters)
    if (validation) {
      setError(validation)
      setHomes([])
      setLoading(false)
      return
    }

    setLoading(true)
    setError('')
    getRentalHomes(filters)
      .then(setHomes)
      .catch((cause) => {
        console.error('Rental homes search failed', cause)
        setError(cause instanceof Error ? cause.message : 'Ev siyahısı yüklənmədi.')
        setHomes([])
      })
      .finally(() => setLoading(false))
  }, [filters])

  const applyFilters = (nextFilters: RentalHomeFilters) => {
    const clean = new URLSearchParams()
    filterKeys.forEach((key) => {
      const value = nextFilters[key]
      if (value?.trim()) clean.set(key, value.trim())
    })
    const validation = validateFilters(Object.fromEntries(clean) as RentalHomeFilters)
    if (validation) {
      setError(validation)
      return
    }
    setParams(clean)
  }

  const clearFilters = () => {
    setError('')
    setParams(new URLSearchParams())
  }

  const updateSort = (nextSort: SortOption) => {
    setSort(nextSort)
    try {
      if (nextSort) window.localStorage.setItem(sortStorageKey, nextSort)
      else window.localStorage.removeItem(sortStorageKey)
    } catch {
      // localStorage may be unavailable in restricted browsers; sorting still works for the current session.
    }
  }

  const toggleCompare = (id: number) => {
    setCompareError('')
    if (isCompared(id)) {
      setCompared(id, false)
      return
    }
    if (compareIds.length >= 3) {
      setCompareError('Ən çox 3 elan müqayisə edilə bilər.')
      return
    }
    setCompared(id, true)
  }

  return (
    <AppLayout>
      <section className="hero-section">
        <div className="hero-glow hero-glow-one" /><div className="hero-glow hero-glow-two" />
        <div className="container hero-content">
          <div className="hero-copy">
            <span className="eyebrow">AZƏRBAYCAN ÜZRƏ GÜNLÜK EVLƏR</span>
            <h1>Bir neçə günlük.<br /><em>Uzun yadda qalacaq.</em></h1>
            <p>Dağ evindən hovuzlu villaya — istirahətinizə uyğun məkanı tapın, tarixləri seçin və rezervasiya sorğusunu rahat göndərin.</p>
          </div>
          <div className="hero-mini-card">
            <img src={`${import.meta.env.BASE_URL}images/ismayilli-cottage.webp`} alt="İsmayıllıda meşə kotteci" />
            <div><span>Bu həftənin seçimi</span><strong>Meşə içində kottec</strong><small>İsmayıllı · 125 ₼</small></div>
          </div>
          <SearchFilters filters={filters} onSearch={applyFilters} onClear={clearFilters} />
        </div>
      </section>

      <section className="listing-section">
        <div className="container">
          <div className="section-heading">
            <div>
              <span className="eyebrow">SEÇİLMİŞ MƏKANLAR</span>
              <h2>{hasFilters(filters) ? 'Axtarış nəticələri' : 'İndi kəşf etməyə dəyər'}</h2>
              <p>{error ? 'Filtrləri düzəldib yenidən yoxlayın' : `${sortedHomes.length} uyğun ev · qiymətlər bir gecə üçündür`}</p>
            </div>
            <div className="listing-actions">
              <label className="sort-control"><span>Sırala</span><select value={sort} onChange={(event) => updateSort(event.target.value as SortOption)}>
                <option value="">Standart</option>
                <option value="newest">Yeni elanlar</option>
                <option value="price-asc">Qiymət (artan)</option>
                <option value="price-desc">Qiymət (azalan)</option>
                <option value="name-asc">Ad (A-Z)</option>
              </select></label>
              <Link className="button button-ghost compare-link" to="/compare">Müqayisə {compareIds.length ? `(${compareIds.length})` : ''}</Link>
              <button className="text-link" onClick={clearFilters}>Hamısına bax <ArrowRight size={17} /></button>
            </div>
          </div>
          {error && <div className="broker-error" role="alert">{error}</div>}
          {compareError && <div className="broker-error" role="alert">{compareError}</div>}
          <RentalHomeGrid homes={sortedHomes} loading={loading} onClear={clearFilters} isCompared={isCompared} onToggleCompare={toggleCompare} />
        </div>
      </section>

      <section className="how-section" id="how">
        <div className="container">
          <div className="section-heading centered"><div><span className="eyebrow">SADƏ VƏ AYDIN</span><h2>Üç addımda istirahətə hazır</h2><p>Ödəniş etməzdən əvvəl hər detalı brokerlə dəqiqləşdirin.</p></div></div>
          <div className="steps-grid">
            <article><span className="step-number">01</span><SearchCheck /><h3>Evi kəşf edin</h3><p>Şəkillərə, rahatlıqlara və qonaq tutumuna baxın.</p></article>
            <article><span className="step-number">02</span><BadgeCheck /><h3>Tarixləri seçin</h3><p>Uyğun günləri ayrıca qeyd edib sorğunuzu hazırlayın.</p></article>
            <article><span className="step-number">03</span><Headphones /><h3>Brokerlə təsdiqləyin</h3><p>Broker sorğunu yoxlayır və son detalları sizinlə bölüşür.</p></article>
          </div>
        </div>
      </section>

      <section className="trust-section"><div className="container trust-card"><div><span className="eyebrow light">ETİBARLI REZERVASİYA AXINI</span><h2>Ev tapmaq rahat, qərar vermək aydın olsun.</h2><p>Yoxlanılan məlumatlar, şəffaf qiymət xülasəsi və brokerlə birbaşa əlaqə.</p></div><div className="trust-points"><span><ShieldCheck /> Təhlükəsiz OTP girişi</span><span><BadgeCheck /> Aydın status izləmə</span><span><Headphones /> Birbaşa əlaqə</span></div></div></section>
    </AppLayout>
  )
}
