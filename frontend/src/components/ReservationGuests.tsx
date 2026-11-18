import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { reservationsApi, uploadsApi } from '../api/endpoints'
import { useAuth } from '../hooks/useAuth'
import { IdCapture, type IdValue } from './IdCapture'
import type { Reservation } from '../types'

/** ID cards of the people on an approved permit. Optional: the permit works without any. */
export function ReservationGuests({ reservation }: { reservation: Reservation }) {
  const { canEdit, canMove } = useAuth()
  const qc = useQueryClient()
  const [adding, setAdding] = useState(false)
  const [name, setName] = useState('')
  const [id, setId] = useState<IdValue>({ nationalId: '', photoPath: null })
  const [error, setError] = useState<string | null>(null)

  function refresh() {
    void qc.invalidateQueries({ queryKey: ['reservation', reservation.id] })
    void qc.invalidateQueries({ queryKey: ['reservations'] })
  }

  const add = useMutation({
    mutationFn: () => reservationsApi.addGuest(reservation.id, { name, nationalId: id.nationalId, idPhotoPath: id.photoPath }),
    onSuccess: () => {
      refresh()
      setAdding(false)
      setName('')
      setId({ nationalId: '', photoPath: null })
      setError(null)
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'تعذر الحفظ'),
  })

  const remove = useMutation({
    mutationFn: (guestId: string) => reservationsApi.removeGuest(reservation.id, guestId),
    onSuccess: refresh,
    onError: (err) => setError(err instanceof Error ? err.message : 'تعذر الحذف'),
  })

  return (
    <section className="card stack">
      <div className="section-title">
        <h2>
          هويات الأشخاص ({reservation.guests.length} من {reservation.personsCount})
        </h2>
        {canMove && !adding && (
          <button type="button" className="btn secondary" onClick={() => setAdding(true)}>
            ＋ إضافة هوية
          </button>
        )}
      </div>

      {reservation.guests.length === 0 && !adding && <div className="muted">لم تُسجل أي هوية بعد. تسجيل الهوية اختياري.</div>}

      {reservation.guests.map((g) => (
        <div className="sub-item" key={g.id}>
          <div className="sub-item-head">
            <strong>{g.name}</strong>
            {canEdit && (
              <button
                type="button"
                className="btn ghost"
                disabled={remove.isPending}
                onClick={() => window.confirm(`حذف هوية ${g.name}؟`) && remove.mutate(g.id)}
              >
                حذف
              </button>
            )}
          </div>
          <span className="muted" dir="ltr">
            {g.nationalId}
          </span>
          <span className="muted">
            {[g.birthDate && `مواليد ${g.birthDate}`, g.gender === 1 ? 'ذكر' : g.gender === 2 ? 'أنثى' : null, g.governorate]
              .filter(Boolean)
              .join(' · ')}
          </span>
          {g.hasPhoto &&
            (g.idPhotoPath ? (
              <a href={uploadsApi.url(g.idPhotoPath)} target="_blank" rel="noreferrer">
                <img className="photo-thumb" src={uploadsApi.url(g.idPhotoPath)} alt="صورة البطاقة" />
              </a>
            ) : (
              <span className="muted">صورة البطاقة محفوظة</span>
            ))}
        </div>
      ))}

      {adding && (
        <div className="form-grid sub-item">
          <IdCapture
            value={id}
            onChange={setId}
            canViewPhoto={canEdit}
          />
          <label>
            الاسم كما في البطاقة *
            <input value={name} onChange={(e) => setName(e.target.value)} maxLength={256} />
          </label>
          {error && <div className="banner error">{error}</div>}
          <div className="actions">
            <button
              type="button"
              className="btn lg"
              disabled={add.isPending || !name.trim() || id.nationalId.length !== 14}
              onClick={() => add.mutate()}
            >
              {add.isPending ? 'جاري الحفظ...' : 'حفظ الهوية'}
            </button>
            <button
              type="button"
              className="btn secondary"
              onClick={() => {
                setAdding(false)
                setError(null)
              }}
            >
              إلغاء
            </button>
          </div>
        </div>
      )}
      {!adding && error && <div className="banner error">{error}</div>}
    </section>
  )
}
