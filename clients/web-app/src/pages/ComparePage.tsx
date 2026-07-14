import { ArrowLeft, Scale } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getRentalHomeById } from '../api/client'
import { AppLayout } from '../components/AppLayout'
import { EmptyState } from '../components/EmptyState'
import { useCompareProperties } from '../hooks/useCompareProperties'
import type { RentalHome } from '../types'

const rows = [
  { label: 'Qiymət', value: (home: RentalHome) => `${home.dailyPrice} ₼ / gecə` },
  { label: 'Şəhər', value: (home: RentalHome) => home.city },
  { label: 'Rayon', value: (home: RentalHome) => home.district || '—' },
  { label: 'Otaq sayı', value: (home: RentalHome) => `${home.roomCount}` },
  { label: 'Qonaq sayı', value: (home: RentalHome) => `${home.guestCount}` },
  { label: 'Sahə', value: () => '—' },
  { label: 'Reytinq', value: (home: RentalHome) => `${home.rating} · ${home.reviews} rəy` },
]

export function ComparePage() {
  const { compareIds, clearCompare } = useCompareProperties()
  const [homes, setHomes] = useState<RentalHome[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    Promise.all(compareIds.map((id) => getRentalHomeById(id)))
      .then((result) => {
        if (!cancelled) setHomes(result.filter(Boolean) as RentalHome[])
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [compareIds])

  return <AppLayout>
    <section className="detail-page container">
      <Link className="back-link" to="/"><ArrowLeft size={16} /> Bütün evlər</Link>
      <div className="detail-title-row">
        <div>
          <span className="eyebrow">MÜQAYİSƏ</span>
          <h1>Seçilmiş elanları müqayisə edin</h1>
          <p className="detail-description">Eyni anda ən çox 3 elan müqayisə oluna bilər.</p>
        </div>
        {compareIds.length > 0 && <button type="button" className="button button-ghost" onClick={clearCompare}>Hamısını təmizlə</button>}
      </div>

      {loading ? <div className="page-loading">Müqayisə məlumatları yüklənir…</div> : homes.length ? (
        <div className="compare-table-wrap">
          <table className="compare-table">
            <thead>
              <tr>
                <th>Göstərici</th>
                {homes.map((home) => <th key={home.id}><img src={home.images[0]} alt={home.imageAlt} /><span>{home.title}</span></th>)}
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => <tr key={row.label}>
                <th>{row.label}</th>
                {homes.map((home) => <td key={home.id}>{row.value(home)}</td>)}
              </tr>)}
            </tbody>
          </table>
        </div>
      ) : <EmptyState icon={<Scale size={32} />} title="Müqayisə üçün elan seçilməyib" description="Ev kartlarında “Müqayisə et” düyməsi ilə 3-ə qədər elan seçə bilərsiniz." action={<Link className="button button-primary" to="/">Elanlara bax</Link>} />}
    </section>
  </AppLayout>
}
