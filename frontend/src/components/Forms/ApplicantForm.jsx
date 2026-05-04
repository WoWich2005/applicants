import { Button, Form, Input, message, Space } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { applicantsApi } from "../../api/applicantsApi"

const { TextArea } = Input

function ApplicantForm(props) {
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)

  const api = applicantsApi

  const onFinish = async (formData) => {
    setIsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 500))

      if(props.elementId) {
        const updatePromise = api.update(props.elementId, formData)
        await Promise.all([delayPromise, updatePromise])

        const updatedEl = (await api.getById(props.elementId)).data

        props.handleRequestResult && props.handleRequestResult(updatedEl)
      }else{
        const createPromise = api.create(formData)
        const [_, createdElResponse] = await Promise.all([delayPromise, createPromise])

        props.handleRequestResult && props.handleRequestResult(createdElResponse.data)
      }

      form.resetFields()
    } catch (err) {
      const serverMessage = err?.response?.data?.message
      messageApi.error(serverMessage ?? "Ошибка сохранения данных абитуриента")
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
              label="ID (номер паспорта, ID карты или любое другое уникальное значение)"
              name="externalId"
              rules={[
                {
                  required: true,
                  message: "ID абитуриента обязателен для заполнения",
                },
                {
                  max: 255,
                  message: "ID не должен превышать 255 символов",
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
              label="Имя абитуриента"
              name="name"
              rules={[
                {
                  required: true,
                  message: "Имя абитуриента обязательно для заполнения",
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
              label="Заметки"
              name="notes"
            >
              <TextArea rows={4} />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? 'Обновить данные абитуриента' : 'Добавить абитуриента'}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default ApplicantForm
