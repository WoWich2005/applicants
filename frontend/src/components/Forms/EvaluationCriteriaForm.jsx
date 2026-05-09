import { Button, Form, Input, InputNumber, message, Select, Space, Typography } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { evaluationCriteriaApi } from "../../api/evaluationCriteriaApi"
import { useTranslation } from "react-i18next"

function EvaluationCriteriaForm(props) {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)
  const [rangeViolators, setRangeViolators] = useState(/** @type {{applicants: any[], total: number} | null} */ (null))
  const [rangeErrorFields, setRangeErrorFields] = useState({ min: false, max: false })

  const api = evaluationCriteriaApi

  const clearRangeErrors = () => {
    setRangeViolators(null)
    setRangeErrorFields({ min: false, max: false })
  }

  const onFinish = async (formData) => {
    setIsLoading(true)

    try {
      if (props.elementId) {
        const minChanged = formData.minValue !== props.initialValues?.minValue
        const maxChanged = formData.maxValue !== props.initialValues?.maxValue
        if (minChanged || maxChanged) {
          const rangeCheck = await api.getRangeCheck(props.elementId, formData.minValue, formData.maxValue)
          const { applicants, applicantTotalCount } = rangeCheck.data
          if (applicantTotalCount > 0) {
            setRangeViolators({ applicants, total: applicantTotalCount })
            setRangeErrorFields({ min: minChanged, max: maxChanged })
            setIsLoading(false)
            return
          }
        }
      }

      const delayPromise = new Promise(resolve => setTimeout(resolve, 500))

      if (props.elementId) {
        const updatePromise = api.update(props.elementId, formData)
        await Promise.all([delayPromise, updatePromise])

        const updatedEl = (await api.getById(props.elementId)).data

        props.handleRequestResult && props.handleRequestResult(updatedEl)
      } else {
        const createPromise = api.create(formData)
        const [_, createdElResponse] = await Promise.all([delayPromise, createPromise])

        props.handleRequestResult && props.handleRequestResult(createdElResponse.data)
      }

      form.resetFields()
    } catch (err) {
      const serverMessage = err?.response?.data?.message
      messageApi.error(serverMessage ?? t('evaluationCriteria.form.saveError'))
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    form.setFieldsValue(props.initialValues)
    clearRangeErrors()
  }, [props.initialValues, form])

  return (
    <>
      {contextHolder}
      <Form
        form={form}
        className={styles.form}
        layout="vertical"
        initialValues={props.initialValues}
        onFinish={onFinish}
        onValuesChange={(changedValues) => {
          if ('minValue' in changedValues || 'maxValue' in changedValues) {
            clearRangeErrors()
          }
        }}
        autoComplete="off"
        disabled={!!props.readOnly}
      >
        <div className={styles.formRow}>
          <div className={styles.formColumn}>
            <Form.Item
              label={t('evaluationCriteria.form.nameLabel')}
              name="name"
              rules={[
                {
                  required: true,
                  message: t('evaluationCriteria.form.nameRequired'),
                }
              ]}
            >
              <Input />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <div className={styles.formColumn}>
            <Form.Item
              label={t('evaluationCriteria.form.minValueLabel')}
              name="minValue"
              validateStatus={rangeErrorFields.min ? "error" : undefined}
              rules={[
                {
                  required: true,
                  message: t('evaluationCriteria.form.minValueRequired'),
                }
              ]}
            >
              <InputNumber style={{ width: "100%" }} />
            </Form.Item>
          </div>
          <div className={styles.formColumn}>
            <Form.Item
              label={t('evaluationCriteria.form.maxValueLabel')}
              name="maxValue"
              validateStatus={rangeErrorFields.max ? "error" : undefined}
              rules={[
                {
                  required: true,
                  message: t('evaluationCriteria.form.maxValueRequired'),
                }
              ]}
            >
              <InputNumber style={{ width: "100%" }} />
            </Form.Item>
          </div>
        </div>
        {rangeViolators && (
          <div style={{ padding: '0 10px', marginTop: -16, marginBottom: 12 }}>
            <Typography.Text type="danger">{t('evaluationCriteria.rangeCheckText')}</Typography.Text>
            <div style={{ paddingLeft: 8 }}>
              {rangeViolators.applicants.map((a) => (
                <div key={a.id}>
                  <Typography.Text type="danger">• {a.name}</Typography.Text>
                </div>
              ))}
              {rangeViolators.total > 5 && (
                <Typography.Text type="secondary">
                  {t('evaluationCriteria.andMoreApplicants', { count: rangeViolators.total - 5 })}
                </Typography.Text>
              )}
            </div>
          </div>
        )}
        <div className={styles.formRow}>
          <div className={styles.formColumn}>
            <Form.Item
              label={t('evaluationCriteria.form.typeLabel')}
              name="type"
              rules={[{ required: true, message: t('evaluationCriteria.form.typeRequired') }]}
            >
              <Select>
                <Select.Option value="higher_is_better">{t('evaluationCriteria.typeHigher')}</Select.Option>
                <Select.Option value="lower_is_better">{t('evaluationCriteria.typeLower')}</Select.Option>
              </Select>
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? t('evaluationCriteria.form.updateButton') : t('evaluationCriteria.form.addButton')}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default EvaluationCriteriaForm
