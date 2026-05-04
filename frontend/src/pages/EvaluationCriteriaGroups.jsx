import { generatePath } from "react-router"
import { List, Typography } from "antd"
import Title from '../components/Title'
import CrudTable from '../components/CrudTable'
import EvaluationCriteriaGroupForm from '../components/Forms/EvaluationCriteriaGroupForm'
import { ROUTES } from "../constants/routes"
import { evaluationCriteriaGroupsApi } from "../api/evaluationCriteriaGroupsApi"
import { useAuth } from '../contexts/AuthContext'

function EvaluationCriteriaGroups() {
  const { auth } = useAuth()
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
          Невозможно удалить группу <strong>"{group?.name}"</strong>, так как она используется в следующих категориях приема:
        </Typography.Paragraph>
        <List
          size="small"
          dataSource={visible}
          renderItem={(item) => (
            <List.Item>
              {item.facultyName} — {item.departmentName} — {item.specialtyName} — {item.competitionListName} — {item.admissionCategoryName}
            </List.Item>
          )}
          footer={remaining > 0 ? <Typography.Text type="secondary">и ещё {remaining} категорий приема</Typography.Text> : null}
        />
      </>
    )
  }

  return (
    <>
      <Title
        title="Группы оценочных параметров"
        helpText={<>Здесь Вы можете создать новые группы оценочных параметров</>}
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

        addButtonTitle="Новая группа оценочных параметров"
        renderEditTitle={(/** @type {any} */ el) => `Редактирование группы "${el?.name}"`}
        renderDeleteText={(/** @type {any} */ el) => `Удалить группу "${el?.name}"?`}

        columns={[
          {
            title: "Название",
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
