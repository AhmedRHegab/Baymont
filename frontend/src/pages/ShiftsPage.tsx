import { type FormEvent, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { siteApi } from '../api/endpoints'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { useAuth } from '../hooks/useAuth'
import { todayLocal } from '../lib/dates'
import { shiftKinds } from '../lib/lookups'
import type { ShiftAssignment, ShiftKind } from '../types'

interface Form {
  kind: ShiftKind
  name: string
  jobTitle: string
  assignedWork: string
  phone: string
  notes: string
}
const blank = (kind: ShiftKind): Form => ({ kind, name: '', jobTitle: '', assignedWork: '', phone: '', notes: '' })

export function ShiftsPage() {
  const { canEdit } = useAuth()
  const qc = useQueryClient()
  const [date, setDate] = useState(todayLocal())
  const [form, setForm] = useState<Form | null>(null)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data, isLoading } = useQuery({ queryKey: ['shifts', date], queryFn: () => siteApi.shifts(date) })

  const saveMutation = useMutation({
    mutationFn: () => {
      const body = { ...form, shiftDate: date }
      return editingId
        ? siteApi.updateShift(editingId, body)
        : siteApi.createShift(body).then(() => undefined)
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['shifts'] })
      setForm(null)
      setEditingId(null)
      setError(null)
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'تعذر الحفظ'),
  })
  const removeMutation = useMutation({
    mutationFn: (id: string) => siteApi.removeShift(id),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['shifts'] }),
    onError: (err) => setError(err instanceof Error ? err.message : 'تعذر الحذف'),
  })

  function edit(a: ShiftAssignment) {
    setForm({
      kind: a.kind,
      name: a.name,
      jobTitle: a.jobTitle ?? '',
      assignedWork: a.assignedWork ?? '',
      phone: a.phone ?? '',
      notes: a.notes ?? '',
    })
    setEditingId(a.id)
  }
  function onSubmit(e: FormEvent) {
    e.preventDefault()
    saveMutation.mutate()
  }
  const set = (p: Partial<Form>) => setForm((f) => (f ? { ...f, ...p } : f))

  return (
    <div className="stack">
      <PageHeader title="الورديات والدوريات" subtitle="الوردية الخارجية والدورية الليلية لكل يوم" showBack={false} />

      <section className="card form-grid">
        <label>
          اليوم
          <input type="date" min="2000-01-01" max="2100-12-31" value={date} onChange={(e) => setDate(e.target.value)} />
        </label>
      </section>

      {error && <div className="banner error">{error}</div>}

      {form && canEdit && (
        <form className="card form-grid" onSubmit={onSubmit}>
          <label>
            النوع
            <select value={form.kind} onChange={(e) => set({ kind: Number(e.target.value) as ShiftKind })}>
              {shiftKinds.map((k) => (
                <option key={k.value} value={k.value}>
                  {k.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            الاسم *
            <input maxLength={256} value={form.name} onChange={(e) => set({ name: e.target.value })} required autoFocus />
          </label>
          <label>
            الوظيفة
            <input maxLength={128} value={form.jobTitle} onChange={(e) => set({ jobTitle: e.target.value })} />
          </label>
          <label>
            العمل المسند
            <input maxLength={256} value={form.assignedWork} onChange={(e) => set({ assignedWork: e.target.value })} />
          </label>
          <label>
            رقم التليفون
            <input maxLength={64} pattern="[0-9٠-٩+ ,.،\/\(\)\-]*" title="اكتب أرقاماً فقط (يمكن استخدام + و - والمسافة)." value={form.phone} onChange={(e) => set({ phone: e.target.value })} inputMode="tel" />
          </label>
          <label>
            ملاحظات
            <textarea maxLength={512} value={form.notes} onChange={(e) => set({ notes: e.target.value })} rows={2} />
          </label>
          <div className="sticky-actions">
            <button className="btn lg" type="submit" disabled={saveMutation.isPending || !form.name.trim()}>
              {editingId ? 'حفظ التعديل' : 'إضافة'}
            </button>
            <button
              className="btn secondary"
              type="button"
              onClick={() => {
                setForm(null)
                setEditingId(null)
              }}
            >
              إلغاء
            </button>
          </div>
        </form>
      )}

      {isLoading ? (
        <LoadingBlock label="جاري تحميل الورديات..." />
      ) : (
        shiftKinds.map((k) => {
          const rows = data?.filter((a) => a.kind === k.value) ?? []
          return (
            <section className="card sub-section" key={k.value}>
              <div className="section-title">
                <h2>
                  {k.label} ({rows.length})
                </h2>
                {canEdit && (
                  <button
                    type="button"
                    className="btn secondary"
                    onClick={() => {
                      setForm(blank(k.value))
                      setEditingId(null)
                    }}
                  >
                    ＋ إضافة
                  </button>
                )}
              </div>
              {rows.length === 0 && <div className="muted">لا توجد بيانات</div>}
              {rows.map((a) => (
                <div className="sub-item" key={a.id}>
                  <div className="sub-item-head">
                    <span>{a.name}</span>
                    {canEdit && (
                      <span className="actions">
                        <button type="button" className="btn secondary" onClick={() => edit(a)}>
                          تعديل
                        </button>
                        <button type="button" className="btn ghost" onClick={() => removeMutation.mutate(a.id)}>
                          حذف
                        </button>
                      </span>
                    )}
                  </div>
                  <div className="muted">
                    {[a.jobTitle, a.assignedWork, a.phone].filter(Boolean).join(' · ') || '—'}
                  </div>
                  {a.notes && <div className="muted">ملاحظات: {a.notes}</div>}
                </div>
              ))}
            </section>
          )
        })
      )}
    </div>
  )
}
