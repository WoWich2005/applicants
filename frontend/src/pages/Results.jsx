import { Alert, Button, Popconfirm, Space, Typography, notification } from "antd"
import { useCallback, useEffect, useState } from "react"
import { useNavigate, generatePath } from "react-router"
import { selectionApi } from "../api/selectionApi"
import { auditApi } from "../api/auditApi"
import { ROUTES } from "../constants/routes"
import { usePermissions } from "../hooks/usePermissions"
import { useServerTable } from "../hooks/useServerTable"
import Title from "../components/Title"
import DataTable from "../components/DataTable"
import { useTranslation } from "react-i18next"

function Results() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { canRecalculate } = usePermissions()

  const [alertsSummary, setAlertsSummary] = useState({})
  const [recalcLoading, setRecalcLoading] = useState(false)

  useEffect(() => {
    auditApi.getAlertsSummary().then(r => setAlertsSummary(r.data)).catch(() => {})
  }, [])

  const fetchAsync = useCallback(
    ({ page, pageSize, filters, sortField, sortOrder }) =>
      selectionApi.getCompetitionListsPaged({
        page,
        pageSize,
        search: filters?.name?.[0] ?? undefined,
        facultySearch: filters?.facultyName?.[0] ?? undefined,
        departmentSearch: filters?.departmentName?.[0] ?? undefined,
        specialtySearch: filters?.specialtyName?.[0] ?? undefined,
        selectedCount: filters?.selectedCount?.[0] ? Number(filters.selectedCount[0]) : undefined,
        sortField: sortField ?? undefined,
        sortOrder: sortOrder ?? undefined,
      }),
    []
  )

  const { data, loading, pagination, setPage, filters, onTableChange } = useServerTable(fetchAsync)

  const handleRecalculate = async () => {
    setRecalcLoading(true)
    try {
      await Promise.all([selectionApi.recalculateAll(), new Promise(r => setTimeout(r, 500))])
      notification.success({ message: t("results.recalculate.success") })
      setPage(1)
    } catch {
      notification.error({ message: t("results.recalculate.error") })
    } finally {
      setRecalcLoading(false)
    }
  }

  const handleDownloadExcel = async (record) => {
    try {
      const response = await selectionApi.downloadCompetitionListExcel(record.id)
      const sanitize = (s) => s.replace(/ /g, '_')
      const now = new Date()
      const dateStr = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}_${String(now.getHours()).padStart(2, '0')}${String(now.getMinutes()).padStart(2, '0')}`
      const fileName = `${sanitize(record.facultyName)}.${sanitize(record.departmentName)}.${sanitize(record.specialtyName)}.${sanitize(record.name)}.${dateStr}.xlsx`
      const url = URL.createObjectURL(response.data)
      const a = document.createElement("a")
      a.href = url
      a.download = fileName
      a.click()
      URL.revokeObjectURL(url)
    } catch {
      notification.error({ message: t("results.downloadError") })
    }
  }

  const columns = [
    {
      title: t("results.colFaculty"),
      dataIndex: "facultyName",
      key: "facultyName",
      withSearch: true,
      filteredValue: filters.facultyName ?? null,
    },
    {
      title: t("results.colDepartment"),
      dataIndex: "departmentName",
      key: "departmentName",
      withSearch: true,
      filteredValue: filters.departmentName ?? null,
    },
    {
      title: t("results.colSpecialty"),
      dataIndex: "specialtyName",
      key: "specialtyName",
      withSearch: true,
      filteredValue: filters.specialtyName ?? null,
    },
    {
      title: t("results.colName"),
      dataIndex: "name",
      key: "name",
      withSearch: true,
      sorter: true,
      filteredValue: filters.name ?? null,
    },
    {
      title: t("results.colApplications"),
      dataIndex: "applicationsCount",
      key: "applicationsCount",
      width: 100,
      sorter: true,
    },
    {
      title: t("results.colPlan"),
      dataIndex: "plan",
      key: "plan",
      width: 100,
      sorter: true,
    },
    {
      title: t("results.colSelected"),
      dataIndex: "selectedCount",
      key: "selectedCount",
      width: 120,
      withSearch: true,
      sorter: true,
      filteredValue: filters.selectedCount ?? null,
    },
    {
      title: t("common.actions"),
      key: "actions",
      width: 220,
      render: (_, record) => (
        <Space>
          <Typography.Link
            onClick={() =>
              navigate(generatePath(ROUTES.COMPETITION_LIST_RESULT, { clId: record.id }))
            }
          >
            {t("common.view")}
          </Typography.Link>
          <Typography.Link onClick={() => handleDownloadExcel(record)}>
            {t("results.downloadExcel")}
          </Typography.Link>
        </Space>
      ),
    },
  ]

  return (
    <>
      {alertsSummary?.unvalidatedCount > 0 && (
        <Alert
          type="warning"
          message={t("audit.alerts.auditNotPassed", { count: alertsSummary.unvalidatedCount })}
          action={<Button size="small" onClick={() => navigate(ROUTES.AUDIT)}>{t("audit.alerts.goToAudit")}</Button>}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}
      {alertsSummary?.incompleteCount > 0 && (
        <Alert
          type="warning"
          message={t("audit.alerts.incompleteData", { count: alertsSummary.incompleteCount })}
          action={<Button size="small" onClick={() => navigate(`${ROUTES.AUDIT}?act=incomplete`)}>{t("audit.alerts.goToIncomplete")}</Button>}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}
      {alertsSummary?.invalidCriteriaGroupsCount > 0 && (
        <Alert
          type="warning"
          message={t("audit.alerts.invalidCriteriaGroups", { count: alertsSummary.invalidCriteriaGroupsCount })}
          action={<Button size="small" onClick={() => navigate(`${ROUTES.AUDIT}?act=invalidGroups`)}>{t("audit.alerts.goToInvalidGroups")}</Button>}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}
      {alertsSummary?.invalidAdmissionCategoriesCount > 0 && (
        <Alert
          type="warning"
          message={t("audit.alerts.invalidAdmissionCategories", { count: alertsSummary.invalidAdmissionCategoriesCount })}
          action={<Button size="small" onClick={() => navigate(`${ROUTES.AUDIT}?act=invalidAdmissionCategories`)}>{t("audit.alerts.goToInvalidAdmissionCategories")}</Button>}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}
      {alertsSummary?.pendingDeletionsCount > 0 && (
        <Alert
          type="warning"
          message={t("audit.alerts.pendingDeletions", { count: alertsSummary.pendingDeletionsCount })}
          action={<Button size="small" onClick={() => navigate(`${ROUTES.AUDIT}?act=applicants`)}>{t("audit.alerts.goToPendingDeletions")}</Button>}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}

      <Title title={t("results.title")} helpText={t("results.helpText")} />
      {canRecalculate && (
        <div style={{ display: "flex", justifyContent: "flex-end", marginBottom: 16 }}>
          <Popconfirm
            title={t("results.recalculate.confirmTitle")}
okText={t("results.recalculate.okText")}
            cancelText={t("common.cancel")}
            okButtonProps={{ danger: true }}
            onConfirm={handleRecalculate}
            disabled={recalcLoading}
          >
            <Button type="primary" danger loading={recalcLoading}>
              {t("results.recalculate.button")}
            </Button>
          </Popconfirm>
        </div>
      )}

      <DataTable
        dataSource={data}
        rowKey="id"
        loading={loading}
        columns={columns}
        serverSidePagination={true}
        pagination={pagination}
        onTableChange={onTableChange}
      />
    </>
  )
}

export default Results
