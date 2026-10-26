interface Props {
  open: boolean
  onReload: () => void
  onClose: () => void
}

export function ConflictDialog({ open, onReload, onClose }: Props) {
  if (!open) return null
  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true">
      <div className="card modal">
        <h2>البيانات تغيّرت</h2>
        <p className="muted">
          زميل آخر عدّل نفس السجل أثناء عملك. اضغط «تحديث الصفحة» لرؤية آخر نسخة، ثم عدّل مرة أخرى.
        </p>
        <div className="actions">
          <button className="btn lg" type="button" onClick={onReload}>
            تحديث الصفحة
          </button>
          <button className="btn secondary" type="button" onClick={onClose}>
            إلغاء
          </button>
        </div>
      </div>
    </div>
  )
}
