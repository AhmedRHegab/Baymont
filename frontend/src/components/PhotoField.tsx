import { useRef, useState } from 'react'
import { uploadsApi } from '../api/endpoints'

interface Props {
  label: string
  value?: string | null
  disabled?: boolean
  /** Only editors can open stored files (the API restricts them). */
  canView: boolean
  onChange: (path: string | null) => void
}

export function PhotoField({ label, value, disabled, canView, onChange }: Props) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const isPdf = value?.endsWith('.pdf')

  async function onPick(file?: File) {
    if (!file) return
    setBusy(true)
    setError(null)
    try {
      onChange(await uploadsApi.upload(file))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'فشل رفع الملف')
    } finally {
      setBusy(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  return (
    <div className="photo-field">
      <span>{label}</span>
      <div className="photo-row">
        {value && canView && !isPdf && (
          <a href={uploadsApi.url(value)} target="_blank" rel="noreferrer">
            <img className="photo-thumb" src={uploadsApi.url(value)} alt={label} />
          </a>
        )}
        {value && canView && isPdf && (
          <a href={uploadsApi.url(value)} target="_blank" rel="noreferrer">
            فتح الملف (PDF)
          </a>
        )}
        {value && !canView && <span className="muted">مرفق</span>}
        {!value && <span className="muted">لا يوجد مرفق</span>}
        {!disabled && (
          <>
            <input
              ref={inputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp,application/pdf"
              hidden
              onChange={(e) => void onPick(e.target.files?.[0])}
            />
            <button
              type="button"
              className="btn secondary"
              disabled={busy}
              onClick={() => inputRef.current?.click()}
            >
              {busy ? 'جاري الرفع...' : value ? 'استبدال' : 'رفع صورة'}
            </button>
            {value && (
              <button type="button" className="btn ghost" onClick={() => onChange(null)}>
                إزالة
              </button>
            )}
          </>
        )}
      </div>
      {error && <span className="banner error">{error}</span>}
    </div>
  )
}
