import { type FormEvent, useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ownersApi } from '../api/endpoints'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { ConflictDialog } from '../components/ConflictDialog'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { Toast } from '../components/Toast'
import { useAuth } from '../hooks/useAuth'
import { ApiError } from '../types'

export function OwnerPage() {
  const { id } = useParams()
  const isNew = id === 'new'
  const { canEdit } = useAuth()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const { data, refetch, isLoading } = useQuery({
    queryKey: ['owner', id],
    queryFn: () => ownersApi.get(id!),
    enabled: !isNew && !!id,
  })

  const [fullName, setFullName] = useState('')
  const [internalCode, setInternalCode] = useState('')
  const [phone, setPhone] = useState('')
  const [notes, setNotes] = useState('')
  const [rowVersion, setRowVersion] = useState(0)
  const [conflict, setConflict] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)

  const clearToast = useCallback(() => setToast(null), [])

  useEffect(() => {
    if (!data) return
    setFullName(data.fullName)
    setInternalCode(data.internalCode)
    setPhone(data.phone ?? '')
    setNotes(data.notes ?? '')
    setRowVersion(data.rowVersion)
  }, [data])

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (isNew) {
        return ownersApi.create({ fullName, internalCode, phone, notes })
      }
      return ownersApi.update(id!, {
        fullName,
        internalCode,
        phone,
        notes,
        rowVersion,
      })
    },
    onSuccess: (saved) => {
      void qc.invalidateQueries({ queryKey: ['owner', saved.id] })
      setToast(isNew ? 'تم حفظ المالك بنجاح' : 'تم حفظ التعديلات')
      navigate(`/owners/${saved.id}`, { replace: true })
    },
    onError: (err) => {
      if (err instanceof ApiError && err.status === 409) setConflict(true)
      else setError(err instanceof Error ? err.message : 'تعذر الحفظ. حاول مرة أخرى.')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => ownersApi.remove(id!),
    onSuccess: () => {
      setConfirmDelete(false)
      setToast('تم إخفاء المالك (يمكن استرجاعه لاحقاً من قاعدة البيانات)')
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

  if (!isNew && isLoading) return <LoadingBlock label="جاري فتح بيانات المالك..." />

  return (
    <div className="stack">
      <Toast message={toast} onClose={clearToast} />
      <PageHeader
        title={isNew ? 'إضافة مالك' : 'بيانات المالك'}
        subtitle={isNew ? 'املأ الاسم ثم اضغط حفظ' : fullName || 'عرض وتعديل بيانات المالك'}
        backTo="/"
      />

      {!canEdit && !isNew && (
        <div className="readonly-note">وضع القراءة فقط — يمكنك مشاهدة البيانات بدون تعديل</div>
      )}

      <form className="card form-grid" onSubmit={onSubmit}>
        <label>
          الاسم الكامل *
          <span className="field-help">اكتب اسم المالك كما هو معروف عند الأمن</span>
          <input maxLength={256}
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            required
            disabled={!canEdit && !isNew}
            placeholder="مثال: أحمد محمد"
          />
        </label>
        <label>
          رقم داخلي
          <span className="field-help">اختياري — إن لم تكتبه سيتم توليده تلقائياً</span>
          <input maxLength={64}
            value={internalCode}
            onChange={(e) => setInternalCode(e.target.value)}
            disabled={!canEdit && !isNew}
            placeholder="OWN-001"
          />
        </label>
        <label>
          رقم الهاتف
          <input maxLength={64} pattern="[0-9٠-٩+ ,.،\/\(\)\-]*" title="اكتب أرقاماً فقط (يمكن استخدام + و - والمسافة)."
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            disabled={!canEdit && !isNew}
            placeholder="01xxxxxxxxx"
            inputMode="tel"
          />
        </label>
        <label>
          ملاحظات
          <textarea maxLength={2000}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            disabled={!canEdit && !isNew}
            placeholder="أي معلومة مفيدة للأمن..."
            rows={3}
          />
        </label>

        {error && <div className="banner error">{error}</div>}

        {canEdit && (
          <div className="sticky-actions">
            <button className="btn lg" type="submit" disabled={saveMutation.isPending || !fullName.trim()}>
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
        <>
          <section className="card">
            <div className="section-title">
              <h2>الوحدات التابعة</h2>
              {canEdit && (
                <Link className="btn ghost" to="/units/new">
                  إضافة وحدة
                </Link>
              )}
            </div>
            {data.units.length === 0 ? (
              <p className="muted">لا توجد وحدات مرتبطة بهذا المالك.</p>
            ) : (
              <ul className="linked-list">
                {data.units.map((u) => (
                  <li key={u.id}>
                    <Link to={`/units/${u.id}`}>
                      <span>{u.constructionNumber ?? `${u.terrace}/${u.building}/${u.apartment}`}</span>
                      <span className="badge unit">وحدة</span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="card">
            <div className="section-title">
              <h2>السيارات التابعة</h2>
              {canEdit && (
                <Link className="btn ghost" to="/vehicles/new">
                  إضافة سيارة
                </Link>
              )}
            </div>
            {data.vehicles.length === 0 ? (
              <p className="muted">لا توجد سيارات مرتبطة بهذا المالك.</p>
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
        </>
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
        title="حذف المالك؟"
        message="لن يظهر في البحث بعد الآن، لكن البيانات تبقى محفوظة ويمكن استرجاعها لاحقاً."
        busy={deleteMutation.isPending}
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() => deleteMutation.mutate()}
      />
    </div>
  )
}
