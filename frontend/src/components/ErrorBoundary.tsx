import { Component, type ErrorInfo, type ReactNode } from 'react'

interface State {
  failed: boolean
}

/** If a screen breaks while drawing itself, show a plain Arabic message instead of a blank page. */
export class ErrorBoundary extends Component<{ children: ReactNode }, State> {
  state: State = { failed: false }

  static getDerivedStateFromError(): State {
    return { failed: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error(error, info.componentStack)
  }

  render() {
    if (!this.state.failed) return this.props.children
    return (
      <div className="login-wrap">
        <div className="card stack center">
          <h1>حدث خطأ في الصفحة</h1>
          <p className="lead muted">لم نتمكن من عرض هذه الصفحة. بياناتك المحفوظة لم تتأثر.</p>
          <button className="btn lg" type="button" onClick={() => window.location.reload()}>
            إعادة تحميل الصفحة
          </button>
        </div>
      </div>
    )
  }
}
