import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { deliveriesApi } from '../api/endpoints'
import { EmptyState } from '../components/EmptyState'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { useAuth } from '../hooks/useAuth'

export function DeliveriesPage() {
  const { canDeliver } = useAuth()
  const [q, setQ] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [received, setReceived] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['deliveries', q, from, to, received],
    queryFn: () => deliveriesApi.list({ q, from, to, received }),
  })

  return (
    <div className="stack">
      <PageHeader title="التسليمات" subtitle="تسليم الوحدات للعملاء" showBack={false} />

      <section className="card form-grid">
        <div className="search-row">
          <input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="ابحث برقم الوحدة أو اسم العميل أو المهندس..."
            aria-label="بحث التسليمات"
          />
          {canDeliver && (
            <Link className="btn lg" to="/deliveries/new">
              ＋ إضافة تسليم
            </Link>
          )}
        </div>
        <label>
          من تاريخ
          <input type="date" min="2000-01-01" max="2100-12-31" value={from} onChange={(e) => setFrom(e.target.value)} />
        </label>
        <label>
          إلى تاريخ
          <input type="date" min="2000-01-01" max="2100-12-31" value={to} onChange={(e) => setTo(e.target.value)} />
        </label>
        <label>
          حالة الاستلام
          <select value={received} onChange={(e) => setReceived(e.target.value)}>
            <option value="">الكل</option>
            <option value="true">تم الاستلام</option>
            <option value="false">لم يتم الاستلام</option>
          </select>
        </label>
      </section>

      {isLoading ? (
        <LoadingBlock label="جاري تحميل التسليمات..." />
      ) : !data || data.items.length === 0 ? (
        <section className="card">
          <EmptyState icon="🔑" title="لا توجد تسليمات" description="غيّر الفلاتر أو أضف تسليماً جديداً." />
        </section>
      ) : (
        <section className="card stack">
          <div className="section-title">
            <h2>النتائج ({data.total})</h2>
            <span className="muted">
              تم الاستلام {data.received} · لم يتم {data.notReceived} · مفروشة {data.furnished}
            </span>
          </div>
          <div className="results">
            {data.items.map((d) => (
              <Link key={d.id} className="sub-item link-card" to={`/deliveries/${d.id}`}>
                <div className="sub-item-head">
                  <span>{d.customerName}</span>
                  <span className={`badge ${d.isReceived ? 'ok' : 'danger'}`}>{d.isReceived ? 'تم الاستلام' : 'لم يتم'}</span>
                </div>
                <div className="chips">
                  <span className="chip code">{d.unitNumber}</span>
                  <span className="chip">{d.deliveryDate.split('-').reverse().slice(0, 2).join('/')}</span>
                  <span className="chip">م. {d.handoverEngineerName}</span>
                  {d.isFurnished && <span className="chip">مفروشة</span>}
                </div>
                {!d.isReceived && d.notReceivedReason && <div className="muted">السبب: {d.notReceivedReason}</div>}
                {(d.complaints || d.comments) && (
                  <div className="muted">{d.complaints ? `شكوى: ${d.complaints}` : d.comments}</div>
                )}
              </Link>
            ))}
          </div>
        </section>
      )}
    </div>
  )
}
