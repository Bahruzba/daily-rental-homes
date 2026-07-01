import { Route, Routes } from 'react-router-dom'
import { BrokerDashboard } from './components/BrokerDashboard'
import { BookingPage } from './pages/BookingPage'
import { HomePage } from './pages/HomePage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RentalDetailPage } from './pages/RentalDetailPage'

export default function App() {
  return <Routes><Route path="/" element={<HomePage />} /><Route path="/homes/:id" element={<RentalDetailPage />} /><Route path="/booking/:homeId" element={<BookingPage />} /><Route path="/broker" element={<BrokerDashboard />} /><Route path="*" element={<NotFoundPage />} /></Routes>
}
