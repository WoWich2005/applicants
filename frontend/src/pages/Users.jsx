import { useCallback, useEffect, useState } from 'react'
import { Button, DatePicker, Input, Space, Tabs, Tag, Typography, message } from 'antd'
import { CalendarOutlined, SearchOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import { useSearchParams } from 'react-router'
import Title from '../components/Title'
import CrudTable from '../components/CrudTable'
import DataTable from '../components/DataTable'
import UserForm from '../components/Forms/UserForm'
import { usersApi } from '../api/usersApi'
import { facultiesApi } from '../api/facultyApi'
import { specialtiesApi } from '../api/specialtiesApi'
import { departmentsApi } from '../api/departmentsApi'
import { auditApi } from '../api/auditApi'
import { useTranslation } from 'react-i18next'
import { useServerTable } from '../hooks/useServerTable'

const { RangePicker } = DatePicker

const ROLE_VALUES = ['SuperAdmin', 'FacultyManager', 'AdmissionsOperator', 'DataViewer']

const ROLE_COLORS = {
  SuperAdmin: 'red',
  FacultyManager: 'blue',
  AdmissionsOperator: 'green',
  DataViewer: 'orange',
}

const EVENT_TYPES = ['login_success', 'login_failure']
const FAILURE_REASONS = ['user_not_found', 'wrong_password', 'inactive']

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
        <Button onClick={() => clearFilters && clearFilters({ confirm: true, closeDropdown: true })} size="small" style={{ width: 90 }}>
          {t('dataTable.reset')}
        </Button>
        <Button type="link" size="small" onClick={() => close()}>
          {t('dataTable.close')}
        </Button>
      </Space>
    </div>
  )
}

function TextSearchFilter({ setSelectedKeys, selectedKeys, confirm, clearFilters, close, t }) {
  return (
    <div style={{ padding: 8 }} onKeyDown={e => e.stopPropagation()}>
      <Input
        placeholder={t('dataTable.searchPlaceholder')}
        value={selectedKeys[0]}
        onChange={e => setSelectedKeys(e.target.value ? [e.target.value] : [])}
        onPressEnter={() => confirm()}
        style={{ marginBottom: 8, display: 'block' }}
      />
      <Space>
        <Button type="primary" onClick={() => confirm()} icon={<SearchOutlined />} size="small" style={{ width: 90 }}>
          {t('dataTable.find')}
        </Button>
        <Button onClick={() => clearFilters && clearFilters({ confirm: true, closeDropdown: true })} size="small" style={{ width: 90 }}>
          {t('dataTable.reset')}
        </Button>
        <Button type="link" size="small" onClick={() => close()}>
          {t('dataTable.close')}
        </Button>
      </Space>
    </div>
  )
}

function AuthLogTab() {
  const { t } = useTranslation()

  const fetchAsync = useCallback(({ page, pageSize, filters }) => {
    const dateRaw = filters?.createdAt?.[0]
    const parsed = dateRaw ? JSON.parse(dateRaw) : null
    return auditApi.getAuthLog({ page, pageSize, filters, from: parsed?.[0], to: parsed?.[1] })
  }, [])

  const { data, loading, pagination, onTableChange } = useServerTable(fetchAsync, { defaultPageSize: 20 })

  const columns = [
    {
      title: t('audit.authLog.columns.date'),
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      filterDropdown: (props) => <DateRangeFilter {...props} t={t} />,
      filterIcon: (filtered) => <CalendarOutlined style={{ color: filtered ? '#1677ff' : undefined }} />,
      render: (v) => v ? dayjs(v).format('DD.MM.YYYY HH:mm') : '—',
    },
    {
      title: t('audit.authLog.columns.userId'),
      dataIndex: 'userId',
      key: 'userId',
      width: 120,
      withSearch: true,
      render: (v) => v ?? '—',
    },
    {
      title: t('audit.authLog.columns.username'),
      dataIndex: 'username',
      key: 'username',
      withSearch: true,
    },
    {
      title: t('audit.authLog.columns.event'),
      dataIndex: 'eventType',
      key: 'eventType',
      filters: EVENT_TYPES.map(v => ({ text: t(`audit.authLog.events.${v}`, v), value: v })),
      filterMultiple: false,
      render: (v) => t(`audit.authLog.events.${v}`, v),
    },
    {
      title: t('audit.authLog.columns.reason'),
      dataIndex: 'failureReason',
      key: 'failureReason',
      filters: FAILURE_REASONS.map(v => ({ text: t(`audit.authLog.failureReasons.${v}`, v), value: v })),
      filterMultiple: false,
      render: (v) => v ? t(`audit.authLog.failureReasons.${v}`, v) : '—',
    },
    {
      title: t('audit.authLog.columns.ip'),
      dataIndex: 'ipAddress',
      key: 'ipAddress',
      withSearch: true,
    },
    {
      title: t('audit.authLog.columns.userAgent'),
      dataIndex: 'userAgent',
      key: 'userAgent',
      filterDropdown: (props) => <TextSearchFilter {...props} t={t} />,
      filterIcon: (filtered) => <SearchOutlined style={{ color: filtered ? '#1677ff' : undefined }} />,
      render: (v) => v
        ? <Typography.Text ellipsis={{ tooltip: v }} style={{ maxWidth: 300 }}>{v}</Typography.Text>
        : '—',
    },
  ]

  return (
    <DataTable
      dataSource={data}
      columns={columns}
      rowKey={(r, i) => r.id ?? i}
      loading={loading}
      serverSidePagination={true}
      onTableChange={onTableChange}
      pagination={pagination}
    />
  )
}

