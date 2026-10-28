import { type FormEvent, useCallback, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usersApi } from '../api/endpoints'
import { LoadingBlock } from '../components/LoadingBlock'
import { PageHeader } from '../components/PageHeader'
import { DemoDataCard } from '../components/DemoDataCard'
import { Toast } from '../components/Toast'

const roleOptions = [
  { value: 'SearchOnly', label: 'بحث فقط (بوابة)' },
  { value: 'Editor', label: 'تعديل البيانات' },
  { value: 'Overhead', label: 'مهندس تسليم' },
  { value: 'Admin', label: 'مدير النظام' },
]

function roleArabic(roles: string[]) {
  if (roles.includes('Admin')) return 'مدير النظام'
  if (roles.includes('Editor')) return 'تعديل البيانات'
  if (roles.includes('Overhead')) return 'مهندس تسليم'
  return 'بحث فقط'
}

export function UsersPage() {
  const qc = useQueryClient()
  const { data, isLoading } = useQuery({ queryKey: ['users'], queryFn: usersApi.list })

  const [userName, setUserName] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState('SearchOnly')
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)
  const clearToast = useCallback(() => setToast(null), [])

  const createMutation = useMutation({
    mutationFn: () => usersApi.create({ userName, displayName, password, role }),
    onSuccess: () => {
      setUserName('')
      setDisplayName('')
      setPassword('')
      setRole('SearchOnly')
      setToast('تم إنشاء المستخدم')
      void qc.invalidateQueries({ queryKey: ['users'] })
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'تعذر إنشاء المستخدم'),
  })

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    createMutation.mutate()
  }

  return (
    <div className="stack">
      <Toast message={toast} onClose={clearToast} />
      <PageHeader
        title="المستخدمون"
        subtitle="إنشاء حسابات لأفراد الأمن والصلاحيات"
        backTo="/"
      />

      <section className="card">
        <div className="section-title">
          <h2>الحسابات الحالية</h2>
        </div>
        {isLoading ? (
          <LoadingBlock />
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>الاسم</th>
                  <th>اسم الدخول</th>
                  <th>الصلاحية</th>
                  <th>الحالة</th>
                </tr>
              </thead>
              <tbody>
                {data?.map((u) => (
                  <tr key={u.id}>
                    <td>{u.displayName}</td>
                    <td>{u.userName}</td>
                    <td>{roleArabic(u.roles)}</td>
                    <td>{u.isActive ? 'نشط' : 'موقوف'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <form className="card form-grid" onSubmit={onSubmit}>
        <h2 style={{ margin: 0 }}>إضافة مستخدم جديد</h2>
        <label>
          اسم الدخول *
          <input maxLength={50} value={userName} onChange={(e) => setUserName(e.target.value)} required placeholder="gate2" />
        </label>
        <label>
          الاسم الظاهر *
          <input maxLength={128}
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            required
            placeholder="أمن البوابة الرئيسية"
          />
        </label>
        <label>
          كلمة المرور *
          <span className="field-help">8 أحرف على الأقل، مع حرف كبير ورقم ورمز</span>
          <input maxLength={128}
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </label>
        <label>
          الصلاحية
          <select value={role} onChange={(e) => setRole(e.target.value)}>
            {roleOptions.map((r) => (
              <option key={r.value} value={r.value}>
                {r.label}
              </option>
            ))}
          </select>
        </label>
        {error && <div className="banner error">{error}</div>}
        <div className="sticky-actions">
          <button className="btn lg" type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending ? 'جاري الإنشاء...' : 'إنشاء المستخدم'}
          </button>
        </div>
      </form>

      <DemoDataCard />
    </div>
  )
}
