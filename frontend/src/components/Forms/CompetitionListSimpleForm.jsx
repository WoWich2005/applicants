import { Button, Form, Input, InputNumber, Space, message } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { competitionListsApi } from "../../api/competitionListsApi"

function CompetitionListSimpleForm(/** @type {any} */ props) {
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    form.setFieldsValue(props.initialValues)
  }, [props.initialValues, form])

  const onFinish = async (/** @type {any} */ formData) => {
    const payload = { ...formData, specialtyId: Number(props.specialtyId) }
    setIsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 500))

      if (props.elementId) {
        const updatePromise = competitionListsApi.update(props.elementId, payload)
        await Promise.all([delayPromise, updatePromise])

        const updatedEl = (await competitionListsApi.getById(props.elementId)).data
        props.handleRequestResult && props.handleRequestResult(updatedEl)
      } else {
        const createPromise = competitionListsApi.create(payload)
        const [_, createdElResponse] = await Promise.all([delayPromise, createPromise])
        props.handleRequestResult && props.handleRequestResult(createdElResponse.data)
      }

      form.resetFields()
    } catch (err) {
      const serverMessage = err?.response?.data
      messageApi.error(
        typeof serverMessage === 'string' && serverMessage.length > 0
          ? serverMessage
          : "Ошибка сохранения конкурсного списка на сервере"
      )
      console.log(err)
    } finally {
      setIsLoading(false)
    }
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
              label="Название конкурсного списка"
              name="name"
              rules={[{ required: true, message: "Название обязательно для заполнения" }]}
            >
              <Input />
            </Form.Item>

            <Form.Item
              label="План набора"
              name="plan"
              rules={[{ required: true, message: "План набора обязателен" }]}
            >
              <InputNumber min={1} style={{ width: '100%' }} />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? 'Обновить список' : 'Добавить список'}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default CompetitionListSimpleForm