function Users() {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [searchParams, setSearchParams] = useSearchParams()

  const activeTab = searchParams.get('act') ?? 'users'

  const onTabChange = (/** @type {string} */ key) => {
    searchParams.set('act', key)
    setSearchParams(searchParams)
  }
  const [faculties, setFaculties] = useState([])
  const [specialties, setSpecialties] = useState([])
  const [departments, setDepartments] = useState([])

  useEffect(() => {
    facultiesApi.getAll().then(r => setFaculties(r.data)).catch(() => {})
    specialtiesApi.getAll().then(r => setSpecialties(r.data)).catch(() => {})
    departmentsApi.getAll().then(r => setDepartments(r.data)).catch(() => {})
  }, [])

  const handleToggleActive = async (/** @type {any} */ user, /** @type {any} */ updateEl) => {
    try {
      await usersApi.toggleActive(user.id)
      const updated = (await usersApi.getById(user.id)).data
      updateEl(updated)
      messageApi.success(user.isActive ? t('users.deactivated') : t('users.activated'))
    } catch {
      messageApi.error(t('users.statusError'))
    }
  }

  const ROLES = ROLE_VALUES.map(value => ({ value, label: t(`users.roles.${value}`) }))

  const usersTableContent = (
    <CrudTable
      elementForm={UserForm}
      elementFormProps={{ faculties, specialties, departments }}

      serverSidePagination={true}
      getPagedAsync={(/** @type {any} */ params) => usersApi.getPaged({
        page: params.page,
        pageSize: params.pageSize,
        search: params.filters?.username?.[0],
        role: params.filters?.role?.[0],
        isActive: params.filters?.isActive?.[0],
        idSearch: params.filters?.id?.[0],
      })}
      deleteAsync={(/** @type {any} */ id) => usersApi.delete(id)}

      addButtonTitle={t('users.addButton')}
      renderEditTitle={(/** @type {any} */ el) => t('users.editTitle', { username: el?.username })}
      renderDeleteText={(/** @type {any} */ el) => t('users.deleteText', { username: el?.username })}

      extraActions={(/** @type {any} */ user, /** @type {any} */ updateEl) => (
        <Button type="link" onClick={() => handleToggleActive(user, updateEl)}>
          {user.isActive ? t('users.deactivate') : t('users.activate')}
        </Button>
      )}

      columns={[
        {
          title: t('common.colId'),
          dataIndex: 'id',
          key: 'id',
          withSearch: true,
          sorter: false,
        },
        {
          title: t('users.colLogin'),
          dataIndex: 'username',
          key: 'username',
          withSearch: true,
        },
        {
          title: t('users.colRole'),
          dataIndex: 'role',
          key: 'role',
          filters: ROLES.map(r => ({ text: r.label, value: r.value })),
          filterMultiple: false,
          render: (/** @type {keyof typeof ROLE_COLORS} */ role) => (
            <Tag color={ROLE_COLORS[role]}>{t(`users.roles.${role}`, role)}</Tag>
          ),
        },
        {
          title: t('users.colStatus'),
          dataIndex: 'isActive',
          key: 'isActive',
          filters: [
            { text: t('users.statusActive'), value: true },
            { text: t('users.statusInactive'), value: false },
          ],
          filterMultiple: false,
          render: (/** @type {boolean} */ active) => (
            <Tag color={active ? 'success' : 'default'}>
              {active ? t('users.statusActive') : t('users.statusInactive')}
            </Tag>
          ),
        },
      ]}
    />
  )

  const tabItems = [
    {
      key: 'users',
      label: t('users.title'),
      children: usersTableContent,
    },
    {
      key: 'authLog',
      label: t('audit.authLog.title'),
      children: <AuthLogTab />,
    },
  ]

  return (
    <>
      {contextHolder}
      <Title title={t('users.title')} />
      <Tabs activeKey={activeTab} onChange={onTabChange} items={tabItems} />
    </>
  )
}

export default Users
