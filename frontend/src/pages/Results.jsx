import { Alert, Button, message, Space, Table, Typography } from "antd"
import Title from "../components/Title"
import { useEffect, useState } from "react"
import { useNavigate } from "react-router"
import { facultiesApi } from "../api/facultyApi"
import { auditApi } from "../api/auditApi"
import { ROUTES } from "../constants/routes"
import { useTranslation } from "react-i18next"
import { useAuth } from "../contexts/AuthContext"

function Results() {
  const { t } = useTranslation()
  const { auth } = useAuth()
  const navigate = useNavigate()
  const [messageApi, contextHolder] = message.useMessage()

  const [dataSource, setDataSource] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [alertsSummary, setAlertsSummary] = useState({ unvalidatedCount: 0, missingCriteriaCount: 0, invalidPrioritiesCount: 0, incompleteCount: 0, invalidCriteriaGroupsCount: 0, invalidAdmissionCategoriesCount: 0 })

  useEffect(() => {
    auditApi.getAlertsSummary()
      .then(r => setAlertsSummary(r.data))
      .catch(() => {})
  }, [])

  useEffect(() => {
    const fetchData = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, facultiesApi.getAll()])
        setDataSource(response.data)
      } catch (err) {
        messageApi.error(t('results.error'))
        console.log(err)
      } finally {
        setIsLoading(false)
      }
    }

    fetchData()
  }, [])

  const columns = [
    {
      title: t('results.colFaculty'),
      dataIndex: "name",
      key: "name",
    },
    {
      title: t('results.colActions'),
      dataIndex: "control",
      key: "control",
      width: "250px",
      render: (_, el) => {
        return (
          <Space>
            <Typography.Link
              onClick={() => window.open(`http://localhost:5059/get_results/${el.id}`, '_blank', 'noopener,noreferrer')}
            >
              {t('results.downloadExcel')}
            </Typography.Link>
          </Space>
        )
      },
    }
  ]

  return (
    <>
      {contextHolder}

      {alertsSummary?.unvalidatedCount > 0 && (
        <Alert
          type="warning"
          message={t('audit.alerts.auditNotPassed', { count: alertsSummary.unvalidatedCount })}
          action={<Button size="small" onClick={() => navigate(ROUTES.AUDIT)}>{t('audit.alerts.goToAudit')}</Button>}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}

      {alertsSummary?.incompleteCount > 0 && (
        <Alert
          type="warning"
          message={t('audit.alerts.incompleteData', { count: alertsSummary.incompleteCount })}
          action={<Button size="small" onClick={() => navigate(`${ROUTES.AUDIT}?act=incomplete`)}>{t('audit.alerts.goToIncomplete')}</Button>}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}

      {alertsSummary.invalidCriteriaGroupsCount > 0 && (
        <Alert
          type="warning"
          message={t('audit.alerts.invalidCriteriaGroups', { count: alertsSummary.invalidCriteriaGroupsCount })}
          action={<Button size="small" onClick={() => navigate(`${ROUTES.AUDIT}?act=invalidGroups`)}>{t('audit.alerts.goToInvalidGroups')}</Button>}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}

      {alertsSummary.invalidAdmissionCategoriesCount > 0 && (
        <Alert
          type="warning"
          message={t('audit.alerts.invalidAdmissionCategories', { count: alertsSummary.invalidAdmissionCategoriesCount })}
          action={<Button size="small" onClick={() => navigate(`${ROUTES.AUDIT}?act=invalidAdmissionCategories`)}>{t('audit.alerts.goToInvalidAdmissionCategories')}</Button>}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}

      {auth?.role === 'SuperAdmin' && alertsSummary.pendingDeletionsCount > 0 && (
        <Alert
          type="warning"
          message={t('audit.alerts.pendingDeletions', { count: alertsSummary.pendingDeletionsCount })}
          action={<Button size="small" onClick={() => navigate(`${ROUTES.AUDIT}?act=applicants`)}>{t('audit.alerts.goToPendingDeletions')}</Button>}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}

      <Title
        title={t('results.title')}
        helpText={t('results.helpText')}
      />

      <Table
        dataSource={dataSource}
        columns={columns}
        rowKey="id"
        loading={isLoading}
      />
    </>
  )
}

export default Results
