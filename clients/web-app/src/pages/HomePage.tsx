import { ArrowRight, BadgeCheck, Headphones, SearchCheck, ShieldCheck } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { AppLayout } from '../components/AppLayout'
import { RentalHomeGrid } from '../components/RentalHomeGrid'
import { SearchFilters } from '../components/SearchFilters'
import { getRentalHomes } from '../api/client'
import type { RentalHome } from '../types'

export function HomePage() {
  const [homes, setHomes] = useState<RentalHome[]>([])
  const [city, setCity] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => { getRentalHomes().then(setHomes).finally(() => setLoading(false)) }, [])
  const visibleHomes = useMemo(() => city ? homes.filter((home) => home.city === city) : homes, [city, homes])

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
          <SearchFilters onSearch={setCity} />
        </div>
      </section>

      <section className="listing-section">
        <div className="container">
          <div className="section-heading">
            <div><span className="eyebrow">SEÇİLMİŞ MƏKANLAR</span><h2>{city ? `${city} üçün evlər` : 'İndi kəşf etməyə dəyər'}</h2><p>{visibleHomes.length} uyğun ev · qiymətlər bir gecə üçündür</p></div>
            <button className="text-link" onClick={() => setCity('')}>Hamısına bax <ArrowRight size={17} /></button>
          </div>
          <RentalHomeGrid homes={visibleHomes} loading={loading} onClear={() => setCity('')} />
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
