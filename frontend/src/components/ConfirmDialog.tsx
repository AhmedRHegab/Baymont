interface Props {
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  busyLabel?: string
  /** Red for destructive actions (the default), normal for the rest. */
  danger?: boolean
  busy?: boolean
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'حذف',
  busyLabel = 'جاري الحذف...',
  danger = true,
  busy,
  onConfirm,
  onCancel,
}: Props) {
  if (!open) return null
  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true">
      <div className="card modal">
        <h2>{title}</h2>
        <p className="muted">{message}</p>
        <div className="actions">
          <button className={`btn ${danger ? 'danger ' : ''}lg`} type="button" onClick={onConfirm} disabled={busy}>
            {busy ? busyLabel : confirmLabel}
          </button>
          <button className="btn secondary" type="button" onClick={onCancel} disabled={busy}>
            إلغاء
          </button>
        </div>
      </div>
    </div>
  )
}
