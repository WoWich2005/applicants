import { useEffect, useState } from 'react'
import { Alert, Button, Input, message, Modal, Space, Spin, Steps, Table } from 'antd'
import dayjs from 'dayjs'
import { auditApi } from '../../../api/auditApi'
import { applicantsApi } from '../../../api/applicantsApi'
import { applicantEvaluationValuesApi } from '../../../api/applicantEvaluationValuesApi'
import { applicantAdmissionCategoriesApi } from '../../../api/applicantAdmissionCategoriesApi'
import { evaluationCriteriaApi } from '../../../api/evaluationCriteriaApi'
import { admissionCategoriesApi } from '../../../api/admissionCategoriesApi'
import { competitionListsApi } from '../../../api/competitionListsApi'
import { specialtiesApi } from '../../../api/specialtiesApi'
import { departmentsApi } from '../../../api/departmentsApi'
import { facultiesApi } from '../../../api/facultyApi'
import ApplicantForm from '../../Forms/ApplicantForm'
import EntityHistory from '../../EntityHistory'
import { useTranslation } from 'react-i18next'

function ApplicantValidationModal({ applicantId, applicantName, open, onClose, onSuccess }) {
  const { t } = useTranslation()

  const [loading, setLoading] = useState(false)
  const [currentStep, setCurrentStep] = useState(0)
  const [applicant, setApplicant] = useState(null)
  const [validationStatus, setValidationStatus] = useState(null)
  const [evaluationValues, setEvaluationValues] = useState([])
  const [evaluationCriteriaDict, setEvaluationCriteriaDict] = useState({})
  const [admissionCategories, setAdmissionCategories] = useState([])
  const [admissionCategoriesDict, setAdmissionCategoriesDict] = useState({})
  const [competitionListsDict, setCompetitionListsDict] = useState({})
  const [specialtiesDict, setSpecialtiesDict] = useState({})
  const [departmentsDict, setDepartmentsDict] = useState({})
  const [facultiesDict, setFacultiesDict] = useState({})
  const [incompleteInfo, setIncompleteInfo] = useState(null)
  const [comment, setComment] = useState('')
  const [showInvalidateInput, setShowInvalidateInput] = useState(false)
  const [actionLoading, setActionLoading] = useState(false)
  const [messageApi, contextHolder] = message.useMessage()

  useEffect(() => {
    if (!open || !applicantId) return

    setCurrentStep(0)
    setLoading(true)
    setComment('')
    setShowInvalidateInput(false)

    const toDict = (arr) => (arr ?? []).reduce((acc, item) => { acc[item.id] = item; return acc }, {})

    Promise.all([
      applicantsApi.getById(applicantId),
      auditApi.getValidationStatus(applicantId),
      applicantEvaluationValuesApi.getAllByApplicant(applicantId),
      evaluationCriteriaApi.getAll(),
      applicantAdmissionCategoriesApi.getAllByApplicant(applicantId),
      admissionCategoriesApi.getAll(),
      competitionListsApi.getAll(),
      specialtiesApi.getAll(),
      departmentsApi.getAll(),
      facultiesApi.getAll(),
      auditApi.getIncomplete(),
    ])
      .then(([applicantRes, statusRes, evalValuesRes, criteriaRes, admCatsRes, allAdmCatsRes, compListsRes, specialtiesRes, departmentsRes, facultiesRes, incompleteRes]) => {
        setApplicant(applicantRes.data)
        setValidationStatus(statusRes.data)
        setEvaluationValues(evalValuesRes.data?.items ?? [])
        setEvaluationCriteriaDict(toDict(criteriaRes.data))
        setAdmissionCategories(admCatsRes.data?.items ?? [])
        setAdmissionCategoriesDict(toDict(allAdmCatsRes.data))
        setCompetitionListsDict(toDict(compListsRes.data))
        setSpecialtiesDict(toDict(specialtiesRes.data))
        setDepartmentsDict(toDict(departmentsRes.data))
        setFacultiesDict(toDict(facultiesRes.data))
        const list = incompleteRes.data ?? []
        setIncompleteInfo(list.find(item => String(item.id) === String(applicantId)) ?? null)
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [open, applicantId])

  const handleValidate = async () => {
    setActionLoading(true)
    try {
      await auditApi.validate(applicantId, { comment })
      onSuccess?.()
      onClose?.()
    } catch (err) {
      const data = err?.response?.data
      if (data?.missingCriteria?.length > 0 || data?.invalidPriorities) {
        const msgs = []
        if (data.missingCriteria?.length > 0)
          msgs.push(`${t('audit.validation.blockerMissingCriteria')}: ${data.missingCriteria.join(', ')}`)
        if (data.invalidPriorities)
          msgs.push(t('audit.validation.blockerInvalidPriorities'))
        messageApi.error(msgs.join('; '))
      } else {
        messageApi.error(data?.message ?? t('audit.validation.validateError'))
      }
    } finally {
      setActionLoading(false)
    }
  }

  const handleInvalidate = async () => {
    if (!comment.trim()) return
    setActionLoading(true)
    try {
      await auditApi.invalidate(applicantId, { comment })
      onSuccess?.()
      onClose?.()
    } catch (err) {
      messageApi.error(err?.response?.data?.message ?? t('audit.validation.invalidateError'))
    } finally {
      setActionLoading(false)
    }
  }

  const isValidated = validationStatus?.validated

  const stepBlockers = {
    1: incompleteInfo?.missingCriteria?.length > 0,
    2: !!incompleteInfo?.hasInvalidPriorities,
  }
  const hasAnyBlocker = Object.values(stepBlockers).some(Boolean)
  const isCurrentStepBlocked = !!stepBlockers[currentStep]

  const steps = [
    {
      title: t('audit.applicantData.info'),
      content: (
        <ApplicantForm
          initialValues={applicant}
          elementId={applicant?.id}
          readOnly={true}
        />
      ),
    },
    {
      title: t('audit.applicantData.evaluationValues'),
      content: (
        <>
          {incompleteInfo?.missingCriteria?.length > 0 && (
            <Alert
              type="warning"
              message={t('audit.validation.blockerMissingCriteria')}
              description={incompleteInfo.missingCriteria.join(', ')}
              style={{ marginBottom: 12 }}
              showIcon
            />
          )}
          <Table
            size="small"
            rowKey="id"
            dataSource={[...evaluationValues].sort((a, b) =>
              (evaluationCriteriaDict[a.evaluationCriteriaId]?.name ?? '').localeCompare(
                evaluationCriteriaDict[b.evaluationCriteriaId]?.name ?? ''
              )
            )}
            columns={[
              {
                title: t('applicantEvaluationValue.colCriteria'),
                dataIndex: 'evaluationCriteriaId',
                key: 'evaluationCriteriaId',
                render: (id) => evaluationCriteriaDict[id]?.name ?? id,
              },
              {
                title: t('applicantEvaluationValue.colValue'),
                dataIndex: 'value',
                key: 'value',
                width: 150,
              },
            ]}
            pagination={false}
          />
        </>
      ),
    },
    {
      title: t('audit.applicantData.admissionCategories'),
      content: (
        <>
          {incompleteInfo?.hasInvalidPriorities && (
            <Alert
              type="warning"
              message={t('audit.validation.blockerInvalidPriorities')}
              style={{ marginBottom: 12 }}
              showIcon
            />
          )}
          <Table
            size="small"
            rowKey="id"
            dataSource={[...admissionCategories].sort((a, b) => a.selectionPriority - b.selectionPriority)}
          columns={[
            {
              title: t('applicantAdmissionCategory.colPriority'),
              dataIndex: 'selectionPriority',
              key: 'selectionPriority',
              width: 90,
            },
            {
              title: t('applicantAdmissionCategory.colFaculty'),
              key: 'faculty',
              render: (_, el) => {
                const compList = competitionListsDict[admissionCategoriesDict[el.admissionCategoryId]?.competitionListId]
                const specialty = specialtiesDict[compList?.specialtyId]
                const department = departmentsDict[specialty?.departmentId]
                return facultiesDict[department?.facultyId]?.name ?? ''
              },
            },
            {
              title: t('applicantAdmissionCategory.colSpecialty'),
              key: 'specialty',
              render: (_, el) => {
                const compList = competitionListsDict[admissionCategoriesDict[el.admissionCategoryId]?.competitionListId]
                return specialtiesDict[compList?.specialtyId]?.name ?? ''
              },
            },
            {
              title: t('applicantAdmissionCategory.colCompetitionList'),
              key: 'competitionList',
              render: (_, el) => {
                const compListId = admissionCategoriesDict[el.admissionCategoryId]?.competitionListId
                return competitionListsDict[compListId]?.name ?? ''
              },
            },
            {
              title: t('applicantAdmissionCategory.colCategory'),
              key: 'admissionCategoryId',
              render: (_, el) => admissionCategoriesDict[el.admissionCategoryId]?.name ?? el.admissionCategoryId,
            },
          ]}
          pagination={false}
          scroll={{ x: 'max-content' }}
        />
        </>
      ),
    },
    {
      title: t('audit.applicantData.historyTab'),
      content: <EntityHistory entityType="applicant" entityId={applicantId} />,
    },
  ]

  const footer = (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
      {showInvalidateInput ? (
        <div style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
          <Input.TextArea
            rows={2}
            placeholder={t('audit.validation.commentLabel')}
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            style={{ flex: 1 }}
          />
          <Button
            danger
            type="primary"
            loading={actionLoading}
            disabled={!comment.trim()}
            onClick={handleInvalidate}
          >
            {t('audit.validation.confirmInvalidate')}
          </Button>
          <Button onClick={() => { setShowInvalidateInput(false); setComment('') }}>
            {t('common.cancel')}
          </Button>
        </div>
      ) : (
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <Space>
            {isValidated && (
              <Button danger onClick={() => setShowInvalidateInput(true)}>
                {t('audit.validation.invalidate')}
              </Button>
            )}
          </Space>
          <Space>
            <Button disabled={currentStep === 0} onClick={() => setCurrentStep(s => s - 1)}>
              {t('common.prev')}
            </Button>
            {currentStep < steps.length - 1 ? (
              <Button type="primary" disabled={isCurrentStepBlocked} onClick={() => setCurrentStep(s => s + 1)}>
                {t('common.next')}
              </Button>
            ) : (
              <Button type="primary" disabled={hasAnyBlocker} onClick={handleValidate} loading={actionLoading}>
                {t('audit.validation.validate')}
              </Button>
            )}
          </Space>
        </div>
      )}
    </div>
  )

  return (
    <>
    {contextHolder}
    <Modal
      open={open}
      title={`${t('audit.validation.validateTitle')} — ${applicantName ?? ''}`}
      onCancel={onClose}
      footer={footer}
      width={800}
      destroyOnClose
    >
      {loading ? (
        <div style={{ textAlign: 'center', padding: 32 }}><Spin /></div>
      ) : (
        <>
          {isValidated && (
            <Alert
              type="success"
              message={t('audit.validation.validated')}
              style={{ marginBottom: 12 }}
            />
          )}
          <Steps
            current={currentStep}
            items={steps.map((s, i) => ({ title: i === currentStep ? s.title : '' }))}
            style={{ marginBottom: 24, marginTop: 8 }}
            onChange={setCurrentStep}
          />
          <div style={{ minHeight: 200 }}>
            {steps[currentStep].content}
          </div>
        </>
      )}
    </Modal>
    </>
  )
}

export default ApplicantValidationModal
