import { Button, Form, InputNumber, message, Select, Skeleton, Space } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { evaluationCriteriaApi } from "../../api/evaluationCriteriaApi"
import { evaluationCriteriaGroupItemsApi } from "../../api/evaluationCriteriaGroupItemsApi"

function EvaluationCriteriaGroupItemForm(props) {
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)
  const [criteria, setCriteria] = useState([])
  const [isInputDataLoading, setIsInputDataLoading] = useState(true)

  useEffect(() => {
    const fetchData = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const responsePromise = evaluationCriteriaApi.getAll()

        const [_, response] = await Promise.all([delayPromise, responsePromise])
        setCriteria(response.data)
        setIsInputDataLoading(false)
      } catch {
        messageApi.error("Не удалось получить данные с сервера")
      }
    }

    fetchData()
  }, [])

  useEffect(() => {
    form.setFieldsValue({
      ...props.initialValues,
      groupId: props.groupId
    })
  }, [props.initialValues, props.groupId, form])

  const api = evaluationCriteriaGroupItemsApi

  const onFinish = async (formData) => {
    formData = { ...formData, groupId: Number(props.groupId) }

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
      messageApi.error("Ошибка сохранения оценочного параметра в группе")
      console.log(err)
    } finally {
      setIsLoading(false)
    }
  }

  if (isInputDataLoading) {
    return <Skeleton paragraph={{ rows: 4 }} />
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
              name="criteriaId"
              rules={[{ required: true, message: "Выберите оценочный параметр" }]}
            >
              <Select>
                {criteria.map(c => (
                  <Select.Option value={c.id} key={c.id}>
                    {c.name}
                  </Select.Option>
                ))}
              </Select>
            </Form.Item>

            <Form.Item
              label="Приоритет"
              name="priority"
              rules={[{ required: true, message: "Укажите приоритет" }]}
            >
              <InputNumber
                className={styles.inputNumberFullWidth}
                min={1}
              />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? 'Обновить параметр' : 'Добавить параметр'}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default EvaluationCriteriaGroupItemForm
