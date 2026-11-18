import { type ReactNode, useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { DemoBanner } from './DemoBanner'
import { GlobalErrors } from './GlobalErrors'
import { BrandMark, Icon, type IconName } from './Icon'

function roleLabel(roles: string[]) {
  if (roles.includes('Admin')) return 'مدير النظام'
  if (roles.includes('Editor')) return 'موظف تعديل'
  if (roles.includes('Overhead')) return 'مهندس تسليم'
  return 'بحث فقط'
}

interface NavItem {
  to: string
  label: string
  icon: IconName
  end?: boolean
}

/** A bottom sheet: the phone-style panel that slides up from the bottom. */
function Sheet({ open, onClose, title, children }: { open: boolean; onClose: () => void; title?: string; children: ReactNode }) {
  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [open, onClose])

  if (!open) return null
  return (
    <div className="sheet-backdrop" onClick={onClose}>
      <div className="sheet" role="dialog" aria-modal="true" aria-label={title} onClick={(e) => e.stopPropagation()}>
        <div className="sheet-handle" aria-hidden />
        {title && <h2 className="sheet-title">{title}</h2>}
        {children}
      </div>
    </div>
  )
}

/** A small dropdown for the desktop top bar. */
function Dropdown({ label, icon, children }: { label: string; icon: IconName; children: ReactNode }) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const location = useLocation()

  useEffect(() => setOpen(false), [location.pathname])
  useEffect(() => {
    const onDoc = (e: MouseEvent) => !ref.current?.contains(e.target as Node) && setOpen(false)
    document.addEventListener('click', onDoc)
    return () => document.removeEventListener('click', onDoc)
  }, [])

  return (
    <div className="add-menu" ref={ref}>
      <button type="button" className="nav-link" onClick={() => setOpen((v) => !v)} aria-expanded={open}>
        <Icon name={icon} size={18} />
        {label}
      </button>
      {open && <div className="add-panel">{children}</div>}
    </div>
  )
}

