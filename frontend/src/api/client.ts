import { ApiError } from '../types'

const jsonHeaders = { 'Content-Type': 'application/json' }

async function parseError(res: Response): Promise<never> {
  let message = 'حدث خطأ'
  try {
    const body = await res.json()
    message = body.message ?? message
  } catch {
    /* ignore */
  }
  throw new ApiError(res.status, message)
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    credentials: 'include',
    ...init,
    headers: {
      ...(init?.body ? jsonHeaders : {}),
      ...init?.headers,
    },
  })

  if (!res.ok) await parseError(res)
  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}
