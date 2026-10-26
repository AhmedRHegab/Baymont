import { ownersApi, unitsApi } from '../api/endpoints'
import type { SelectOption } from '../components/SearchableSelect'

export async function fetchOwnerOptions(query: string): Promise<SelectOption[]> {
  const rows = await ownersApi.lookup(query)
  return rows.map((o) => ({
    value: o.id,
    label: o.fullName,
    hint: [o.internalCode, o.phone].filter(Boolean).join(' · '),
  }))
}

export function unitLabel(u: {
  constructionNumber?: string | null
  terrace?: string | null
  building?: string | null
  floor?: string | null
  apartment?: string | null
}) {
  if (u.constructionNumber) return u.constructionNumber
  return [u.terrace, u.building, u.floor, u.apartment].filter(Boolean).join(' / ') || 'وحدة'
}

export async function fetchUnitOptions(query: string): Promise<SelectOption[]> {
  const rows = await unitsApi.lookup(query)
  return rows.map((u) => ({
    value: u.id,
    label: unitLabel(u),
    hint: u.ownerName ? `المالك: ${u.ownerName}` : undefined,
  }))
}

export const shiftPeriods = [
  { value: 1, label: 'الصباحية' },
  { value: 2, label: 'المسائية' },
  { value: 3, label: 'الليلية' },
] as const

export const shiftKinds = [
  { value: 1, label: 'الوردية الخارجية' },
  { value: 2, label: 'الدورية الليلية' },
] as const

export const equipmentCategories = [
  { value: 1, label: 'عدة خفيفة' },
  { value: 2, label: 'عدة ثقيلة' },
] as const
