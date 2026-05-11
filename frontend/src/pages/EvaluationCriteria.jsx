import { List, Typography } from 'antd'
import Title from '../components/Title'
import CrudTable from '../components/CrudTable'
import EvaluationCriteriaForm from '../components/Forms/EvaluationCriteriaForm'
import { evaluationCriteriaApi } from '../api/evaluationCriteriaApi'
import { usePermissions } from '../hooks/usePermissions'
import { useTranslation } from 'react-i18next'

function EvaluationCriteria() {
  const { canWriteStructure } = usePermissions()
  const { t } = useTranslation()
  const readOnly = !canWriteStructure

  const getDeleteBlockers = async (/** @type {any} */ criteria) => {
    const response = await evaluationCriteriaApi.getDeleteCheck(criteria.id)
    const { usedInGroups, applicants, applicantTotalCount } = response.data
    if (usedInGroups.length === 0 && applicantTotalCount === 0) return null
    return { usedInGroups, applicants, applicantTotalCount }
  }

  const renderDeleteBlockersContent = (/** @type {any} */ criteria, /** @type {any} */ blockers) => (
    <>
      <Typography.Paragraph>
        {t('evaluationCriteria.deleteBlocked', { name: criteria?.name })}
      </Typography.Paragraph>
      {blockers.usedInGroups.length > 0 && (() => {
        const visible = blockers.usedInGroups.slice(0, 5)
        const remaining = blockers.usedInGroups.length - 5
        return (
          <>
            <Typography.Paragraph>
              {t('evaluationCriteria.usedInGroups')}
            </Typography.Paragraph>
            <List
              size="small"
              dataSource={visible}
              renderItem={(group) => <List.Item>{group.name}</List.Item>}
              footer={remaining > 0 ? <Typography.Text type="secondary">{t('evaluationCriteria.andMoreGroups', { count: remaining })}</Typography.Text> : null}
            />
          </>
        )
      })()}
      {blockers.applicantTotalCount > 0 && (
        <>
          <Typography.Paragraph style={{ marginTop: blockers.usedInGroups.length > 0 ? 12 : 0 }}>
            {t('evaluationCriteria.usedByApplicants')}
          </Typography.Paragraph>
          <List
            size="small"
            dataSource={blockers.applicants}
            renderItem={(applicant) => <List.Item>{applicant.name}</List.Item>}
            footer={blockers.applicantTotalCount > 5
              ? <Typography.Text type="secondary">{t('evaluationCriteria.andMoreApplicants', { count: blockers.applicantTotalCount - 5 })}</Typography.Text>
              : null}
          />
        </>
      )}
    </>
  )

  return (
    <>
      <Title
        title={t('evaluationCriteria.title')}
        helpText={t('evaluationCriteria.helpText')}
      />

      <CrudTable
        elementForm={EvaluationCriteriaForm}
        readOnly={readOnly}

        serverSidePagination={true}
        getPagedAsync={(/** @type {any} */ params) => evaluationCriteriaApi.getPaged(params)}
        deleteAsync={(/** @type {any} */ id) => evaluationCriteriaApi.delete(id)}

        getDeleteBlockers={getDeleteBlockers}
        renderDeleteBlockersContent={renderDeleteBlockersContent}

        addButtonTitle={t('evaluationCriteria.addButton')}
        renderEditTitle={(/** @type {any} */ el) => t('evaluationCriteria.editTitle', { name: el?.name })}
        renderDeleteText={(/** @type {any} */ el) => t('evaluationCriteria.deleteText', { name: el?.name })}

        columns={[
          {
            title: t('common.colId'),
            dataIndex: "id",
            key: "id",
            withSearch: true,
            sorter: true,
          },
          {
            title: t('evaluationCriteria.colName'),
            dataIndex: "name",
            key: "name",
            withSearch: true,
            sorter: true
          },
          {
            title: t('evaluationCriteria.colMinValue'),
            dataIndex: "minValue",
            key: "minValue",
            sorter: true
          },
          {
            title: t('evaluationCriteria.colMaxValue'),
            dataIndex: "maxValue",
            key: "maxValue",
            sorter: true
          },
          {
            title: t('evaluationCriteria.colType'),
            dataIndex: "type",
            key: "type",
            filters: [
              { text: t('evaluationCriteria.typeHigher'), value: "higher_is_better" },
              { text: t('evaluationCriteria.typeLower'), value: "lower_is_better" }
            ],
            filterMultiple: false,
            render: (/** @type {string} */ type) => (
              <span style={{ whiteSpace: 'nowrap' }}>
                {type === "higher_is_better" ? t('evaluationCriteria.typeHigher') : t('evaluationCriteria.typeLower')}
              </span>
            )
          }
        ]}
      />
    </>
  )
}

export default EvaluationCriteria
