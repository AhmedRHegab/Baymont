import { useNavigate } from 'react-router-dom'

interface Props {
  title: string
  subtitle?: string
  showBack?: boolean
  backTo?: string
}

export function PageHeader({ title, subtitle, showBack = true, backTo }: Props) {
  const navigate = useNavigate()

  return (
    <div className="page-header">
      <div>
        <h1>{title}</h1>
        {subtitle && <p>{subtitle}</p>}
      </div>
      {showBack && (
        <button
          type="button"
          className="btn secondary"
          onClick={() => (backTo ? navigate(backTo) : navigate(-1))}
        >
          رجوع
        </button>
      )}
    </div>
  )
}
