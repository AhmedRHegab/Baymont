import { type FormEvent, useState } from 'react'
import { importApi } from '../api/endpoints'
import { PageHeader } from '../components/PageHeader'

export function ImportPage() {
  const [file, setFile] = useState<File | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (!file) return
    setBusy(true)
    setMessage(null)
    try {
      const result = await importApi.preview(file)
      setMessage(result.message)
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'فشل رفع الملف')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="stack">
      <PageHeader
        title="استيراد ملف Excel"
        subtitle="لاحقاً — بعد استلام ملف المصطبة والتسليمات"
        backTo="/"
      />

      <form className="card form-grid" onSubmit={onSubmit}>
        <div className="banner info">
          هذه الشاشة جاهزة للتجربة فقط. ربط الأعمدة الحقيقية سيتم بعد وصول ملف الإكسيل من التشغيل.
        </div>

        <label>
          اختر ملف Excel
          <input
            type="file"
            accept=".xlsx,.xls"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
        </label>

        {file && <p className="muted">الملف المختار: {file.name}</p>}
        {message && <div className="banner info">{message}</div>}

        <div className="sticky-actions">
          <button className="btn lg" type="submit" disabled={!file || busy}>
            {busy ? 'جاري الرفع...' : 'معاينة الملف'}
          </button>
        </div>
      </form>
    </div>
  )
}
