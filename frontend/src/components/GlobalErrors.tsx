import { useEffect, useState } from 'react'
import { LOAD_FAILED_EVENT } from '../api/client'

/** One banner for any list or page that could not load, so a failed load is never mistaken for "no results". */
export function GlobalErrors() {
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    let timer: number | undefined
    function onFailed(e: Event) {
      setMessage((e as CustomEvent<string>).detail)
      window.clearTimeout(timer)
      timer = window.setTimeout(() => setMessage(null), 12_000)
    }
    window.addEventListener(LOAD_FAILED_EVENT, onFailed)
    return () => {
      window.removeEventListener(LOAD_FAILED_EVENT, onFailed)
      window.clearTimeout(timer)
    }
  }, [])

  if (!message) return null
  return (
    <div className="banner error" role="alert">
      <span>{message}</span>{' '}
      <button type="button" className="btn ghost" onClick={() => setMessage(null)}>
        إغلاق
      </button>
    </div>
  )
}
