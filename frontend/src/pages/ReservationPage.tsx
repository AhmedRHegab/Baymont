import { type FormEvent, useEffect, useState } from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { reservationsApi } from '../api/endpoints'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { useAuth } from '../hooks/useAuth'
import { dayName, formatDateTime, formatDayDate } from '../lib/dates'
import { visitorTypes } from '../lib/lookups'
import type { VisitorType } from '../types'

export function ReservationPage() {
  const { id } = useParams()
  const isNew = id === 'new'
  const { canEdit, isAdmin } = useAuth()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const ro = !canEdit

  const { data, isLoading } = useQuery({
    queryKey: ['reservation', id],
    queryFn: () => reservationsApi.get(id!),
    enabled: !isNew && !!id,
  })

  const [unitCode, setUnitCode] = useState('')
  const [guestNames, setGuestNames] = useState('')
  const [persons, setPersons] = useState(1)
  const [type, setType] = useState<VisitorType | ''>('')
  const [checkIn, setCheckIn] = useState('')
  const [checkOut, setCheckOut] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [confirmDelete, setConfirmDelete] = useState(false)

  useEffect(() => {
    if (!data) return
    setUnitCode(data.unitCode)
    setGuestNames(data.guestNames)
    setPersons(data.personsCount)
    setType(data.type ?? '')
    setCheckIn(data.checkInDate ?? '')
    setCheckOut(data.checkOutDate ?? '')
    setNotes(data.notes ?? '')
  }, [data])

  function refresh() {
    void qc.invalidateQueries({ queryKey: ['reservations'] })
    void qc.invalidateQueries({ queryKey: ['reservations-pending'] })
    void qc.invalidateQueries({ queryKey: ['reservation-dashboard'] })
    void qc.invalidateQueries({ queryKey: ['reservation', id] })
  }

  const save = useMutation({
    mutationFn: (approve: boolean) => {
      const body = {
        unitCode,
        guestNames,
        personsCount: persons,
        type: type || null,
        checkInDate: checkIn || null,
        checkOutDate: checkOut || null,
        notes,
        approve,
      }
      return isNew ? reservationsApi.create(body) : reservationsApi.update(id!, body)
    },
    onSuccess: (saved) => {
      refresh()
      navigate(saved.status === 1 ? '/reservations' : `/reservations/${saved.id}`, { replace: true })
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'تعذر الحفظ'),
  })
  const reject = useMutation({
    mutationFn: () => reservationsApi.reject(id!),
    onSuccess: () => {
      refresh()
      navigate('/reservations', { replace: true })
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'تعذر الرفض'),
  })
  const remove = useMutation({
    mutationFn: () => reservationsApi.remove(id!),
    onSuccess: () => {
      refresh()
      navigate('/reservations', { replace: true })
    },
    onError: (err) => {
      setConfirmDelete(false)
      setError(err instanceof Error ? err.message : 'تعذر الحذف')
    },
  })

  // Permits arrive by email; adding one by hand is an admin-only fallback.
  if (isNew && !isAdmin) return <Navigate to="/reservations" replace />
  if (!isNew && isLoading) return <LoadingBlock label="جاري فتح الحجز..." />

  const pending = isNew || data?.status === 0
  const approved = data?.status === 1
  const busy = save.isPending || reject.isPending
  const filled = unitCode.trim() && guestNames.trim()

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    save.mutate(approved)
  }

  return (
    <div className="stack">
      <PageHeader
        title={isNew ? 'حجز جديد' : pending ? 'مراجعة الحجز' : 'بيانات الحجز'}
        subtitle={pending && !isNew ? 'راجع البيانات ثم اعتمد أو ارفض' : undefined}
        backTo="/reservations"
      />

      {!isNew && data && (
        <div className="banner info">
          المصدر: {data.source === 'Email' ? 'بريد إلكتروني' : data.source === 'Upload' ? 'ملف مرفوع' : 'إدخال يدوي'}
          {data.sourceFrom ? ` · ${data.sourceFrom}` : ''}
          {data.sourceFileName ? ` · ${data.sourceFileName}` : ''}
          {data.sourceSubject ? ` · ${data.sourceSubject}` : ''}
          {' · '}
          {data.unitId ? (
            <Link to={`/units/${data.unitId}`}>الوحدة موجودة بالنظام ✓</Link>
          ) : (
            <strong>⚠ كود الوحدة غير مطابق لأي وحدة بالنظام</strong>
          )}
        </div>
      )}
      {data?.sourceSentAt && (
        <div className="muted">أُرسل البريد: {formatDayDate(data.sourceSentAt)}</div>
      )}
      {data?.periodEstimated && (
        <div className="banner error">
          <strong>⚠ فترة التصريح تقديرية.</strong> الملف لا يحتوي تواريخ، فتم اعتبارها من أقرب جمعة إلى الجمعة التي تليها حسب يوم إرسال البريد.
          راجع التاريخين قبل الاعتماد.
        </div>
      )}
      {data && data.movements.length > 0 && (
        <section className="card stack">
          <div className="section-title">
            <h2>سجل الدخول والخروج ({data.movements.length})</h2>
          </div>
          {data.movements.map((m, i) => (
            <div className="sub-item" key={m.id}>
              <strong>المرة {i + 1}</strong>
              <span className="muted">
                دخول {formatDateTime(m.enteredAt)} · {m.exitedAt ? `خروج ${formatDateTime(m.exitedAt)}${m.isFinal ? ' (مغادرة نهائية)' : ''}` : 'لا يزال بالداخل'}
              </span>
            </div>
          ))}
          {data.finalDepartureAt && (
            <div className="banner info">
              سُجلت المغادرة النهائية {formatDateTime(data.finalDepartureAt)}. لا يمكنه الدخول مرة أخرى إلا بتصريح جديد يصل بالبريد.
            </div>
          )}
        </section>
      )}

      <form className="card form-grid" onSubmit={onSubmit}>
        <label>
          كود الوحدة *
          <input value={unitCode} onChange={(e) => setUnitCode(e.target.value)} required disabled={ro} dir="ltr" />
        </label>
        <label>
          اسم الضيف / الضيوف *
          <span className="field-help">إذا كان أكثر من شخص افصل بينهم بعلامة /</span>
          <textarea value={guestNames} onChange={(e) => setGuestNames(e.target.value)} required disabled={ro} rows={2} dir="ltr" />
        </label>
        <label>
          عدد الأشخاص
          <input type="number" min={1} max={100} value={persons} onChange={(e) => setPersons(Number(e.target.value))} disabled={ro} />
        </label>
        <label>
          نوع الزيارة {pending && '*'}
          <select value={type} onChange={(e) => setType(e.target.value ? (Number(e.target.value) as VisitorType) : '')} disabled={ro}>
            <option value="">— اختر —</option>
            {visitorTypes.map((t) => (
              <option key={t.value} value={t.value}>
                {t.label}
              </option>
            ))}
          </select>
        </label>
        <label>
          تاريخ بداية التصريح {pending && '*'}{checkIn ? ` — ${dayName(checkIn)}` : ''}
          <input type="date" value={checkIn} onChange={(e) => setCheckIn(e.target.value)} disabled={ro} />
        </label>
        <label>
          تاريخ نهاية التصريح {pending && '*'}{checkOut ? ` — ${dayName(checkOut)}` : ''}
          <input type="date" value={checkOut} onChange={(e) => setCheckOut(e.target.value)} disabled={ro} />
        </label>
        <label>
          ملاحظات
          <textarea value={notes} onChange={(e) => setNotes(e.target.value)} disabled={ro} rows={2} />
        </label>

        {error && <div className="banner error">{error}</div>}

        {canEdit && (
          <div className="sticky-actions">
            {pending ? (
              <>
                <button className="btn lg ok" type="button" disabled={busy || !filled} onClick={() => save.mutate(true)}>
                  اعتماد
                </button>
                <button className="btn secondary" type="submit" disabled={busy || !filled}>
                  حفظ دون اعتماد
                </button>
                {!isNew && (
                  <button className="btn danger" type="button" disabled={busy} onClick={() => window.confirm('رفض هذا الحجز؟') && reject.mutate()}>
                    رفض
                  </button>
                )}
              </>
            ) : (
              <button className="btn lg" type="submit" disabled={busy || !filled}>
                {data?.status === 2 ? 'اعتماد بعد التعديل' : 'حفظ التعديلات'}
              </button>
            )}
            {!isNew && (
              <button className="btn ghost" type="button" onClick={() => setConfirmDelete(true)}>
                حذف
              </button>
            )}
          </div>
        )}
      </form>

      <ConfirmDialog
        open={confirmDelete}
        title="حذف الحجز؟"
        message="سيُحذف الحجز نهائياً مع تسجيل الوصول والمغادرة."
        busy={remove.isPending}
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() => remove.mutate()}
      />
    </div>
  )
}
