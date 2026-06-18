import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

/**
 * Manages server-side table state: pagination, filters, sort, loading, data.
 *
 * @param {(({ page, pageSize, filters, sortField, sortOrder }) => Promise<any>) | null} fetchAsync
 *   Stable callback (wrap in useCallback at call site). Pass null to disable fetching.
 * @param {{ defaultPageSize?: number, minDelay?: number }} [options]
 */
export function useServerTable(fetchAsync, { defaultPageSize = 10, minDelay = 500, defaultSortField = null, defaultSortOrder = null } = {}) {
  const { t } = useTranslation()
  const [data, setData] = useState([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(defaultPageSize)
  const [loading, setLoading] = useState(false)
  const [filters, setFilters] = useState({})
  const [sortField, setSortField] = useState(defaultSortField)
  const [sortOrder, setSortOrder] = useState(defaultSortOrder)

  useEffect(() => {
    if (!fetchAsync) return
    let cancelled = false
    setLoading(true)
    const delay = new Promise(res => setTimeout(res, minDelay))
    Promise.all([delay, fetchAsync({ page, pageSize, filters, sortField, sortOrder })])
      .then(([, r]) => {
        if (cancelled) return
        setData(r.data.items ?? r.data)
        setTotal(r.data.total ?? r.data.length)
      })
      .catch(() => {})
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [fetchAsync, page, pageSize, filters, sortField, sortOrder])

  const onTableChange = useCallback((newFilters, sorter) => {
    setFilters(newFilters ?? {})
    setSortField(sorter?.field ?? null)
    setSortOrder(sorter?.order ?? null)
    setPage(1)
  }, [])

  const pagination = {
    current: page,
    pageSize,
    total,
    showSizeChanger: true,
    showTotal: (tot) => t('common.total', { total: tot }),
    onChange: (p, ps) => {
      setPage(p)
      setPageSize(ps)
    },
  }

  return { data, loading, total, page, setPage, pageSize, filters, setFilters, setData, setTotal, pagination, onTableChange }
}