export function Layout() {
  const { user, logout, isAdmin, canEdit, canDeliver } = useAuth()
  const [sheet, setSheet] = useState<'add' | 'more' | null>(null)
  const location = useLocation()
  const navigate = useNavigate()

  useEffect(() => setSheet(null), [location.pathname])

  const main: NavItem[] = [
    { to: '/', label: 'البحث', icon: 'search', end: true },
    { to: '/reservations', label: 'الحجوزات', icon: 'ticket' },
    { to: '/follow-up', label: 'المتابعة', icon: 'activity' },
    { to: '/contractors', label: 'المقاولون', icon: 'hardhat' },
    { to: '/deliveries', label: 'التسليمات', icon: 'key' },
    { to: '/overnight', label: 'المبيت', icon: 'moon' },
    { to: '/staff', label: 'الموظفون', icon: 'users' },
    { to: '/shifts', label: 'الورديات', icon: 'shield' },
  ]
  const admin: NavItem[] = [
    ...(canEdit ? [{ to: '/inbox', label: 'البريد', icon: 'mail' } as NavItem] : []),
    ...(isAdmin || canEdit ? [{ to: '/audit', label: 'سجل العمليات', icon: 'clipboard' } as NavItem] : []),
    ...(isAdmin ? [{ to: '/users', label: 'المستخدمون', icon: 'userPlus' } as NavItem] : []),
    ...(isAdmin ? [{ to: '/import', label: 'استيراد ملف', icon: 'upload' } as NavItem] : []),
  ]
  const add: NavItem[] = canEdit
    ? [
        { to: '/owners/new', label: 'مالك', icon: 'user' },
        { to: '/units/new', label: 'وحدة', icon: 'building' },
        { to: '/vehicles/new', label: 'سيارة', icon: 'car' },
        { to: '/contractors/new', label: 'مقاول', icon: 'hardhat' },
        { to: '/deliveries/new', label: 'تسليم', icon: 'key' },
      ]
    : []

  // The phone's bottom bar keeps the three screens people use all day; everything else lives under "المزيد".
  const tabs = [main[0], main[1], main[2]]
  const more = [...main.slice(3), ...admin]
  const link = ({ isActive }: { isActive: boolean }) => `nav-link${isActive ? ' active' : ''}`
  const tab = ({ isActive }: { isActive: boolean }) => `bottom-item${isActive ? ' active' : ''}`
  const initial = (user?.displayName ?? '?').trim().charAt(0)

  function onAddTap() {
    if (canEdit) setSheet('add')
    else if (canDeliver) navigate('/deliveries/new')
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="topbar-inner">
          <NavLink to="/" className="brand" aria-label="الرئيسية">
            <BrandMark size={38} />
            <div>
              <strong>باي مونت السخنة</strong>
              <span>بواسطة اللواء محمد عبدالرازق</span>
            </div>
          </NavLink>

          <nav className="nav nav-desktop" aria-label="التنقل">
            {main.map((i) => (
              <NavLink key={i.to} to={i.to} end={i.end} className={link}>
                <Icon name={i.icon} size={18} />
                {i.label}
              </NavLink>
            ))}
            {canEdit && (
              <Dropdown label="إضافة" icon="plus">
                {add.map((i) => (
                  <NavLink key={i.to} to={i.to}>
                    <Icon name={i.icon} size={18} />
                    {i.label}
                  </NavLink>
                ))}
              </Dropdown>
            )}
            {!canEdit && canDeliver && (
              <NavLink to="/deliveries/new" className={link}>
                <Icon name="plus" size={18} />
                إضافة تسليم
              </NavLink>
            )}
            {admin.length > 0 && (
              <Dropdown label="الإدارة" icon="grid">
                {admin.map((i) => (
                  <NavLink key={i.to} to={i.to}>
                    <Icon name={i.icon} size={18} />
                    {i.label}
                  </NavLink>
                ))}
              </Dropdown>
            )}
          </nav>

          <div className="nav-user">
            <div className="user-chip" title={user?.displayName}>
              <span className="avatar">{initial}</span>
              <span className="user-text">
                <strong>{user?.displayName}</strong>
                <small>{roleLabel(user?.roles ?? [])}</small>
              </span>
            </div>
            <button className="icon-btn logout-btn" type="button" onClick={() => void logout()} aria-label="تسجيل الخروج" title="تسجيل الخروج">
              <Icon name="logout" size={20} />
            </button>
          </div>
        </div>
      </header>

      <main className="main">
        <DemoBanner />
        <GlobalErrors />
        <div className="page" key={location.pathname}>
          <Outlet />
        </div>
      </main>

      <nav className="bottom-bar" aria-label="القائمة السريعة">
        {tabs.slice(0, 2).map((i) => (
          <NavLink key={i.to} to={i.to} end={i.end} className={tab}>
            <span className="bottom-icon">
              <Icon name={i.icon} size={22} />
            </span>
            {i.label}
          </NavLink>
        ))}

        {(canEdit || canDeliver) && (
          <button type="button" className="bottom-fab" onClick={onAddTap} aria-label="إضافة">
            <Icon name="plus" size={28} />
          </button>
        )}

        <NavLink to={tabs[2].to} className={tab}>
          <span className="bottom-icon">
            <Icon name={tabs[2].icon} size={22} />
          </span>
          {tabs[2].label}
        </NavLink>
        <button type="button" className={`bottom-item${sheet === 'more' ? ' active' : ''}`} onClick={() => setSheet('more')} aria-label="المزيد">
          <span className="bottom-icon">
            <Icon name="grid" size={22} />
          </span>
          المزيد
        </button>
      </nav>

      <Sheet open={sheet === 'add'} onClose={() => setSheet(null)} title="إضافة جديد">
        <div className="sheet-grid">
          {add.map((i) => (
            <NavLink key={i.to} to={i.to} className="sheet-tile">
              <span className="tile-icon">
                <Icon name={i.icon} size={24} />
              </span>
              {i.label}
            </NavLink>
          ))}
        </div>
      </Sheet>

      <Sheet open={sheet === 'more'} onClose={() => setSheet(null)}>
        <div className="sheet-user">
          <span className="avatar lg">{initial}</span>
          <div>
            <strong>{user?.displayName}</strong>
            <small>{roleLabel(user?.roles ?? [])}</small>
          </div>
          <button type="button" className="btn secondary sm" onClick={() => void logout()}>
            <Icon name="logout" size={18} />
            خروج
          </button>
        </div>
        <div className="sheet-grid">
          {more.map((i) => (
            <NavLink key={i.to} to={i.to} className="sheet-tile">
              <span className="tile-icon">
                <Icon name={i.icon} size={24} />
              </span>
              {i.label}
            </NavLink>
          ))}
        </div>
      </Sheet>
    </div>
  )
}
