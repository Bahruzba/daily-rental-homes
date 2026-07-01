import { Heart, MessageCircle } from 'lucide-react'
import { Link } from 'react-router-dom'

export function Footer() {
  return (
    <footer className="site-footer">
      <div className="container footer-grid">
        <div>
          <Link className="brand footer-brand" to="/"><span className="brand-mark">D</span><span>daily<span>homes</span></span></Link>
          <p>Azərbaycanda günlük kirayə evləri daha rahat tapmaq və rezervasiya etmək üçün sadə platforma.</p>
        </div>
        <div><h4>Platforma</h4><Link to="/">Evlər</Link><a href="/#how">Necə işləyir?</a><Link to="/broker">Broker paneli</Link></div>
        <div><h4>Dəstək</h4><a href="mailto:hello@dailyhomes.az">Əlaqə</a><a href="#privacy">Məxfilik</a><a href="#help">Tez-tez verilən suallar</a></div>
        <div><h4>Bizi izləyin</h4><div className="social-row"><a className="icon-button" href="#instagram" aria-label="Instagram"><Heart size={18} /></a><a className="icon-button" href="#whatsapp" aria-label="WhatsApp"><MessageCircle size={18} /></a></div></div>
      </div>
      <div className="container footer-bottom"><span>© 2026 Daily Homes</span><span>AZ <span className="footer-dot">·</span> RU <span className="footer-dot">·</span> EN</span></div>
    </footer>
  )
}
