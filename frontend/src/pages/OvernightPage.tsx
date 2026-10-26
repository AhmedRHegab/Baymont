import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ownersApi, siteApi } from '../api/endpoints'
import { EmptyState } from '../components/EmptyState'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { SearchableSelect } from '../components/SearchableSelect'
import { Toast } from '../components/Toast'
import { useAuth } from '../hooks/useAuth'
import { todayLocal } from '../lib/dates'
import { fetchOwnerOptions, fetchUnitOptions, shiftPeriods } from '../lib/lookups'
import type { OvernightItem, OvernightReport, ShiftPeriod } from '../types'

const keycaps = ['0️⃣', '1️⃣', '2️⃣', '3️⃣', '4️⃣', '5️⃣', '6️⃣', '7️⃣', '8️⃣', '9️⃣']
const keycap = (v: string) => (/^\d$/.test(v) ? keycaps[Number(v)] : v)

function location(i: OvernightItem) {
  return [
    i.building && `مبني ${i.building}`,
    i.floor && `الدور ${i.floor}`,
    i.apartment && `شقه ${i.apartment}`,
  ]
    .filter(Boolean)
    .join(' - ')
}

/** Same layout as the list the security team posts in the group chat. */
function buildText(r: OvernightReport) {
  const day = new Date(`${r.date}T00:00:00Z`).toLocaleDateString('ar-EG', {
    weekday: 'long',
    timeZone: 'UTC',
  })
  const [, m, d] = r.date.split('-')
  const shift = shiftPeriods.find((s) => s.value === r.shift)?.label ?? ''
  const lines = [`تمام بالملاك المتواجدون مبيت بالموقع عن يوم / ${day} الموافق : ${d}/${m}  بالورديه ${shift}`]
  for (const g of r.groups) {
    lines.push(`مصطبه ${keycap(g.terrace)}`, '')
    g.items.forEach((it, n) => {
      lines.push(`${n + 1} - أ /${it.ownerName}`)
      const loc = location(it)
      if (loc) lines.push(loc)
      lines.push('')
    })
  }
  return lines.join('\n').trim()
}

export function OvernightPage() {
  const { canEdit } = useAuth()
  const qc = useQueryClient()
  const [date, setDate] = useState(todayLocal())
  const [shift, setShift] = useState<ShiftPeriod>(1)
  const [ownerId, setOwnerId] = useState('')
  const [ownerLabel, setOwnerLabel] = useState('')
  const [unitId, setUnitId] = useState('')
  const [unitLabelText, setUnitLabelText] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['overnight', date, shift],
    queryFn: () => siteApi.overnight(date, shift),
  })

  const addMutation = useMutation({
    mutationFn: () =>
      siteApi.addOvernight({ stayDate: date, shift, ownerId, unitId: unitId || null }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['overnight'] })
      setOwnerId('')
      setOwnerLabel('')
      setUnitId('')
      setUnitLabelText('')
      setError(null)
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'تعذر الحفظ'),
  })

  const removeMutation = useMutation({
    mutationFn: (id: string) => siteApi.removeOvernight(id),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['overnight'] }),
    onError: (err) => setError(err instanceof Error ? err.message : 'تعذر الحذف'),
  })

  async function pickOwner(id: string, label?: string) {
    setOwnerId(id)
    setOwnerLabel(label ?? '')
    if (!id) return
    try {
      // One unit: fill it for the guard. Several: leave the choice to them.
      const owner = await ownersApi.get(id)
      if (owner.units.length === 1) {
        setUnitId(owner.units[0].id)
        setUnitLabelText(owner.units[0].constructionNumber ?? 'الوحدة')
      }
    } catch {
      /* the unit can be picked by hand */
    }
  }

  async function copyText() {
    if (!data) return
    try {
      await navigator.clipboard.writeText(buildText(data))
      setToast('تم نسخ القائمة')
    } catch {
      setToast('تعذر النسخ، حدّد النص يدوياً')
    }
  }

  return (
    <div className="stack">
      <Toast message={toast} onClose={() => setToast(null)} />
      <PageHeader title="الملاك المبيت" subtitle="قائمة الملاك المتواجدين مبيت بالموقع" showBack={false} />

      <section className="card form-grid">
        <label>
          التاريخ
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </label>
        <label>
          الوردية
          <select value={shift} onChange={(e) => setShift(Number(e.target.value) as ShiftPeriod)}>
            {shiftPeriods.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </label>
      </section>

      {canEdit && (
        <section className="card form-grid">
          <SearchableSelect
            label="إضافة مالك"
            help="ابحث بالاسم ثم اختر"
            value={ownerId}
            selectedLabel={ownerLabel}
            placeholder="اختر المالك..."
            emptyLabel="بدون"
            queryKey="owner-lookup"
            fetchOptions={fetchOwnerOptions}
            onChange={(id, o) => void pickOwner(id, o?.label)}
          />
          <SearchableSelect
            label="الوحدة"
            help="تُعبّأ تلقائياً إذا كان للمالك وحدة واحدة"
            value={unitId}
            selectedLabel={unitLabelText}
            placeholder="اختر الوحدة..."
            emptyLabel="بدون وحدة"
            queryKey="unit-lookup"
            fetchOptions={fetchUnitOptions}
            onChange={(id, o) => {
              setUnitId(id)
              setUnitLabelText(o?.label ?? '')
            }}
          />
          {error && <div className="banner error">{error}</div>}
          <button className="btn lg" type="button" disabled={!ownerId || addMutation.isPending} onClick={() => addMutation.mutate()}>
            {addMutation.isPending ? 'جاري الإضافة...' : '＋ إضافة للقائمة'}
          </button>
        </section>
      )}

      {isLoading ? (
        <LoadingBlock label="جاري تحميل القائمة..." />
      ) : !data || data.total === 0 ? (
        <section className="card">
          <EmptyState icon="🌙" title="لا يوجد ملاك مسجلون" description="لم يُسجل أحد في هذا اليوم والوردية." />
        </section>
      ) : (
        <section className="card stack">
          <div className="section-title">
            <h2>الإجمالي ({data.total})</h2>
            <span className="actions">
              <button type="button" className="btn secondary" onClick={() => void copyText()}>
                نسخ كنص
              </button>
              <button type="button" className="btn secondary" onClick={() => window.print()}>
                طباعة
              </button>
            </span>
          </div>
          {data.groups.map((g) => (
            <div className="sub-section" key={g.terrace}>
              <h3>{/^\d+$/.test(g.terrace) ? `مصطبة ${g.terrace}` : g.terrace}</h3>
              {g.items.map((it, n) => (
                <div className="sub-item" key={it.id}>
                  <div className="sub-item-head">
                    <span>
                      {n + 1} - {it.ownerName}
                    </span>
                    {canEdit && (
                      <button type="button" className="btn ghost" onClick={() => removeMutation.mutate(it.id)}>
                        حذف
                      </button>
                    )}
                  </div>
                  <div className="muted">{location(it) || 'بدون وحدة'}</div>
                </div>
              ))}
            </div>
          ))}
        </section>
      )}
    </div>
  )
}
