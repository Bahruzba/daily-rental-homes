import { Bell, CalendarCheck, ChevronRight, CircleDollarSign, Home, LayoutDashboard, Menu, MessageSquareText, Plus, Search, Settings, TrendingUp, UsersRound } from 'lucide-react'
import { Link } from 'react-router-dom'

const bookings = [
  { name: 'Aysel Məmmədova', home: 'Qəbələ hovuzlu ev', dates: '12–14 iyul', status: 'Yeni sorğu', tone: 'new' },
  { name: 'Murad Rzayev', home: 'Nabran dəniz villası', dates: '18–20 iyul', status: 'Beh gözlənilir', tone: 'waiting' },
  { name: 'Leyla Həsənli', home: 'İsmayıllı kottec', dates: '22–24 iyul', status: 'Təsdiqləndi', tone: 'success' },
]

export function DashboardPage() {
  return (
    <div className="dashboard-shell">
      <aside className="dashboard-sidebar">
        <Link className="brand dashboard-brand" to="/"><span className="brand-mark">D</span><span>daily<span>homes</span></span></Link>
        <nav><a className="active" href="#overview"><LayoutDashboard /> İcmal</a><a href="#homes"><Home /> Evlərim <span>8</span></a><a href="#bookings"><CalendarCheck /> Rezervasiyalar <span>5</span></a><a href="#deposits"><CircleDollarSign /> Beh ödənişləri <span>3</span></a><a href="#messages"><MessageSquareText /> Mesajlar</a></nav>
        <div className="sidebar-bottom"><a href="#settings"><Settings /> Tənzimləmələr</a><Link to="/">← Sayta qayıt</Link></div>
      </aside>
      <main className="dashboard-main">
        <header className="dashboard-topbar"><button className="icon-button dashboard-menu"><Menu /></button><div className="dashboard-search"><Search /><input placeholder="Rezervasiya və ya ev axtar" /></div><button className="icon-button"><Bell /></button><div className="profile-chip"><div>ƏM</div><span><strong>Əli Məmmədov</strong><small>Broker</small></span></div></header>
        <div className="dashboard-content">
          <div className="dashboard-heading"><div><span>30 iyun, bazar ertəsi</span><h1>Sabahınız xeyir, Əli.</h1><p>Bu gün diqqət tələb edən 4 iş var.</p></div><button className="button button-primary"><Plus /> Yeni ev əlavə et</button></div>
          <section className="kpi-grid"><article><div className="kpi-icon teal"><Home /></div><span>Aktiv evlər</span><strong>8</strong><small><TrendingUp /> 2 qaralama</small></article><article><div className="kpi-icon blue"><CalendarCheck /></div><span>Gözləyən sorğular</span><strong>5</strong><small>3-ü bu gün gəlib</small></article><article><div className="kpi-icon amber"><CircleDollarSign /></div><span>Beh gözlənilir</span><strong>3</strong><small>1 müddət bu gün bitir</small></article><article><div className="kpi-icon purple"><UsersRound /></div><span>Bu ay qonaq</span><strong>42</strong><small><TrendingUp /> +12% keçən aydan</small></article></section>
          <div className="dashboard-panels"><section className="dashboard-panel bookings-panel"><div className="panel-heading"><div><h2>Son rezervasiya sorğuları</h2><p>Ən yeni sorğular əvvəl göstərilir</p></div><button>Hamısına bax</button></div><div className="booking-table"><div className="table-head"><span>Müştəri</span><span>Ev</span><span>Tarixlər</span><span>Status</span><span /></div>{bookings.map((booking) => <div className="table-row" key={booking.name}><span><div className="mini-avatar">{booking.name.split(' ').map((part) => part[0]).join('')}</div>{booking.name}</span><span>{booking.home}</span><span>{booking.dates}</span><span><em className={`status-pill ${booking.tone}`}>{booking.status}</em></span><button><ChevronRight /></button></div>)}</div></section><aside className="dashboard-panel alerts-panel"><div className="panel-heading"><div><h2>Yaxın müddətlər</h2><p>Növbəti 24 saat</p></div></div><div className="deadline-item"><span className="deadline-dot urgent" /><div><strong>Beh üçün 2 saat qalıb</strong><p>#BK-104 · 120 ₼</p><button>WhatsApp xatırlatması</button></div></div><div className="deadline-item"><span className="deadline-dot" /><div><strong>Beh üçün 7 saat qalıb</strong><p>#BK-108 · 80 ₼</p><button>Müddəti uzat</button></div></div></aside></div>
        </div>
      </main>
    </div>
  )
}
