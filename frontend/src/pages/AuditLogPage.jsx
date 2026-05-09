import { useCallback, useState } from 'react'
import { Button, DatePicker, Modal, Space } from 'antd'
import { CalendarOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import Title from '../components/Title'
import DataTable from '../components/DataTable'
import { auditApi } from '../api/auditApi'
import { useTranslation } from 'react-i18next'
import { useServerTable } from '../hooks/useServerTable'

const { RangePicker } = DatePicker

const ENTITY_TYPES = ['applicant', 'applicant_evaluation_value', 'applicant_admission_category', 'faculty', 'department', 'specialty', 'competition_list', 'admission_category', 'evaluation_criteria', 'evaluation_criteria_group', 'evaluation_criteria_group_item', 'user']
const ACTIONS = ['create', 'update', 'delete', 'validate', 'invalidate', 'delete_confirmed', 'delete_rejected']

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
        <Button
          type="primary"
          onClick={() => confirm()}
          size="small"
          style={{ width: 90 }}
        >
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

function AuditLogPage() {
  const { t } = useTranslation()
  const [changesModalOpen, setChangesModalOpen] = useState(false)
  const [selectedChanges, setSelectedChanges] = useState(null)

  const fetchAsync = useCallback(
    (/** @type {{ page: number, pageSize: number, filters: Record<string, any[]> }} */ { page, pageSize, filters }) => {
      const dateRaw = filters?.createdAt?.[0]
      const parsed = dateRaw ? JSON.parse(dateRaw) : null
      return auditApi.getAuditLog({
        page, pageSize, filters,
        from: parsed?.[0], to: parsed?.[1],
      })
    },
    []
  )

  const { data, loading, pagination, onTableChange } = useServerTable(fetchAsync, { defaultPageSize: 20 })

  const handleShowChanges = (entry) => {
    if (!entry.changes) return
    try {
      setSelectedChanges(JSON.stringify(JSON.parse(entry.changes), null, 2))
    } catch {
      setSelectedChanges(entry.changes)
    }
    setChangesModalOpen(true)
  }

  const columns = [
    {
      title: t('audit.log.columns.date'),
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      filterDropdown: (props) => <DateRangeFilter {...props} t={t} />,
      filterIcon: (filtered) => <CalendarOutlined style={{ color: filtered ? '#1677ff' : undefined }} />,
      render: (v) => v ? dayjs(v).format('DD.MM.YYYY HH:mm') : '—',
    },
    {
      title: t('audit.log.columns.user'),
      dataIndex: 'username',
      key: 'username',
      withSearch: true,
    },
    {
      title: t('audit.log.columns.action'),
      dataIndex: 'action',
      key: 'action',
      filters: ACTIONS.map(v => ({ text: t(`audit.actions.${v}`), value: v })),
      filterMultiple: false,
      render: (v) => t(`audit.actions.${v}`, v),
    },
    {
      title: t('audit.log.columns.entity'),
      dataIndex: 'entityType',
      key: 'entityType',
      filters: ENTITY_TYPES.map(v => ({ text: t(`audit.entityTypes.${v}`, v), value: v })),
      filterMultiple: false,
      render: (v) => t(`audit.entityTypes.${v}`, v),
    },
    {
      title: t('audit.log.columns.entityId'),
      dataIndex: 'entityId',
      key: 'entityId',
      width: 100,
      withSearch: true,
    },
    {
      title: t('audit.log.columns.changes'),
      key: 'changes',
      render: (_, entry) =>
        entry.changes ? (
          <Button type="link" size="small" onClick={() => handleShowChanges(entry)}>
            {t('common.view')}
          </Button>
        ) : '—',
    },
  ]

  return (
    <>
      <Title title={t('audit.log.title')} />

      <DataTable
        dataSource={data}
        columns={columns}
        rowKey={(r, i) => r.id ?? i}
        loading={loading}
        serverSidePagination={true}
        onTableChange={onTableChange}
        pagination={pagination}
      />

      <Modal
        open={changesModalOpen}
        title={t('audit.log.columns.changes')}
        footer={<Button onClick={() => setChangesModalOpen(false)}>{t('common.close')}</Button>}
        onCancel={() => setChangesModalOpen(false)}
        width={700}
      >
        <pre style={{ maxHeight: 500, overflow: 'auto', fontSize: 12 }}>{selectedChanges}</pre>
      </Modal>
    </>
  )
}

export default AuditLogPage
