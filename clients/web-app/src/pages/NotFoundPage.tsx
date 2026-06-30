import { Link } from 'react-router-dom'
import { AppLayout } from '../components/AppLayout'

export function NotFoundPage() {
  return <AppLayout><section className="not-found container"><span className="eyebrow">404 · SƏHİFƏ TAPILMADI</span><h1>Bu ünvan bizi bir evə aparmadı.</h1><p>Link köhnəlmiş və ya səhv yazılmış ola bilər.</p><Link className="button button-primary" to="/">Ana səhifəyə qayıt</Link></section></AppLayout>
}
