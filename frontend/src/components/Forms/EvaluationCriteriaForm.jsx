import { Button, Form, Input, InputNumber, message, Select, Space } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { evaluationCriteriaApi } from "../../api/evaluationCriteriaApi"

function EvaluationCriteriaForm(props) {
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
          : "Ошибка сохранения оценочного параметра на сервере"
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
              label="Название оценочного параметра"
              name="name"
              rules={[
                {
                  required: true,
                  message: "Название оценочного параметра обязательно для заполнения",
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
              label="Минимальное значение"
              name="minValue"
              rules={[
                {
                  required: true,
                  message: "Минимальное значение обязательно для заполнения",
                }
              ]}
            >
              <InputNumber style={{ width: "100%" }} />
            </Form.Item>
          </div>
          <div className={styles.formColumn}>
            <Form.Item
              label="Максимальное значение"
              name="maxValue"
              rules={[
                {
                  required: true,
                  message: "Максимальное значение обязательно для заполнения",
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
              label="Тип"
              name="type"
              rules={[{ required: true, message: "Тип обязателен для заполнения" }]}
            >
              <Select>
                <Select.Option value="higher_is_better">Больше — лучше</Select.Option>
                <Select.Option value="lower_is_better">Меньше — лучше</Select.Option>
              </Select>
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? 'Обновить оценочный параметр' : 'Добавить оценочный параметр'}
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
