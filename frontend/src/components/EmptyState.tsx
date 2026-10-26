interface Props {
  icon?: string
  title: string
  description: string
}

export function EmptyState({ icon = '🔍', title, description }: Props) {
  return (
    <div className="empty">
      <div className="empty-icon" aria-hidden>
        {icon}
      </div>
      <h3>{title}</h3>
      <p>{description}</p>
    </div>
  )
}
