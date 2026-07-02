import { Building2, CalendarDays, ClipboardList, CreditCard, Heart, Home, Settings, Users } from 'lucide-react'
import { Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { BookingPage } from './pages/BookingPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RentalDetailPage } from './pages/RentalDetailPage'
import { RoleDashboardPage } from './pages/RoleDashboardPage'
import { UnauthorizedPage } from './pages/UnauthorizedPage'

const adminItems = [
  { title: 'Evlər', description: 'Platformadakı ev elanları üçün idarəetmə sahəsi.', icon: Home },
  { title: 'Brokerlər', description: 'Broker hesabları və gələcək təsdiq axını.', icon: Users },
  { title: 'Rezervasiyalar', description: 'Bütün rezervasiya sorğularının xülasəsi.', icon: ClipboardList },
  { title: 'Tənzimləmələr', description: 'Sistem parametrləri üçün ayrılmış sahə.', icon: Settings },
]
const brokerItems = [
  { title: 'Evlərim', description: 'Brokerə bağlı ev elanlarının gələcək siyahısı.', icon: Building2 },
  { title: 'Rezervasiyalar', description: 'Gələn rezervasiya sorğularının xülasəsi.', icon: ClipboardList },
  { title: 'Gözləyən beh', description: 'Ödəniş gözləyən sorğular üçün ayrılmış sahə.', icon: CreditCard },
  { title: 'Təqvim', description: 'Ev və rezervasiya tarixlərinin gələcək görünüşü.', icon: CalendarDays },
]
const customerItems = [
  { title: 'Rezervasiyalarım', description: 'Göndərdiyiniz sorğuların gələcək siyahısı.', icon: ClipboardList },
  { title: 'Gözləyən ödənişlər', description: 'Ödəniş addımları üçün ayrılmış sahə.', icon: CreditCard },
  { title: 'Seçilmiş evlər', description: 'Bəyəndiyiniz evlərin gələcək siyahısı.', icon: Heart },
  { title: 'Paylaşılan linklər', description: 'Paylaşdığınız elanlar üçün ayrılmış sahə.', icon: Home },
]

export default function App() {
  return <Routes>
    <Route path="/" element={<HomePage />} />
    <Route path="/homes/:id" element={<RentalDetailPage />} />
    <Route path="/booking/:homeId" element={<BookingPage />} />
    <Route path="/login" element={<LoginPage />} />
    <Route path="/unauthorized" element={<UnauthorizedPage />} />
    <Route path="/admin" element={<ProtectedRoute roles={['Admin']}><RoleDashboardPage role="Admin" eyebrow="ADMİN PANELİ" title="İdarəetmə xülasəsi" description="Platformanın əsas idarəetmə bölmələri." items={adminItems} /></ProtectedRoute>} />
    <Route path="/broker" element={<ProtectedRoute roles={['Broker']}><RoleDashboardPage role="Broker" eyebrow="BROKER PANELİ" title="İş sahəniz" description="Elan və rezervasiya axınlarına qısa baxış." items={brokerItems} /></ProtectedRoute>} />
    <Route path="/account" element={<ProtectedRoute roles={['Customer']}><RoleDashboardPage role="Customer" eyebrow="MÜŞTƏRİ HESABI" title="Səyahət planlarınız" description="Rezervasiya və seçilmiş evlər üçün şəxsi sahə." items={customerItems} /></ProtectedRoute>} />
    <Route path="*" element={<NotFoundPage />} />
  </Routes>
}
