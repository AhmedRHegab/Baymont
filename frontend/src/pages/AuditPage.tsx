import { useQuery } from '@tanstack/react-query'
import { auditApi } from '../api/endpoints'
import { EmptyState } from '../components/EmptyState'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { formatDateTime } from '../lib/dates'

const actionLabel: Record<string, string> = {
  Create: 'إضافة',
  Update: 'تعديل',
  Delete: 'حذف',
  Import: 'استيراد',
}

const entityLabel: Record<string, string> = {
  Owner: 'مالك',
  Unit: 'وحدة',
  Vehicle: 'سيارة',
  Contractor: 'مقاول',
  ContractorWorker: 'عامل مقاول',
  ContractorVehicle: 'سيارة مقاول',
  ContractorEquipment: 'معدة مقاول',
  ContractorMaterial: 'خامة مقاول',
  ContractorEntry: 'دخول/خروج مقاول',
  Delivery: 'تسليم',
  Reservation: 'حجز',
  OvernightStay: 'مبيت مالك',
  StaffMember: 'موظف',
  StaffEntry: 'دخول/خروج موظف',
  ShiftAssignment: 'وردية',
}

export function AuditPage() {
  const { data, isLoading } = useQuery({
    queryKey: ['audit'],
    queryFn: () => auditApi.list({ take: '100' }),
  })

  return (
    <div className="stack">
      <PageHeader
        title="سجل العمليات"
        subtitle="من عدّل ماذا ومتى — للمراجعة فقط"
        backTo="/"
      />

      <section className="card">
        {isLoading ? (
          <LoadingBlock label="جاري تحميل السجل..." />
        ) : !data?.length ? (
          <EmptyState
            icon="📋"
            title="لا توجد عمليات بعد"
            description="ستظهر هنا أي إضافة أو تعديل على الملاك والوحدات والسيارات."
          />
        ) : (
          <>
          <div className="stack only-mobile">
            {data.map((row) => (
              <div className="sub-item" key={row.id}>
                <div className="sub-item-head">
                  <span>
                    {actionLabel[row.action] ?? row.action} · {entityLabel[row.entityType] ?? row.entityType}
                  </span>
                  <span className="chip">{row.userName || '—'}</span>
                </div>
                <div className="muted">{formatDateTime(row.occurredAt)}</div>
              </div>
            ))}
          </div>
          <div className="table-wrap only-desktop">
            <table className="table">
              <thead>
                <tr>
                  <th>الوقت</th>
                  <th>العملية</th>
                  <th>النوع</th>
                  <th>المعرف</th>
                  <th>المستخدم</th>
                </tr>
              </thead>
              <tbody>
                {data.map((row) => (
                  <tr key={row.id}>
                    <td>{formatDateTime(row.occurredAt)}</td>
                    <td>{actionLabel[row.action] ?? row.action}</td>
                    <td>{entityLabel[row.entityType] ?? row.entityType}</td>
                    <td style={{ fontSize: '0.9rem' }}>{row.entityId.slice(0, 8)}…</td>
                    <td>{row.userName || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          </>
        )}
      </section>
    </div>
  )
}
