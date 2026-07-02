import { Heart, LogOut, Menu, UserRound, X } from 'lucide-react'
import { useState } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { dashboardPath } from '../auth/types'

export function Header() {
  const [open, setOpen] = useState(false)
  const { session, logout } = useAuth()
  const panelPath = session ? dashboardPath(session.user.role) : '/login'
  const close = () => setOpen(false)
  const logoutAndClose = () => { logout(); close() }

  return (
    <header className="site-header">
      <div className="container header-inner">
        <Link className="brand" to="/" aria-label="Daily Homes ana səhifə"><span className="brand-mark">D</span><span>daily<span>homes</span></span></Link>
        <nav className={`main-nav ${open ? 'is-open' : ''}`} aria-label="Əsas menyu">
          <NavLink to="/" onClick={close}>Evlər</NavLink>
          <a href="/#how" onClick={close}>Necə işləyir?</a>
          {session && <NavLink to={panelPath} onClick={close}>Mənim panelim</NavLink>}
          <div className="mobile-nav-actions">
            {session ? <><Link className="button button-ghost" to={panelPath} onClick={close}><UserRound size={17} /> {session.user.role}</Link><button className="button button-ghost" onClick={logoutAndClose}><LogOut size={17} /> Çıxış</button></> : <Link className="button button-ghost" to="/login" onClick={close}><UserRound size={17} /> Daxil ol</Link>}
            {(session?.user.role === 'Admin' || session?.user.role === 'Broker') && <Link className="button button-primary" to={panelPath} onClick={close}>Elan əlavə et</Link>}
          </div>
        </nav>
        <div className="header-actions">
          <button className="icon-button" aria-label="Seçilmişlər"><Heart size={19} /></button>
          {session ? <><Link className="button button-ghost login-button" to={panelPath}><UserRound size={17} /> {session.user.role}</Link><button className="button button-ghost login-button" onClick={logout}><LogOut size={17} /> Çıxış</button></> : <Link className="button button-ghost login-button" to="/login"><UserRound size={17} /> Daxil ol</Link>}
          {(session?.user.role === 'Admin' || session?.user.role === 'Broker') && <Link className="button button-primary listing-button" to={panelPath}>Elan əlavə et</Link>}
          <button className="icon-button menu-button" onClick={() => setOpen((value) => !value)} aria-label="Menyunu aç" aria-expanded={open}>{open ? <X size={21} /> : <Menu size={21} />}</button>
        </div>
      </div>
    </header>
  )
}
