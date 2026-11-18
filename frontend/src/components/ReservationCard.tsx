import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { reservationsApi } from '../api/endpoints'
import { useAuth } from '../hooks/useAuth'
import { formatDateTime, shortDay, todayLocal } from '../lib/dates'
import { visitorTypeLabel } from '../lib/lookups'
import { Icon } from './Icon'
import type { Reservation } from '../types'

/** Where the visit stands, in one plain phrase. */
export function movementState(r: Reservation, today = todayLocal()) {
  if (r.finalDepartureAt) return { label: 'غادر نهائياً', tone: 'muted' }
  const late = r.movements.length > 0 && !!r.checkOutDate && r.checkOutDate < today
  if (r.isInside) return late ? { label: 'متأخر وداخل الموقع', tone: 'danger' } : { label: 'داخل الموقع', tone: 'ok' }
  if (r.movements.length > 0) return late ? { label: 'متأخر وخارج مؤقتاً', tone: 'danger' } : { label: 'خارج مؤقتاً', tone: 'warn' }
  return { label: 'لم يدخل بعد', tone: 'info' }
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
        <Link to={`/reservations/${r.id}`}>{r.guestNames}</Link>
        <span className={`badge ${state.tone}`}>{state.label}</span>
      </div>
      <div className="chips">
        <span className="chip code">{r.unitCode}</span>
        <span className="chip">{visitorTypeLabel(r.type)}</span>
        <span className="chip">
          <Icon name="users" size={14} />
          {r.personsCount}
        </span>
      </div>
      <div className="date-range">
        <bdi>{shortDay(r.checkInDate)}</bdi>
        <Icon name="back" size={16} />
        <bdi>{shortDay(r.checkOutDate)}</bdi>
        {r.periodEstimated && <span className="badge warn">تقديري</span>}
      </div>
      {r.status === 1 && r.guests.length > 0 && (
        <div className="muted">
          هويات مسجلة: {r.guests.length} من {r.personsCount}
        </div>
      )}
      {r.movements.length > 0 && (
        <div className="muted">
          دخل {r.movements.length} {r.movements.length === 1 ? 'مرة' : 'مرات'} · آخر دخول <bdi>{formatDateTime(last.enteredAt)}</bdi>
          {last.exitedAt && (
            <>
              {' · آخر خروج '}
              <bdi>{formatDateTime(last.exitedAt)}</bdi>
            </>
          )}
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
