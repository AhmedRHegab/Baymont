import { useNavigate } from 'react-router-dom'
import { Icon } from './Icon'

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
      {showBack && (
        <button
          type="button"
          className="icon-btn"
          aria-label="رجوع"
          onClick={() => (backTo ? navigate(backTo) : navigate(-1))}
        >
          <Icon name="back" size={22} />
        </button>
      )}
      <div>
        <h1>{title}</h1>
        {subtitle && <p>{subtitle}</p>}
      </div>
    </div>
  )
}
