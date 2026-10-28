/** Checks done in the browser before anything is sent, so the person gets the answer at once, in Arabic. */

interface FileRule {
  /** What the file is, used in the message: "ملف Excel", "الصورة". */
  what: string
  maxMb: number
  /** Allowed extensions, lower case, with the dot. */
  extensions: string[]
  /** Said to the person when the type is wrong, e.g. "xlsx". */
  allowedText: string
}

/** Returns an Arabic problem description, or null when the file is fine. */
export function checkFile(file: File, rule: FileRule): string | null {
  const name = file.name.toLowerCase()
  if (!rule.extensions.some((ext) => name.endsWith(ext))) {
    return `نوع ${rule.what} غير مقبول. المسموح: ${rule.allowedText}.`
  }
  if (file.size === 0) return `${rule.what} فارغ.`
  if (file.size > rule.maxMb * 1024 * 1024) {
    return `حجم ${rule.what} أكبر من ${rule.maxMb} ميجابايت.`
  }
  return null
}

export const excelRule: FileRule = { what: 'الملف', maxMb: 5, extensions: ['.xlsx'], allowedText: 'ملف Excel بصيغة xlsx (افتحه واحفظه باسم جديد بهذه الصيغة)' }

export const photoRule: FileRule = {
  what: 'الملف',
  maxMb: 8,
  extensions: ['.jpg', '.jpeg', '.png', '.webp', '.pdf'],
  allowedText: 'صورة (jpg أو png أو webp) أو ملف PDF',
}

/** The end of a period may not come before its start. Dates are yyyy-MM-dd, which compare correctly as text. */
export function endBeforeStart(start?: string | null, end?: string | null) {
  return !!start && !!end && end < start
}
