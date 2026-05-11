import { Button, Form, Input, message, Select, Skeleton, Space } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { departmentsApi } from "../../api/departmentsApi"
import { facultiesApi } from "../../api/facultyApi"
import { useTranslation } from "react-i18next"

function DepartmentForm(props) {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)
  const [faculties, setFaculties] = useState([])
  const [isFacultiesLoading, setIsFacultiesLoading] = useState(true)

  useEffect(() => {
    const fetchData = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const responsePromise = facultiesApi.getAll()

        const [_, response] = await Promise.all([delayPromise, responsePromise])
        setFaculties(response.data)
        setIsFacultiesLoading(false)
      } catch {
        messageApi.error(t('department.form.fetchFacultiesError'))
      }
    }

    fetchData()
  }, [])

  useEffect(() => {
    form.setFieldsValue(props.initialValues)
  }, [props.initialValues, props.elementId, form])

  const api = departmentsApi

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
      const serverMessage = err?.response?.data?.message
      messageApi.error(serverMessage ?? t('department.form.saveError'))
    } finally {
      setIsLoading(false)
    }
  }

  if (isFacultiesLoading) {
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
              label={t('department.form.nameLabel')}
              name="name"
              rules={[{ required: true, message: t('department.form.nameRequired') }]}
            >
              <Input />
            </Form.Item>

            <Form.Item
              label={t('department.form.facultyLabel')}
              name="facultyId"
              rules={[{ required: true, message: t('department.form.facultyRequired') }]}
            >
              <Select disabled={!!props.readOnly}>
                {faculties.map(faculty => (
                  <Select.Option value={faculty.id} key={faculty.id}>
                    {faculty.name}
                  </Select.Option>
                ))}
              </Select>
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? t('department.form.updateButton') : t('department.form.addButton')}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default DepartmentForm
