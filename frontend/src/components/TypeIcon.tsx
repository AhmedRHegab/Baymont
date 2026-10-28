type RecordType = 'owner' | 'unit' | 'vehicle' | string

export function TypeIcon({ type, className = '' }: { type: RecordType; className?: string }) {
  const kind = type === 'owner' || type === 'unit' || type === 'vehicle' || type === 'contractor' ? type : 'owner'

  return (
    <div className={`result-icon ${kind} ${className}`.trim()} aria-hidden>
      {kind === 'owner' && (
        <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="12" cy="8" r="3.5" />
          <path d="M5 19.5c1.8-3.2 4.2-4.8 7-4.8s5.2 1.6 7 4.8" strokeLinecap="round" />
        </svg>
      )}
      {kind === 'unit' && (
        <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M4 20V9.5L12 4l8 5.5V20" strokeLinecap="round" strokeLinejoin="round" />
          <path d="M9 20v-6h6v6" strokeLinecap="round" strokeLinejoin="round" />
          <path d="M9 11h.01M15 11h.01" strokeLinecap="round" />
        </svg>
      )}
      {kind === 'vehicle' && (
        <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2">
          <path
            d="M4 15.5V13c0-.8.4-1.5 1-2l1.4-1.2A3 3 0 0 1 8.3 9h7.4c.7 0 1.4.3 1.9.8L19 11c.6.5 1 1.2 1 2v2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          <path d="M4 15.5h16v2a1 1 0 0 1-1 1h-1.2a2.2 2.2 0 0 1-4.2 0H9.4a2.2 2.2 0 0 1-4.2 0H4a1 1 0 0 1-1-1v-1z" />
          <circle cx="7.3" cy="18" r="1.3" />
          <circle cx="16.7" cy="18" r="1.3" />
        </svg>
      )}
      {kind === 'contractor' && (
        <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M3 18h18M5 18v-3a7 7 0 0 1 14 0v3" strokeLinecap="round" strokeLinejoin="round" />
          <path d="M12 8V5M9.5 6h5" strokeLinecap="round" />
        </svg>
      )}
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
