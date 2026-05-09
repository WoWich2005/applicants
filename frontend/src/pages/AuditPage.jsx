import { useCallback, useState } from 'react'
import { Button, Tag, Tabs, Modal, message, Divider, Typography } from 'antd'
import { CheckOutlined, CloseOutlined } from '@ant-design/icons'
import { Link, useSearchParams } from 'react-router'
import Title from '../components/Title'
import DataTable from '../components/DataTable'
import ApplicantValidationModal from '../components/Modals/ApplicantValidationModal'
import { auditApi } from '../api/auditApi'
import { ROUTES } from '../constants/routes'
import { useTranslation } from 'react-i18next'
import { useServerTable } from '../hooks/useServerTable'
import { useAuth } from '../contexts/AuthContext'

function ApplicantsAuditTab() {
  const { t } = useTranslation()
  const { auth } = useAuth()
  const [modalOpen, setModalOpen] = useState(false)
  const [selectedApplicant, setSelectedApplicant] = useState(null)

  const fetchAsync = useCallback(
    ({ page, pageSize, filters }) =>
      auditApi.getUnvalidated({ page, pageSize, filters }),
    []
  )

  const { data, loading, pagination, onTableChange, setFilters } = useServerTable(fetchAsync, { defaultPageSize: 20 })

  const handleOpenModal = (applicant) => {
    setSelectedApplicant(applicant)
    setModalOpen(true)
  }

  const handleModalSuccess = () => {
    setFilters(f => ({ ...f }))
  }

  const columns = [
    {
      title: t('audit.unvalidated.columns.externalId'),
      dataIndex: 'externalId',
      key: 'externalId',
      width: 120,
      withSearch: true,
    },
    {
      title: t('audit.unvalidated.columns.name'),
      dataIndex: 'name',
      key: 'name',
      withSearch: true,
    },
    {
      title: t('audit.unvalidated.columns.status'),
      dataIndex: 'validated',
      key: 'validated',
      width: 160,
      filters: [
        { text: t('audit.validation.validated'), value: 'validated' },
        { text: t('audit.validation.notValidated'), value: 'unvalidated' },
      ],
      filterMultiple: false,
      render: (v) => v
        ? <Tag color="success">{t('audit.validation.validated')}</Tag>
        : <Tag color="error">{t('audit.validation.notValidated')}</Tag>,
    },
    {
      title: t('audit.unvalidated.columns.action'),
      key: 'action',
      width: 180,
      render: (_, row) => (
        <Button type="link" size="small" onClick={() => handleOpenModal(row)}>
          {row.validated ? t('audit.validation.invalidate') : t('audit.validation.validate')}
        </Button>
      ),
    },
  ]

  return (
    <>
      <Divider orientation="left">
        <Typography.Text strong>{t('audit.unvalidated.sectionTitle')}</Typography.Text>
      </Divider>
      <DataTable
        dataSource={data}
        columns={columns}
        rowKey={(r, i) => r.id ?? i}
        loading={loading}
        serverSidePagination={true}
        onTableChange={onTableChange}
        pagination={pagination}
      />

      {selectedApplicant && (
        <ApplicantValidationModal
          applicantId={selectedApplicant.id}
          applicantName={selectedApplicant.name}
          open={modalOpen}
          onClose={() => setModalOpen(false)}
          onSuccess={handleModalSuccess}
        />
      )}

      {auth?.role === 'SuperAdmin' && (
        <>
          <Divider orientation="left">
            <Typography.Text strong>{t('audit.pendingDeletions.sectionTitle')}</Typography.Text>
          </Divider>
          <PendingDeletionsSection onApplicantRestored={() => setFilters(f => ({ ...f }))} />
        </>
      )}
    </>
  )
}

function IncompleteTab() {
  const { t } = useTranslation()

  const fetchAsync = useCallback(
    (/** @type {any} */ params) => auditApi.getIncomplete(params),
    []
  )

  const { data, loading, pagination, onTableChange } = useServerTable(fetchAsync, { defaultPageSize: 20 })

  const columns = [
    {
      title: t('audit.incomplete.columns.externalId'),
      dataIndex: 'externalId',
      key: 'externalId',
      width: 120,
      withSearch: true,
    },
    {
      title: t('audit.incomplete.columns.name'),
      dataIndex: 'name',
      key: 'name',
      withSearch: true,
    },
    {
      title: t('audit.incomplete.columns.issues'),
      key: 'issues',
      render: (_, row) => {
        const issues = []
        if (row.missingCriteria?.length > 0) {
          issues.push(t('audit.incomplete.missingCriteria', { criteria: row.missingCriteria.join(', ') }))
        }
        if (row.hasInvalidPriorities) {
          issues.push(t('audit.incomplete.invalidPriorities'))
        }
        return <ul style={{ margin: 0, paddingLeft: 16 }}>{issues.map((iss, i) => <li key={i}>{iss}</li>)}</ul>
      },
    },
    {
      title: t('audit.incomplete.columns.action'),
      key: 'action',
      width: 120,
      render: (_, row) => (
        <Link to={ROUTES.APPLICANT_EDIT.replace(':applicantId', row.id)}>
          {t('audit.incomplete.goToEdit')}
        </Link>
      ),
    },
  ]

  return (
    <DataTable
      dataSource={data}
      columns={columns}
      rowKey={(r, i) => r.id ?? i}
      loading={loading}
      serverSidePagination={true}
      pagination={pagination}
      onTableChange={onTableChange}
    />
  )
}

