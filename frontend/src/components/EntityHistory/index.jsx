import { useCallback, useState } from 'react'
import { Button, DatePicker, Modal, Space } from 'antd'
import { CalendarOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import { auditApi } from '../../api/auditApi'
import { useTranslation } from 'react-i18next'
import DataTable from '../DataTable'
import { useServerTable } from '../../hooks/useServerTable'

const { RangePicker } = DatePicker

const ACTIONS = ['create', 'update', 'delete', 'validate', 'invalidate']
const APPLICANT_ENTITY_TYPES = ['applicant', 'applicant_evaluation_value', 'applicant_admission_category']
const SPECIALTY_ENTITY_TYPES = ['specialty', 'competition_list']
const COMPETITION_LIST_ENTITY_TYPES = ['competition_list', 'admission_category']
const EVALUATION_CRITERIA_GROUP_ENTITY_TYPES = ['evaluation_criteria_group', 'evaluation_criteria_group_item']

function DateRangeFilter({ setSelectedKeys, selectedKeys, confirm, clearFilters, close, t }) {
  const parsed = selectedKeys[0] ? JSON.parse(selectedKeys[0]) : null
  const value = parsed ? [dayjs(parsed[0]), dayjs(parsed[1])] : null

  return (
    <div style={{ padding: 8 }} onKeyDown={e => e.stopPropagation()}>
      <RangePicker
        value={value}
        showTime={{ format: 'HH:mm' }}
        format="DD.MM.YYYY HH:mm"
        suffixIcon={null}
        separator={null}
        onChange={(dates) => {
          if (dates?.[0] && dates?.[1]) {
            setSelectedKeys([JSON.stringify([dates[0].toISOString(), dates[1].toISOString()])])
          } else {
            setSelectedKeys([])
          }
        }}
        style={{ marginBottom: 8, display: 'block' }}
      />
      <Space>
        <Button type="primary" onClick={() => confirm()} size="small" style={{ width: 90 }}>
          {t('dataTable.find')}
        </Button>
        <Button
          onClick={() => clearFilters && clearFilters({ confirm: true, closeDropdown: true })}
          size="small"
          style={{ width: 90 }}
        >
          {t('dataTable.reset')}
        </Button>
        <Button type="link" size="small" onClick={() => close()}>
          {t('dataTable.close')}
        </Button>
      </Space>
    </div>
  )
}

/**
 * @param {{ entityType: string, entityId: string | number | null }} props
 */
function EntityHistory({ entityType, entityId }) {
  const { t } = useTranslation()
  const [changesModalOpen, setChangesModalOpen] = useState(false)
  const [selectedChanges, setSelectedChanges] = useState(/** @type {string | null} */ (null))

  const fetchAsync = useCallback(
    (/** @type {{ page: number, pageSize: number, filters: Record<string, any[]>, sortField: string|null, sortOrder: string|null }} */ { page, pageSize, filters, sortField, sortOrder }) => {
      if (!entityId) return Promise.resolve({ data: { items: [], total: 0 } })
      const dateRaw = filters?.createdAt?.[0]
      const parsed = dateRaw ? JSON.parse(dateRaw) : null
      return auditApi.getEntityHistory(entityType, entityId, page, pageSize, {
        username: filters?.username?.[0],
        action: filters?.action?.[0],
        logEntityType: filters?.entityType?.[0],
        from: parsed?.[0],
        to: parsed?.[1],
        sortOrder: sortField === 'createdAt' ? sortOrder : null,
      })
    },
    [entityType, entityId]
  )

  const { data, loading, pagination, onTableChange } = useServerTable(fetchAsync, { defaultPageSize: 10 })

  if (!entityId) return null

  const handleShowChanges = (/** @type {any} */ entry) => {
    if (!entry.changes) return
    try {
      setSelectedChanges(JSON.stringify(JSON.parse(entry.changes), null, 2))
    } catch {
      setSelectedChanges(entry.changes)
    }
    setChangesModalOpen(true)
  }

  const showSubEntityColumns = entityType === 'applicant' || entityType === 'specialty' || entityType === 'competition_list' || entityType === 'evaluation_criteria_group'
  const subEntityTypes =
    entityType === 'applicant' ? APPLICANT_ENTITY_TYPES :
    entityType === 'specialty' ? SPECIALTY_ENTITY_TYPES :
    entityType === 'competition_list' ? COMPETITION_LIST_ENTITY_TYPES :
    EVALUATION_CRITERIA_GROUP_ENTITY_TYPES

  const columns = /** @type {any[]} */ ([
    {
      title: t('audit.history.columns.date'),
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      sorter: true,
      defaultSortOrder: 'descend',
      filterDropdown: (props) => <DateRangeFilter {...props} t={t} />,
      filterIcon: (filtered) => <CalendarOutlined style={{ color: filtered ? '#1677ff' : undefined }} />,
      render: (/** @type {any} */ v) => v ? dayjs(v).format('DD.MM.YYYY HH:mm') : '—',
    },
    {
      title: t('audit.history.columns.user'),
      dataIndex: 'username',
      key: 'username',
      withSearch: true,
    },
    {
      title: t('audit.history.columns.action'),
      dataIndex: 'action',
      key: 'action',
      filters: ACTIONS.map(v => ({ text: t(`audit.actions.${v}`), value: v })),
      filterMultiple: false,
      render: (/** @type {any} */ v) => String(t(`audit.actions.${v}`, v)),
    },
    ...(showSubEntityColumns ? [
      {
        title: t('audit.history.columns.entity'),
        dataIndex: 'entityType',
        key: 'entityType',
        filters: subEntityTypes.map(v => ({ text: t(`audit.entityTypes.${v}`, v), value: v })),
        filterMultiple: false,
        render: (/** @type {string} */ v) => String(t(`audit.entityTypes.${v}`, v)),
      },
      {
        title: t('audit.log.columns.entityId'),
        dataIndex: 'entityId',
        key: 'entityId',
        width: 80,
      },
    ] : []),
    {
      title: t('audit.history.columns.changes'),
      key: 'changes',
      render: (/** @type {any} */ _, /** @type {any} */ entry) =>
        entry.changes ? (
          <Button type="link" size="small" onClick={() => handleShowChanges(entry)}>
            {t('common.view')}
          </Button>
        ) : '—',
    },
  ])

  return (
    <>
      <DataTable
        dataSource={data}
        columns={columns}
        rowKey={(/** @type {any} */ r, /** @type {any} */ i) => r.id ?? i}
        loading={loading}
        serverSidePagination={true}
        pagination={pagination}
        onTableChange={onTableChange}
        locale={{ emptyText: t('audit.history.empty') }}
      />

      <Modal
        open={changesModalOpen}
        title={t('audit.history.columns.changes')}
        footer={<Button onClick={() => setChangesModalOpen(false)}>{t('common.close')}</Button>}
        onCancel={() => setChangesModalOpen(false)}
        width={700}
      >
        <pre style={{ maxHeight: 500, overflow: 'auto', fontSize: 12 }}>{selectedChanges}</pre>
      </Modal>
    </>
  )
}

export default EntityHistory
