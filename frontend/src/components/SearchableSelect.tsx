import { useEffect, useId, useMemo, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'

export interface SelectOption {
  value: string
  label: string
  hint?: string
}

interface Props {
  label: string
  help?: string
  value: string
  selectedLabel?: string
  disabled?: boolean
  placeholder?: string
  emptyLabel?: string
  queryKey: string
  fetchOptions: (query: string) => Promise<SelectOption[]>
  onChange: (value: string, option: SelectOption | null) => void
}

export function SearchableSelect({
  label,
  help,
  value,
  selectedLabel,
  disabled,
  placeholder = 'ابحث واختر...',
  emptyLabel = 'بدون ربط',
  queryKey,
  fetchOptions,
  onChange,
}: Props) {
  const id = useId()
  const rootRef = useRef<HTMLDivElement>(null)
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [debounced, setDebounced] = useState('')

  useEffect(() => {
    const t = window.setTimeout(() => setDebounced(query.trim()), 220)
    return () => window.clearTimeout(t)
  }, [query])

  useEffect(() => {
    function onDocClick(e: MouseEvent) {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onDocClick)
    return () => document.removeEventListener('mousedown', onDocClick)
  }, [])

  const { data: options = [], isFetching } = useQuery({
    queryKey: [queryKey, debounced],
    queryFn: () => fetchOptions(debounced),
    enabled: open && !disabled,
  })

  const display = useMemo(() => {
    if (!value) return ''
    const found = options.find((o) => o.value === value)
    return found?.label ?? selectedLabel ?? value
  }, [value, options, selectedLabel])

  function select(option: SelectOption | null) {
    onChange(option?.value ?? '', option)
    setQuery('')
    setOpen(false)
  }

  return (
    <div className="search-select" ref={rootRef}>
      <label htmlFor={id}>
        {label}
        {help && <span className="field-help">{help}</span>}
      </label>

      <button
        id={id}
        type="button"
        className={`search-select-trigger${open ? ' open' : ''}${!value ? ' placeholder' : ''}`}
        disabled={disabled}
        onClick={() => {
          if (disabled) return
          setOpen((v) => !v)
          if (!open) setQuery('')
        }}
      >
        <span>{value ? display : placeholder}</span>
        <span className="search-select-caret" aria-hidden>
          ▾
        </span>
      </button>

      {open && !disabled && (
        <div className="search-select-panel">
          <input
            className="search-select-input"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="اكتب للبحث..."
            autoFocus
          />
          <button type="button" className="search-select-option clear" onClick={() => select(null)}>
            {emptyLabel}
          </button>
          {isFetching && <div className="search-select-empty">جاري البحث...</div>}
          {!isFetching && options.length === 0 && (
            <div className="search-select-empty">لا توجد نتائج</div>
          )}
          {!isFetching &&
            options.map((option) => (
              <button
                key={option.value}
                type="button"
                className={`search-select-option${option.value === value ? ' selected' : ''}`}
                onClick={() => select(option)}
              >
                <strong>{option.label}</strong>
                {option.hint && <span>{option.hint}</span>}
              </button>
            ))}
        </div>
      )}
    </div>
  )
}
