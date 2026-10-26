import { type FormEvent, type ReactNode, useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { contractorsApi } from '../api/endpoints'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { ConflictDialog } from '../components/ConflictDialog'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { PhotoField } from '../components/PhotoField'
import { Toast } from '../components/Toast'
import { useAuth } from '../hooks/useAuth'
import { equipmentCategories } from '../lib/lookups'
import {
  ApiError,
  type ContractorEntry,
  type ContractorEquipment,
  type ContractorMaterial,
  type ContractorVehicle,
  type ContractorWorker,
} from '../types'

/** Rows get a client-only key so React keeps inputs stable while rows are added/removed. */
type Row<T> = T & { _k: string }
let counter = 0
const withKey = <T extends object>(item: T): Row<T> => ({ ...item, _k: `k${++counter}` })
const stripKeys = <T extends { _k: string }>(rows: T[]) =>
  rows.map((row) => {
    const rest: Record<string, unknown> = { ...row }
    delete rest._k
    return rest as Omit<T, '_k'>
  })

function updateAt<T extends { _k: string }>(rows: T[], key: string, patch: NoInfer<Partial<T>>): T[] {
  return rows.map((r) => (r._k === key ? ({ ...r, ...patch } as T) : r))
}

function Section<T extends { _k: string }>(props: {
  title: string
  addLabel: string
  rows: T[]
  editable: boolean
  onAdd: () => void
  onRemove: (key: string) => void
  children: (row: T, index: number) => ReactNode
}) {
  return (
    <section className="card sub-section">
      <div className="section-title">
        <h2>
          {props.title} ({props.rows.length})
        </h2>
        {props.editable && (
          <button type="button" className="btn secondary" onClick={props.onAdd}>
            ＋ {props.addLabel}
          </button>
        )}
      </div>
      {props.rows.length === 0 && <div className="muted">لا توجد بيانات</div>}
      {props.rows.map((row, i) => (
        <div className="sub-item" key={row._k}>
          <div className="sub-item-head">
            <span>#{i + 1}</span>
            {props.editable && (
              <button type="button" className="btn ghost" onClick={() => props.onRemove(row._k)}>
                حذف
              </button>
            )}
          </div>
          {props.children(row, i)}
        </div>
      ))}
    </section>
  )
}

export function ContractorPage() {
  const { id } = useParams()
  const isNew = id === 'new'
  const { canEdit } = useAuth()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const editable = canEdit

  const { data, refetch, isLoading } = useQuery({
    queryKey: ['contractor', id],
    queryFn: () => contractorsApi.get(id!),
    enabled: !isNew && !!id,
  })

  const [companyName, setCompanyName] = useState('')
  const [contractorName, setContractorName] = useState('')
  const [phone, setPhone] = useState('')
  const [assignment, setAssignment] = useState('')
  const [workStartDate, setWorkStartDate] = useState('')
  const [notes, setNotes] = useState('')
  const [rowVersion, setRowVersion] = useState(0)
  const [workers, setWorkers] = useState<Row<ContractorWorker>[]>([])
  const [vehicles, setVehicles] = useState<Row<ContractorVehicle>[]>([])
  const [equipment, setEquipment] = useState<Row<ContractorEquipment>[]>([])
  const [materials, setMaterials] = useState<Row<ContractorMaterial>[]>([])
  const [entries, setEntries] = useState<Row<ContractorEntry>[]>([])
  const [conflict, setConflict] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)
  const clearToast = useCallback(() => setToast(null), [])

  useEffect(() => {
    if (!data) return
    setCompanyName(data.companyName)
    setContractorName(data.contractorName)
    setPhone(data.phone ?? '')
    setAssignment(data.assignment ?? '')
    setWorkStartDate(data.workStartDate ?? '')
    setNotes(data.notes ?? '')
    setRowVersion(data.rowVersion)
    setWorkers(data.workers.map(withKey))
    setVehicles(data.vehicles.map(withKey))
    setEquipment(data.equipment.map(withKey))
    setMaterials(data.materials.map(withKey))
    setEntries(data.entries.map(withKey))
  }, [data])

  const saveMutation = useMutation({
    mutationFn: async () => {
      const body = {
        companyName,
        contractorName,
        phone,
        assignment,
        workStartDate: workStartDate || null,
        notes,
        rowVersion,
        workers: stripKeys(workers),
        vehicles: stripKeys(vehicles),
        equipment: stripKeys(equipment),
        materials: stripKeys(materials),
        entries: stripKeys(entries).map((e) => ({
          ...e,
          entryDate: e.entryDate || null,
          exitDate: e.exitDate || null,
        })),
      }
      return isNew ? contractorsApi.create(body) : contractorsApi.update(id!, body)
    },
    onSuccess: (saved) => {
      void qc.invalidateQueries({ queryKey: ['contractor', saved.id] })
      void qc.invalidateQueries({ queryKey: ['contractors'] })
      setToast(isNew ? 'تم حفظ المقاول' : 'تم حفظ التعديلات')
      navigate(`/contractors/${saved.id}`, { replace: true })
    },
    onError: (err) => {
      if (err instanceof ApiError && err.status === 409) setConflict(true)
      else setError(err instanceof Error ? err.message : 'تعذر الحفظ. حاول مرة أخرى.')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => contractorsApi.remove(id!),
    onSuccess: () => {
      setConfirmDelete(false)
      void qc.invalidateQueries({ queryKey: ['contractors'] })
      navigate('/contractors', { replace: true })
    },
    onError: (err) => {
      setConfirmDelete(false)
      setError(err instanceof Error ? err.message : 'تعذر الحذف. حاول مرة أخرى.')
    },
  })

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    saveMutation.mutate()
  }

  if (!isNew && isLoading) return <LoadingBlock label="جاري فتح بيانات المقاول..." />

  const ro = !editable

  return (
    <div className="stack">
      <Toast message={toast} onClose={clearToast} />
      <PageHeader
        title={isNew ? 'إضافة مقاول' : 'بيانات المقاول'}
        subtitle={companyName || 'الشركة والعمالة والسيارات والمعدات والخامات'}
        backTo="/contractors"
      />

      {ro && <div className="readonly-note">وضع القراءة فقط — يمكنك مشاهدة البيانات بدون تعديل</div>}

      <form className="stack" onSubmit={onSubmit}>
        <section className="card form-grid">
          <label>
            اسم الشركة *
            <input value={companyName} onChange={(e) => setCompanyName(e.target.value)} required disabled={ro} autoFocus={isNew} />
          </label>
          <label>
            اسم المقاول *
            <input value={contractorName} onChange={(e) => setContractorName(e.target.value)} required disabled={ro} />
          </label>
          <label>
            الهاتف
            <input value={phone} onChange={(e) => setPhone(e.target.value)} disabled={ro} inputMode="tel" />
          </label>
          <label>
            التكليف (العمل المطلوب)
            <textarea value={assignment} onChange={(e) => setAssignment(e.target.value)} disabled={ro} rows={2} />
          </label>
          <label>
            تاريخ استلام العمل
            <input type="date" value={workStartDate} onChange={(e) => setWorkStartDate(e.target.value)} disabled={ro} />
          </label>
          <div className="banner info">عدد العمالة: <strong>{workers.length}</strong></div>
          <label>
            ملاحظات
            <textarea value={notes} onChange={(e) => setNotes(e.target.value)} disabled={ro} rows={2} />
          </label>
        </section>

        <Section
          title="العمالة"
          addLabel="إضافة عامل"
          rows={workers}
          editable={editable}
          onAdd={() => setWorkers((r) => [...r, withKey({ name: '' })])}
          onRemove={(k) => setWorkers((r) => r.filter((x) => x._k !== k))}
        >
          {(w) => {
            const set = (p: Partial<ContractorWorker>) => setWorkers((r) => updateAt(r, w._k, p))
            return (
              <div className="form-grid">
                <label>
                  اسم العامل *
                  <input value={w.name} onChange={(e) => set({ name: e.target.value })} required disabled={ro} />
                </label>
                <label>
                  الوظيفة
                  <input value={w.jobTitle ?? ''} onChange={(e) => set({ jobTitle: e.target.value })} disabled={ro} />
                </label>
                <label>
                  مكان السكن
                  <input value={w.housing ?? ''} onChange={(e) => set({ housing: e.target.value })} disabled={ro} />
                </label>
                <PhotoField label="صورة البطاقة" value={w.idPhotoPath} disabled={ro} canView={canEdit} onChange={(p) => set({ idPhotoPath: p })} />
                <PhotoField label="صورة الفيش الجنائي" value={w.criminalRecordPhotoPath} disabled={ro} canView={canEdit} onChange={(p) => set({ criminalRecordPhotoPath: p })} />
              </div>
            )
          }}
        </Section>

        <Section
          title="سيارات المقاول / سيارة خدمة الموقع"
          addLabel="إضافة سيارة"
          rows={vehicles}
          editable={editable}
          onAdd={() => setVehicles((r) => [...r, withKey({ plateNumber: '', isSiteServiceCar: false })])}
          onRemove={(k) => setVehicles((r) => r.filter((x) => x._k !== k))}
        >
          {(v) => {
            const set = (p: Partial<ContractorVehicle>) => setVehicles((r) => updateAt(r, v._k, p))
            return (
              <div className="form-grid">
                <label>
                  رقم اللوحة *
                  <input value={v.plateNumber} onChange={(e) => set({ plateNumber: e.target.value })} required disabled={ro} />
                </label>
                <label className="check">
                  <input type="checkbox" checked={v.isSiteServiceCar} onChange={(e) => set({ isSiteServiceCar: e.target.checked })} disabled={ro} />
                  سيارة خدمة الموقع التابعة للمقاول
                </label>
                <label>
                  ملاحظات
                  <input value={v.notes ?? ''} onChange={(e) => set({ notes: e.target.value })} disabled={ro} />
                </label>
              </div>
            )
          }}
        </Section>

        <Section
          title="المعدات"
          addLabel="إضافة معدة"
          rows={equipment}
          editable={editable}
          onAdd={() => setEquipment((r) => [...r, withKey({ type: '', category: 1 as const })])}
          onRemove={(k) => setEquipment((r) => r.filter((x) => x._k !== k))}
        >
          {(m) => {
            const set = (p: Partial<ContractorEquipment>) => setEquipment((r) => updateAt(r, m._k, p))
            return (
              <div className="form-grid">
                <label>
                  نوع المعدة *
                  <input value={m.type} onChange={(e) => set({ type: e.target.value })} required disabled={ro} />
                </label>
                <label>
                  رقم المعدة
                  <input value={m.number ?? ''} onChange={(e) => set({ number: e.target.value })} disabled={ro} />
                </label>
                <label>
                  التصنيف
                  <select value={m.category} onChange={(e) => set({ category: Number(e.target.value) as 1 | 2 })} disabled={ro}>
                    {equipmentCategories.map((c) => (
                      <option key={c.value} value={c.value}>
                        {c.label}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  تصريح حركة المعدة
                  <input value={m.movementPermit ?? ''} onChange={(e) => set({ movementPermit: e.target.value })} disabled={ro} />
                </label>
                <PhotoField label="صورة المعدة (حالتها)" value={m.photoPath} disabled={ro} canView={canEdit} onChange={(p) => set({ photoPath: p })} />
                <label>
                  ملاحظات
                  <input value={m.notes ?? ''} onChange={(e) => set({ notes: e.target.value })} disabled={ro} />
                </label>
              </div>
            )
          }}
        </Section>

        <Section
          title="الخامات"
          addLabel="إضافة خامة"
          rows={materials}
          editable={editable}
          onAdd={() => setMaterials((r) => [...r, withKey({ description: '' })])}
          onRemove={(k) => setMaterials((r) => r.filter((x) => x._k !== k))}
        >
          {(m) => {
            const set = (p: Partial<ContractorMaterial>) => setMaterials((r) => updateAt(r, m._k, p))
            return (
              <div className="form-grid">
                <label>
                  وصف الخامة *
                  <input value={m.description} onChange={(e) => set({ description: e.target.value })} required disabled={ro} />
                </label>
                <label>
                  مكان التشوين / المخزن
                  <input value={m.storageLocation ?? ''} onChange={(e) => set({ storageLocation: e.target.value })} disabled={ro} />
                </label>
                <PhotoField label="صورة الخامة" value={m.photoPath} disabled={ro} canView={canEdit} onChange={(p) => set({ photoPath: p })} />
              </div>
            )
          }}
        </Section>

        <Section
          title="الدخول والخروج"
          addLabel="إضافة دخول"
          rows={entries}
          editable={editable}
          onAdd={() => setEntries((r) => [...r, withKey({})])}
          onRemove={(k) => setEntries((r) => r.filter((x) => x._k !== k))}
        >
          {(en) => {
            const set = (p: Partial<ContractorEntry>) => setEntries((r) => updateAt(r, en._k, p))
            return (
              <div className="form-grid">
                <label>
                  تاريخ الدخول
                  <input type="date" value={en.entryDate ?? ''} onChange={(e) => set({ entryDate: e.target.value })} disabled={ro} />
                </label>
                <label>
                  سبب الدخول
                  <input value={en.entryReason ?? ''} onChange={(e) => set({ entryReason: e.target.value })} disabled={ro} />
                </label>
                <label>
                  تصريح الدخول
                  <input value={en.entryPermit ?? ''} onChange={(e) => set({ entryPermit: e.target.value })} disabled={ro} />
                </label>
                <label>
                  تاريخ الخروج
                  <input type="date" value={en.exitDate ?? ''} onChange={(e) => set({ exitDate: e.target.value })} disabled={ro} />
                </label>
                <label>
                  سبب الخروج
                  <input value={en.exitReason ?? ''} onChange={(e) => set({ exitReason: e.target.value })} disabled={ro} />
                </label>
                <label>
                  تصريح الخروج
                  <input value={en.exitPermit ?? ''} onChange={(e) => set({ exitPermit: e.target.value })} disabled={ro} />
                </label>
              </div>
            )
          }}
        </Section>

        {error && <div className="banner error">{error}</div>}

        {editable && (
          <div className="sticky-actions">
            <button className="btn lg" type="submit" disabled={saveMutation.isPending || !companyName.trim() || !contractorName.trim()}>
              {saveMutation.isPending ? 'جاري الحفظ...' : 'حفظ'}
            </button>
            <button className="btn secondary" type="button" onClick={() => navigate('/contractors')}>
              إلغاء
            </button>
            {!isNew && (
              <button className="btn danger" type="button" onClick={() => setConfirmDelete(true)} disabled={deleteMutation.isPending}>
                حذف
              </button>
            )}
          </div>
        )}
      </form>

      <ConflictDialog
        open={conflict}
        onClose={() => setConflict(false)}
        onReload={() => {
          setConflict(false)
          void refetch()
        }}
      />

      <ConfirmDialog
        open={confirmDelete}
        title="حذف المقاول؟"
        message="لن يظهر في البحث بعد الآن، لكن البيانات تبقى محفوظة ويمكن استرجاعها لاحقاً."
        busy={deleteMutation.isPending}
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() => deleteMutation.mutate()}
      />
    </div>
  )
}
