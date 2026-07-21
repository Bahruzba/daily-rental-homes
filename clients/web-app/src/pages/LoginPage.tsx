import { ArrowRight, KeyRound, Phone, ShieldCheck } from 'lucide-react'
import { type FormEvent, useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { AuthRequestError, isLiveApiEnabled, requestOtp, verifyOtp } from '../api/auth'
import { useAuth } from '../auth/AuthContext'
import { authRoles, dashboardPath, type AuthRole } from '../auth/types'
import { AppLayout } from '../components/AppLayout'

type LoginLocationState = { from?: string }

export function LoginPage() {
  const { session, setSession } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [phone, setPhone] = useState('')
  const [pin, setPin] = useState('')
  const [mockRole, setMockRole] = useState<AuthRole>('Customer')
  const [otpSent, setOtpSent] = useState(false)
  const [devPin, setDevPin] = useState<string>()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const showDevelopmentMockRole = !isLiveApiEnabled && import.meta.env.DEV

  if (session) return <Navigate to={dashboardPath(session.user.role)} replace />

  const handleRequest = async (event: FormEvent) => {
    event.preventDefault()
    setError('')
    if (!phone.trim()) return setError('Telefon nömrəsini daxil edin.')
    setBusy(true)
    try {
      const result = await requestOtp(phone.trim())
      setDevPin(result.devPin ?? undefined)
      setOtpSent(true)
    } catch (cause) {
      console.error('OTP request failed', cause)
      setError(cause instanceof AuthRequestError ? cause.message : 'OTP göndərilə bilmədi.')
    } finally {
      setBusy(false)
    }
  }

  const handleConfirm = async (event: FormEvent) => {
    event.preventDefault()
    setError('')
    setBusy(true)
    try {
      const nextSession = await verifyOtp({ phone: phone.trim(), pin: pin.trim(), mockRole })
      setSession(nextSession)
      const requestedPath = (location.state as LoginLocationState | null)?.from
      navigate(requestedPath && requestedPath === dashboardPath(nextSession.user.role) ? requestedPath : dashboardPath(nextSession.user.role), { replace: true })
    } catch (cause) {
      console.error('OTP confirmation failed', cause)
      setError(cause instanceof AuthRequestError ? cause.message : 'Giriş təsdiqlənmədi.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <AppLayout>
      <section className="auth-page">
        <div className="container auth-layout">
          <div className="auth-intro">
            <span className="eyebrow">TƏHLÜKƏSİZ GİRİŞ</span>
            <h1>Şifrəsiz, sadə giriş.</h1>
            <p>Telefon nömrənizə göndərilən birdəfəlik kodla hesabınıza daxil olun.</p>
            <span><ShieldCheck /> Kod beş dəqiqə etibarlıdır</span>
          </div>
          <form className="auth-card" onSubmit={otpSent ? handleConfirm : handleRequest}>
            <div className="auth-icon">{otpSent ? <KeyRound /> : <Phone />}</div>
            <h2>{otpSent ? 'Kodu təsdiqləyin' : 'Hesaba daxil olun'}</h2>
            <p>{otpSent ? `${phone} nömrəsinə göndərilən 6 rəqəmli kodu yazın.` : 'Davam etmək üçün telefon nömrənizi daxil edin.'}</p>

            <label className="auth-field">
              <span>Telefon nömrəsi</span>
              <input value={phone} onChange={(event) => setPhone(event.target.value)} maxLength={30} disabled={otpSent} placeholder="+994 50 000 00 00" />
            </label>

            {showDevelopmentMockRole && !otpSent && (
              <label className="auth-field">
                <span>Development demo rol</span>
                <select value={mockRole} onChange={(event) => setMockRole(event.target.value as AuthRole)}>
                  {authRoles.map((role) => <option key={role} value={role}>{role}</option>)}
                </select>
              </label>
            )}

            {otpSent && (
              <label className="auth-field">
                <span>OTP kod</span>
                <input inputMode="numeric" autoComplete="one-time-code" value={pin} onChange={(event) => setPin(event.target.value.replace(/\D/g, '').slice(0, 6))} placeholder="000000" />
              </label>
            )}

            {devPin && <div className="demo-notice">Development kodu: <strong>{devPin}</strong></div>}
            {error && <p className="auth-error" role="alert">{error}</p>}
            <button className="button button-primary button-full" disabled={busy}>{busy ? 'Gözləyin…' : otpSent ? <>Daxil ol <ArrowRight size={17} /></> : 'OTP kodu al'}</button>
            {otpSent && <button type="button" className="auth-back" onClick={() => { setOtpSent(false); setPin(''); setError('') }}>Nömrəni dəyiş</button>}
          </form>
        </div>
      </section>
    </AppLayout>
  )
}
