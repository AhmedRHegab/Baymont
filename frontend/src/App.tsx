import { Navigate, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { useAuth } from './hooks/useAuth'
import { AuditPage } from './pages/AuditPage'
import { ContractorPage } from './pages/ContractorPage'
import { ContractorsPage } from './pages/ContractorsPage'
import { DeliveriesPage } from './pages/DeliveriesPage'
import { DeliveryPage } from './pages/DeliveryPage'
import { OvernightPage } from './pages/OvernightPage'
import { ShiftsPage } from './pages/ShiftsPage'
import { StaffPage } from './pages/StaffPage'
import { ImportPage } from './pages/ImportPage'
import { LoginPage } from './pages/LoginPage'
import { OwnerPage } from './pages/OwnerPage'
import { SearchPage } from './pages/SearchPage'
import { UnitPage } from './pages/UnitPage'
import { UsersPage } from './pages/UsersPage'
import { VehiclePage } from './pages/VehiclePage'

function Protected({ children }: { children: React.ReactNode }) {
  const { user, loading } = useAuth()
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
  if (!user) return <Navigate to="/login" replace />
  return children
}

function AdminOnly({ children }: { children: React.ReactNode }) {
  const { isAdmin } = useAuth()
  if (!isAdmin) return <Navigate to="/" replace />
  return children
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <Protected>
            <Layout />
          </Protected>
        }
      >
        <Route index element={<SearchPage />} />
        <Route path="owners/:id" element={<OwnerPage />} />
        <Route path="units/:id" element={<UnitPage />} />
        <Route path="vehicles/:id" element={<VehiclePage />} />
        <Route path="contractors" element={<ContractorsPage />} />
        <Route path="contractors/:id" element={<ContractorPage />} />
        <Route path="deliveries" element={<DeliveriesPage />} />
        <Route path="deliveries/:id" element={<DeliveryPage />} />
        <Route path="overnight" element={<OvernightPage />} />
        <Route path="staff" element={<StaffPage />} />
        <Route path="shifts" element={<ShiftsPage />} />
        <Route path="audit" element={<AuditPage />} />
        <Route
          path="users"
          element={
            <AdminOnly>
              <UsersPage />
            </AdminOnly>
          }
        />
        <Route
          path="import"
          element={
            <AdminOnly>
              <ImportPage />
            </AdminOnly>
          }
        />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
