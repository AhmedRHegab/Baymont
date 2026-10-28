import { type FormEvent, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { siteApi } from '../api/endpoints'
import { EmptyState } from '../components/EmptyState'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { useAuth } from '../hooks/useAuth'
import { dayBounds, formatDateTime, todayLocal } from '../lib/dates'
import { visitorTypeLabel } from '../lib/lookups'
import type { StaffDto } from '../types'

interface Form {
  name: string
  company: string
  jobTitle: string
  plateNumber: string
  phone: string
}
const empty: Form = { name: '', company: '', jobTitle: '', plateNumber: '', phone: '' }

export function StaffPage() {
  const { canEdit } = useAuth()
  const qc = useQueryClient()
  const [params] = useSearchParams()
  const [q, setQ] = useState(params.get('q') ?? '')
  const [form, setForm] = useState<Form>(empty)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const staff = useQuery({ queryKey: ['staff', q], queryFn: () => siteApi.staff(q) })
  const onSite = useQuery({ queryKey: ['on-site'], queryFn: siteApi.onSite, refetchInterval: 60_000 })
  const log = useQuery({
    queryKey: ['staff-log', todayLocal()],
    queryFn: () => {
      const { from, to } = dayBounds(todayLocal())
      return siteApi.staffLog(from, to)
    },
  })

  function refresh() {
    void qc.invalidateQueries({ queryKey: ['staff'] })
    void qc.invalidateQueries({ queryKey: ['on-site'] })
    void qc.invalidateQueries({ queryKey: ['staff-log'] })
  }
  const fail = (err: unknown) => setError(err instanceof Error ? err.message : 'حدث خطأ')

  const saveMutation = useMutation({
    mutationFn: () =>
      editingId
        ? siteApi.updateStaff(editingId, form)
        : siteApi.createStaff(form).then(() => undefined),
    onSuccess: () => {
      refresh()
      setForm(empty)
      setEditingId(null)
      setShowForm(false)
      setError(null)
    },
    onError: fail,
  })
  const moveMutation = useMutation({
    mutationFn: ({ id, enter }: { id: string; enter: boolean }) =>
      enter ? siteApi.enter(id) : siteApi.exit(id),
    onSuccess: () => {
      refresh()
      setError(null)
    },
    onError: fail,
  })
  const removeMutation = useMutation({
    mutationFn: (id: string) => siteApi.removeStaff(id),
    onSuccess: refresh,
    onError: fail,
  })

  function edit(s: StaffDto) {
    setForm({
      name: s.name,
      company: s.company ?? '',
      jobTitle: s.jobTitle ?? '',
      plateNumber: s.plateNumber ?? '',
      phone: s.phone ?? '',
    })
    setEditingId(s.id)
    setShowForm(true)
  }

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    saveMutation.mutate()
  }
  const set = (p: Partial<Form>) => setForm((f) => ({ ...f, ...p }))

  return (
    <div className="stack">
      <PageHeader title="الموظفون والمتواجدون" subtitle="دخول وخروج موظفي الشركة، ومن بالموقع الآن" showBack={false} />

      <section className="card stack">
        <div className="section-title">
          <h2>المتواجدون بالموقع الآن</h2>
          <button type="button" className="btn secondary" onClick={() => window.print()}>
            طباعة
          </button>
        </div>
        {onSite.isLoading ? (
          <LoadingBlock label="جاري التحميل..." />
        ) : (
          <>
            <div className="banner info">
              <strong>إجمالي من بالموقع الآن: {onSite.data?.totalPeople ?? 0}</strong>
              <br />
              موظفون: <strong>{onSite.data?.staff.length ?? 0}</strong> · عمال المقاولين:{' '}
              <strong>{onSite.data?.contractorWorkers ?? 0}</strong> ({onSite.data?.contractors.length ?? 0} مقاول) · ضيوف
              الاستضافة والتصاريح: <strong>{onSite.data?.guestPersons ?? 0}</strong>
            </div>
            {onSite.data?.guests.map((g) => (
              <div className="sub-item" key={g.id}>
                <strong>{g.guestNames}</strong>
                <span className="muted">
                  ضيف · {visitorTypeLabel(g.type)} · وحدة {g.unitCode} · {g.personsCount} شخص — دخل {formatDateTime(g.enteredAt)}
                </span>
              </div>
            ))}
            {onSite.data?.staff.map((s) => (
              <div className="sub-item" key={s.id}>
                <strong>{s.name}</strong>
                <span className="muted">
                  {[s.company, s.jobTitle, s.plateNumber].filter(Boolean).join(' · ')} — دخل {formatDateTime(s.entryAt)}
                </span>
              </div>
            ))}
            {onSite.data?.contractors.map((c) => (
              <div className="sub-item" key={c.id}>
                <strong>{c.companyName}</strong>
                <span className="muted">
                  {c.contractorName} · {c.workerCount} عامل
                  {c.entryDate ? ` — دخل ${c.entryDate}` : ''}
                  {c.entryReason ? ` · ${c.entryReason}` : ''}
                </span>
              </div>
            ))}
          </>
        )}
      </section>

      {error && <div className="banner error">{error}</div>}

      <section className="card stack">
        <div className="search-row">
          <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="ابحث بالاسم أو الشركة أو اللوحة..." aria-label="بحث الموظفين" />
          {canEdit && (
            <button
              type="button"
              className="btn lg"
              onClick={() => {
                setForm(empty)
                setEditingId(null)
                setShowForm((v) => !v)
              }}
            >
              ＋ موظف جديد
            </button>
          )}
        </div>

        {showForm && canEdit && (
          <form className="form-grid sub-item" onSubmit={onSubmit}>
            <label>
              الاسم *
              <input value={form.name} onChange={(e) => set({ name: e.target.value })} required autoFocus />
            </label>
            <label>
              الشركة
              <input value={form.company} onChange={(e) => set({ company: e.target.value })} />
            </label>
            <label>
              الوظيفة
              <input value={form.jobTitle} onChange={(e) => set({ jobTitle: e.target.value })} />
            </label>
            <label>
              رقم السيارة
              <input value={form.plateNumber} onChange={(e) => set({ plateNumber: e.target.value })} />
            </label>
            <label>
              الموبايل
              <input value={form.phone} onChange={(e) => set({ phone: e.target.value })} inputMode="tel" />
            </label>
            <div className="sticky-actions">
              <button className="btn lg" type="submit" disabled={saveMutation.isPending || !form.name.trim()}>
                {editingId ? 'حفظ التعديل' : 'إضافة'}
              </button>
              <button className="btn secondary" type="button" onClick={() => setShowForm(false)}>
                إلغاء
              </button>
            </div>
          </form>
        )}

        {staff.isLoading ? (
          <LoadingBlock label="جاري تحميل الموظفين..." />
        ) : !staff.data?.length ? (
          <EmptyState icon="🧑‍💼" title="لا يوجد موظفون" description="أضف الموظفين ثم سجّل دخولهم وخروجهم." />
        ) : (
          staff.data.map((s) => (
            <div className="sub-item" key={s.id}>
              <div className="sub-item-head">
                <span>{s.name}</span>
                <span className={`badge ${s.isOnSite ? 'contractor' : 'vehicle'}`}>{s.isOnSite ? 'بالموقع' : 'خارج الموقع'}</span>
              </div>
              <div className="muted">
                {[s.company, s.jobTitle, s.plateNumber, s.phone].filter(Boolean).join(' · ') || '—'}
              </div>
              {s.lastEntryAt && (
                <div className="muted">
                  آخر دخول: {formatDateTime(s.lastEntryAt)}
                  {s.lastExitAt ? ` · آخر خروج: ${formatDateTime(s.lastExitAt)}` : ''}
                </div>
              )}
              {canEdit && (
                <div className="actions">
                  <button
                    type="button"
                    className="btn"
                    disabled={moveMutation.isPending}
                    onClick={() => moveMutation.mutate({ id: s.id, enter: !s.isOnSite })}
                  >
                    {s.isOnSite ? 'تسجيل خروج' : 'تسجيل دخول'}
                  </button>
                  <button type="button" className="btn secondary" onClick={() => edit(s)}>
                    تعديل
                  </button>
                  <button
                    type="button"
                    className="btn ghost"
                    onClick={() => {
                      if (window.confirm(`حذف ${s.name} وسجل دخوله وخروجه؟`)) removeMutation.mutate(s.id)
                    }}
                  >
                    حذف
                  </button>
                </div>
              )}
            </div>
          ))
        )}
      </section>

      <section className="card stack">
        <div className="section-title">
          <h2>سجل اليوم ({log.data?.length ?? 0})</h2>
        </div>
        {!log.data?.length ? (
          <div className="muted">لا توجد حركة اليوم</div>
        ) : (
          log.data.map((l) => (
            <div className="sub-item" key={l.id}>
              <strong>{l.name}</strong>
              <span className="muted">
                دخول {formatDateTime(l.entryAt)} · خروج {formatDateTime(l.exitAt)}
              </span>
            </div>
          ))
        )}
      </section>
    </div>
  )
}
