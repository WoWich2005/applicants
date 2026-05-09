import { generatePath } from "react-router"
import { List, Typography } from "antd"
import Title from '../components/Title'
import CrudTable from '../components/CrudTable'
import EvaluationCriteriaGroupForm from '../components/Forms/EvaluationCriteriaGroupForm'
import { ROUTES } from "../constants/routes"
import { evaluationCriteriaGroupsApi } from "../api/evaluationCriteriaGroupsApi"
import { useAuth } from '../contexts/AuthContext'
import { useTranslation } from 'react-i18next'

function EvaluationCriteriaGroups() {
  const { auth } = useAuth()
  const { t } = useTranslation()
  const readOnly = auth?.role === 'DataViewer'

  const getDeleteBlockers = async (/** @type {any} */ group) => {
    const response = await evaluationCriteriaGroupsApi.getDeleteCheck(group.id)
    return response.data.length > 0 ? response.data : null
  }

  const renderDeleteBlockersContent = (/** @type {any} */ group, /** @type {any} */ categories) => {
    const visible = categories.slice(0, 5)
    const remaining = categories.length - 5
    return (
      <>
        <Typography.Paragraph>
          {t('evaluationCriteriaGroup.deleteBlocked', { name: group?.name })}
        </Typography.Paragraph>
        <List
          size="small"
          dataSource={visible}
          renderItem={(item) => (
            <List.Item>
              {item.facultyName} — {item.departmentName} — {item.specialtyName} — {item.competitionListName} — {item.admissionCategoryName}
            </List.Item>
          )}
          footer={remaining > 0 ? <Typography.Text type="secondary">{t('evaluationCriteriaGroup.andMoreCategories', { count: remaining })}</Typography.Text> : null}
        />
      </>
    )
  }

  return (
    <>
      <Title
        title={t('evaluationCriteriaGroup.title')}
        helpText={t('evaluationCriteriaGroup.helpText')}
      />

      <CrudTable
        elementForm={EvaluationCriteriaGroupForm}
        readOnly={readOnly}

        editType="page"
        renderEditUrl={(/** @type {any} */ el) => generatePath(ROUTES.EVALUATION_CRITERIA_GROUP_EDIT, { groupId: el.id })}

        serverSidePagination={true}
        getPagedAsync={(/** @type {any} */ params) => evaluationCriteriaGroupsApi.getPaged(params)}
        deleteAsync={(/** @type {any} */ id) => evaluationCriteriaGroupsApi.delete(id)}

        getDeleteBlockers={getDeleteBlockers}
        renderDeleteBlockersContent={renderDeleteBlockersContent}

        addButtonTitle={t('evaluationCriteriaGroup.addButton')}
        renderEditTitle={(/** @type {any} */ el) => t('evaluationCriteriaGroup.editTitle', { name: el?.name })}
        renderDeleteText={(/** @type {any} */ el) => t('evaluationCriteriaGroup.deleteText', { name: el?.name })}

        columns={[
          {
            title: t('common.colId'),
            dataIndex: "id",
            key: "id",
            withSearch: true,
            sorter: true,
          },
          {
            title: t('evaluationCriteriaGroup.colName'),
            dataIndex: "name",
            key: "name",
            withSearch: true,
            sorter: true
          }
        ]}
      />
    </>
  )
}

export default EvaluationCriteriaGroups
