/** Arabic words, Western digits (0-9): the same digits as unit codes, plates and permit dates elsewhere in the app. */
const SITE_LOCALE = 'ar-EG-u-nu-latn'

/** Keeps a date/time together as one unit so right-to-left text around it cannot reorder its parts. */
const isolate = (text: string) => `\u2068${text}\u2069`

/** Local calendar date as yyyy-MM-dd (toISOString would give the UTC date, wrong after midnight in Egypt). */
export function todayLocal() {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

export function formatDateTime(iso?: string | null) {
  return iso ? isolate(new Date(iso).toLocaleString(SITE_LOCALE, { dateStyle: 'short', timeStyle: 'short' })) : '—'
}

/** UTC bounds of a local calendar day, for the staff log query. */
export function dayBounds(date: string) {
  const from = new Date(`${date}T00:00:00`)
  const to = new Date(from)
  to.setDate(to.getDate() + 1)
  return { from: from.toISOString(), to: to.toISOString() }
}

/** "الجمعة 20/11" from yyyy-MM-dd: the weekday and day/month, which is what a weekly permit needs. */
export function shortDay(date?: string | null) {
  if (!date) return '؟'
  const [, m, d] = date.split('-')
  return `${dayName(date)} ${d}/${m}`
}

export function formatTime(iso?: string | null) {
  return iso ? isolate(new Date(iso).toLocaleString(SITE_LOCALE, { dateStyle: 'short', timeStyle: 'short' })) : ''
}

const SITE_ZONE = 'Africa/Cairo'

/** "الأربعاء ٢٨‏/١٠‏/٢٠٢٦" — weekday and date as seen at the site. */
export function formatDayDate(iso?: string | null) {
  if (!iso) return '—'
  const d = new Date(iso)
  const day = d.toLocaleDateString(SITE_LOCALE, { weekday: 'long', timeZone: SITE_ZONE })
  const date = d.toLocaleDateString(SITE_LOCALE, { dateStyle: 'short', timeZone: SITE_ZONE })
  return `${day} ${isolate(date)}`
}

/** Weekday of a yyyy-MM-dd date, in Arabic. */
export function dayName(date?: string | null) {
  return date ? new Date(`${date}T12:00:00`).toLocaleDateString(SITE_LOCALE, { weekday: 'long' }) : ''
}
