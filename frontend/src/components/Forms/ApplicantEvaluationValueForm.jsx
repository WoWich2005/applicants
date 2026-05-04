import { Button, Form, InputNumber, Select, Skeleton, Space, message } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { applicantEvaluationValuesApi } from "../../api/applicantEvaluationValuesApi"
import { evaluationCriteriaApi } from "../../api/evaluationCriteriaApi"

function ApplicantEvaluationValueForm(props) {
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)
  const [criteria, setCriteria] = useState([])
  const [isCriteriaLoading, setIsCriteriaLoading] = useState(true)
  const [selectedCriteria, setSelectedCriteria] = useState(null)

  useEffect(() => {
    const fetchData = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, evaluationCriteriaApi.getAll()])
        setCriteria(response.data)
        setIsCriteriaLoading(false)
      } catch {
        messageApi.error("Не удалось получить список оценочных параметров")
      }
    }

    fetchData()
  }, [])

  useEffect(() => {
    form.setFieldsValue({ ...props.initialValues, applicantId: props.applicantId })
    if (props.initialValues?.evaluationCriteriaId) {
      const found = criteria.find(c => c.id === props.initialValues.evaluationCriteriaId)
      setSelectedCriteria(found ?? null)
    }
  }, [props.initialValues, props.applicantId, form, criteria])

  const onFinish = async (formData) => {
    formData = { ...formData, applicantId: Number(props.applicantId) }
    setIsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 500))

      if (props.elementId) {
        const updatePromise = applicantEvaluationValuesApi.update(props.elementId, formData)
        await Promise.all([delayPromise, updatePromise])

        const updatedEl = (await applicantEvaluationValuesApi.getById(props.elementId)).data
        props.handleRequestResult && props.handleRequestResult(updatedEl)
      } else {
        const createPromise = applicantEvaluationValuesApi.create(formData)
        const [_, createdElResponse] = await Promise.all([delayPromise, createPromise])
        props.handleRequestResult && props.handleRequestResult(createdElResponse.data)
      }

      form.resetFields()
    } catch (err) {
      if ('response' in err && err.response.status === 400) {
        messageApi.error(err.response.data)
      } else {
        messageApi.error("Ошибка сохранения данных на сервере")
      }
    } finally {
      setIsLoading(false)
    }
  }

  if (isCriteriaLoading) {
    return <Skeleton paragraph={{ rows: 3 }} />
  }

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
              label="Оценочный параметр"
              name="evaluationCriteriaId"
              rules={[{ required: true, message: "Выберите оценочный параметр" }]}
            >
              <Select
                showSearch
                filterOption={(input, option) =>
                  option.label?.toLowerCase().includes(input.toLowerCase())
                }
                onChange={(id) => {
                  const found = criteria.find(c => c.id === id) ?? null
                  setSelectedCriteria(found)
                  form.validateFields(["value"])
                }}
              >
                {criteria.map(c => (
                  <Select.Option value={c.id} key={c.id}>{c.name}</Select.Option>
                ))}
              </Select>
            </Form.Item>

            <Form.Item
              label="Значение параметра"
              name="value"
              extra={selectedCriteria ? `Допустимый диапазон: ${selectedCriteria.minValue} – ${selectedCriteria.maxValue}` : null}
              rules={[
                { required: true, message: "Укажите значение" },
                {
                  validator(_, value) {
                    if (value == null || !selectedCriteria) return Promise.resolve()
                    if (value < selectedCriteria.minValue || value > selectedCriteria.maxValue)
                      return Promise.reject(new Error(`Значение должно быть от ${selectedCriteria.minValue} до ${selectedCriteria.maxValue}`))
                    return Promise.resolve()
                  }
                }
              ]}
            >
              <InputNumber
                className={styles.inputNumberFullWidth}
                min={selectedCriteria?.minValue ?? 1}
                max={selectedCriteria?.maxValue}
              />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? 'Обновить значение' : 'Добавить параметр'}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default ApplicantEvaluationValueForm
