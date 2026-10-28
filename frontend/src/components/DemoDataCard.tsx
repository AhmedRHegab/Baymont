import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { demoApi } from '../api/endpoints'
import { ConfirmDialog } from './ConfirmDialog'

/** Admin tool: put the sample data into the system (for training or a demo), or see that it is there. */
export function DemoDataCard() {
  const qc = useQueryClient()
  const [confirm, setConfirm] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const { data } = useQuery({ queryKey: ['demo'], queryFn: demoApi.status })

  const load = useMutation({
    mutationFn: demoApi.load,
    onSuccess: async (r) => {
      setConfirm(false)
      setError(null)
      setMessage(`تم تحميل ${r.created} سجلاً تجريبياً.`)
      await qc.invalidateQueries()
    },
    onError: (err) => {
      setConfirm(false)
      setMessage(null)
      setError(err instanceof Error ? err.message : 'تعذر التحميل')
    },
  })

  if (!data) return null

  return (
    <section className="card stack">
      <div className="section-title">
        <h2>البيانات التجريبية</h2>
      </div>
      {data.hasDemoData ? (
        <div className="banner info">
          البيانات التجريبية موجودة الآن ({data.records} سجلاً). لحذفها استخدم الشريط الأصفر أعلى الصفحة.
        </div>
      ) : (
        <>
          <p className="muted">
            لعرض النظام على المستخدمين أو التدريب: تُضاف بيانات مخترعة (ملاك، وحدات، مقاولون، تسليمات، حجوزات، موظفون...) بجانب
            بياناتك الحالية، وتُحذف كلها بزر واحد دون المساس ببياناتك الحقيقية.
          </p>
          <button className="btn lg" type="button" disabled={load.isPending} onClick={() => setConfirm(true)}>
            {load.isPending ? 'جاري التحميل...' : 'تحميل البيانات التجريبية'}
          </button>
        </>
      )}
      {message && <div className="banner success">{message}</div>}
      {error && <div className="banner error">{error}</div>}
      <ConfirmDialog
        open={confirm}
        title="تحميل البيانات التجريبية؟"
        message="ستُضاف سجلات تجريبية مخترعة إلى النظام الحالي. يمكن حذفها لاحقاً بالكامل من الشريط الأصفر."
        confirmLabel="تحميل"
        busyLabel="جاري التحميل..."
        danger={false}
        busy={load.isPending}
        onCancel={() => setConfirm(false)}
        onConfirm={() => load.mutate()}
      />
    </section>
  )
}
