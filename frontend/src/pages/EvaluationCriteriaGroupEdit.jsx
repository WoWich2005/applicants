import { Alert, Breadcrumb, Result, Skeleton, Tabs } from "antd"
import Title from "../components/Title"
import { useParams, useSearchParams, Link } from "react-router"
import { ROUTES } from "../constants/routes"
import { useCallback, useEffect, useState } from "react"
import useMessage from "antd/es/message/useMessage"
import CrudTable from "../components/CrudTable"
import EvaluationCriteriaGroupForm from "../components/Forms/EvaluationCriteriaGroupForm"
import EvaluationCriteriaGroupItemForm from "../components/Forms/EvaluationCriteriaGroupItemForm"
import { evaluationCriteriaGroupsApi } from "../api/evaluationCriteriaGroupsApi"
import { evaluationCriteriaGroupItemsApi } from "../api/evaluationCriteriaGroupItemsApi"
import EntityHistory from "../components/EntityHistory"
import { usePermissions } from "../hooks/usePermissions"
import { useTranslation } from "react-i18next"

function EvaluationCriteriaGroupEdit() {
  const { canWriteStructure } = usePermissions()
  const { t } = useTranslation()
  const readOnly = !canWriteStructure
  const [messageApi, contextHolder] = useMessage()
  const [searchParams, setSearchParams] = useSearchParams()
  const { groupId } = useParams()

  const [isLoading, setIsLoading] = useState(true)
  const [responseStatus, setResponseStatus] = useState(null)
  const [group, setGroup] = useState({ id: null, name: null })
  const [groupItems, setGroupItems] = useState(/** @type {any[]} */ ([]))
  const [historyKey, setHistoryKey] = useState(0)

  useEffect(() => {
    if (!/^\d+$/.test(groupId)) {
      setResponseStatus(404)
      setIsLoading(false)
      return
    }

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

  useEffect(() => {
    evaluationCriteriaGroupItemsApi.getAllByGroup(groupId)
      .then(r => setGroupItems(r.data))
      .catch(() => {})
  }, [groupId])

  const fetchCriteriaItemsPaged = useCallback(
    (params) => evaluationCriteriaGroupItemsApi.getPagedByGroup(groupId, params),
    [groupId]
  )

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
    if (key === 'history') setHistoryKey(k => k + 1)
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

          serverSidePagination={true}
          getPagedAsync={fetchCriteriaItemsPaged}
          deleteAsync={(id) => evaluationCriteriaGroupItemsApi.delete(id)}

          addButtonTitle={t('evaluationCriteriaGroupItem.addButton')}
          renderEditTitle={() => t('evaluationCriteriaGroupItem.editTitle')}
          renderDeleteText={() => t('evaluationCriteriaGroupItem.deleteText')}

          columns={[
            {
              title: t('common.colId'),
              dataIndex: "id",
              key: "id",
              sorter: true,
              withSearch: true,
            },
            {
              title: t('evaluationCriteriaGroupItem.colPriority'),
              dataIndex: "priority",
              key: "priority",
              width: 120,
              defaultSortOrder: "ascend",
              sorter: true,
              withSearch: true,
            },
            {
              title: t('evaluationCriteriaGroupItem.colCriteria'),
              dataIndex: "criteriaName",
              key: "criteriaName",
              sorter: true,
              withSearch: true,
            }
          ]}
        />
      ),
    },
    {
      key: "history",
      label: t('evaluationCriteriaGroup.edit.tabHistory'),
      children: <EntityHistory key={historyKey} entityType="evaluation_criteria_group" entityId={group?.id} />,
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

      {(() => {
        const priorities = groupItems.map(i => i.priority)
        const n = priorities.length
        const invalid = n > 0 && (
          new Set(priorities).size !== n ||
          Math.min(...priorities) !== 1 ||
          Math.max(...priorities) !== n
        )
        return invalid ? (
          <Alert
            type="warning"
            message={t('evaluationCriteriaGroupItem.invalidPriorities')}
            style={{ marginBottom: 16 }}
            showIcon
          />
        ) : null
      })()}

      <Tabs
        activeKey={searchParams.get("act") ?? "data"}
        items={tabs}
        onChange={onTabChange}
      />
    </>
  )
}

export default EvaluationCriteriaGroupEdit
