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
