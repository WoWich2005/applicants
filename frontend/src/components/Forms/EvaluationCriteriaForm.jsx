import { Button, Form, Input, InputNumber, message, Select, Space } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { evaluationCriteriaApi } from "../../api/evaluationCriteriaApi"
import { useTranslation } from "react-i18next"

function EvaluationCriteriaForm(props) {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)

  const api = evaluationCriteriaApi

  const onFinish = async (formData) => {
    setIsLoading(true)

    try {
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
      const serverMessage = err?.response?.data
      messageApi.error(
        typeof serverMessage === 'string' && serverMessage.length > 0
          ? serverMessage
          : t('evaluationCriteria.form.saveError')
      )
      console.log(err)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    form.setFieldsValue(props.initialValues)
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
