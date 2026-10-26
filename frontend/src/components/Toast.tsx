import { useEffect } from 'react'

interface Props {
  message: string | null
  onClose: () => void
}

export function Toast({ message, onClose }: Props) {
  useEffect(() => {
    if (!message) return
    const t = window.setTimeout(onClose, 2800)
    return () => window.clearTimeout(t)
  }, [message, onClose])

  if (!message) return null

  return (
    <div className="toast" role="status">
      <div className="toast-inner">✓ {message}</div>
    </div>
  )
}
