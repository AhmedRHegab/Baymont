import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { reservationsApi } from '../api/endpoints'
import { useAuth } from '../hooks/useAuth'
import { dayName, formatDateTime, todayLocal } from '../lib/dates'
import { visitorTypeLabel } from '../lib/lookups'
import type { Reservation } from '../types'

/** Where the visit stands, in one plain phrase. */
export function movementState(r: Reservation, today = todayLocal()) {
  if (r.finalDepartureAt) return { label: 'غادر نهائياً', tone: 'vehicle' }
  const late = r.movements.length > 0 && !!r.checkOutDate && r.checkOutDate < today
  if (r.isInside) return late ? { label: 'داخل الموقع — متأخر عن المغادرة', tone: 'vehicle' } : { label: 'داخل الموقع', tone: 'contractor' }
  if (r.movements.length > 0) return late ? { label: 'خارج مؤقتاً — متأخر عن المغادرة', tone: 'vehicle' } : { label: 'خارج مؤقتاً', tone: 'unit' }
  return { label: 'لم يدخل بعد', tone: 'unit' }
}

export function ReservationCard({ r }: { r: Reservation }) {
  const { canMove } = useAuth()
  const qc = useQueryClient()
  const today = todayLocal()
  const state = movementState(r, today)
  const outside = !!r.checkInDate && !!r.checkOutDate && (today < r.checkInDate || today > r.checkOutDate)
  const last = r.movements[r.movements.length - 1]

  const move = useMutation({
    mutationFn: (kind: 'enter' | 'exit' | 'finalLeave' | 'undo') => reservationsApi[kind](r.id),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['reservations'] })
      void qc.invalidateQueries({ queryKey: ['reservation-dashboard'] })
      void qc.invalidateQueries({ queryKey: ['on-site'] })
    },
    onError: (err) => window.alert(err instanceof Error ? err.message : 'حدث خطأ'),
  })

  function enter() {
    if (outside && !window.confirm('اليوم خارج فترة التصريح. هل تريد تسجيل الدخول رغم ذلك؟')) return
    move.mutate('enter')
  }

  function finalLeave() {
    const ok = window.confirm(
      'مغادرة نهائية؟\nبعد تسجيلها لا يمكنه الدخول مرة أخرى إلا بتصريح جديد يصل بالبريد.',
    )
    if (ok) move.mutate('finalLeave')
  }

  return (
    <div className="sub-item">
      <div className="sub-item-head">
        <Link to={`/reservations/${r.id}`}>
          <strong>{r.guestNames}</strong>
        </Link>
        <span className={`badge ${state.tone}`}>{state.label}</span>
      </div>
      <div className="muted">
        وحدة {r.unitCode} · {visitorTypeLabel(r.type)} · {r.personsCount} شخص
      </div>
      <div className="muted">
        التصريح: {dayName(r.checkInDate)} {r.checkInDate ?? '؟'} ← {dayName(r.checkOutDate)} {r.checkOutDate ?? '؟'}
        {r.periodEstimated ? ' (تقديري)' : ''}
      </div>
      {r.movements.length > 0 && (
        <div className="muted">
          دخل {r.movements.length} {r.movements.length === 1 ? 'مرة' : 'مرات'} · آخر دخول {formatDateTime(last.enteredAt)}
          {last.exitedAt ? ` · آخر خروج ${formatDateTime(last.exitedAt)}` : ''}
        </div>
      )}
      {r.finalDepartureAt && (
        <div className="muted">لا يمكنه الدخول مرة أخرى إلا بتصريح جديد يصل بالبريد.</div>
      )}
      {canMove && r.status === 1 && (
        <div className="actions">
          {!r.finalDepartureAt && !r.isInside && (
            <button type="button" className="btn lg ok" disabled={move.isPending} onClick={enter}>
              تسجيل دخول
            </button>
          )}
          {r.isInside && (
            <button type="button" className="btn lg" disabled={move.isPending} onClick={() => move.mutate('exit')}>
              تسجيل خروج
            </button>
          )}
          {!r.finalDepartureAt && r.movements.length > 0 && (
            <button type="button" className="btn danger" disabled={move.isPending} onClick={finalLeave}>
              مغادرة نهائية
            </button>
          )}
          {r.movements.length > 0 && (
            <button
              type="button"
              className="btn ghost"
              disabled={move.isPending}
              onClick={() => window.confirm('التراجع عن آخر تسجيل؟') && move.mutate('undo')}
            >
              تراجع
            </button>
          )}
        </div>
      )}
    </div>
  )
}
