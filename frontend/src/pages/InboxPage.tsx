import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { inboxApi } from '../api/endpoints'
import { EmptyState } from '../components/EmptyState'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { formatDateTime } from '../lib/dates'

const statusText = {
  Imported: { label: 'تمت القراءة', tone: 'contractor' },
  Ignored: { label: 'تم تجاهلها', tone: 'unit' },
  Failed: { label: 'فشلت', tone: 'vehicle' },
} as const

export function InboxPage() {
  const qc = useQueryClient()
  const { data, isLoading } = useQuery({
    queryKey: ['inbox'],
    queryFn: inboxApi.get,
    refetchInterval: 20_000,
  })

  const check = useMutation({
    mutationFn: inboxApi.check,
    // The reader answers within a few seconds, so look again shortly after asking.
    onSuccess: () => {
      setTimeout(() => void qc.invalidateQueries({ queryKey: ['inbox'] }), 4000)
    },
  })

  if (isLoading || !data) return <LoadingBlock label="جاري تحميل البريد..." />

  return (
    <div className="stack">
      <PageHeader title="البريد الوارد" subtitle="رسائل التصاريح التي قرأها النظام" showBack={false} />

      <section className="card stack">
        {data.enabled ? (
          <>
            <div className="banner success">
              <strong>قارئ البريد يعمل</strong> على {data.mailbox} · يفحص كل {Math.round(data.pollSeconds / 60)} دقائق
              {data.lastPollAt ? ` · آخر فحص ${formatDateTime(data.lastPollAt)}` : ''}
            </div>
            {data.lastError && <div className="banner error">آخر خطأ: {data.lastError}</div>}
            <div className="actions">
              <button className="btn lg" type="button" disabled={check.isPending || data.polling} onClick={() => check.mutate()}>
                {data.polling || check.isPending ? 'جاري الفحص...' : 'افحص البريد الآن'}
              </button>
              <Link className="btn secondary" to="/reservations">
                الحجوزات بانتظار المراجعة
              </Link>
            </div>
            {check.error && <div className="banner error">{(check.error as Error).message}</div>}
          </>
        ) : (
          <div className="banner error">
            <strong>قارئ البريد غير مفعّل</strong>
          </div>
        )}
      </section>

      <section className="card stack">
        <div className="section-title">
          <h2>آخر الرسائل ({data.messages.length})</h2>
        </div>
        {data.messages.length === 0 ? (
          <EmptyState icon="📭" title="لا توجد رسائل" description="لم يصل شيء من المرسلين المسموح لهم بعد." />
        ) : (
          data.messages.map((m) => {
            const s = statusText[m.status]
            return (
              <div className="sub-item" key={m.id}>
                <div className="sub-item-head">
                  <strong>{m.subject || '(بدون عنوان)'}</strong>
                  <span className={`badge ${s.tone}`}>
                    {s.label}
                    {m.permitsCreated > 0 ? ` · ${m.permitsCreated} حجز` : ''}
                  </span>
                </div>
                <div className="muted" dir="ltr">
                  {m.fromAddress}
                </div>
                <div className="muted">وصلت {formatDateTime(m.receivedAt)}</div>
                {m.detail && <div className="muted">{m.detail}</div>}
              </div>
            )
          })
        )}
      </section>
    </div>
  )
}
