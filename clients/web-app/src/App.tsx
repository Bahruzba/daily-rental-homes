import { Bell, ClipboardList, Home, Settings, Users } from 'lucide-react'
import { Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { AccountBookingDetailPage } from './pages/AccountBookingDetailPage'
import { AccountDashboardPage } from './pages/AccountDashboardPage'
import { AdminNotificationsPage } from './pages/AdminNotificationsPage'
import { BookingPage } from './pages/BookingPage'
import { BrokerBookingDetailPage } from './pages/BrokerBookingDetailPage'
import { BrokerCalendarPage } from './pages/BrokerCalendarPage'
import { BrokerDashboardPage } from './pages/BrokerDashboardPage'
import { BrokerRentalHomeManagePage } from './pages/BrokerRentalHomeManagePage'
import { ComparePage } from './pages/ComparePage'
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
  { title: 'Bildirişlər', description: 'WhatsApp/SMS outbox mesajlarını və statuslarını oxu.', icon: Bell, to: '/admin/notifications' },
  { title: 'Tənzimləmələr', description: 'Sistem parametrləri üçün ayrılmış sahə.', icon: Settings },
]

export default function App() {
  return <Routes>
    <Route path="/" element={<HomePage />} />
    <Route path="/homes/:id" element={<RentalDetailPage />} />
    <Route path="/compare" element={<ComparePage />} />
    <Route path="/booking/:homeId" element={<BookingPage />} />
    <Route path="/login" element={<LoginPage />} />
    <Route path="/unauthorized" element={<UnauthorizedPage />} />
    <Route path="/admin" element={<ProtectedRoute roles={['Admin']}><RoleDashboardPage role="Admin" eyebrow="ADMİN PANELİ" title="İdarəetmə xülasəsi" description="Platformanın əsas idarəetmə bölmələri." items={adminItems} /></ProtectedRoute>} />
    <Route path="/admin/notifications" element={<ProtectedRoute roles={['Admin']}><AdminNotificationsPage /></ProtectedRoute>} />
    <Route path="/broker" element={<ProtectedRoute roles={['Broker']}><BrokerDashboardPage /></ProtectedRoute>} />
    <Route path="/broker/calendar" element={<ProtectedRoute roles={['Broker']}><BrokerCalendarPage /></ProtectedRoute>} />
    <Route path="/broker/bookings/:id" element={<ProtectedRoute roles={['Broker']}><BrokerBookingDetailPage /></ProtectedRoute>} />
    <Route path="/broker/rental-homes/new" element={<ProtectedRoute roles={['Broker']}><BrokerRentalHomeManagePage /></ProtectedRoute>} />
    <Route path="/broker/rental-homes/:id/edit" element={<ProtectedRoute roles={['Broker']}><BrokerRentalHomeManagePage /></ProtectedRoute>} />
    <Route path="/account" element={<ProtectedRoute roles={['Customer']}><AccountDashboardPage /></ProtectedRoute>} />
    <Route path="/account/bookings/:id" element={<ProtectedRoute roles={['Customer']}><AccountBookingDetailPage /></ProtectedRoute>} />
    <Route path="*" element={<NotFoundPage />} />
  </Routes>
}
