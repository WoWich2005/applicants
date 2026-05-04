import { List, Typography } from 'antd'
import Title from '../components/Title'
import CrudTable from '../components/CrudTable'
import EvaluationCriteriaForm from '../components/Forms/EvaluationCriteriaForm'
import { evaluationCriteriaApi } from '../api/evaluationCriteriaApi'
import { useAuth } from '../contexts/AuthContext'

function EvaluationCriteria() {
  const { auth } = useAuth()
  const readOnly = auth?.role === 'DataViewer'
  const getDeleteBlockers = async (/** @type {any} */ criteria) => {
    const response = await evaluationCriteriaApi.getDeleteCheck(criteria.id)
    const { usedInGroups, applicants, applicantTotalCount } = response.data
    if (usedInGroups.length === 0 && applicantTotalCount === 0) return null
    return { usedInGroups, applicants, applicantTotalCount }
  }

  const renderDeleteBlockersContent = (/** @type {any} */ criteria, /** @type {any} */ blockers) => (
    <>
      <Typography.Paragraph>
        Невозможно удалить оценочный параметр <strong>"{criteria?.name}"</strong>:
      </Typography.Paragraph>
      {blockers.usedInGroups.length > 0 && (() => {
        const visible = blockers.usedInGroups.slice(0, 5)
        const remaining = blockers.usedInGroups.length - 5
        return (
          <>
            <Typography.Paragraph>
              Используется в следующих группах оценочных параметров:
            </Typography.Paragraph>
            <List
              size="small"
              dataSource={visible}
              renderItem={(group) => <List.Item>{group.name}</List.Item>}
              footer={remaining > 0 ? <Typography.Text type="secondary">и ещё {remaining} групп</Typography.Text> : null}
            />
          </>
        )
      })()}
      {blockers.applicantTotalCount > 0 && (
        <>
          <Typography.Paragraph style={{ marginTop: blockers.usedInGroups.length > 0 ? 12 : 0 }}>
            Есть значения данного параметра у следующих абитуриентов:
          </Typography.Paragraph>
          <List
            size="small"
            dataSource={blockers.applicants}
            renderItem={(applicant) => <List.Item>{applicant.name}</List.Item>}
            footer={blockers.applicantTotalCount > 5
              ? <Typography.Text type="secondary">и ещё {blockers.applicantTotalCount - 5} абитуриентов</Typography.Text>
              : null}
          />
        </>
      )}
    </>
  )

  return (
    <>
      <Title
        title="Оценочные параметры"
        helpText={<>Здесь Вы можете создать новые оценочные параметры</>}
      />

      <CrudTable
        elementForm={EvaluationCriteriaForm}
        readOnly={readOnly}

        serverSidePagination={true}
        getPagedAsync={(/** @type {any} */ params) => evaluationCriteriaApi.getPaged(params)}
        deleteAsync={(/** @type {any} */ id) => evaluationCriteriaApi.delete(id)}

        getDeleteBlockers={getDeleteBlockers}
        renderDeleteBlockersContent={renderDeleteBlockersContent}

        addButtonTitle="Новый оценочный параметр"
        renderEditTitle={(/** @type {any} */ el) => `Редактирование оценочного параметра "${el?.name}"`}
        renderDeleteText={(/** @type {any} */ el) => `Удалить оценочный параметр "${el?.name}"?`}

        columns={[
          {
            title: "Название",
            dataIndex: "name",
            key: "name",
            withSearch: true,
            sorter: true
          },
          {
            title: "Минимальное значение",
            dataIndex: "minValue",
            key: "minValue",
            sorter: true
          },
          {
            title: "Максимальное значение",
            dataIndex: "maxValue",
            key: "maxValue",
            sorter: true
          },
          {
            title: "Тип",
            dataIndex: "type",
            key: "type",
            filters: [
              { text: "Больше — лучше", value: "higher_is_better" },
              { text: "Меньше — лучше", value: "lower_is_better" }
            ],
            filterMultiple: false,
            render: (/** @type {string} */ type) => (
              <span style={{ whiteSpace: 'nowrap' }}>
                {type === "higher_is_better" ? "Больше — лучше" : "Меньше — лучше"}
              </span>
            )
          }
        ]}
      />
    </>
  )
}

export default EvaluationCriteria
