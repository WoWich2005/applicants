import { Breadcrumb, List, Result, Skeleton, Tabs, Typography } from "antd"
import Title from "../components/Title"
import { useParams, useSearchParams, generatePath, Link } from "react-router"
import { ROUTES } from "../constants/routes"
import { useEffect, useState } from "react"
import useMessage from "antd/es/message/useMessage"
import CrudTable from "../components/CrudTable"
import CompetitionListForm from "../components/Forms/CompetitionListForm"
import AdmissionCategoryForm from "../components/Forms/AdmissionCategoryForm"
import { competitionListsApi } from "../api/competitionListsApi"
import { admissionCategoriesApi } from "../api/admissionCategoriesApi"
import { applicantAdmissionCategoriesApi } from "../api/applicantAdmissionCategoriesApi"
import { evaluationCriteriaGroupsApi } from "../api/evaluationCriteriaGroupsApi"
import { specialtiesApi } from "../api/specialtiesApi"
import { useAuth } from "../contexts/AuthContext"
import { useTranslation } from "react-i18next"

function CompetitionListEdit() {
  const { auth } = useAuth()
  const { t } = useTranslation()
  const readOnly = auth?.role === 'DataViewer'
  const [messageApi, contextHolder] = useMessage()
  const [searchParams, setSearchParams] = useSearchParams()
  const { listId } = useParams()

  const [groupsDict, setGroupsDict] = useState({})
  const [isLoading, setIsLoading] = useState(true)
  const [responseStatus, setResponseStatus] = useState(null)
  const [list, setList] = useState({ id: null, name: null, plan: null, specialtyId: null })
  const [specialtyName, setSpecialtyName] = useState(null)

  useEffect(() => {
    const fetchGroups = async () => {
      try {
        const response = await evaluationCriteriaGroupsApi.getAll()
        setGroupsDict(response.data.reduce((acc, g) => {
          acc[g.id] = g
          return acc
        }, {}))
      } catch {
        messageApi.error(t('competitionList.edit.fetchError'))
      }
    }

    fetchGroups()
  }, [])

  useEffect(() => {
    const fetchList = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, competitionListsApi.getById(listId)])

        setResponseStatus(response.status)
        setList(response.data)
      } catch (err) {
        if (err.response === undefined) {
          setResponseStatus(500)
        } else {
          setResponseStatus(err.response.status)
        }
      } finally {
        setIsLoading(false)
      }
    }

    fetchList()
  }, [listId])

  useEffect(() => {
    if (!list.specialtyId) return
    specialtiesApi.getById(list.specialtyId)
      .then(r => setSpecialtyName(r.data.name))
      .catch(() => {})
  }, [list.specialtyId])

  if (isLoading) {
    return <Skeleton paragraph={{ rows: 12 }} />
  }

  if (responseStatus === 404) {
    return (
      <Result
        status="404"
        title="404"
        subTitle={t('competitionList.edit.notFound')}
      />
    )
  } else if (responseStatus === 500) {
    return (
      <Result
        status="500"
        title="500"
        subTitle={t('competitionList.edit.serverError')}
      />
    )
  }

  const onTabChange = (key) => {
    searchParams.set("act", key)
    setSearchParams(searchParams)
  }

  const getDeleteBlockers = async (category) => {
    const response = await applicantAdmissionCategoriesApi.getApplicantsByCategory(category.id)
    const applicants = response.data
    if (applicants.length === 0) return null
    return { applicants: applicants.slice(0, 5), totalCount: applicants.length }
  }

  const renderDeleteBlockersContent = (category, blockers) => (
    <>
      <Typography.Paragraph>
        {t('admissionCategory.deleteBlocked', { name: category?.name })}
      </Typography.Paragraph>
      <List
        size="small"
        dataSource={blockers.applicants}
        renderItem={(applicant) => <List.Item>{applicant.name}</List.Item>}
        footer={blockers.totalCount > 5
          ? <Typography.Text type="secondary">{t('admissionCategory.andMoreApplicants', { count: blockers.totalCount - 5 })}</Typography.Text>
          : null}
      />
    </>
  )

  const tabs = [
    {
      key: "data",
      label: t('competitionList.edit.tabData'),
      children: (
        <CompetitionListForm
          initialValues={list}
          elementId={list?.id}
          handleRequestResult={(updated) => setList(updated)}
          readOnly={readOnly}
        />
      ),
    },
    {
      key: "categories",
      label: t('competitionList.edit.tabCategories'),
      children: (
        <CrudTable
          elementForm={AdmissionCategoryForm}
          elementFormProps={{ listId }}

          readOnly={readOnly}

          serverSidePagination={true}
          getPagedAsync={(params) => admissionCategoriesApi.getPagedByCompetitionList(listId, params)}
          deleteAsync={(id) => admissionCategoriesApi.delete(id)}

          getDeleteBlockers={getDeleteBlockers}
          renderDeleteBlockersContent={renderDeleteBlockersContent}

          addButtonTitle={t('admissionCategory.addButton')}
          renderEditTitle={() => t('admissionCategory.editTitle')}
          renderDeleteText={() => t('admissionCategory.deleteText')}

          columns={[
            {
              title: t('admissionCategory.colPriority'),
              dataIndex: "priority",
              key: "priority",
              width: 120,
              sorter: true
            },
            {
              title: t('admissionCategory.colName'),
              dataIndex: "name",
              key: "name",
              withSearch: true,
              sorter: true
            },
            {
              title: t('admissionCategory.colGroup'),
              dataIndex: "evaluationCriteriaGroupId",
              key: "evaluationCriteriaGroupId",
              filters: Object.values(groupsDict).map(g => ({ text: g.name, value: g.id })),
              filterMultiple: false,
              filterSearch: true,
              render: (_, el) => groupsDict[el.evaluationCriteriaGroupId]?.name ?? el.evaluationCriteriaGroupId,
              sorter: true
            },
            {
              title: t('admissionCategory.colQuota'),
              dataIndex: "quota",
              key: "quota",
              sorter: true
            }
          ]}
        />
      ),
    },
  ]

  return (
    <>
      {contextHolder}
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <Link to={ROUTES.SPECIALTIES}>{t('specialty.edit.breadcrumb')}</Link> },
          { title: <Link to={generatePath(ROUTES.SPECIALTY_EDIT, { specialtyId: list.specialtyId })}>{specialtyName ?? "..."}</Link> },
          { title: <Link to={`${generatePath(ROUTES.SPECIALTY_EDIT, { specialtyId: list.specialtyId })}?act=competition-lists`}>{t('competitionList.edit.breadcrumb')}</Link> },
          { title: list.name },
        ]}
      />
      <Title title={readOnly ? t('competitionList.edit.titleView') : t('competitionList.edit.titleEdit')} />

      <Tabs
        activeKey={searchParams.get("act") ?? "data"}
        items={tabs}
        onChange={onTabChange}
      />
    </>
  )
}

export default CompetitionListEdit
