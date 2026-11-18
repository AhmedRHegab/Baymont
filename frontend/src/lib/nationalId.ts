/**
 * The Egyptian national number: 14 digits = century (2 = 1900s, 3 = 2000s), birth date YYMMDD, governorate (2),
 * serial (3), gender digit (odd = man, even = woman), check digit. Same rules as the server, which is the one that
 * decides: the browser only answers sooner. The check digit has no published rule, so only the structure is checked.
 */

export const GOVERNORATES: Record<string, string> = {
  '01': 'القاهرة', '02': 'الإسكندرية', '03': 'بورسعيد', '04': 'السويس',
  '11': 'دمياط', '12': 'الدقهلية', '13': 'الشرقية', '14': 'القليوبية',
  '15': 'كفر الشيخ', '16': 'الغربية', '17': 'المنوفية', '18': 'البحيرة',
  '19': 'الإسماعيلية', '21': 'الجيزة', '22': 'بني سويف', '23': 'الفيوم',
  '24': 'المنيا', '25': 'أسيوط', '26': 'سوهاج', '27': 'قنا',
  '28': 'أسوان', '29': 'الأقصر', '31': 'البحر الأحمر', '32': 'الوادي الجديد',
  '33': 'مطروح', '34': 'شمال سيناء', '35': 'جنوب سيناء', '88': 'خارج الجمهورية',
}

export interface IdInfo {
  number: string
  /** yyyy-MM-dd */
  birthDate: string
  gender: 'male' | 'female'
  governorate: string
  age: number
}

export type IdParse = { ok: true; info: IdInfo } | { ok: false; error: string }

/** Keeps digits only, turning Arabic-Indic (٠-٩) and Persian (۰-۹) digits into 0-9. */
export function normalizeDigits(s: string): string {
  let out = ''
  for (const ch of s) {
    const code = ch.charCodeAt(0)
    if (code >= 0x30 && code <= 0x39) out += ch
    else if (code >= 0x0660 && code <= 0x0669) out += String(code - 0x0660)
    else if (code >= 0x06f0 && code <= 0x06f9) out += String(code - 0x06f0)
  }
  return out
}

export function parseNationalId(raw: string, today = new Date()): IdParse {
  const n = normalizeDigits(raw)
  if (n.length !== 14) return { ok: false, error: 'الرقم القومي يجب أن يتكون من 14 رقماً.' }

  const century = n[0] === '2' ? 1900 : n[0] === '3' ? 2000 : 0
  if (!century) return { ok: false, error: 'الرقم القومي غير صحيح: أول رقم يجب أن يكون 2 أو 3.' }

  const year = century + Number(n.slice(1, 3))
  const month = Number(n.slice(3, 5))
  const day = Number(n.slice(5, 7))
  const birth = new Date(year, month - 1, day)
  if (month < 1 || month > 12 || day < 1 || birth.getMonth() !== month - 1 || birth.getDate() !== day) {
    return { ok: false, error: 'الرقم القومي غير صحيح: تاريخ الميلاد بداخله غير منطقي.' }
  }
  if (birth > today) return { ok: false, error: 'الرقم القومي غير صحيح: تاريخ الميلاد بداخله في المستقبل.' }

  const code = n.slice(7, 9)
  const governorate = GOVERNORATES[code]
  if (!governorate) return { ok: false, error: 'الرقم القومي غير صحيح: كود المحافظة غير معروف.' }

  let age = today.getFullYear() - year
  if (today.getMonth() < month - 1 || (today.getMonth() === month - 1 && today.getDate() < day)) age -= 1

  const pad = (v: number) => String(v).padStart(2, '0')
  return {
    ok: true,
    info: {
      number: n,
      birthDate: `${year}-${pad(month)}-${pad(day)}`,
      gender: Number(n[12]) % 2 === 1 ? 'male' : 'female',
      governorate,
      age,
    },
  }
}

/** "**********5678" */
export function maskNationalId(n: string): string {
  return n.length <= 4 ? n : '*'.repeat(n.length - 4) + n.slice(-4)
}
