import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { reservationsApi } from '../api/endpoints'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { ReservationCard } from '../components/ReservationCard'
import { todayLocal } from '../lib/dates'
import type { CountPair } from '../types'

function Stat({ title, hint, value, tone }: { title: string; hint: string; value: CountPair; tone?: 'ok' | 'danger' }) {
  return (
    <div className={`sub-item stat ${tone ?? ''}`}>
      <strong>{title}</strong>
      <span className="muted">{hint}</span>
      <div className="stat-numbers">
        <span>
          <b>{value.reservations}</b> حجز
        </span>
        <span>
          <b>{value.persons}</b> شخص
        </span>
      </div>
    </div>
  )
}

export function FollowUpPage() {
  const today = todayLocal()
  const [start, setStart] = useState('')
  const { data, isLoading } = useQuery({
    queryKey: ['reservation-dashboard', today, start],
    queryFn: () => reservationsApi.dashboard(today, start || undefined),
    refetchInterval: 60_000,
  })

  if (isLoading || !data) return <LoadingBlock label="جاري تحميل المتابعة..." />

  return (
    <div className="stack">
      <PageHeader title="متابعة الحجوزات" subtitle={`اليوم ${data.today} · أسبوع التصاريح من الجمعة ${data.weekStart} إلى الجمعة التي تليها`} showBack={false} />

      {data.pendingReview > 0 && (
        <Link className="banner info" to="/reservations">
          يوجد <strong>{data.pendingReview}</strong> حجز بانتظار المراجعة ←
        </Link>
      )}

      <section className="stat-grid">
        <Stat title="تصاريح الأسبوع" hint="حجوزات هذا الأسبوع (جمعة إلى جمعة)" value={data.thisWeekPermits} />
        <Stat title="سجلوا دخولاً" hint="دخلوا الموقع هذا الأسبوع" value={data.thisWeekArrived} />
        <Stat title="سجلوا مغادرة نهائية" hint="غادروا نهائياً هذا الأسبوع" value={data.thisWeekDeparted} />
        <Stat title="داخل الموقع الآن" hint="دخلوا ولم يخرجوا بعد" value={data.presentNow} tone="ok" />
        <Stat title="متأخرون عن المغادرة" hint="انتهى التصريح ولم تُسجل مغادرتهم النهائية" value={data.overdue} tone="danger" />
      </section>

      <section className="card stack">
        <div className="section-title">
          <h2>حالات تتطلب متابعة فورية ({data.followUp.length})</h2>
        </div>
        {data.followUp.length === 0 ? (
          <div className="banner success">لا توجد حالات تتطلب متابعة فورية.</div>
        ) : (
          data.followUp.map((r) => <ReservationCard key={r.id} r={r} />)
        )}
      </section>

      <section className="card stack">
        <div className="section-title">
          <h2>تفصيل الأسابيع — 13 أسبوعاً</h2>
        </div>
        <label className="form-grid">
          <span>بداية الجدول (أي يوم)</span>
          <input type="date" min="2000-01-01" max="2100-12-31" value={start} onChange={(e) => setStart(e.target.value)} />
        </label>
        <div className="table-scroll">
          <table className="table">
            <thead>
              <tr>
                <th>الأسبوع</th>
                <th>بداية الجمعة</th>
                <th>الجمعة التالية</th>
                <th>تصاريح</th>
                <th>المصرح لهم</th>
                <th>دخلوا</th>
                <th>غادروا نهائياً</th>
                <th>داخل الآن</th>
                <th>متأخرون</th>
              </tr>
            </thead>
            <tbody>
              {data.weeks.map((w) => (
                <tr key={w.start} className={w.start === data.weekStart ? 'current-week' : ''}>
                  <td>أسبوع {String(w.number).padStart(2, '0')}</td>
                  <td>{w.start}</td>
                  <td>{w.nextStart}</td>
                  <td>{w.permits}</td>
                  <td>{w.authorized}</td>
                  <td>{w.arrived}</td>
                  <td>{w.departed}</td>
                  <td>{w.present}</td>
                  <td className={w.late > 0 ? 'late' : ''}>{w.late}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}
