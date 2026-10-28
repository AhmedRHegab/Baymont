import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { demoApi } from '../api/endpoints'
import { useAuth } from '../hooks/useAuth'
import { ConfirmDialog } from './ConfirmDialog'

/** Shown while sample data exists, so nobody mistakes it for real records. */
export function DemoBanner() {
  const { isAdmin } = useAuth()
  const qc = useQueryClient()
  const [confirm, setConfirm] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const { data } = useQuery({ queryKey: ['demo'], queryFn: demoApi.status, staleTime: 30_000 })

  const clear = useMutation({
    mutationFn: demoApi.clear,
    onSuccess: async () => {
      setConfirm(false)
      setError(null)
      await qc.invalidateQueries()
    },
    onError: (err) => {
      setConfirm(false)
      setError(err instanceof Error ? err.message : 'تعذر الحذف')
    },
  })

  if (!data?.hasDemoData) return null

  return (
    <>
      <div className="demo-banner" role="status">
        <span>
          🧪 <strong>بيانات تجريبية للتدريب والعرض فقط</strong> — الأسماء والأرقام مخترعة وليست حقيقية.
        </span>
        {isAdmin && (
          <button type="button" className="btn danger" onClick={() => setConfirm(true)}>
            حذف البيانات التجريبية
          </button>
        )}
      </div>
      {error && <div className="banner error">{error}</div>}
      <ConfirmDialog
        open={confirm}
        title="حذف كل البيانات التجريبية؟"
        message={`سيتم حذف ${data.records} سجلاً تجريبياً نهائياً (ملاك، وحدات، مقاولون، تسليمات، حجوزات، موظفون...) مع كل ما سُجل عليها. بياناتك الحقيقية لا تتأثر. لا يمكن التراجع.`}
        confirmLabel="حذف نهائي"
        busy={clear.isPending}
        onCancel={() => setConfirm(false)}
        onConfirm={() => clear.mutate()}
      />
    </>
  )
}