function InvalidCriteriaGroupsTab() {
  const { t } = useTranslation()

  const fetchAsync = useCallback(
    (/** @type {any} */ params) => auditApi.getInvalidCriteriaGroups(params),
    []
  )

  const { data, loading, pagination, onTableChange } = useServerTable(fetchAsync, { defaultPageSize: 20 })

  const columns = [
    {
      title: t('audit.invalidGroups.columns.name'),
      dataIndex: 'name',
      key: 'name',
      withSearch: true,
    },
    {
      title: t('audit.invalidGroups.columns.action'),
      key: 'action',
      width: 120,
      render: (_, row) => (
        <Link to={ROUTES.EVALUATION_CRITERIA_GROUP_EDIT.replace(':groupId', row.id) + '?act=criteria'}>
          {t('audit.invalidGroups.goToEdit')}
        </Link>
      ),
    },
  ]

  return (
    <DataTable
      dataSource={data}
      columns={columns}
      rowKey={(r, i) => r.id ?? i}
      loading={loading}
      serverSidePagination={true}
      pagination={pagination}
      onTableChange={onTableChange}
    />
  )
}

function InvalidAdmissionCategoriesTab() {
  const { t } = useTranslation()

  const fetchAsync = useCallback(
    (/** @type {any} */ params) => auditApi.getInvalidAdmissionCategories(params),
    []
  )

  const { data, loading, pagination, onTableChange } = useServerTable(fetchAsync, { defaultPageSize: 20 })

  const columns = [
    {
      title: t('audit.invalidAdmissionCategories.columns.facultyName'),
      dataIndex: 'facultyName',
      key: 'facultyName',
      withSearch: true,
    },
    {
      title: t('audit.invalidAdmissionCategories.columns.departmentName'),
      dataIndex: 'departmentName',
      key: 'departmentName',
      withSearch: true,
    },
    {
      title: t('audit.invalidAdmissionCategories.columns.specialtyName'),
      dataIndex: 'specialtyName',
      key: 'specialtyName',
      withSearch: true,
    },
    {
      title: t('audit.invalidAdmissionCategories.columns.name'),
      dataIndex: 'name',
      key: 'name',
      withSearch: true,
    },
    {
      title: t('audit.invalidAdmissionCategories.columns.action'),
      key: 'action',
      width: 120,
      render: (/** @type {any} */ _, /** @type {any} */ row) => (
        <Link to={ROUTES.COMPETITION_LIST_EDIT.replace(':listId', row.id) + '?act=categories'}>
          {t('audit.invalidAdmissionCategories.goToEdit')}
        </Link>
      ),
    },
  ]

  return (
    <DataTable
      dataSource={data}
      columns={columns}
      rowKey={(/** @type {any} */ r, /** @type {any} */ i) => r.id ?? i}
      loading={loading}
      serverSidePagination={true}
      pagination={pagination}
      onTableChange={onTableChange}
    />
  )
}

