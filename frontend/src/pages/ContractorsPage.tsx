import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { contractorsApi } from '../api/endpoints'
import { EmptyState } from '../components/EmptyState'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { TypeIcon } from '../components/TypeIcon'
import { useAuth } from '../hooks/useAuth'

export function ContractorsPage() {
  const { canEdit } = useAuth()
  const [q, setQ] = useState('')
  const { data, isLoading } = useQuery({
    queryKey: ['contractors', q],
    queryFn: () => contractorsApi.list(q),
  })

  return (
    <div className="stack">
      <PageHeader title="المقاولون" subtitle="الشركات والمقاولون الموجودون بالموقع" showBack={false} />

      <section className="card stack">
        <div className="search-row">
          <input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="ابحث باسم الشركة أو المقاول أو الهاتف..."
            aria-label="بحث المقاولين"
          />
          {canEdit && (
            <Link className="btn lg" to="/contractors/new">
              ＋ إضافة مقاول
            </Link>
          )}
        </div>

        {isLoading ? (
          <LoadingBlock label="جاري تحميل المقاولين..." />
        ) : !data || data.length === 0 ? (
          <EmptyState icon="👷" title="لا يوجد مقاولون" description="أضف أول مقاول من الزر أعلاه." />
        ) : (
          <div className="results">
            {data.map((c) => (
              <Link key={c.id} className="result-item" to={`/contractors/${c.id}`}>
                <TypeIcon type="contractor" />
                <div>
                  <div className="result-title">{c.companyName}</div>
                  <div className="result-sub">
                    {c.contractorName}
                    {c.phone ? ` · ${c.phone}` : ''}
                  </div>
                </div>
                <span className="badge contractor">{c.workerCount} عامل</span>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
