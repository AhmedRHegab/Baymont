import { type FormEvent, useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { deliveriesApi, unitsApi } from '../api/endpoints'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { ConflictDialog } from '../components/ConflictDialog'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { SearchableSelect } from '../components/SearchableSelect'
import { Toast } from '../components/Toast'
import { useAuth } from '../hooks/useAuth'
import { todayLocal } from '../lib/dates'
import { fetchUnitOptions, unitLabel } from '../lib/lookups'
import { ApiError } from '../types'


export function DeliveryPage() {
  const { id } = useParams()
  const isNew = id === 'new'
  const { user, canDeliver, canEdit } = useAuth()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const ro = !canDeliver

  const { data, refetch, isLoading } = useQuery({
    queryKey: ['delivery', id],
    queryFn: () => deliveriesApi.get(id!),
    enabled: !isNew && !!id,
  })

  const [deliveryDate, setDeliveryDate] = useState(todayLocal())
  const [unitId, setUnitId] = useState('')
  const [unitText, setUnitText] = useState('')
  const [unitNumber, setUnitNumber] = useState('')
  const [customerName, setCustomerName] = useState('')
  const [engineer, setEngineer] = useState(isNew ? (user?.displayName ?? '') : '')
  const [isReceived, setIsReceived] = useState(true)
  const [reason, setReason] = useState('')
  const [isFurnished, setIsFurnished] = useState(false)
  const [comments, setComments] = useState('')
  const [complaints, setComplaints] = useState('')
  const [rowVersion, setRowVersion] = useState(0)
  const [conflict, setConflict] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)
  const clearToast = useCallback(() => setToast(null), [])

  useEffect(() => {
    if (!data) return
    setDeliveryDate(data.deliveryDate)
    setUnitId(data.unitId ?? '')
    setUnitText(data.unitNumber)
    setUnitNumber(data.unitNumber)
    setCustomerName(data.customerName)
    setEngineer(data.handoverEngineerName)
    setIsReceived(data.isReceived)
    setReason(data.notReceivedReason ?? '')
    setIsFurnished(data.isFurnished)
    setComments(data.comments ?? '')
    setComplaints(data.complaints ?? '')
    setRowVersion(data.rowVersion)
  }, [data])

  async function pickUnit(unit: string, label?: string) {
    setUnitId(unit)
    setUnitText(label ?? '')
    if (!unit) return
    if (label) setUnitNumber(label)
    try {
      const u = await unitsApi.get(unit)
      setUnitNumber(unitLabel(u))
      // Only fill the customer when empty, so a typed name is never overwritten.
      setCustomerName((c) => c || u.ownerName || '')
    } catch {
      /* keep what the engineer typed */
    }
  }

  const saveMutation = useMutation({
    mutationFn: () => {
      const body = {
        deliveryDate,
        unitId: unitId || null,
        unitNumber,
        customerName,
        handoverEngineerName: engineer,
        isReceived,
        notReceivedReason: isReceived ? null : reason,
        isFurnished,
        comments,
        complaints,
        rowVersion,
      }
      return isNew ? deliveriesApi.create(body) : deliveriesApi.update(id!, body)
    },
    onSuccess: (saved) => {
      void qc.invalidateQueries({ queryKey: ['delivery', saved.id] })
      void qc.invalidateQueries({ queryKey: ['deliveries'] })
      setToast(isNew ? 'تم حفظ التسليم' : 'تم حفظ التعديلات')
      navigate(`/deliveries/${saved.id}`, { replace: true })
    },
    onError: (err) => {
      if (err instanceof ApiError && err.status === 409) setConflict(true)
      else setError(err instanceof Error ? err.message : 'تعذر الحفظ. حاول مرة أخرى.')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => deliveriesApi.remove(id!),
    onSuccess: () => {
      setConfirmDelete(false)
      void qc.invalidateQueries({ queryKey: ['deliveries'] })
      navigate('/deliveries', { replace: true })
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

  if (!isNew && isLoading) return <LoadingBlock label="جاري فتح بيانات التسليم..." />

  const valid =
    unitNumber.trim() && customerName.trim() && engineer.trim() && (isReceived || reason.trim())

  return (
    <div className="stack">
      <Toast message={toast} onClose={clearToast} />
      <PageHeader
        title={isNew ? 'إضافة تسليم' : 'بيانات التسليم'}
        subtitle={unitNumber ? `وحدة ${unitNumber}` : 'تسليم وحدة لعميل'}
        backTo="/deliveries"
      />

      {ro && <div className="readonly-note">وضع القراءة فقط — يمكنك مشاهدة البيانات بدون تعديل</div>}

      <form className="card form-grid" onSubmit={onSubmit}>
        <label>
          تاريخ التسليم
          <input type="date" value={deliveryDate} onChange={(e) => setDeliveryDate(e.target.value)} required disabled={ro} />
        </label>
        <label>
          اسم مهندس التسليم *
          <input value={engineer} onChange={(e) => setEngineer(e.target.value)} required disabled={ro} />
        </label>

        <SearchableSelect
          label="الوحدة (اختياري للربط)"
          help="ابحث ثم اختر لتعبئة رقم الوحدة واسم المالك تلقائياً"
          value={unitId}
          selectedLabel={unitText}
          disabled={ro}
          placeholder="اختر الوحدة..."
          emptyLabel="بدون ربط"
          queryKey="unit-lookup"
          fetchOptions={fetchUnitOptions}
          onChange={(uid, option) => void pickUnit(uid, option?.label)}
        />
        <label>
          رقم الوحدة *
          <input value={unitNumber} onChange={(e) => setUnitNumber(e.target.value)} required disabled={ro} />
        </label>
        <label>
          اسم العميل *
          <input value={customerName} onChange={(e) => setCustomerName(e.target.value)} required disabled={ro} />
        </label>

        <label className="check">
          <input type="checkbox" checked={isReceived} onChange={(e) => setIsReceived(e.target.checked)} disabled={ro} />
          تم الاستلام
        </label>
        {!isReceived && (
          <label>
            سبب عدم الاستلام *
            <textarea value={reason} onChange={(e) => setReason(e.target.value)} required disabled={ro} rows={2} />
          </label>
        )}
        <label className="check">
          <input type="checkbox" checked={isFurnished} onChange={(e) => setIsFurnished(e.target.checked)} disabled={ro} />
          تم فرش الوحدة
        </label>

        <label>
          تعليقات
          <textarea value={comments} onChange={(e) => setComments(e.target.value)} disabled={ro} rows={3} />
        </label>
        <label>
          شكاوى
          <textarea value={complaints} onChange={(e) => setComplaints(e.target.value)} disabled={ro} rows={3} />
        </label>

        {error && <div className="banner error">{error}</div>}

        {canDeliver && (
          <div className="sticky-actions">
            <button className="btn lg" type="submit" disabled={saveMutation.isPending || !valid}>
              {saveMutation.isPending ? 'جاري الحفظ...' : 'حفظ'}
            </button>
            <button className="btn secondary" type="button" onClick={() => navigate('/deliveries')}>
              إلغاء
            </button>
            {!isNew && canEdit && (
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
        title="حذف التسليم؟"
        message="لن يظهر في القائمة بعد الآن، لكن البيانات تبقى محفوظة ويمكن استرجاعها لاحقاً."
        busy={deleteMutation.isPending}
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() => deleteMutation.mutate()}
      />
    </div>
  )
}
