export type Role = 'Admin' | 'Editor' | 'SearchOnly'

export interface UserDto {
  id: string
  userName: string
  displayName: string
  roles: string[]
  isActive: boolean
  mustChangePassword: boolean
}

export interface SearchHit {
  type: 'owner' | 'unit' | 'vehicle' | string
  id: string
  title: string
  subtitle?: string | null
  rank: number
}

export interface SearchResponse {
  query: string
  hits: SearchHit[]
}

export interface OwnerSummary {
  id: string
  internalCode: string
  fullName: string
  phone?: string | null
}

export interface OwnerDetail {
  id: string
  internalCode: string
  fullName: string
  phone?: string | null
  notes?: string | null
  rowVersion: number
  units: UnitSummary[]
  vehicles: VehicleSummary[]
}

export interface UnitSummary {
  id: string
  constructionNumber?: string | null
  terrace?: string | null
  building?: string | null
  floor?: string | null
  apartment?: string | null
  status: number
  ownerId?: string | null
  ownerName?: string | null
}

export interface UnitDetail {
  id: string
  constructionNumber?: string | null
  terrace?: string | null
  building?: string | null
  floor?: string | null
  apartment?: string | null
  status: number
  ownerId?: string | null
  ownerName?: string | null
  notes?: string | null
  rowVersion: number
  vehicles: VehicleSummary[]
}

export interface VehicleSummary {
  id: string
  plateNumber: string
  make?: string | null
  model?: string | null
  color?: string | null
  ownerId?: string | null
  ownerName?: string | null
  unitId?: string | null
  constructionNumber?: string | null
}

export interface VehicleDetail {
  id: string
  plateNumber: string
  make?: string | null
  model?: string | null
  color?: string | null
  ownerId?: string | null
  ownerName?: string | null
  unitId?: string | null
  constructionNumber?: string | null
  notes?: string | null
  rowVersion: number
}

export interface ContractorSummary {
  id: string
  companyName: string
  contractorName: string
  phone?: string | null
  workerCount: number
}

export interface ContractorWorker {
  id?: string | null
  name: string
  jobTitle?: string | null
  housing?: string | null
  idPhotoPath?: string | null
  criminalRecordPhotoPath?: string | null
}

export interface ContractorVehicle {
  id?: string | null
  plateNumber: string
  isSiteServiceCar: boolean
  notes?: string | null
}

export interface ContractorEquipment {
  id?: string | null
  number?: string | null
  type: string
  category: 1 | 2
  movementPermit?: string | null
  photoPath?: string | null
  notes?: string | null
}

export interface ContractorMaterial {
  id?: string | null
  description: string
  storageLocation?: string | null
  photoPath?: string | null
}

export interface ContractorEntry {
  id?: string | null
  entryDate?: string | null
  entryReason?: string | null
  entryPermit?: string | null
  exitDate?: string | null
  exitReason?: string | null
  exitPermit?: string | null
}

export interface ContractorDetail {
  id: string
  companyName: string
  contractorName: string
  phone?: string | null
  assignment?: string | null
  workStartDate?: string | null
  notes?: string | null
  rowVersion: number
  workers: ContractorWorker[]
  vehicles: ContractorVehicle[]
  equipment: ContractorEquipment[]
  materials: ContractorMaterial[]
  entries: ContractorEntry[]
}

export interface Delivery {
  id: string
  deliveryDate: string
  unitId?: string | null
  unitNumber: string
  customerName: string
  handoverEngineerName: string
  isReceived: boolean
  notReceivedReason?: string | null
  isFurnished: boolean
  comments?: string | null
  complaints?: string | null
  rowVersion: number
}

export interface DeliveryList {
  items: Delivery[]
  total: number
  received: number
  notReceived: number
  furnished: number
}

export type ShiftPeriod = 1 | 2 | 3
export type ShiftKind = 1 | 2

export interface OvernightItem {
  id: string
  ownerId: string
  ownerName: string
  unitId?: string | null
  building?: string | null
  floor?: string | null
  apartment?: string | null
  notes?: string | null
}

export interface OvernightReport {
  date: string
  shift: ShiftPeriod
  total: number
  groups: { terrace: string; items: OvernightItem[] }[]
}

export interface StaffDto {
  id: string
  name: string
  company?: string | null
  jobTitle?: string | null
  plateNumber?: string | null
  phone?: string | null
  isOnSite: boolean
  lastEntryAt?: string | null
  lastExitAt?: string | null
}

export interface StaffLog {
  id: string
  staffMemberId: string
  name: string
  company?: string | null
  jobTitle?: string | null
  entryAt: string
  exitAt?: string | null
}

export interface OnSite {
  staff: (Omit<StaffDto, 'isOnSite' | 'lastEntryAt' | 'lastExitAt'> & { entryAt: string })[]
  contractors: {
    id: string
    companyName: string
    contractorName: string
    workerCount: number
    entryDate?: string | null
    entryReason?: string | null
  }[]
  contractorWorkers: number
}

export interface ShiftAssignment {
  id: string
  shiftDate: string
  kind: ShiftKind
  name: string
  jobTitle?: string | null
  assignedWork?: string | null
  phone?: string | null
  notes?: string | null
}

export interface AuditLogDto {
  id: string
  userId?: string | null
  userName?: string | null
  entityType: string
  entityId: string
  action: string
  occurredAt: string
  oldValuesJson?: string | null
  newValuesJson?: string | null
  ipAddress?: string | null
}

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}
