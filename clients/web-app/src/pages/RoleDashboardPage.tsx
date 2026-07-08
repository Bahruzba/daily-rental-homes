import { ArrowLeft, LogOut, type LucideIcon } from 'lucide-react'
import { Link, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { dashboardPath, type AuthRole } from '../auth/types'
import { AppLayout } from '../components/AppLayout'

type DashboardItem = { title: string; description: string; icon: LucideIcon; to?: string }

type Props = {
  role: AuthRole
  eyebrow: string
  title: string
  description: string
  items: DashboardItem[]
}

export function RoleDashboardPage({ role, eyebrow, title, description, items }: Props) {
  const { session, logout } = useAuth()
  if (!session) return null
  if (session.user.role !== role) return <Navigate to={dashboardPath(session.user.role)} replace />

  return (
    <AppLayout>
      <section className="role-dashboard-page">
        <div className="container">
          <div className="role-dashboard-heading">
            <div><span className="eyebrow">{eyebrow}</span><h1>{title}</h1><p>{description}</p></div>
            <Link className="button button-ghost" to="/"><ArrowLeft size={17} /> Ana səhifə</Link>
          </div>
          <div className="session-banner">
            <div className="session-avatar">{session.user.fullName.slice(0, 2).toUpperCase()}</div>
            <div><strong>{session.user.fullName}</strong><span>{session.user.phone} · {session.user.role}</span></div>
            <button className="button button-ghost" onClick={logout}><LogOut size={17} /> Çıxış</button>
          </div>
          <div className="role-dashboard-grid">{items.map(({ title: itemTitle, description: itemDescription, icon: Icon, to }) => {
            const content = <><Icon /><h2>{itemTitle}</h2><p>{itemDescription}</p><span>{to ? 'Bax' : 'MVP placeholder'}</span></>
            return to
              ? <Link className="role-dashboard-card-link" to={to} key={itemTitle}>{content}</Link>
              : <article key={itemTitle}>{content}</article>
          })}</div>
        </div>
      </section>
    </AppLayout>
  )
}
