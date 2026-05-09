import { Breadcrumb, List, Result, Skeleton, Tabs, Typography } from "antd"
import Title from "../components/Title"
import { useParams, useSearchParams, generatePath, Link } from "react-router"
import { useEffect, useState } from "react"
import useMessage from "antd/es/message/useMessage"
import { specialtiesApi } from "../api/specialtiesApi"
import { competitionListsApi } from "../api/competitionListsApi"
import { admissionCategoriesApi } from "../api/admissionCategoriesApi"
import SpecialtyForm from "../components/Forms/SpecialtyForm"
import CompetitionListSimpleForm from "../components/Forms/CompetitionListSimpleForm"
import CrudTable from "../components/CrudTable"
import EntityHistory from "../components/EntityHistory"
import { ROUTES } from "../constants/routes"
import { useAuth } from "../contexts/AuthContext"
import { useTranslation } from "react-i18next"

function SpecialtyEdit() {
  const { auth } = useAuth()
  const { t } = useTranslation()
  const readOnly = auth?.role === 'DataViewer'
  const [, contextHolder] = useMessage()
  const [searchParams, setSearchParams] = useSearchParams()
  const { specialtyId } = useParams()

  const [isLoading, setIsLoading] = useState(true)
  const [responseStatus, setResponseStatus] = useState(null)
  const [specialty, setSpecialty] = useState({ id: null, name: null, departmentId: null })
  const [historyKey, setHistoryKey] = useState(0)

  useEffect(() => {
    if (!/^\d+$/.test(specialtyId)) {
      setResponseStatus(404)
      setIsLoading(false)
      return
    }

    const fetchData = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, specialtiesApi.getById(specialtyId)])

        setResponseStatus(response.status)
        setSpecialty(response.data)
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
  }, [specialtyId])

  if (isLoading) {
    return <Skeleton paragraph={{ rows: 12 }} />
  }

  if (responseStatus === 404) {
    return (
      <Result
        status="404"
        title="404"
        subTitle={t('specialty.edit.notFound')}
      />
    )
  } else if (responseStatus === 500) {
    return (
      <Result
        status="500"
        title="500"
        subTitle={t('specialty.edit.serverError')}
      />
    )
  }

  const getDeleteBlockers = async (/** @type {any} */ competitionList) => {
    const response = await admissionCategoriesApi.getAllByCompetitionList(competitionList.id)
    return response.data.length > 0 ? response.data : null
  }

  const renderDeleteBlockersContent = (/** @type {any} */ competitionList, /** @type {any[]} */ categories) => {
    const visible = categories.slice(0, 5)
    const remaining = categories.length - 5
    return (
      <>
        <Typography.Paragraph>
          {t('competitionList.deleteBlocked', { name: competitionList?.name })}
        </Typography.Paragraph>
        <List
          size="small"
          dataSource={visible}
          renderItem={(category) => <List.Item>{category.name}</List.Item>}
          footer={remaining > 0 ? <Typography.Text type="secondary">{t('competitionList.andMoreCategories', { count: remaining })}</Typography.Text> : null}
        />
      </>
    )
  }

  const onTabChange = (key) => {
    if (key === 'history') setHistoryKey(k => k + 1)
    searchParams.set("act", key)
    setSearchParams(searchParams)
  }

  const tabs = [
    {
      key: "data",
      label: t('specialty.edit.tabData'),
      children: (
        <SpecialtyForm
          initialValues={specialty}
          elementId={specialty?.id}
          handleRequestResult={(updated) => setSpecialty(updated)}
          readOnly={readOnly}
        />
      ),
    },
    {
      key: "competition-lists",
      label: t('specialty.edit.tabCompetitionLists'),
      children: (
        <CrudTable
          elementForm={CompetitionListSimpleForm}
          elementFormProps={{ specialtyId }}

          editType="page"
          renderEditUrl={(el) => generatePath(ROUTES.COMPETITION_LIST_EDIT, { listId: el.id })}

          readOnly={readOnly}

          serverSidePagination={true}
          getPagedAsync={(params) => competitionListsApi.getPagedBySpecialtyId(specialtyId, params)}
          deleteAsync={(id) => competitionListsApi.delete(id)}

          getDeleteBlockers={getDeleteBlockers}
          renderDeleteBlockersContent={renderDeleteBlockersContent}

          addButtonTitle={t('competitionList.addButton')}
          renderEditTitle={(el) => t('competitionList.editTitle', { name: el?.name })}
          renderDeleteText={(el) => t('competitionList.deleteText', { name: el?.name })}

          columns={[
            {
              title: t('common.colId'),
              dataIndex: "id",
              key: "id",
              withSearch: true,
              sorter: true,
            },
            {
              title: t('competitionList.colName'),
              dataIndex: "name",
              key: "name",
              withSearch: true,
              sorter: true
            },
            {
              title: t('competitionList.colPlan'),
              dataIndex: "plan",
              key: "plan",
              sorter: true,
              withSearch: true,
            }
          ]}
        />
      ),
    },
    {
      key: "history",
      label: t('specialty.edit.tabHistory'),
      children: <EntityHistory key={historyKey} entityType="specialty" entityId={specialty?.id} />,
    },
  ]

  return (
    <>
      {contextHolder}
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <Link to={ROUTES.SPECIALTIES}>{t('specialty.edit.breadcrumb')}</Link> },
          { title: specialty.name },
        ]}
      />
      <Title title={readOnly ? t('specialty.edit.titleView') : t('specialty.edit.titleEdit')} />

      <Tabs
        activeKey={searchParams.get("act") ?? "data"}
        items={tabs}
        onChange={onTabChange}
      />
    </>
  )
}

export default SpecialtyEdit
