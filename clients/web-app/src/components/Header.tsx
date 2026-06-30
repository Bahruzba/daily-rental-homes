import { Heart, Menu, UserRound, X } from 'lucide-react'
import { useState } from 'react'
import { Link, NavLink } from 'react-router-dom'

export function Header() {
  const [open, setOpen] = useState(false)

  return (
    <header className="site-header">
      <div className="container header-inner">
        <Link className="brand" to="/" aria-label="Daily Homes ana səhifə">
          <span className="brand-mark">D</span>
          <span>daily<span>homes</span></span>
        </Link>

        <nav className={`main-nav ${open ? 'is-open' : ''}`} aria-label="Əsas menyu">
          <NavLink to="/" onClick={() => setOpen(false)}>Evlər</NavLink>
          <a href="/#how" onClick={() => setOpen(false)}>Necə işləyir?</a>
          <NavLink to="/broker" onClick={() => setOpen(false)}>Broker paneli</NavLink>
          <div className="mobile-nav-actions">
            <button className="button button-ghost"><UserRound size={17} /> Daxil ol</button>
            <button className="button button-primary">Elan əlavə et</button>
          </div>
        </nav>

        <div className="header-actions">
          <button className="icon-button" aria-label="Seçilmişlər"><Heart size={19} /></button>
          <button className="button button-ghost login-button"><UserRound size={17} /> Daxil ol</button>
          <button className="button button-primary listing-button">Elan əlavə et</button>
          <button className="icon-button menu-button" onClick={() => setOpen((value) => !value)} aria-label="Menyunu aç" aria-expanded={open}>
            {open ? <X size={21} /> : <Menu size={21} />}
          </button>
        </div>
      </div>
    </header>
  )
}
