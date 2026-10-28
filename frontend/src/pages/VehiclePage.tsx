import { type FormEvent, useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { vehiclesApi } from '../api/endpoints'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { ConflictDialog } from '../components/ConflictDialog'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { SearchableSelect } from '../components/SearchableSelect'
import { Toast } from '../components/Toast'
import { useAuth } from '../hooks/useAuth'
import { fetchOwnerOptions, fetchUnitOptions } from '../lib/lookups'
import { ApiError } from '../types'

export function VehiclePage() {
  const { id } = useParams()
  const isNew = id === 'new'
  const { canEdit } = useAuth()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const { data, refetch, isLoading } = useQuery({
    queryKey: ['vehicle', id],
    queryFn: () => vehiclesApi.get(id!),
    enabled: !isNew && !!id,
  })

  const [plateNumber, setPlateNumber] = useState('')
  const [make, setMake] = useState('')
  const [model, setModel] = useState('')
  const [color, setColor] = useState('')
  const [ownerId, setOwnerId] = useState('')
  const [ownerLabel, setOwnerLabel] = useState('')
  const [unitId, setUnitId] = useState('')
  const [unitLabelText, setUnitLabelText] = useState('')
  const [notes, setNotes] = useState('')
  const [rowVersion, setRowVersion] = useState(0)
  const [conflict, setConflict] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)
  const clearToast = useCallback(() => setToast(null), [])

  useEffect(() => {
    if (!data) return
    setPlateNumber(data.plateNumber)
    setMake(data.make ?? '')
    setModel(data.model ?? '')
    setColor(data.color ?? '')
    setOwnerId(data.ownerId ?? '')
    setOwnerLabel(data.ownerName ?? '')
    setUnitId(data.unitId ?? '')
    setUnitLabelText(data.constructionNumber ?? '')
    setNotes(data.notes ?? '')
    setRowVersion(data.rowVersion)
  }, [data])

  const saveMutation = useMutation({
    mutationFn: async () => {
      const body = {
        plateNumber,
        make,
        model,
        color,
        ownerId: ownerId || null,
        unitId: unitId || null,
        notes,
        rowVersion,
      }
      if (isNew) {
        const { rowVersion: _, ...createBody } = body
        return vehiclesApi.create(createBody)
      }
      return vehiclesApi.update(id!, body)
    },
    onSuccess: (saved) => {
      void qc.invalidateQueries({ queryKey: ['vehicle', saved.id] })
      setToast(isNew ? 'تم حفظ السيارة' : 'تم حفظ التعديلات')
      navigate(`/vehicles/${saved.id}`, { replace: true })
    },
    onError: (err) => {
      if (err instanceof ApiError && err.status === 409) setConflict(true)
      else setError(err instanceof Error ? err.message : 'تعذر الحفظ. حاول مرة أخرى.')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => vehiclesApi.remove(id!),
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

  if (!isNew && isLoading) return <LoadingBlock label="جاري فتح بيانات السيارة..." />

  return (
    <div className="stack">
      <Toast message={toast} onClose={clearToast} />
      <PageHeader
        title={isNew ? 'إضافة سيارة' : 'بيانات السيارة'}
        subtitle={plateNumber || 'رقم اللوحة هو الأهم للبحث عند البوابة'}
        backTo="/"
      />

      {!canEdit && !isNew && (
        <div className="readonly-note">وضع القراءة فقط — يمكنك مشاهدة البيانات بدون تعديل</div>
      )}

      <form className="card form-grid" onSubmit={onSubmit}>
        <label>
          رقم اللوحة *
          <span className="field-help">اكتب الرقم كما يظهر على السيارة</span>
          <input maxLength={64}
            value={plateNumber}
            onChange={(e) => setPlateNumber(e.target.value)}
            required
            disabled={!canEdit && !isNew}
            placeholder="مثال: أ ب ج 1234"
            autoFocus={isNew}
          />
        </label>
        <label>
          الماركة
          <input maxLength={64}
            value={make}
            onChange={(e) => setMake(e.target.value)}
            disabled={!canEdit && !isNew}
            placeholder="تويوتا"
          />
        </label>
        <label>
          الموديل
          <input maxLength={64}
            value={model}
            onChange={(e) => setModel(e.target.value)}
            disabled={!canEdit && !isNew}
            placeholder="كورولا"
          />
        </label>
        <label>
          اللون
          <input maxLength={64}
            value={color}
            onChange={(e) => setColor(e.target.value)}
            disabled={!canEdit && !isNew}
            placeholder="أبيض"
          />
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

        <SearchableSelect
          label="ربط بوحدة (اختياري)"
          help="ابحث بالرقم الإنشائي أو اسم المالك ثم اختر"
          value={unitId}
          selectedLabel={unitLabelText}
          disabled={!canEdit && !isNew}
          placeholder="اختر الوحدة..."
          emptyLabel="بدون وحدة"
          queryKey="unit-lookup"
          fetchOptions={fetchUnitOptions}
          onChange={(id, option) => {
            setUnitId(id)
            setUnitLabelText(option?.label ?? '')
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
        {unitId && (
          <div className="banner info">
            الوحدة المختارة:{' '}
            <Link to={`/units/${unitId}`}>
              <strong>{unitLabelText || 'فتح الوحدة'}</strong>
            </Link>
          </div>
        )}

        <label>
          ملاحظات
          <textarea maxLength={2000}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            disabled={!canEdit && !isNew}
            rows={3}
          />
        </label>

        {error && <div className="banner error">{error}</div>}

        {canEdit && (
          <div className="sticky-actions">
            <button
              className="btn lg"
              type="submit"
              disabled={saveMutation.isPending || !plateNumber.trim()}
            >
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
        title="حذف السيارة؟"
        message="لن تظهر في البحث بعد الآن، لكن البيانات تبقى محفوظة ويمكن استرجاعها لاحقاً."
        busy={deleteMutation.isPending}
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() => deleteMutation.mutate()}
      />
    </div>
  )
}
