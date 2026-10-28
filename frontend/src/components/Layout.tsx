import { useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { DemoBanner } from './DemoBanner'
import { GlobalErrors } from './GlobalErrors'

function roleLabel(roles: string[]) {
  if (roles.includes('Admin')) return 'مدير النظام'
  if (roles.includes('Editor')) return 'موظف تعديل'
  if (roles.includes('Overhead')) return 'مهندس تسليم'
  return 'بحث فقط'
}

export function Layout() {
  const { user, logout, isAdmin, canEdit, canDeliver } = useAuth()
  const [addOpen, setAddOpen] = useState(false)
  const [moreOpen, setMoreOpen] = useState(false)
  const addRef = useRef<HTMLDivElement>(null)
  const location = useLocation()
  const navigate = useNavigate()

  useEffect(() => {
    setAddOpen(false)
    setMoreOpen(false)
  }, [location.pathname])

  useEffect(() => {
    function onDocClick(e: MouseEvent) {
      if (!addRef.current?.contains(e.target as Node)) setAddOpen(false)
    }
    document.addEventListener('click', onDocClick)
    return () => document.removeEventListener('click', onDocClick)
  }, [])

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark" aria-hidden>
            ب
          </div>
          <div>
            <strong>باي مونت السخنة</strong>
            <span>نظام الأمن · بحث سريع وواضح</span>
          </div>
        </div>

        <nav className="nav nav-desktop">
          <NavLink to="/" end className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            🔍 البحث
          </NavLink>

          <NavLink to="/contractors" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            المقاولون
          </NavLink>

          <NavLink to="/follow-up" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            المتابعة
          </NavLink>
          <NavLink to="/reservations" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            الحجوزات
          </NavLink>
          <NavLink to="/overnight" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            المبيت
          </NavLink>
          <NavLink to="/staff" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            الموظفون
          </NavLink>
          <NavLink to="/shifts" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            الورديات
          </NavLink>
          <NavLink to="/deliveries" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            التسليمات
          </NavLink>

          {canEdit && (
            <div className="add-menu" ref={addRef}>
              <button
                type="button"
                className="nav-link"
                onClick={() => setAddOpen((v) => !v)}
                aria-expanded={addOpen}
              >
                ＋ إضافة
              </button>
              {addOpen && (
                <div className="add-panel">
                  <NavLink to="/owners/new">إضافة مالك</NavLink>
                  <NavLink to="/units/new">إضافة وحدة</NavLink>
                  <NavLink to="/vehicles/new">إضافة سيارة</NavLink>
                  <NavLink to="/contractors/new">إضافة مقاول</NavLink>
                  <NavLink to="/deliveries/new">إضافة تسليم</NavLink>
                </div>
              )}
            </div>
          )}

          {canDeliver && !canEdit && (
            <NavLink to="/deliveries/new" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              ＋ إضافة تسليم
            </NavLink>
          )}

          {canEdit && (
            <NavLink to="/inbox" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              البريد
            </NavLink>
          )}

          {(isAdmin || canEdit) && (
            <NavLink to="/audit" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              سجل العمليات
            </NavLink>
          )}
          {isAdmin && (
            <NavLink to="/users" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              المستخدمون
            </NavLink>
          )}
          {isAdmin && (
            <NavLink to="/import" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              استيراد ملف
            </NavLink>
          )}

          <div className="nav-user">
            <span className="user-chip">
              {user?.displayName} · {roleLabel(user?.roles ?? [])}
            </span>
            <button className="btn secondary" type="button" onClick={() => void logout()}>
              خروج
            </button>
          </div>
        </nav>
      </header>

      <main className="main">
        <DemoBanner />
        <GlobalErrors />
        <Outlet />
      </main>

      {moreOpen && (
        <div className="more-panel">
          <NavLink to="/follow-up">المتابعة</NavLink>
          <NavLink to="/reservations">الحجوزات</NavLink>
          <NavLink to="/contractors">المقاولون</NavLink>
          <NavLink to="/deliveries">التسليمات</NavLink>
          <NavLink to="/overnight">المبيت</NavLink>
          <NavLink to="/staff">الموظفون</NavLink>
          <NavLink to="/shifts">الورديات</NavLink>
          {canDeliver && !canEdit && <NavLink to="/deliveries/new">＋ إضافة تسليم</NavLink>}
          {canEdit && <NavLink to="/inbox">البريد</NavLink>}
          {(isAdmin || canEdit) && <NavLink to="/audit">سجل العمليات</NavLink>}
          {isAdmin && <NavLink to="/users">المستخدمون</NavLink>}
          {isAdmin && <NavLink to="/import">استيراد ملف</NavLink>}
        </div>
      )}

      <nav className="bottom-bar" aria-label="القائمة السريعة">
        <button
          type="button"
          className={`bottom-item${location.pathname === '/' ? ' active' : ''}`}
          onClick={() => navigate('/')}
        >
          <span>🔍</span>
          بحث
        </button>
        {canEdit && (
          <button
            type="button"
            className={`bottom-item${location.pathname.includes('/new') ? ' active' : ''}`}
            onClick={() => navigate('/vehicles/new')}
          >
            <span>＋</span>
            إضافة
          </button>
        )}
        <button
          type="button"
          className={`bottom-item${moreOpen ? ' active' : ''}`}
          onClick={() => setMoreOpen((v) => !v)}
          aria-expanded={moreOpen}
        >
          <span>☰</span>
          القائمة
        </button>
        <button type="button" className="bottom-item" onClick={() => void logout()}>
          <span>⎋</span>
          خروج
        </button>
      </nav>
    </div>
  )
}
