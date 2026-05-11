import { Alert, Breadcrumb, Result, Skeleton, Tabs } from "antd"
import Title from "../components/Title"
import { useParams, useSearchParams, Link } from "react-router"
import { ROUTES } from "../constants/routes"
import { useCallback, useEffect, useState } from "react"
import ApplicantForm from "../components/Forms/ApplicantForm"
import { applicantsApi } from "../api/applicantsApi"
import { usePermissions } from "../hooks/usePermissions"
import CrudTable from "../components/CrudTable"
import useMessage from "antd/es/message/useMessage"
import { applicantAdmissionCategoriesApi } from "../api/applicantAdmissionCategoriesApi"
import ApplicantAdmissionCategoryForm from "../components/Forms/ApplicantAdmissionCategoryForm"
import { applicantEvaluationValuesApi } from "../api/applicantEvaluationValuesApi"
import ApplicantEvaluationValueForm from "../components/Forms/ApplicantEvaluationValueForm"
import { useTranslation } from "react-i18next"
import { auditApi } from "../api/auditApi"
import EntityHistory from "../components/EntityHistory"

function ApplicantEdit() {
  const { canWriteApplicants } = usePermissions()
  const { t } = useTranslation()
  const readOnly = !canWriteApplicants
  const [messageApi, contextHolder] = useMessage()

  const [searchParams, setSearchParams] = useSearchParams()
  const { applicantId } = useParams()

  const [isLoading, setIsLoading] = useState(true)
  const [responseStatus, setResponseStatus] = useState(null)
  const [applicant, setApplicant] = useState({ id: null, name: null })

  const [incompleteInfo, setIncompleteInfo] = useState(null)
  const [refreshKey, setRefreshKey] = useState(0)
  const triggerRefresh = () => setRefreshKey(k => k + 1)

  const fetchApplicationsPaged = useCallback(
    (params) => applicantAdmissionCategoriesApi.getAllByApplicant(applicantId, params),
    [applicantId]
  )

  const fetchEvaluationPaged = useCallback(
    (params) => applicantEvaluationValuesApi.getAllByApplicant(applicantId, params),
    [applicantId]
  )

  useEffect(() => {
    if (!/^\d+$/.test(applicantId)) {
      setResponseStatus(404)
      setIsLoading(false)
      return
    }

    const fetchData = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, applicantsApi.getById(applicantId)])

        setResponseStatus(response.status)
        setApplicant(response.data)
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

    fetchData()
  }, [applicantId])

  useEffect(() => {
    if (!applicantId) return
    auditApi.getIncompleteForApplicant(applicantId)
      .then(r => setIncompleteInfo(r.data ?? null))
      .catch(() => {})
  }, [applicantId, refreshKey])

  if (isLoading) {
    return <Skeleton paragraph={{ rows: 12 }} />
  }

  if (responseStatus == 404) {
    return (
      <Result
        status="404"
        title="404"
        subTitle={t('applicant.edit.notFound')}
      />
    )
  } else if (responseStatus == 500) {
    return (
      <Result
        status="500"
        title="500"
        subTitle={t('applicant.edit.serverError')}
      />
    )
  }

  const onTabChange = (key) => {
    if (key === 'history') triggerRefresh()
    searchParams.set("act", key)
    setSearchParams(searchParams)
  }

  const tabs = [
    {
      key: "data",
      label: t('applicant.edit.tabData'),
      children: (
        <ApplicantForm
          initialValues={applicant}
          elementId={applicant?.id}
          handleRequestResult={(updated) => { setApplicant(updated); triggerRefresh() }}
          readOnly={readOnly}
        />
      ),
    },
    {
      key: "applications",
      label: t('applicant.edit.tabApplications'),
      children: (
        <CrudTable
          elementForm={ApplicantAdmissionCategoryForm}
          elementFormProps={{ applicantId }}
          readOnly={readOnly}
          onDataChange={triggerRefresh}
          serverSidePagination={true}

          getPagedAsync={fetchApplicationsPaged}
          deleteAsync={(id) => applicantAdmissionCategoriesApi.delete(id)}

          addButtonTitle={t('applicantAdmissionCategory.addButton')}
          renderEditTitle={() => t('applicantAdmissionCategory.editTitle')}
          renderDeleteText={() => t('applicantAdmissionCategory.deleteText')}

          columns={[
            {
              title: t('common.colId'),
              dataIndex: "id",
              key: "id",
              width: "80px",
              sorter: true,
              withSearch: true,
            },
            {
              title: t('applicantAdmissionCategory.colPriority'),
              dataIndex: "selectionPriority",
              key: "selectionPriority",
              width: "120px",
              defaultSortOrder: "ascend",
              sorter: true,
              withSearch: true,
            },
            {
              title: t('applicantAdmissionCategory.colFaculty'),
              dataIndex: "facultyName",
              key: "facultyName",
              sorter: true,
              withSearch: true,
            },
            {
              title: t('applicantAdmissionCategory.colDepartment'),
              dataIndex: "departmentName",
              key: "departmentName",
              sorter: true,
              withSearch: true,
            },
            {
              title: t('applicantAdmissionCategory.colSpecialty'),
              dataIndex: "specialtyName",
              key: "specialtyName",
              sorter: true,
              withSearch: true,
            },
            {
              title: t('applicantAdmissionCategory.colCompetitionList'),
              dataIndex: "competitionListName",
              key: "competitionListName",
              sorter: true,
              withSearch: true,
            },
            {
              title: t('applicantAdmissionCategory.colCategory'),
              dataIndex: "categoryName",
              key: "categoryName",
              sorter: true,
              withSearch: true,
            },
          ]}
        />
      ),
    },
    {
      key: "evaluation",
      label: t('applicant.edit.tabEvaluation'),
      children: (
        <CrudTable
          elementForm={ApplicantEvaluationValueForm}
          elementFormProps={{ applicantId }}
          readOnly={readOnly}
          onDataChange={triggerRefresh}
          serverSidePagination={true}

          getPagedAsync={fetchEvaluationPaged}
          deleteAsync={(id) => applicantEvaluationValuesApi.delete(id)}

          addButtonTitle={t('applicantEvaluationValue.addButton')}
          renderEditTitle={() => t('applicantEvaluationValue.editTitle')}
          renderDeleteText={() => t('applicantEvaluationValue.deleteText')}

          columns={[
            {
              title: t('common.colId'),
              dataIndex: "id",
              key: "id",
              width: "100px",
              sorter: true,
              withSearch: true,
            },
            {
              title: t('applicantEvaluationValue.colCriteria'),
              dataIndex: "criteriaName",
              key: "criteriaName",
              sorter: true,
              withSearch: true,
            },
            {
              title: t('applicantEvaluationValue.colValue'),
              dataIndex: "value",
              key: "value",
              width: "150px",
              sorter: true,
              withSearch: true,
            },
          ]}
        />
      ),
    },
    {
      key: "history",
      label: t('audit.applicantData.historyTab'),
      children: <EntityHistory key={refreshKey} entityType="applicant" entityId={applicant?.id} />,
    },
  ]

  return (
    <>
      {contextHolder}
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <Link to={ROUTES.APPLICANTS}>{t('applicant.edit.breadcrumb')}</Link> },
          { title: applicant.name },
        ]}
      />
      <Title title={readOnly ? t('applicant.edit.titleView') : t('applicant.edit.titleEdit')} />

      {incompleteInfo?.missingCriteria?.length > 0 && (
        <Alert
          type="warning"
          message={t('audit.validation.blockerMissingCriteria')}
          description={incompleteInfo.missingCriteria.join(', ')}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}

      {incompleteInfo?.hasInvalidPriorities && (
        <Alert
          type="warning"
          message={t('audit.validation.blockerInvalidPriorities')}
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}

      <Tabs
        activeKey={searchParams.get("act") ?? "data"}
        items={tabs}
        onChange={onTabChange}
      />
    </>
  )
}

export default ApplicantEdit
