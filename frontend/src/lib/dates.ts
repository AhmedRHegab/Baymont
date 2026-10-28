/** Local calendar date as yyyy-MM-dd (toISOString would give the UTC date, wrong after midnight in Egypt). */
export function todayLocal() {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

export function formatDateTime(iso?: string | null) {
  return iso ? new Date(iso).toLocaleString('ar-EG', { dateStyle: 'short', timeStyle: 'short' }) : '—'
}

/** UTC bounds of a local calendar day, for the staff log query. */
export function dayBounds(date: string) {
  const from = new Date(`${date}T00:00:00`)
  const to = new Date(from)
  to.setDate(to.getDate() + 1)
  return { from: from.toISOString(), to: to.toISOString() }
}

export function formatTime(iso?: string | null) {
  return iso ? new Date(iso).toLocaleString('ar-EG', { dateStyle: 'short', timeStyle: 'short' }) : ''
}

const SITE_ZONE = 'Africa/Cairo'

/** "الأربعاء ٢٨‏/١٠‏/٢٠٢٦" — weekday and date as seen at the site. */
export function formatDayDate(iso?: string | null) {
  if (!iso) return '—'
  const d = new Date(iso)
  const day = d.toLocaleDateString('ar-EG', { weekday: 'long', timeZone: SITE_ZONE })
  const date = d.toLocaleDateString('ar-EG', { dateStyle: 'short', timeZone: SITE_ZONE })
  return `${day} ${date}`
}

/** Weekday of a yyyy-MM-dd date, in Arabic. */
export function dayName(date?: string | null) {
  return date ? new Date(`${date}T12:00:00`).toLocaleDateString('ar-EG', { weekday: 'long' }) : ''
}
