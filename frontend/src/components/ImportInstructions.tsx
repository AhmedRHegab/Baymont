import { useState } from 'react'

interface Props {
  open: boolean
  templateUrl: string
  onClose: () => void
  onContinue: () => void
}

const steps = [
  {
    title: 'حمّل النموذج الجاهز',
    text: 'اضغط الزر الأخضر «تحميل النموذج» بالأسفل. سيُحمَّل ملف Excel جاهز على جهازك. لا تكتب بياناتك في ملف آخر.',
  },
  {
    title: 'افتح الملف واكتب البيانات',
    text: 'السطر الأول فيه عناوين الأعمدة، لا تغيّره ولا تمسحه ولا تغيّر ترتيب الأعمدة. السطر الثاني مثال، امسحه أو اكتب فوقه. اكتب كل مالك في سطر مستقل، بدءاً من السطر الثاني.',
  },
  {
    title: 'اكتب الأعمدة بالترتيب',
    text: '',
    list: [
      'اسم المالك: مطلوب. لا يُترك فارغاً أبداً.',
      'رقم الموبايل: اختياري.',
      'المصطبة: مثل 1 أو 2.',
      'المبنى: رقم المبنى.',
      'الدور: رقم الدور (الأرضي اكتب فيه: الأرضي).',
      'الشقة: رقم الشقة.',
      'الرقم الإنشائي: اختياري. إن كتبته فسيُعتمد لتمييز الوحدة.',
    ],
  },
  {
    title: 'مالك له أكثر من وحدة',
    text: 'اكتب اسمه في كل سطر، وفي كل سطر بيانات وحدة مختلفة. سيتم تسجيل المالك مرة واحدة فقط وربط كل وحداته به.',
  },
  {
    title: 'احفظ الملف بصيغة xlsx',
    text: 'من Excel اختر: ملف ← حفظ باسم ← صيغة «Excel Workbook (*.xlsx)». الصيغة القديمة xls وملفات CSV لا تعمل. لا تضع كلمة سر على الملف.',
  },
  {
    title: 'اختر الملف وراجع النتيجة',
    text: 'اضغط «فهمت، اختر الملف» ثم اختر الملف. سيفحصه النظام ويعرض لك كم مالك وكم وحدة سيتم إضافتها، دون أن يحفظ أي شيء بعد.',
  },
  {
    title: 'أكّد الاستيراد',
    text: 'إذا ظهرت النتيجة سليمة اضغط «تأكيد الاستيراد». وإذا ظهرت أخطاء، سيذكر لك رقم السطر وما المطلوب تصحيحه. صحّح الملف ثم اختره من جديد. لن يُحفظ شيء إلا إذا كان الملف كله سليماً.',
  },
]

const notes = [
  'الحد الأقصى 2000 سطر و5 ميجابايت للملف الواحد.',
  'إذا كان المالك مسجلاً بنفس الاسم من قبل فلن يتكرر، وسيُستخدم المالك الموجود.',
  'إذا كانت الوحدة مسجلة من قبل (نفس الرقم الإنشائي، أو نفس المصطبة والمبنى والدور والشقة) فسيتم تجاهلها ولن تتكرر.',
  'الاستيراد يضيف فقط، ولا يعدّل ولا يمسح أي بيانات موجودة.',
  'قبل الاستيراد الكبير يُفضل أخذ نسخة احتياطية.',
]

export function ImportInstructions({ open, templateUrl, onClose, onContinue }: Props) {
  const [understood, setUnderstood] = useState(false)
  if (!open) return null

  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="import-help-title">
      <div className="card modal modal-wide">
        <h2 id="import-help-title">تعليمات الاستيراد — اقرأها قبل البدء</h2>

        <ol className="help-steps">
          {steps.map((s) => (
            <li key={s.title}>
              <strong>{s.title}</strong>
              {s.text && <p>{s.text}</p>}
              {s.list && (
                <ul>
                  {s.list.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              )}
            </li>
          ))}
        </ol>

        <div className="banner info">
          <strong>ملاحظات مهمة</strong>
          <ul className="help-notes">
            {notes.map((n) => (
              <li key={n}>{n}</li>
            ))}
          </ul>
        </div>

        <a className="btn lg ok" href={templateUrl} download>
          ⬇ تحميل النموذج
        </a>

        <label className="check-row">
          <input type="checkbox" checked={understood} onChange={(e) => setUnderstood(e.target.checked)} />
          قرأت التعليمات وفهمتها
        </label>

        <div className="actions">
          <button className="btn lg" type="button" disabled={!understood} onClick={onContinue}>
            فهمت، اختر الملف
          </button>
          <button className="btn secondary" type="button" onClick={onClose}>
            إلغاء
          </button>
        </div>
      </div>
    </div>
  )
}
