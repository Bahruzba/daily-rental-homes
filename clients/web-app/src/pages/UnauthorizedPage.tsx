import { ShieldX } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { dashboardPath } from '../auth/types'
import { AppLayout } from '../components/AppLayout'

export function UnauthorizedPage() {
  const { session } = useAuth()
  return <AppLayout><section className="unauthorized-page container"><ShieldX /><span className="eyebrow">İCAZƏ YOXDUR</span><h1>Bu səhifə rolunuza uyğun deyil.</h1><p>Sizin üçün ayrılmış panelə keçə və ya ana səhifəyə qayıda bilərsiniz.</p><div>{session && <Link className="button button-primary" to={dashboardPath(session.user.role)}>Mənim panelim</Link>}<Link className="button button-ghost" to="/">Ana səhifə</Link></div></section></AppLayout>
}
