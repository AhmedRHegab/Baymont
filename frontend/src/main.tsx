import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryCache, QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import '@fontsource/cairo/400.css'
import '@fontsource/cairo/500.css'
import '@fontsource/cairo/600.css'
import '@fontsource/cairo/700.css'
import '@fontsource/cairo/800.css'
import App from './App'
import { ErrorBoundary } from './components/ErrorBoundary'
import { LOAD_FAILED_EVENT } from './api/client'
import { installArabicValidation } from './lib/arabicValidation'
import { AuthProvider } from './hooks/useAuth'
import './styles/app.css'

installArabicValidation()

const queryClient = new QueryClient({
  queryCache: new QueryCache({
    // Any list or page that fails to load tells the person, in Arabic. An expired session is handled separately.
    onError: (error) => {
      if ((error as { status?: number }).status === 401) return
      window.dispatchEvent(new CustomEvent(LOAD_FAILED_EVENT, { detail: error.message }))
    },
  }),
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
    </ErrorBoundary>
  </StrictMode>,
)
