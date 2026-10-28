import { useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { reservationsApi } from '../api/endpoints'
import { EmptyState } from '../components/EmptyState'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { ReservationCard } from '../components/ReservationCard'
import { useAuth } from '../hooks/useAuth'
import { checkFile, excelRule } from '../lib/validation'
import type { ReservationImportResult } from '../types'

const tabs = [
  { status: 0, label: 'بانتظار المراجعة' },
  { status: 1, label: 'المعتمدة' },
  { status: 2, label: 'المرفوضة' },
] as const

export function ReservationsPage() {
  const { canEdit, isAdmin } = useAuth()
  const qc = useQueryClient()
  const [status, setStatus] = useState<number>(canEdit ? 0 : 1)
  const [q, setQ] = useState('')
  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<ReservationImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const fileRef = useRef<HTMLInputElement>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['reservations', status, q],
    queryFn: () => reservationsApi.list(status, q),
  })
  const pending = useQuery({
    queryKey: ['reservations-pending'],
    queryFn: reservationsApi.pendingCount,
    enabled: canEdit,
  })

  async function onPick(file?: File) {
    if (fileRef.current) fileRef.current.value = ''
    if (!file) return
    const problem = checkFile(file, excelRule)
    if (problem) {
      setError(problem)
      setResult(null)
      return
    }
    setBusy(true)
    setError(null)
    setResult(null)
    try {
      setResult(await reservationsApi.import(file))
      void qc.invalidateQueries({ queryKey: ['reservations'] })
      void qc.invalidateQueries({ queryKey: ['reservations-pending'] })
      setStatus(0)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'تعذر رفع الملف. حاول مرة أخرى.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="stack">
      <PageHeader title="الحجوزات والتصاريح" subtitle="تصاريح الزيارة القادمة من الإدارة" showBack={false} />

      {canEdit && (
        <div className="banner info">
          التصاريح تصل تلقائياً من البريد الإلكتروني وتظهر هنا للمراجعة.{' '}
          <Link to="/inbox">متابعة البريد الوارد ←</Link>
        </div>
      )}

      {isAdmin && (
        <details className="card stack">
          <summary>إدخال يدوي (للطوارئ فقط — للمدير)</summary>
          <p className="muted">استخدمه فقط إذا تعذر وصول التصريح بالبريد، مثل تصريح عاجل أو تعطل البريد.</p>
          <input ref={fileRef} type="file" accept=".xlsx" hidden onChange={(e) => void onPick(e.target.files?.[0])} />
          <div className="actions">
            <button className="btn secondary" type="button" disabled={busy} onClick={() => fileRef.current?.click()}>
              {busy ? 'جاري القراءة...' : '⬆ رفع ملف التصاريح (Excel)'}
            </button>
            <Link className="btn secondary" to="/reservations/new">
              ＋ حجز جديد
            </Link>
          </div>
          {error && <div className="banner error">{error}</div>}
          {result && (
            <div className="banner info">
              <strong>تمت قراءة الملف:</strong> أُضيف {result.created} حجز للمراجعة
              {result.duplicates > 0 ? `، وتم تجاهل ${result.duplicates} مكرر` : ''}.
              {result.issues.length > 0 && (
                <ul className="help-notes">
                  {result.issues.slice(0, 10).map((i) => (
                    <li key={i.message}>{i.message}</li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </details>
      )}

      <section className="card stack">
        {canEdit && (
          <div className="actions">
            {tabs.map((t) => (
              <button
                key={t.status}
                type="button"
                className={`btn ${status === t.status ? '' : 'secondary'}`}
                onClick={() => setStatus(t.status)}
              >
                {t.label}
                {t.status === 0 && pending.data?.count ? ` (${pending.data.count})` : ''}
              </button>
            ))}
          </div>
        )}
        <div className="search-row">
          <input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="ابحث باسم الضيف أو كود الوحدة..."
            aria-label="بحث الحجوزات"
          />
        </div>

        {isLoading ? (
          <LoadingBlock label="جاري التحميل..." />
        ) : !data?.length ? (
          <EmptyState icon="🎫" title="لا توجد حجوزات" description={status === 0 ? 'لا شيء بانتظار المراجعة. التصاريح الجديدة تصل من البريد.' : 'لا توجد نتائج.'} />
        ) : (
          data.map((r) => <ReservationCard key={r.id} r={r} />)
        )}
      </section>
    </div>
  )
}
