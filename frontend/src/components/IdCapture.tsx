import { useMemo, useRef, useState } from 'react'
import { uploadsApi } from '../api/endpoints'
import { Icon } from './Icon'
import { prepareImage } from '../lib/idImage'
import { parseNationalId, normalizeDigits } from '../lib/nationalId'

export interface IdValue {
  nationalId: string
  photoPath?: string | null
}

interface Props {
  value: IdValue
  onChange: (value: IdValue) => void
  disabled?: boolean
  /** Admins and editors may open stored photos; everyone else only sees that one exists. */
  canViewPhoto: boolean
  label?: string
}

type Step = 'idle' | 'preparing'

/**
 * Optional ID card capture. The photo is taken with the camera or chosen from the gallery and only saved: nothing is
 * read from it. The national number is typed by a person, and the date of birth, gender and governorate are worked
 * out from the number.
 */
export function IdCapture({ value, onChange, disabled, canViewPhoto, label = 'بطاقة الرقم القومي (اختياري)' }: Props) {
  const cameraRef = useRef<HTMLInputElement>(null)
  const galleryRef = useRef<HTMLInputElement>(null)
  const [step, setStep] = useState<Step>('idle')
  const [error, setError] = useState<string | null>(null)

  // Anyone who may not see the full number gets it masked ('**********1234'): show it as is.
  const masked = value.nationalId.includes('*')
  const digits = normalizeDigits(value.nationalId)
  const parsed = useMemo(() => (digits.length === 14 ? parseNationalId(digits) : null), [digits])
  const busy = step !== 'idle'

  async function onPick(input: HTMLInputElement | null) {
    const file = input?.files?.[0]
    if (input) input.value = ''
    if (!file) return
    setError(null)

    if (!file.type.startsWith('image/')) {
      setError('اختر صورة للبطاقة (وليس ملفاً آخر).')
      return
    }
    if (file.size > 30 * 1024 * 1024) {
      setError('حجم الصورة كبير جداً. اختر صورة أخرى.')
      return
    }

    try {
      setStep('preparing')
      const blob = await prepareImage(file)
      const photoPath = await uploadsApi.uploadId(new File([blob], 'id.jpg', { type: 'image/jpeg' }))
      onChange({ ...value, photoPath })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'تعذر حفظ الصورة. حاول مرة أخرى.')
    } finally {
      setStep('idle')
    }
  }

  return (
    <div className="id-capture">
      <span className="id-label">{label}</span>

      {!disabled && (
        <>
          {/* "capture" opens the camera straight away; without it the phone offers the gallery and files. */}
          <input ref={cameraRef} type="file" accept="image/*" capture="environment" hidden onChange={() => void onPick(cameraRef.current)} />
          <input ref={galleryRef} type="file" accept="image/*" hidden onChange={() => void onPick(galleryRef.current)} />
          <div className="photo-row">
            <button type="button" className="btn secondary" disabled={busy} onClick={() => cameraRef.current?.click()}>
              <Icon name="camera" size={18} />
              {step === 'preparing' ? 'جاري حفظ الصورة...' : 'التقاط صورة'}
            </button>
            <button type="button" className="btn secondary" disabled={busy} onClick={() => galleryRef.current?.click()}>
              <Icon name="image" size={18} />
              اختيار من المعرض
            </button>
            {value.photoPath && !busy && (
              <button type="button" className="btn ghost" onClick={() => onChange({ ...value, photoPath: null })}>
                إزالة الصورة
              </button>
            )}
          </div>
        </>
      )}

      {value.photoPath && (
        <div className="photo-row">
          {canViewPhoto ? (
            <a href={uploadsApi.url(value.photoPath)} target="_blank" rel="noreferrer">
              <img className="photo-thumb" src={uploadsApi.url(value.photoPath)} alt="صورة البطاقة" />
            </a>
          ) : (
            <span className="muted">صورة البطاقة محفوظة</span>
          )}
        </div>
      )}

      <label className="id-number">
        الرقم القومي
        <input
          value={value.nationalId}
          onChange={(e) => onChange({ ...value, nationalId: normalizeDigits(e.target.value).slice(0, 14) })}
          disabled={disabled || masked}
          inputMode="numeric"
          dir="ltr"
          maxLength={14}
          placeholder="14 رقماً"
          autoComplete="off"
        />
      </label>

      {!masked && digits.length > 0 && digits.length < 14 && <span className="muted">{digits.length} من 14 رقماً</span>}
      {!masked && parsed?.ok === false && <span className="banner error">{parsed.error}</span>}
      {!masked && parsed?.ok && (
        <span className="banner success">
          ✔ مواليد {parsed.info.birthDate} (عمره {parsed.info.age} سنة) · {parsed.info.gender === 'male' ? 'ذكر' : 'أنثى'} ·{' '}
          {parsed.info.governorate}
        </span>
      )}

      {error && <span className="banner error">{error}</span>}
    </div>
  )
}
