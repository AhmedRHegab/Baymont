export function LoadingBlock({ label = 'جاري التحميل...' }: { label?: string }) {
  return (
    <div className="card loading-block">
      <div className="spinner" aria-hidden />
      <div>{label}</div>
    </div>
  )
}
