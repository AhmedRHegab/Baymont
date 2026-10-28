import { ApiError } from '../types'

const jsonHeaders = { 'Content-Type': 'application/json' }

/** Fired when the server says the login has expired, so the app can send the person back to the login page. */
export const SESSION_EXPIRED_EVENT = 'baymont:session-expired'
/** Fired for failed background loads (lists, dashboards) so a banner can tell the person, instead of showing "no results". */
export const LOAD_FAILED_EVENT = 'baymont:load-failed'

export const NETWORK_MESSAGE = 'تعذر الاتصال بالخادم. تأكد من اتصال الإنترنت ثم حاول مرة أخرى.'

const byStatus: Record<number, string> = {
  400: 'البيانات المدخلة غير صحيحة. راجع الحقول وحاول مرة أخرى.',
  401: 'انتهت جلستك. سجّل الدخول من جديد.',
  403: 'ليست لديك صلاحية لهذا الإجراء.',
  404: 'العنصر المطلوب غير موجود.',
  409: 'تعذر الحفظ بسبب تعارض في البيانات. أعد تحميل الصفحة وحاول مرة أخرى.',
  413: 'حجم البيانات أو الملف أكبر من المسموح.',
  429: 'محاولات كثيرة. انتظر قليلاً ثم حاول مرة أخرى.',
}

/** Turns any failed response into an ApiError whose message is a short Arabic sentence. */
async function failure(res: Response, path: string): Promise<never> {
  let message: string | undefined
  try {
    const body = await res.json()
    if (typeof body?.message === 'string' && body.message.trim()) message = body.message
  } catch {
    /* not JSON (a proxy error page, for example): use the fallback below */
  }

  // The login and "who am I" calls answer 401 in normal use; only an expired session elsewhere is news.
  if (res.status === 401 && !path.startsWith('/api/auth/')) {
    try {
      sessionStorage.setItem(SESSION_EXPIRED_EVENT, '1')
    } catch {
      /* storage may be blocked: the redirect still happens */
    }
    window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
  }

  throw new ApiError(
    res.status,
    message ?? byStatus[res.status] ?? (res.status >= 500 ? 'حدث خطأ في الخادم. حاول مرة أخرى بعد قليل.' : 'تعذر تنفيذ الطلب.'),
  )
}

async function send(path: string, init?: RequestInit): Promise<Response> {
  try {
    return await fetch(path, { credentials: 'include', ...init })
  } catch {
    throw new ApiError(0, NETWORK_MESSAGE)
  }
}

async function readJson<T>(res: Response): Promise<T> {
  if (res.status === 204) return undefined as T
  try {
    return (await res.json()) as T
  } catch {
    throw new ApiError(res.status, 'وصلت من الخادم استجابة غير مفهومة. حاول مرة أخرى.')
  }
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await send(path, {
    ...init,
    headers: {
      ...(init?.body ? jsonHeaders : {}),
      ...init?.headers,
    },
  })
  if (!res.ok) await failure(res, path)
  return readJson<T>(res)
}

/** Uploads one file in the form field "file". */
export async function postFile<T>(path: string, file: File): Promise<T> {
  const form = new FormData()
  form.append('file', file)
  const res = await send(path, { method: 'POST', body: form })
  if (!res.ok) await failure(res, path)
  return readJson<T>(res)
}
