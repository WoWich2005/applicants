import { Breadcrumb, Result, Skeleton, Tabs } from "antd"
import Title from "../components/Title"
import { useParams, useSearchParams, Link } from "react-router"
import { ROUTES } from "../constants/routes"
import { useEffect, useState } from "react"
import useMessage from "antd/es/message/useMessage"
import CrudTable from "../components/CrudTable"
import EvaluationCriteriaGroupForm from "../components/Forms/EvaluationCriteriaGroupForm"
import EvaluationCriteriaGroupItemForm from "../components/Forms/EvaluationCriteriaGroupItemForm"
import { evaluationCriteriaGroupsApi } from "../api/evaluationCriteriaGroupsApi"
import { evaluationCriteriaGroupItemsApi } from "../api/evaluationCriteriaGroupItemsApi"
import { evaluationCriteriaApi } from "../api/evaluationCriteriaApi"
import { useAuth } from "../contexts/AuthContext"
import { useTranslation } from "react-i18next"

function EvaluationCriteriaGroupEdit() {
  const { auth } = useAuth()
  const { t } = useTranslation()
  const readOnly = auth?.role === 'DataViewer'
  const [messageApi, contextHolder] = useMessage()
  const [searchParams, setSearchParams] = useSearchParams()
  const { groupId } = useParams()

  const [criteriaDict, setCriteriaDict] = useState({})
  const [isLoading, setIsLoading] = useState(true)
  const [responseStatus, setResponseStatus] = useState(null)
  const [group, setGroup] = useState({ id: null, name: null })

  useEffect(() => {
    const fetchCriteria = async () => {
      try {
        const response = await evaluationCriteriaApi.getAll()
        setCriteriaDict(response.data.reduce((acc, c) => {
          acc[c.id] = c
          return acc
        }, {}))
      } catch {
        messageApi.error(t('evaluationCriteriaGroup.edit.fetchError'))
      }
    }

    fetchCriteria()
  }, [])

  useEffect(() => {
    const fetchGroup = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, evaluationCriteriaGroupsApi.getById(groupId)])

        setResponseStatus(response.status)
        setGroup(response.data)
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

    fetchGroup()
  }, [groupId])

  if (isLoading) {
    return <Skeleton paragraph={{ rows: 12 }} />
  }

  if (responseStatus === 404) {
    return (
      <Result
        status="404"
        title="404"
        subTitle={t('evaluationCriteriaGroup.edit.notFound')}
      />
    )
  } else if (responseStatus === 500) {
    return (
      <Result
        status="500"
        title="500"
        subTitle={t('evaluationCriteriaGroup.edit.serverError')}
      />
    )
  }

  const onTabChange = (key) => {
    searchParams.set("act", key)
    setSearchParams(searchParams)
  }

  const tabs = [
    {
      key: "data",
      label: t('evaluationCriteriaGroup.edit.tabData'),
      children: (
        <EvaluationCriteriaGroupForm
          initialValues={group}
          elementId={group?.id}
          handleRequestResult={(updated) => setGroup(updated)}
          readOnly={readOnly}
        />
      ),
    },
    {
      key: "criteria",
      label: t('evaluationCriteriaGroup.edit.tabCriteria'),
      children: (
        <CrudTable
          elementForm={EvaluationCriteriaGroupItemForm}
          elementFormProps={{ groupId }}

          readOnly={readOnly}

          getAllAsync={() => evaluationCriteriaGroupItemsApi.getAllByGroup(groupId)}
          deleteAsync={(id) => evaluationCriteriaGroupItemsApi.delete(id)}

          addButtonTitle={t('evaluationCriteriaGroupItem.addButton')}
          renderEditTitle={() => t('evaluationCriteriaGroupItem.editTitle')}
          renderDeleteText={() => t('evaluationCriteriaGroupItem.deleteText')}

          columns={[
            {
              title: t('evaluationCriteriaGroupItem.colPriority'),
              dataIndex: "priority",
              key: "priority",
              width: 120,
              defaultSortOrder: "ascend",
              sorter: (a, b) => a.priority - b.priority
            },
            {
              title: t('evaluationCriteriaGroupItem.colCriteria'),
              dataIndex: "criteriaId",
              key: "criteriaId",
              render: (_, el) => criteriaDict[el.criteriaId]?.name ?? el.criteriaId,
              sorter: (a, b) =>
                (criteriaDict[a.criteriaId]?.name ?? "").localeCompare(
                  criteriaDict[b.criteriaId]?.name ?? ""
                )
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
          { title: <Link to={ROUTES.EVALUATION_CRITERIA_GROUPS}>{t('evaluationCriteriaGroup.edit.breadcrumb')}</Link> },
          { title: group.name },
        ]}
      />
      <Title title={readOnly ? t('evaluationCriteriaGroup.edit.titleView') : t('evaluationCriteriaGroup.edit.titleEdit')} />

      <Tabs
        activeKey={searchParams.get("act") ?? "data"}
        items={tabs}
        onChange={onTabChange}
      />
    </>
  )
}

export default EvaluationCriteriaGroupEdit