function PendingDeletionsSection({ onApplicantRestored }) {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [actionModal, setActionModal] = useState(null)
  const [submitting, setSubmitting] = useState(false)

  const fetchAsync = useCallback(
    (params) => auditApi.getPendingDeletions(params),
    []
  )

  const { data, loading, pagination, onTableChange, setFilters } = useServerTable(fetchAsync, { defaultPageSize: 20 })

  const openModal = (applicant, type) => {
    setActionModal({ applicant, type })
  }

  const handleSubmit = async () => {
    if (!actionModal) return
    setSubmitting(true)
    try {
      const { applicant, type } = actionModal
      if (type === 'confirm') {
        await auditApi.confirmDeletion(applicant.applicantId)
        messageApi.success(t('audit.pendingDeletions.confirmedSuccess'))
      } else {
        await auditApi.rejectDeletion(applicant.applicantId)
        messageApi.success(t('audit.pendingDeletions.rejectedSuccess'))
        onApplicantRestored?.()
      }
      setActionModal(null)
      setFilters(f => ({ ...f }))
    } catch (err) {
      const serverMessage = /** @type {any} */ (err)?.response?.data?.message
      messageApi.error(serverMessage ?? t('common.error.deleteServer'))
    } finally {
      setSubmitting(false)
    }
  }

  const columns = [
    {
      title: t('audit.pendingDeletions.columns.id'),
      dataIndex: 'applicantId',
      key: 'applicantId',
      width: 80,
      withSearch: true,
    },
    {
      title: t('audit.pendingDeletions.columns.externalId'),
      dataIndex: 'applicantExternalId',
      key: 'externalId',
      width: 120,
      withSearch: true,
    },
    {
      title: t('audit.pendingDeletions.columns.name'),
      dataIndex: 'applicantName',
      key: 'name',
      withSearch: true,
    },
    {
      title: t('audit.pendingDeletions.columns.requestedBy'),
      dataIndex: 'requestedByUsername',
      key: 'requestedByUsername',
      width: 160,
      withSearch: true,
    },
    {
      title: t('audit.pendingDeletions.columns.status'),
      dataIndex: 'status',
      key: 'status',
      width: 160,
      filters: [
        { text: t('audit.pendingDeletions.statusPending'), value: 'pending' },
        { text: t('audit.pendingDeletions.statusConfirmed'), value: 'confirmed' },
      ],
      filterMultiple: false,
      render: (v) => v === 'confirmed'
        ? <Tag color="success">{t('audit.pendingDeletions.statusConfirmed')}</Tag>
        : <Tag color="error">{t('audit.pendingDeletions.statusPending')}</Tag>,
    },
    {
      title: t('audit.pendingDeletions.columns.actions'),
      key: 'actions',
      width: 120,
      render: (_, row) => (
        <span>
          {row.status === 'pending' && (
            <Button
              type="link"
              size="small"
              icon={<CheckOutlined />}
              style={{ color: '#52c41a' }}
              onClick={() => openModal(row, 'confirm')}
            />
          )}
          <Button
            type="link"
            size="small"
            icon={<CloseOutlined />}
            danger
            title={t(row.status === 'confirmed' ? 'audit.pendingDeletions.restoreAction' : 'audit.pendingDeletions.rejectAction')}
            onClick={() => openModal(row, 'reject')}
          />
        </span>
      ),
    },
  ]

  const isConfirm = actionModal?.type === 'confirm'
  const isRestore = actionModal?.type === 'reject' && actionModal?.applicant?.status === 'confirmed'

  return (
    <>
      {contextHolder}
      <DataTable
        dataSource={data}
        columns={columns}
        rowKey={(r) => r.applicantId}
        loading={loading}
        serverSidePagination={true}
        onTableChange={onTableChange}
        pagination={pagination}
        locale={{ emptyText: t('audit.pendingDeletions.empty') }}
      />

      <Modal
        open={!!actionModal}
        title={isConfirm
          ? t('audit.pendingDeletions.confirmTitle')
          : isRestore
            ? t('audit.pendingDeletions.restoreTitle')
            : t('audit.pendingDeletions.rejectTitle')}
        okText={isConfirm
          ? t('audit.pendingDeletions.confirmOk')
          : t('audit.pendingDeletions.restoreOk')}
        okButtonProps={{ danger: isConfirm, loading: submitting }}
        cancelText={t('common.cancel')}
        onOk={handleSubmit}
        onCancel={() => setActionModal(null)}
      >
        <p>
          {isConfirm
            ? t('audit.pendingDeletions.confirmText', { name: actionModal?.applicant?.applicantName })
            : isRestore
              ? t('audit.pendingDeletions.restoreText', { name: actionModal?.applicant?.applicantName })
              : t('audit.pendingDeletions.rejectText', { name: actionModal?.applicant?.applicantName })}
        </p>
      </Modal>
    </>
  )
}

function AuditPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()

  const activeTab = searchParams.get('act') ?? 'applicants'

  const onTabChange = (/** @type {string} */ key) => {
    searchParams.set('act', key)
    setSearchParams(searchParams)
  }

  const tabs = [
    {
      key: 'applicants',
      label: t('audit.tabs.applicants'),
      children: <ApplicantsAuditTab />,
    },
    {
      key: 'incomplete',
      label: t('audit.tabs.incomplete'),
      children: <IncompleteTab />,
    },
    {
      key: 'invalidGroups',
      label: t('audit.tabs.invalidGroups'),
      children: <InvalidCriteriaGroupsTab />,
    },
    {
      key: 'invalidAdmissionCategories',
      label: t('audit.tabs.invalidAdmissionCategories'),
      children: <InvalidAdmissionCategoriesTab />,
    },
  ]

  return (
    <>
      <Title title={t('audit.nav')} />
      <Tabs activeKey={activeTab} onChange={onTabChange} items={tabs} />
    </>
  )
}

export default AuditPage
