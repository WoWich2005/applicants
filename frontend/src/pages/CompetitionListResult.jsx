import { Breadcrumb, Descriptions, Skeleton, Tabs, Typography, notification } from "antd"
import { useCallback, useEffect, useState } from "react"
import { useParams, Link } from "react-router"
import { selectionApi } from "../api/selectionApi"
import { ROUTES } from "../constants/routes"
import Title from "../components/Title"
import DataTable from "../components/DataTable"
import { useServerTable } from "../hooks/useServerTable"
import { useTranslation } from "react-i18next"

function ApplicantTable({ clId, catId, type, criteria }) {
  const isNotAdmitted = type === "notAdmitted"
  const { t } = useTranslation()

  const fetchAsync = useCallback(
    ({ page, pageSize, filters, sortField, sortOrder }) => {
      const scoreFilters = {}
      criteria.forEach(c => {
        const val = filters?.[`score_${c.id}`]?.[0]
        if (val) scoreFilters[`score_${c.id}`] = val
      })
      return selectionApi.getCategoryApplicantsPaged({
        clId,
        catId,
        type,
        page,
        pageSize,
        id: filters?.id?.[0] ?? undefined,
        externalId: filters?.externalId?.[0] ?? undefined,
        name: filters?.name?.[0] ?? undefined,
        sortField: sortField ?? undefined,
        sortOrder: sortOrder ?? undefined,
        ...scoreFilters,
      }).then(r => ({
        ...r,
        data: {
          ...r.data,
          items: (r.data.items ?? []).map(a => ({
            ...a,
            ...Object.fromEntries(criteria.map(c => [`score_${c.id}`, a.scores?.[c.id] ?? 0]))
          }))
        }
      }))
    },
    [clId, catId, type, criteria]
  )

  const { data, loading, pagination, filters, onTableChange } = useServerTable(fetchAsync)

  const columns = [
    {
      title: "ID",
      dataIndex: "id",
      key: "id",
      width: 80,
      withSearch: true,
      sorter: true,
      filteredValue: filters.id ?? null,
    },
    {
      title: t("results.colExternalId"),
      dataIndex: "externalId",
      key: "externalId",
      width: 160,
      withSearch: true,
      sorter: true,
      filteredValue: filters.externalId ?? null,
    },
    {
      title: t("results.colApplicantName"),
      dataIndex: "name",
      key: "name",
      withSearch: true,
      sorter: true,
      filteredValue: filters.name ?? null,
    },
    ...(isNotAdmitted ? [{
      title: t("results.colAdmittedTo"),
      dataIndex: "admittedTo",
      key: "admittedTo",
      render: (val) => val ?? t("results.notAdmittedAnywhere"),
    }] : []),
    ...criteria.map(c => ({
      title: c.name,
      dataIndex: `score_${c.id}`,
      key: `score_${c.id}`,
      width: 120,
      withSearch: true,
      sorter: true,
      filteredValue: filters[`score_${c.id}`] ?? null,
    })),
  ]

  return (
    <DataTable
      dataSource={data}
      rowKey="id"
      loading={loading}
      columns={columns}
      serverSidePagination={true}
      pagination={pagination}
      onTableChange={onTableChange}
      size="small"
    />
  )
}

function CategoryTab({ clId, category }) {
  const { t } = useTranslation()

  return (
    <div>
      {category.minScores && Object.keys(category.minScores).length > 0 && (
        <Descriptions
          title={t("results.minScores")}
          size="small"
          column={1}
          style={{ marginBottom: 16 }}
          items={category.criteria.map(c => ({
            key: String(c.id),
            label: c.name,
            children: category.minScores[c.id] ?? 0,
          }))}
        />
      )}

      <Typography.Title level={5} style={{ marginBottom: 8 }}>
        {t("results.admittedSection")} ({category.admittedCount}/{category.quota})
      </Typography.Title>
      <ApplicantTable clId={clId} catId={category.id} type="admitted" criteria={category.criteria} />

      <Typography.Title level={5} style={{ marginTop: 24, marginBottom: 8 }}>
        {t("results.notAdmittedSection")}
      </Typography.Title>
      <ApplicantTable clId={clId} catId={category.id} type="notAdmitted" criteria={category.criteria} />
    </div>
  )
}

function CompetitionListResult() {
  const { clId } = useParams()
  const { t } = useTranslation()

  const [header, setHeader] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    selectionApi.getCompetitionListHeader(clId)
      .then(r => setHeader(r.data))
      .catch(() => notification.error({ message: t("common.error.fetchData") }))
      .finally(() => setLoading(false))
  }, [clId, t])

  if (loading) return <Skeleton paragraph={{ rows: 12 }} />
  if (!header) return null

  const tabItems = header.categories.map(cat => ({
    key: String(cat.id),
    label: `${cat.name} (${cat.admittedCount}/${cat.quota})`,
    children: <CategoryTab clId={clId} category={cat} />,
  }))

  return (
    <>
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <Link to={ROUTES.RESULTS}>{t("results.title")}</Link> },
          { title: `${header.facultyName} / ${header.departmentName} / ${header.specialtyName} / ${header.name}` },
        ]}
      />

      <Title title={header.name} />

      <Typography.Text type="secondary">
        {header.facultyName} / {header.departmentName} / {header.specialtyName}
      </Typography.Text>
      <br />
      <Typography.Text>
        {t("results.plan")}: <strong>{header.plan}</strong>
      </Typography.Text>
      <br />

      {tabItems.length > 0 ? (
        <Tabs items={tabItems} />
      ) : (
        <Typography.Text type="secondary">{t("results.noCategories")}</Typography.Text>
      )}
    </>
  )
}

export default CompetitionListResult
