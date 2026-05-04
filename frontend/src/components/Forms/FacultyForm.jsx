import { Button, Form, Input, message, Space } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { facultiesApi } from "../../api/facultyApi"

function FacultyForm(props) {
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)

  const api = facultiesApi

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
      const serverMessage = err?.response?.data
      messageApi.error(
        typeof serverMessage === 'string' && serverMessage.length > 0
          ? serverMessage
          : "Ошибка обновления факультета на сервере"
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
              label="Имя факультета"
              name="name"
              rules={[
                {
                  required: true,
                  message: "Имя факультета обязательно для заполнения",
                }
              ]}
            >
              <Input />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? 'Обновить факультет' : 'Добавить факультет'}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default FacultyForm
