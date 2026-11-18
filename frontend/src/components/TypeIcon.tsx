import { Icon, type IconName } from './Icon'

type RecordType = 'owner' | 'unit' | 'vehicle' | 'contractor' | 'staff' | 'delivery' | 'reservation' | string

const icons: Record<string, IconName> = {
  owner: 'user',
  unit: 'building',
  vehicle: 'car',
  contractor: 'hardhat',
  staff: 'users',
  delivery: 'key',
  reservation: 'ticket',
}

export function TypeIcon({ type, className = '' }: { type: RecordType; className?: string }) {
  const kind = icons[type] ? type : 'owner'
  return (
    <div className={`result-icon ${kind} ${className}`.trim()} aria-hidden>
      <Icon name={icons[kind]} size={24} />
    </div>
  )
}

export const typeLabels: Record<string, string> = {
  owner: 'مالك',
  unit: 'وحدة',
  vehicle: 'سيارة',
  contractor: 'مقاول',
  staff: 'موظف',
  delivery: 'تسليم',
  reservation: 'حجز',
}
