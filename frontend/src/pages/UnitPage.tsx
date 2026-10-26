import { type FormEvent, useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { unitsApi } from '../api/endpoints'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { ConflictDialog } from '../components/ConflictDialog'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { SearchableSelect } from '../components/SearchableSelect'
import { Toast } from '../components/Toast'
import { useAuth } from '../hooks/useAuth'
import { fetchOwnerOptions } from '../lib/lookups'
import { ApiError } from '../types'

const statuses = [
  { value: 0, label: 'غير معروف' },
  { value: 1, label: 'نشطة' },
  { value: 2, label: 'محجوزة' },
  { value: 3, label: 'تم التسليم' },
]

export function UnitPage() {
  const { id } = useParams()
  const isNew = id === 'new'
  const { canEdit } = useAuth()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const { data, refetch, isLoading } = useQuery({
    queryKey: ['unit', id],
    queryFn: () => unitsApi.get(id!),
    enabled: !isNew && !!id,
  })

  const [constructionNumber, setConstructionNumber] = useState('')
  const [terrace, setTerrace] = useState('')
  const [building, setBuilding] = useState('')
  const [floor, setFloor] = useState('')
  const [apartment, setApartment] = useState('')
  const [status, setStatus] = useState(0)
  const [ownerId, setOwnerId] = useState('')
  const [ownerLabel, setOwnerLabel] = useState('')
  const [notes, setNotes] = useState('')
  const [rowVersion, setRowVersion] = useState(0)
  const [conflict, setConflict] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)
  const clearToast = useCallback(() => setToast(null), [])

  useEffect(() => {
    if (!data) return
    setConstructionNumber(data.constructionNumber ?? '')
    setTerrace(data.terrace ?? '')
    setBuilding(data.building ?? '')
    setFloor(data.floor ?? '')
    setApartment(data.apartment ?? '')
    setStatus(data.status)
    setOwnerId(data.ownerId ?? '')
    setOwnerLabel(data.ownerName ?? '')
    setNotes(data.notes ?? '')
    setRowVersion(data.rowVersion)
  }, [data])

  const saveMutation = useMutation({
    mutationFn: async () => {
      const body = {
        constructionNumber,
        terrace,
        building,
        floor,
        apartment,
        status,
        ownerId: ownerId || null,
        notes,
        rowVersion,
      }
      if (isNew) {
        const { rowVersion: _, ...createBody } = body
        return unitsApi.create(createBody)
      }
      return unitsApi.update(id!, body)
    },
    onSuccess: (saved) => {
      void qc.invalidateQueries({ queryKey: ['unit', saved.id] })
      setToast(isNew ? 'تم حفظ الوحدة' : 'تم حفظ التعديلات')
      navigate(`/units/${saved.id}`, { replace: true })
    },
    onError: (err) => {
      if (err instanceof ApiError && err.status === 409) setConflict(true)
      else setError(err instanceof Error ? err.message : 'تعذر الحفظ. حاول مرة أخرى.')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => unitsApi.remove(id!),
    onSuccess: () => {
      setConfirmDelete(false)
      navigate('/', { replace: true })
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

  if (!isNew && isLoading) return <LoadingBlock label="جاري فتح بيانات الوحدة..." />

  return (
    <div className="stack">
      <Toast message={toast} onClose={clearToast} />
      <PageHeader
        title={isNew ? 'إضافة وحدة' : 'بيانات الوحدة'}
        subtitle={constructionNumber || 'رقم إنشائي / مصطبة / مبنى'}
        backTo="/"
      />

      {!canEdit && !isNew && (
        <div className="readonly-note">وضع القراءة فقط — يمكنك مشاهدة البيانات بدون تعديل</div>
      )}

      <form className="card form-grid" onSubmit={onSubmit}>
        <label>
          الرقم الإنشائي
          <span className="field-help">الأهم للبحث عند البوابة</span>
          <input
            value={constructionNumber}
            onChange={(e) => setConstructionNumber(e.target.value)}
            disabled={!canEdit && !isNew}
            placeholder="مثال: 12-A"
          />
        </label>
        <label>
          المصطبة
          <input
            value={terrace}
            onChange={(e) => setTerrace(e.target.value)}
            disabled={!canEdit && !isNew}
            placeholder="1 أو 2"
          />
        </label>
        <label>
          المبنى
          <input
            value={building}
            onChange={(e) => setBuilding(e.target.value)}
            disabled={!canEdit && !isNew}
          />
        </label>
        <label>
          الدور
          <input
            value={floor}
            onChange={(e) => setFloor(e.target.value)}
            disabled={!canEdit && !isNew}
          />
        </label>
        <label>
          رقم الشقة
          <input
            value={apartment}
            onChange={(e) => setApartment(e.target.value)}
            disabled={!canEdit && !isNew}
          />
        </label>
        <label>
          الحالة
          <select
            value={status}
            onChange={(e) => setStatus(Number(e.target.value))}
            disabled={!canEdit && !isNew}
          >
            {statuses.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </label>
        <SearchableSelect
          label="ربط بمالك (اختياري)"
          help="ابحث بالاسم أو الرقم الداخلي ثم اختر"
          value={ownerId}
          selectedLabel={ownerLabel}
          disabled={!canEdit && !isNew}
          placeholder="اختر المالك..."
          emptyLabel="بدون مالك"
          queryKey="owner-lookup"
          fetchOptions={fetchOwnerOptions}
          onChange={(id, option) => {
            setOwnerId(id)
            setOwnerLabel(option?.label ?? '')
          }}
        />
        {ownerId && (
          <div className="banner info">
            المالك المختار:{' '}
            <Link to={`/owners/${ownerId}`}>
              <strong>{ownerLabel || 'فتح المالك'}</strong>
            </Link>
          </div>
        )}
        <label>
          ملاحظات
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            disabled={!canEdit && !isNew}
            rows={3}
          />
        </label>

        {error && <div className="banner error">{error}</div>}

        {canEdit && (
          <div className="sticky-actions">
            <button className="btn lg" type="submit" disabled={saveMutation.isPending}>
              {saveMutation.isPending ? 'جاري الحفظ...' : 'حفظ'}
            </button>
            <button className="btn secondary" type="button" onClick={() => navigate('/')}>
              إلغاء
            </button>
            {!isNew && (
              <button
                className="btn danger"
                type="button"
                onClick={() => setConfirmDelete(true)}
                disabled={deleteMutation.isPending}
              >
                حذف
              </button>
            )}
          </div>
        )}
      </form>

      {!isNew && data && (
        <section className="card">
          <div className="section-title">
            <h2>سيارات هذه الوحدة</h2>
          </div>
          {data.vehicles.length === 0 ? (
            <p className="muted">لا توجد سيارات مرتبطة.</p>
          ) : (
            <ul className="linked-list">
              {data.vehicles.map((v) => (
                <li key={v.id}>
                  <Link to={`/vehicles/${v.id}`}>
                    <span>{v.plateNumber}</span>
                    <span className="badge vehicle">سيارة</span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}

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
        title="حذف الوحدة؟"
        message="لن تظهر في البحث بعد الآن، لكن البيانات تبقى محفوظة ويمكن استرجاعها لاحقاً."
        busy={deleteMutation.isPending}
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() => deleteMutation.mutate()}
      />
    </div>
  )
}
