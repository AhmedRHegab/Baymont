import { api } from './client'
import type {
  InboxStatus,
  Reservation,
  ReservationDashboard,
  ReservationImportResult,
  OnSite,
  OvernightReport,
  ShiftAssignment,
  StaffDto,
  StaffLog,
  AuditLogDto,
  ContractorDetail,
  ContractorSummary,
  Delivery,
  DeliveryList,
  OwnerDetail,
  OwnerSummary,
  SearchResponse,
  UnitDetail,
  UnitSummary,
  UserDto,
  VehicleDetail,
} from '../types'

export const authApi = {
  login: (userName: string, password: string) =>
    api<UserDto>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ userName, password }),
    }),
  logout: () => api<{ message: string }>('/api/auth/logout', { method: 'POST' }),
  me: () => api<UserDto>('/api/auth/me'),
}

export const searchApi = {
  search: (q: string) => api<SearchResponse>(`/api/search?q=${encodeURIComponent(q)}`),
}

export const ownersApi = {
  lookup: (q = '') =>
    api<OwnerSummary[]>(`/api/owners?q=${encodeURIComponent(q)}`),
  get: (id: string) => api<OwnerDetail>(`/api/owners/${id}`),
  create: (body: object) =>
    api<OwnerDetail>('/api/owners', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: object) =>
    api<OwnerDetail>(`/api/owners/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  remove: (id: string) => api<void>(`/api/owners/${id}`, { method: 'DELETE' }),
}

export const unitsApi = {
  lookup: (q = '') =>
    api<UnitSummary[]>(`/api/units?q=${encodeURIComponent(q)}`),
  get: (id: string) => api<UnitDetail>(`/api/units/${id}`),
  create: (body: object) =>
    api<UnitDetail>('/api/units', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: object) =>
    api<UnitDetail>(`/api/units/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  remove: (id: string) => api<void>(`/api/units/${id}`, { method: 'DELETE' }),
}

export const vehiclesApi = {
  get: (id: string) => api<VehicleDetail>(`/api/vehicles/${id}`),
  create: (body: object) =>
    api<VehicleDetail>('/api/vehicles', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: object) =>
    api<VehicleDetail>(`/api/vehicles/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  remove: (id: string) => api<void>(`/api/vehicles/${id}`, { method: 'DELETE' }),
}

export const contractorsApi = {
  list: (q = '') =>
    api<ContractorSummary[]>(`/api/contractors?q=${encodeURIComponent(q)}`),
  get: (id: string) => api<ContractorDetail>(`/api/contractors/${id}`),
  create: (body: object) =>
    api<ContractorDetail>('/api/contractors', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: object) =>
    api<ContractorDetail>(`/api/contractors/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  remove: (id: string) => api<void>(`/api/contractors/${id}`, { method: 'DELETE' }),
}

export const deliveriesApi = {
  list: (params: Record<string, string> = {}) => {
    const qs = new URLSearchParams(Object.entries(params).filter(([, v]) => v !== ''))
    return api<DeliveryList>(`/api/deliveries?${qs}`)
  },
  get: (id: string) => api<Delivery>(`/api/deliveries/${id}`),
  create: (body: object) =>
    api<Delivery>('/api/deliveries', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: object) =>
    api<Delivery>(`/api/deliveries/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  remove: (id: string) => api<void>(`/api/deliveries/${id}`, { method: 'DELETE' }),
}

export const siteApi = {
  overnight: (date: string, shift: number) =>
    api<OvernightReport>(`/api/overnight-stays?date=${date}&shift=${shift}`),
  addOvernight: (body: object) =>
    api<{ id: string }>('/api/overnight-stays', { method: 'POST', body: JSON.stringify(body) }),
  removeOvernight: (id: string) => api<void>(`/api/overnight-stays/${id}`, { method: 'DELETE' }),

  staff: (q = '') => api<StaffDto[]>(`/api/staff?q=${encodeURIComponent(q)}`),
  createStaff: (body: object) =>
    api<{ id: string }>('/api/staff', { method: 'POST', body: JSON.stringify(body) }),
  updateStaff: (id: string, body: object) =>
    api<void>(`/api/staff/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  removeStaff: (id: string) => api<void>(`/api/staff/${id}`, { method: 'DELETE' }),
  enter: (id: string) => api<void>(`/api/staff/${id}/enter`, { method: 'POST' }),
  exit: (id: string) => api<void>(`/api/staff/${id}/exit`, { method: 'POST' }),
  staffLog: (from: string, to: string) =>
    api<StaffLog[]>(`/api/staff/log?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
  onSite: () => api<OnSite>('/api/on-site'),

  shifts: (date: string) => api<ShiftAssignment[]>(`/api/shifts?date=${date}`),
  createShift: (body: object) =>
    api<{ id: string }>('/api/shifts', { method: 'POST', body: JSON.stringify(body) }),
  updateShift: (id: string, body: object) =>
    api<void>(`/api/shifts/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  removeShift: (id: string) => api<void>(`/api/shifts/${id}`, { method: 'DELETE' }),
}

export const reservationsApi = {
  list: (status: number, q = '') =>
    api<Reservation[]>(`/api/reservations?status=${status}&q=${encodeURIComponent(q)}`),
  get: (id: string) => api<Reservation>(`/api/reservations/${id}`),
  create: (body: object) =>
    api<Reservation>('/api/reservations', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: object) =>
    api<Reservation>(`/api/reservations/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  reject: (id: string) => api<void>(`/api/reservations/${id}/reject`, { method: 'POST' }),
  remove: (id: string) => api<void>(`/api/reservations/${id}`, { method: 'DELETE' }),
  enter: (id: string) => api<void>(`/api/reservations/${id}/enter`, { method: 'POST' }),
  exit: (id: string) => api<void>(`/api/reservations/${id}/exit`, { method: 'POST' }),
  finalLeave: (id: string) => api<void>(`/api/reservations/${id}/final-leave`, { method: 'POST' }),
  undo: (id: string) => api<void>(`/api/reservations/${id}/undo`, { method: 'POST' }),
  dashboard: (today: string, start?: string) =>
    api<ReservationDashboard>(
      `/api/reservations/dashboard?today=${today}${start ? `&start=${start}` : ''}`,
    ),
  pendingCount: () => api<{ count: number }>('/api/reservations/pending-count'),
  import: async (file: File): Promise<ReservationImportResult> => {
    const form = new FormData()
    form.append('file', file)
    const res = await fetch('/api/reservations/import', { method: 'POST', credentials: 'include', body: form })
    const body = await res.json().catch(() => ({}))
    if (!res.ok) throw new Error((body as { message?: string }).message ?? 'فشل رفع الملف')
    return body as ReservationImportResult
  },
}

export const inboxApi = {
  get: () => api<InboxStatus>('/api/inbox'),
  check: () => api<void>('/api/inbox/check', { method: 'POST' }),
}

export const demoApi = {
  status: () => api<{ hasDemoData: boolean; records: number }>('/api/demo'),
  load: () => api<{ created: number }>('/api/demo/load', { method: 'POST' }),
  clear: () => api<{ deleted: number }>('/api/demo/clear', { method: 'POST' }),
}

export const uploadsApi = {
  url: (path: string) => `/api/uploads/${path}`,
  upload: async (file: File) => {
    const form = new FormData()
    form.append('file', file)
    const res = await fetch('/api/uploads', { method: 'POST', credentials: 'include', body: form })
    const body = await res.json().catch(() => ({}))
    if (!res.ok) throw new Error(body.message ?? 'فشل رفع الملف')
    return (body as { path: string }).path
  },
}

export const usersApi = {
  list: () => api<UserDto[]>('/api/users'),
  create: (body: object) =>
    api<UserDto>('/api/users', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: string, body: object) =>
    api<UserDto>(`/api/users/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
}

export const auditApi = {
  list: (params?: Record<string, string>) => {
    const qs = params ? `?${new URLSearchParams(params)}` : ''
    return api<AuditLogDto[]>(`/api/audit-logs${qs}`)
  },
}

export interface ImportResult {
  totalRows: number
  newOwners: number
  newUnits: number
  skippedExisting: number
  errors: { row: number; message: string }[]
  committed: boolean
}

async function postImport(path: string, file: File): Promise<ImportResult> {
  const form = new FormData()
  form.append('file', file)
  const res = await fetch(path, { method: 'POST', credentials: 'include', body: form })
  const body = await res.json().catch(() => ({}))
  if (!res.ok) throw new Error((body as { message?: string }).message ?? 'فشل رفع الملف')
  return body as ImportResult
}

export const importApi = {
  templateUrl: '/api/import/template',
  preview: (file: File) => postImport('/api/import/preview', file),
  commit: (file: File) => postImport('/api/import/commit', file),
}
