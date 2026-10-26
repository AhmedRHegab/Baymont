import { useRef, useState } from 'react'
import { importApi, type ImportResult } from '../api/endpoints'
import { ImportInstructions } from '../components/ImportInstructions'
import { PageHeader } from '../components/PageHeader'

export function ImportPage() {
  const [helpOpen, setHelpOpen] = useState(false)
  const [file, setFile] = useState<File | null>(null)
  const [result, setResult] = useState<ImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  function start() {
    setResult(null)
    setError(null)
    setHelpOpen(true)
  }

  async function onPick(picked?: File) {
    if (inputRef.current) inputRef.current.value = ''
    if (!picked) return
    setFile(picked)
    setResult(null)
    setError(null)
    setBusy(true)
    try {
      setResult(await importApi.preview(picked))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'فشل فحص الملف')
    } finally {
      setBusy(false)
    }
  }

  async function confirm() {
    if (!file) return
    setBusy(true)
    setError(null)
    try {
      setResult(await importApi.commit(file))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'فشل الاستيراد')
    } finally {
      setBusy(false)
    }
  }

  const ok = result && result.errors.length === 0
  const nothingNew = ok && result.newOwners === 0 && result.newUnits === 0

  return (
    <div className="stack">
      <PageHeader title="استيراد الملاك من Excel" subtitle="إضافة عدد كبير من الملاك ووحداتهم مرة واحدة" backTo="/" />

      <input
        ref={inputRef}
        type="file"
        accept=".xlsx"
        hidden
        onChange={(e) => void onPick(e.target.files?.[0])}
      />

      <ImportInstructions
        open={helpOpen}
        templateUrl={importApi.templateUrl}
        onClose={() => setHelpOpen(false)}
        onContinue={() => {
          setHelpOpen(false)
          inputRef.current?.click()
        }}
      />

      <section className="card stack center">
        <p className="lead">اضغط الزر وسنشرح لك الخطوات.</p>
        <button className="btn lg" type="button" onClick={start} disabled={busy}>
          {busy ? 'جاري الفحص...' : 'ابدأ الاستيراد'}
        </button>
        {file && <p className="muted">الملف: {file.name}</p>}
      </section>

      {error && <div className="banner error">{error}</div>}

      {result && result.errors.length > 0 && (
        <section className="card stack">
          <div className="banner error">
            <strong>الملف فيه أخطاء، ولم يتم حفظ أي شيء.</strong> صحّح الأسطر التالية في الملف ثم اضغط «ابدأ الاستيراد» واختره من جديد.
          </div>
          <ul className="issue-list">
            {result.errors.slice(0, 50).map((e) => (
              <li key={`${e.row}-${e.message}`}>
                {e.row > 0 && <strong>السطر {e.row}: </strong>}
                {e.message}
              </li>
            ))}
          </ul>
          {result.errors.length > 50 && <p className="muted">وأخطاء أخرى ({result.errors.length - 50}) ستظهر بعد تصحيح هذه.</p>}
        </section>
      )}

      {ok && !result.committed && (
        <section className="card stack">
          <div className="banner info">
            <strong>الملف سليم. هذا ما سيحدث عند التأكيد:</strong>
            <ul className="help-notes">
              <li>سيتم إضافة {result.newOwners} مالك جديد</li>
              <li>سيتم إضافة {result.newUnits} وحدة جديدة</li>
              <li>سيتم تجاهل {result.skippedExisting} وحدة لأنها مسجلة من قبل</li>
            </ul>
          </div>
          {nothingNew ? (
            <p className="muted">لا يوجد جديد لإضافته.</p>
          ) : (
            <button className="btn lg ok" type="button" onClick={() => void confirm()} disabled={busy}>
              {busy ? 'جاري الحفظ...' : 'تأكيد الاستيراد'}
            </button>
          )}
        </section>
      )}

      {ok && result.committed && (
        <section className="card stack">
          <div className="banner success">
            <strong>تم الاستيراد بنجاح.</strong> أُضيف {result.newOwners} مالك و{result.newUnits} وحدة.
          </div>
          <button className="btn secondary" type="button" onClick={start}>
            استيراد ملف آخر
          </button>
        </section>
      )}
    </div>
  )
}
