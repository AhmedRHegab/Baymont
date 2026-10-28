import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { SESSION_EXPIRED_EVENT } from '../api/client'
import { authApi } from '../api/endpoints'
import type { UserDto } from '../types'

interface AuthContextValue {
  user: UserDto | null
  loading: boolean
  canEdit: boolean
  isAdmin: boolean
  /** Can record deliveries (handover engineers too). */
  canDeliver: boolean
  /** Gate guards record arrivals and departures. */
  canMove: boolean
  login: (userName: string, password: string) => Promise<void>
  logout: () => Promise<void>
  refresh: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null)
  const [loading, setLoading] = useState(true)

  const refresh = useCallback(async () => {
    try {
      const me = await authApi.me()
      setUser(me)
    } catch {
      setUser(null)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
  }, [refresh])

  // The server says the login expired while the page was open: leave the screen and show the login page.
  useEffect(() => {
    const onExpired = () => setUser(null)
    window.addEventListener(SESSION_EXPIRED_EVENT, onExpired)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired)
  }, [])

  const login = useCallback(async (userName: string, password: string) => {
    const me = await authApi.login(userName, password)
    setUser(me)
  }, [])

  const logout = useCallback(async () => {
    await authApi.logout()
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      loading,
      canEdit: !!user?.roles.some((r) => r === 'Admin' || r === 'Editor'),
      isAdmin: !!user?.roles.includes('Admin'),
      canMove: !!user?.roles.some((r) => r === 'Admin' || r === 'Editor' || r === 'SearchOnly'),
      canDeliver: !!user?.roles.some((r) => r === 'Admin' || r === 'Editor' || r === 'Overhead'),
      login,
      logout,
      refresh,
    }),
    [user, loading, login, logout, refresh],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
