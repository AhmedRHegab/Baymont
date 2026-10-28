/**
 * Browsers show their own validation bubbles ("Please fill out this field") in the language of the browser.
 * This replaces them with short Arabic messages for every form in the app, and also stops a field that
 * holds only spaces from counting as filled in.
 */
type Field = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement

function messageFor(el: Field): string {
  const v = el.validity
  if (v.valueMissing) {
    if (el instanceof HTMLSelectElement) return 'اختر قيمة من القائمة.'
    if (el.type === 'checkbox') return 'يجب تحديد هذا الخيار.'
    if (el.type === 'file') return 'اختر ملفاً.'
    return 'هذا الحقل مطلوب.'
  }
  if (v.typeMismatch) {
    if (el.type === 'email') return 'اكتب بريداً إلكترونياً صحيحاً.'
    if (el.type === 'url') return 'اكتب رابطاً صحيحاً.'
    return 'القيمة المكتوبة غير صحيحة.'
  }
  if (v.patternMismatch) return el.title || 'الصيغة غير صحيحة.'
  if (v.tooShort) return `اكتب ${(el as HTMLInputElement).minLength} أحرف على الأقل.`
  if (v.tooLong) return `الحد الأقصى ${(el as HTMLInputElement).maxLength} حرفاً.`
  if ((v.rangeUnderflow || v.rangeOverflow) && el.type === 'date') return 'اكتب تاريخاً بين سنة 2000 وسنة 2100.'
  if (v.rangeUnderflow) return `القيمة يجب ألا تقل عن ${(el as HTMLInputElement).min}.`
  if (v.rangeOverflow) return `القيمة يجب ألا تزيد على ${(el as HTMLInputElement).max}.`
  if (v.stepMismatch || v.badInput) return 'اكتب قيمة صحيحة.'
  return ''
}

const isField = (t: EventTarget | null): t is Field =>
  t instanceof HTMLInputElement || t instanceof HTMLSelectElement || t instanceof HTMLTextAreaElement

export function installArabicValidation() {
  // The browser is about to show its bubble: put our sentence in it.
  document.addEventListener(
    'invalid',
    (e) => {
      if (!isField(e.target)) return
      e.target.setCustomValidity('')
      const message = messageFor(e.target)
      if (message) e.target.setCustomValidity(message)
    },
    true,
  )

  // Typing or choosing again clears our message so the field can be re-checked.
  const clear = (e: Event) => {
    if (isField(e.target)) e.target.setCustomValidity('')
  }
  document.addEventListener('input', clear, true)
  document.addEventListener('change', clear, true)

  // "   " satisfies `required` in the browser but is not an answer.
  document.addEventListener(
    'submit',
    (e) => {
      const form = e.target
      if (!(form instanceof HTMLFormElement)) return
      for (const el of Array.from(form.elements)) {
        const isText =
          (el instanceof HTMLInputElement && ['text', 'search', 'tel', 'password', 'email', 'url'].includes(el.type)) ||
          el instanceof HTMLTextAreaElement
        if (isText && el.required && el.value.length > 0 && el.value.trim().length === 0) {
          el.setCustomValidity('هذا الحقل مطلوب. لا تكتب مسافات فقط.')
          el.reportValidity()
          e.preventDefault()
          e.stopImmediatePropagation()
          return
        }
      }
    },
    true,
  )
}
