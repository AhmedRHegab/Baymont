import { type FormEvent, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { SESSION_EXPIRED_EVENT } from '../api/client'
import { useAuth } from '../hooks/useAuth'
import { ApiError } from '../types'

export function LoginPage() {
  const { user, loading, login } = useAuth()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [expired] = useState(() => {
    try {
      const flag = sessionStorage.getItem(SESSION_EXPIRED_EVENT) === '1'
      sessionStorage.removeItem(SESSION_EXPIRED_EVENT)
      return flag
    } catch {
      return false
    }
  })

  if (!loading && user) return <Navigate to="/" replace />

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login(userName.trim(), password)
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : 'تعذر الدخول. تأكد من اسم المستخدم وكلمة المرور.',
      )
    } finally {
      setBusy(false)
    }
  }

  if (loading) {
    return (
      <div className="login-wrap">
        <div className="card loading-block">
          <div className="spinner" />
          <div>جاري فتح النظام...</div>
        </div>
      </div>
    )
  }

  return (
    <div className="login-wrap">
      <form className="card login-card form-grid" onSubmit={onSubmit}>
        <div className="login-brand">
          <div className="brand-mark" aria-hidden>
            ب
          </div>
          <h1>باي مونت السخنة</h1>
          <p className="lead muted">نظام الأمن الداخلي — اكتب بياناتك ثم اضغط دخول</p>
        </div>

        <label>
          اسم المستخدم
          <input
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
            autoFocus
            required
            autoComplete="username"
            placeholder="مثال: gate"
          />
        </label>
        <label>
          كلمة المرور
          <input
            type={showPassword ? 'text' : 'password'}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoComplete="current-password"
            placeholder="••••••••"
          />
        </label>
        <label className="check">
          <input
            type="checkbox"
            checked={showPassword}
            onChange={(e) => setShowPassword(e.target.checked)}
          />
          إظهار كلمة المرور
        </label>

        {expired && !error && <div className="banner info">انتهت جلستك. سجّل الدخول من جديد.</div>}
        {error && <div className="banner error">{error}</div>}

        <button className="btn lg" type="submit" disabled={busy || !userName || !password}>
          {busy ? 'جاري الدخول...' : 'دخول'}
        </button>
      </form>
    </div>
  )
}
