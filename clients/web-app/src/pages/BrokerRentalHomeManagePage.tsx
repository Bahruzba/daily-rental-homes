import { ArrowLeft, CalendarX, ChevronLeft, ChevronRight, ImagePlus, Save, Star, Trash2, X } from 'lucide-react'
import { type ChangeEvent, type FormEvent, useEffect, useMemo, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import {
  BrokerRequestError,
  addBrokerAvailabilityBlock,
  createBrokerRentalHome,
  deleteBrokerAvailabilityBlock,
  deleteBrokerRentalHomeMedia,
  getBrokerRentalHome,
  publishBrokerRentalHome,
  resolveApiAssetUrl,
  setBrokerRentalHomeMainMedia,
  updateBrokerRentalHome,
  uploadBrokerRentalHomeMedia,
  type BrokerRentalHomeDetail,
  type BrokerRentalHomePayload,
} from '../api/broker'
import { useAuth } from '../auth/AuthContext'
import { AppLayout } from '../components/AppLayout'

const emptyForm: BrokerRentalHomePayload = {
  title: '',
  description: '',
  city: '',
  district: '',
  address: '',
  dailyPrice: 100,
  roomCount: 1,
  guestCount: 2,
  isPublished: false,
}

const brokerApiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
const useLiveBrokerApi = import.meta.env.VITE_USE_LIVE_API === 'true'

export function BrokerRentalHomeManagePage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  const { session } = useAuth()
  const homeId = Number(id)
  const isNew = id === 'new'
  const [home, setHome] = useState<BrokerRentalHomeDetail>()
  const [form, setForm] = useState<BrokerRentalHomePayload>(emptyForm)
  const [loading, setLoading] = useState(!isNew)
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const [mediaBusyId, setMediaBusyId] = useState<number>()
  const [previewIndex, setPreviewIndex] = useState<number | null>(null)
  const [blockBusyId, setBlockBusyId] = useState<number>()
  const [blockForm, setBlockForm] = useState({ startDate: '', endDate: '', note: '' })
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(() => {
    const state = location.state as { success?: string } | null
    return state?.success ?? ''
  })
  const title = useMemo(() => isNew ? 'Yeni ev əlavə et' : 'Evi idarə et', [isNew])
  const previewMedia = previewIndex !== null ? home?.media[previewIndex] : undefined

  useEffect(() => {
    if (!session || isNew) return
    setLoading(true)
    setError('')
    getBrokerRentalHome(homeId, session.accessToken)
      .then((nextHome) => {
        setHome(nextHome)
        setForm({
          title: nextHome.title,
          description: nextHome.description,
          city: nextHome.city,
          district: nextHome.district ?? '',
          address: nextHome.address ?? '',
          dailyPrice: nextHome.dailyPrice,
          roomCount: nextHome.roomCount,
          guestCount: nextHome.guestCount,
          isPublished: nextHome.isPublished,
        })
      })
      .catch((cause) => setError(cause instanceof Error ? cause.message : 'Ev məlumatı yüklənmədi.'))
      .finally(() => setLoading(false))
  }, [homeId, isNew, session])

  function update<K extends keyof BrokerRentalHomePayload>(key: K, value: BrokerRentalHomePayload[K]) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  function movePreview(direction: -1 | 1) {
    setPreviewIndex((current) => {
      if (current === null || !home?.media.length) return current
      return (current + direction + home.media.length) % home.media.length
    })
  }

  useEffect(() => {
    if (!previewMedia) return
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setPreviewIndex(null)
      if (event.key === 'ArrowLeft') movePreview(-1)
      if (event.key === 'ArrowRight') movePreview(1)
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [previewMedia, home?.media.length])

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!session) return
    setSaving(true)
    setError('')
    setSuccess('')
    try {
      if (isNew) {
        const result = await createBrokerRentalHome(form, session.accessToken)
        navigate(`/broker/rental-homes/${result.id}/edit`, { replace: true })
        setSuccess('Ev yaradıldı. İndi şəkil əlavə edə və yayımlaya bilərsiniz.')
      } else {
        await updateBrokerRentalHome(homeId, form, session.accessToken)
        setHome(await getBrokerRentalHome(homeId, session.accessToken))
        setSuccess('Dəyişikliklər yadda saxlanıldı.')
      }
    } catch (cause) {
      console.error('Broker rental home save failed', cause)
      setError(cause instanceof Error ? cause.message : 'Ev yadda saxlanılmadı.')
    } finally {
      setSaving(false)
    }
  }

  async function togglePublish(nextValue: boolean) {
    if (!session || isNew) return
    setSaving(true)
    setError('')
    setSuccess('')
    try {
      await publishBrokerRentalHome(homeId, session.accessToken, nextValue)
      setHome(await getBrokerRentalHome(homeId, session.accessToken))
      setForm((current) => ({ ...current, isPublished: nextValue }))
      setSuccess(nextValue ? 'Ev publik siyahıda yayımlandı.' : 'Ev publik siyahıdan gizlədildi.')
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Yayım statusu dəyişmədi.')
    } finally {
      setSaving(false)
    }
  }

  async function deleteHome() {
    if (!session || isNew) return
    setDeleting(true)
    setError('')
    setSuccess('')
    try {
      if (useLiveBrokerApi) {
        const response = await fetch(`${brokerApiBaseUrl}/api/broker/rental-homes/${homeId}`, {
          method: 'DELETE',
          headers: { Authorization: `Bearer ${session.accessToken}` },
        })

        if (!response.ok) {
          let message = 'Elan silinmədi.'
          try {
            const payload = await response.json() as { error?: string }
            message = payload.error || message
          } catch {
            // Keep the friendly fallback message.
          }
          throw new Error(message)
        }
      }

      setShowDeleteConfirm(false)
      setSuccess('Elan silindi.')
      window.setTimeout(() => navigate('/broker', { replace: true }), 700)
    } catch (cause) {
      console.error('Broker rental home delete failed', cause)
      setError(cause instanceof Error ? cause.message : 'Elan silinmədi.')
      setShowDeleteConfirm(false)
    } finally {
      setDeleting(false)
    }
  }

  async function upload(event: ChangeEvent<HTMLInputElement>) {
    if (!session || isNew) return
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
      setError('Şəkil JPG, PNG və ya WebP olmalı, ölçü 5 MB-dan böyük olmamalıdır.')
      return
    }
    setSaving(true)
    setError('')
    try {
      await uploadBrokerRentalHomeMedia(homeId, file, session.accessToken)
      setHome(await getBrokerRentalHome(homeId, session.accessToken))
      setSuccess('Şəkil yükləndi.')
    } catch (cause) {
      console.error('Home image upload failed', cause)
      setError(cause instanceof BrokerRequestError ? cause.message : 'Şəkil yüklənmədi.')
    } finally {
      setSaving(false)
    }
  }

  async function mediaAction(action: () => Promise<unknown>, message: string, mediaId: number) {
    if (!session) return
    setMediaBusyId(mediaId)
    setError('')
    setSuccess('')
    try {
      await action()
      setHome(await getBrokerRentalHome(homeId, session.accessToken))
      setSuccess(message)
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Şəkil əməliyyatı alınmadı.')
    } finally {
      setMediaBusyId(undefined)
    }
  }

  async function addBlock(event: FormEvent) {
    event.preventDefault()
    if (!session || isNew) return
    setSaving(true)
    setError('')
    setSuccess('')
    try {
      await addBrokerAvailabilityBlock(homeId, blockForm, session.accessToken)
      setHome(await getBrokerRentalHome(homeId, session.accessToken))
      setBlockForm({ startDate: '', endDate: '', note: '' })
      setSuccess('Tarix bloku əlavə edildi.')
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Tarix bloku əlavə edilmədi.')
    } finally {
      setSaving(false)
    }
  }

  async function deleteBlock(blockId: number) {
    if (!session || isNew) return
    setBlockBusyId(blockId)
    setError('')
    setSuccess('')
    try {
      await deleteBrokerAvailabilityBlock(homeId, blockId, session.accessToken)
      setHome(await getBrokerRentalHome(homeId, session.accessToken))
      setSuccess('Tarix bloku silindi.')
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Tarix bloku silinmədi.')
    } finally {
      setBlockBusyId(undefined)
    }
  }

  return <AppLayout><section className="broker-detail-page"><div className="container">
    <Link className="back-link" to="/broker"><ArrowLeft size={16} /> Broker panelinə qayıt</Link>
    <div className="broker-detail-heading"><div><span className="eyebrow">EV İDARƏETMƏSİ</span><h1>{title}</h1><p>Öz elanınızı yaradın, redaktə edin və şəkilləri idarə edin.</p></div>{!isNew && <div><strong>{form.isPublished ? 'Yayımda' : 'Qaralama'}</strong><button className="button button-ghost" disabled={saving || deleting} onClick={() => void togglePublish(!form.isPublished)}>{form.isPublished ? 'Gizlət' : 'Yayımla'}</button><button className="button button-ghost" disabled={saving || deleting} onClick={() => setShowDeleteConfirm(true)}><Trash2 size={16} /> Sil</button></div>}</div>
    {error && <div className="broker-error" role="alert">{error}</div>}
    {success && <div className="account-success">{success}</div>}
    {loading ? <div className="broker-loading">Ev məlumatı yüklənir…</div> : <div className="broker-property-layout">
      <form className="broker-property-form" onSubmit={save}>
        <h2>Əsas məlumatlar</h2>
        <div className="input-grid">
          <label className="full"><span>Başlıq</span><input required maxLength={200} value={form.title} onChange={(event) => update('title', event.target.value)} /></label>
          <label className="full"><span>Təsvir</span><textarea required maxLength={4000} value={form.description} onChange={(event) => update('description', event.target.value)} /></label>
          <label><span>Şəhər</span><input required maxLength={100} value={form.city} onChange={(event) => update('city', event.target.value)} /></label>
          <label><span>Rayon/kənd</span><input maxLength={100} value={form.district ?? ''} onChange={(event) => update('district', event.target.value)} /></label>
          <label className="full"><span>Ünvan</span><input required maxLength={500} value={form.address ?? ''} onChange={(event) => update('address', event.target.value)} /></label>
          <label><span>Gecəlik qiymət</span><input required min={1} max={10000} type="number" value={form.dailyPrice} onChange={(event) => update('dailyPrice', Number(event.target.value))} /></label>
          <label><span>Otaq sayı</span><input required min={1} max={50} type="number" value={form.roomCount} onChange={(event) => update('roomCount', Number(event.target.value))} /></label>
          <label><span>Qonaq tutumu</span><input required min={1} max={100} type="number" value={form.guestCount} onChange={(event) => update('guestCount', Number(event.target.value))} /></label>
        </div>
        <label className="broker-publish-check"><input type="checkbox" checked={Boolean(form.isPublished)} onChange={(event) => update('isPublished', event.target.checked)} /> <span>Yadda saxlayanda publik siyahıda yayımla</span></label>
        <button className="button button-primary" disabled={saving || deleting}><Save size={16} /> {saving ? 'Saxlanılır…' : 'Yadda saxla'}</button>
      </form>
      <aside className="broker-media-panel">
        <h2>Şəkillər</h2>
        {isNew ? <p>Əvvəlcə evi yaradın, sonra şəkil əlavə edin.</p> : <>
          <label className="button button-primary broker-upload-button"><ImagePlus size={16} /> Şəkil yüklə<input type="file" accept="image/jpeg,image/png,image/webp" disabled={saving || deleting} onChange={upload} /></label>
          <div className="broker-media-list">
            {home?.media.length ? home.media.map((media, index) => <article key={media.id}>
              <button type="button" className="broker-media-thumb" aria-label="Şəkilə böyük bax" onClick={() => setPreviewIndex(index)}>
                <img src={resolveApiAssetUrl(media.url)} alt="Ev şəkli" />
                {media.isMain && <span>Əsas</span>}
              </button>
              <div><strong>{media.isMain ? 'Əsas şəkil' : 'Ev şəkli'}</strong><span>{media.contentType} · {media.sizeBytes ? Math.round(media.sizeBytes / 1024) : 0} KB</span></div>
              <button type="button" className="icon-button" title="Əsas şəkil et" disabled={mediaBusyId === media.id || media.isMain} onClick={() => void mediaAction(() => setBrokerRentalHomeMainMedia(homeId, media.id, session!.accessToken), 'Əsas şəkil dəyişdirildi.', media.id)}><Star size={16} /></button>
              <button type="button" className="icon-button danger" title="Sil" disabled={mediaBusyId === media.id} onClick={() => void mediaAction(() => deleteBrokerRentalHomeMedia(homeId, media.id, session!.accessToken), 'Şəkil silindi.', media.id)}><Trash2 size={16} /></button>
            </article>) : <p>Hələ şəkil yoxdur. İlk yüklənən şəkil əsas şəkil olacaq.</p>}
          </div>
        </>}
      </aside>
      <aside className="broker-media-panel broker-availability-panel">
        <h2>Uyğun olmayan tarixlər</h2>
        {isNew ? <p>Əvvəlcə evi yaradın, sonra tarix bloklayın.</p> : <>
          <form className="availability-form" onSubmit={addBlock}>
            <label><span>Başlanğıc</span><input type="date" required value={blockForm.startDate} onChange={(event) => setBlockForm((current) => ({ ...current, startDate: event.target.value }))} /></label>
            <label><span>Bitiş</span><input type="date" required value={blockForm.endDate} onChange={(event) => setBlockForm((current) => ({ ...current, endDate: event.target.value }))} /></label>
            <label className="full"><span>Qeyd <em>yalnız broker üçündür</em></span><input maxLength={500} value={blockForm.note} onChange={(event) => setBlockForm((current) => ({ ...current, note: event.target.value }))} /></label>
            <button className="button button-primary" disabled={saving || deleting}><CalendarX size={16} /> Tarixi blokla</button>
          </form>
          <div className="broker-media-list">
            {home?.availabilityBlocks.length ? home.availabilityBlocks.map((block) => <article key={block.id}>
              <CalendarX />
              <div><strong>{block.startDate} — {block.endDate}</strong><span>{block.note || 'Qeyd yoxdur'}</span></div>
              <button type="button" className="icon-button danger" title="Sil" disabled={blockBusyId === block.id} onClick={() => void deleteBlock(block.id)}><Trash2 size={16} /></button>
            </article>) : <p>Bu ev üçün bloklanmış tarix yoxdur.</p>}
          </div>
        </>}
      </aside>
    </div>}
    {previewMedia && home?.media.length ? <div className="broker-media-preview" role="dialog" aria-modal="true" aria-label="Şəkil önbaxışı" onClick={() => setPreviewIndex(null)}>
      <div className="broker-media-preview-card" onClick={(event) => event.stopPropagation()}>
        <button type="button" className="broker-preview-close" aria-label="Bağla" onClick={() => setPreviewIndex(null)}><X size={20} /></button>
        <div className="broker-media-preview-image">
          <button type="button" className="broker-preview-nav previous" aria-label="Əvvəlki şəkil" onClick={() => movePreview(-1)}><ChevronLeft size={24} /></button>
          <img src={resolveApiAssetUrl(previewMedia.url)} alt="Ev şəkli önbaxışı" />
          <button type="button" className="broker-preview-nav next" aria-label="Növbəti şəkil" onClick={() => movePreview(1)}><ChevronRight size={24} /></button>
        </div>
        <div className="broker-media-preview-footer">
          <div>
            <strong>{previewMedia.isMain ? 'Əsas şəkil' : 'Ev şəkli'}</strong>
            <span>{previewIndex! + 1} / {home.media.length}</span>
          </div>
          {previewMedia.isMain ? <span className="status-badge status-confirmed"><Star size={14} /> Cari əsas şəkil</span> : <button type="button" className="button button-primary" disabled={mediaBusyId === previewMedia.id} onClick={() => void mediaAction(() => setBrokerRentalHomeMainMedia(homeId, previewMedia.id, session!.accessToken), 'Əsas şəkil dəyişdirildi.', previewMedia.id)}><Star size={16} /> Əsas şəkil et</button>}
        </div>
      </div>
    </div> : null}
    {showDeleteConfirm && <div className="broker-media-preview" role="dialog" aria-modal="true" aria-label="Elanı sil" onClick={() => setShowDeleteConfirm(false)}>
      <div className="broker-media-preview-card" style={{ width: 'min(430px, 100%)', gridTemplateRows: 'auto' }} onClick={(event) => event.stopPropagation()}>
        <div style={{ padding: 24, display: 'grid', gap: 14 }}>
          <h2 style={{ margin: 0 }}>Elanı sil</h2>
          <p style={{ margin: 0, color: '#982b24', lineHeight: 1.55 }}>Bu elanı silmək istədiyinizə əminsiniz?</p>
          <strong>{home?.title ?? form.title}</strong>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, flexWrap: 'wrap' }}>
            <button className="button button-ghost" type="button" disabled={deleting} onClick={() => setShowDeleteConfirm(false)}>Ləğv et</button>
            <button className="button button-ghost" type="button" disabled={deleting} onClick={() => void deleteHome()}><Trash2 size={16} /> {deleting ? 'Silinir…' : 'Sil'}</button>
          </div>
        </div>
      </div>
    </div>}
  </div></section></AppLayout>
}
