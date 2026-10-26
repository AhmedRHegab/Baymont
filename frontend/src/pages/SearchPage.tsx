import { type FormEvent, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { searchApi } from '../api/endpoints'
import { EmptyState } from '../components/EmptyState'
import { TypeIcon, typeLabels } from '../components/TypeIcon'

/** Where each kind of hit opens. Staff have no detail page, so they open the staff list filtered by name. */
function hitPath(hit: { type: string; id: string; title: string }) {
  switch (hit.type) {
    case 'staff':
      return `/staff?q=${encodeURIComponent(hit.title)}`
    case 'delivery':
      return `/deliveries/${hit.id}`
    default:
      return `/${hit.type}s/${hit.id}`
  }
}

export function SearchPage() {
  const [q, setQ] = useState('')
  const [submitted, setSubmitted] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  const { data, isFetching, error, isFetched } = useQuery({
    queryKey: ['search', submitted],
    queryFn: () => searchApi.search(submitted),
    enabled: submitted.trim().length > 0,
  })

  useEffect(() => {
    inputRef.current?.focus()
  }, [])

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    const value = q.trim()
    if (!value) {
      inputRef.current?.focus()
      return
    }
    setSubmitted(value)
  }

  function fillExample(example: string) {
    setQ(example)
    setSubmitted(example)
    inputRef.current?.focus()
  }

  return (
    <div className="stack">
      <section className="card hero-search">
        <h1>ابحث هنا</h1>
        <p className="lead">اكتب اسم المالك أو المقاول أو الموظف أو رقم اللوحة أو رقم الوحدة ثم اضغط بحث</p>

        <form className="search-row" onSubmit={onSubmit}>
          <input
            ref={inputRef}
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="اكتب هنا للبحث..."
            aria-label="كلمة البحث"
          />
          <button className="btn lg" type="submit" disabled={isFetching}>
            {isFetching ? 'جاري البحث...' : 'بحث'}
          </button>
        </form>

        <div className="hint-grid">
          <button type="button" className="hint-card" onClick={() => fillExample('أحمد')}>
            <strong>بالاسم</strong>
            <span>مثال: أحمد</span>
          </button>
          <button type="button" className="hint-card" onClick={() => fillExample('1234')}>
            <strong>برقم اللوحة</strong>
            <span>مثال: 1234</span>
          </button>
          <button type="button" className="hint-card" onClick={() => fillExample('12-A')}>
            <strong>برقم الوحدة</strong>
            <span>مثال: 12-A</span>
          </button>
        </div>
      </section>

      {error && (
        <div className="banner error">تعذر البحث الآن. حاول مرة أخرى بعد لحظات.</div>
      )}

      {!submitted && (
        <section className="card">
          <EmptyState
            icon="👋"
            title="مرحباً"
            description="ابدأ بالبحث من المربع الكبير أعلاه. النتيجة تظهر هنا فوراً."
          />
        </section>
      )}

      {submitted && isFetching && (
        <section className="card loading-block">
          <div className="spinner" />
          <div>جاري البحث عن «{submitted}»...</div>
        </section>
      )}

      {data && isFetched && !isFetching && (
        <section className="card">
          <div className="section-title">
            <h2>
              النتائج
              {data.hits.length > 0 ? ` (${data.hits.length})` : ''}
            </h2>
            <span className="muted">بحثت عن: {data.query}</span>
          </div>

          {data.hits.length === 0 ? (
            <EmptyState
              icon="😕"
              title="لا توجد نتائج"
              description="جرّب جزءاً من الاسم أو رقم اللوحة فقط، أو تأكد من الإملاء."
            />
          ) : (
            <div className="results">
              {data.hits.map((hit) => (
                <Link
                  key={`${hit.type}-${hit.id}`}
                  className="result-item"
                  to={hitPath(hit)}
                >
                  <TypeIcon type={hit.type} />
                  <div>
                    <div className="result-title">{hit.title}</div>
                    {hit.subtitle && <div className="result-sub">{hit.subtitle}</div>}
                    <div className="result-sub">اضغط لفتح التفاصيل</div>
                  </div>
                  <span className={`badge ${hit.type}`}>{typeLabels[hit.type] ?? hit.type}</span>
                </Link>
              ))}
            </div>
          )}
        </section>
      )}
    </div>
  )
}
