import { Button, Form, Input, InputNumber, Select, Skeleton, Space, message } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { admissionCategoriesApi } from "../../api/admissionCategoriesApi"
import { evaluationCriteriaGroupsApi } from "../../api/evaluationCriteriaGroupsApi"

function AdmissionCategoryForm(props) {
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)
  const [groups, setGroups] = useState([])
  const [isGroupsLoading, setIsGroupsLoading] = useState(true)

  useEffect(() => {
    const fetchData = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, evaluationCriteriaGroupsApi.getAll()])
        setGroups(response.data)
        setIsGroupsLoading(false)
      } catch {
        messageApi.error("Не удалось получить список групп оценочных параметров")
      }
    }

    fetchData()
  }, [])

  useEffect(() => {
    form.setFieldsValue(props.initialValues)
  }, [props.initialValues, form])

  const onFinish = async (formData) => {
    setIsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
      const payload = { ...formData, competitionListId: Number(props.listId) }

      if (props.elementId) {
        const updatePromise = admissionCategoriesApi.update(props.elementId, payload)
        await Promise.all([delayPromise, updatePromise])

        const updatedEl = (await admissionCategoriesApi.getById(props.elementId)).data
        props.handleRequestResult && props.handleRequestResult(updatedEl)
      } else {
        const createPromise = admissionCategoriesApi.create(payload)
        const [_, createdElResponse] = await Promise.all([delayPromise, createPromise])
        props.handleRequestResult && props.handleRequestResult(createdElResponse.data)
      }

      form.resetFields()
    } catch (err) {
      const serverMessage = err?.response?.data
      messageApi.error(
        typeof serverMessage === 'string' && serverMessage.length > 0
          ? serverMessage
          : "Ошибка сохранения категории приема на сервере"
      )
    } finally {
      setIsLoading(false)
    }
  }

  if (isGroupsLoading) {
    return <Skeleton paragraph={{ rows: 5 }} />
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
              label="Название категории приема"
              name="name"
              rules={[{ required: true, message: "Название обязательно для заполнения" }]}
            >
              <Input />
            </Form.Item>

            <Form.Item
              label="Группа оценочных параметров"
              name="evaluationCriteriaGroupId"
              rules={[{ required: true, message: "Группа оценочных параметров обязательна" }]}
            >
              <Select showSearch filterOption={(input, option) =>
                option.label?.toLowerCase().includes(input.toLowerCase())
              }>
                {groups.map(g => (
                  <Select.Option value={g.id} key={g.id}>{g.name}</Select.Option>
                ))}
              </Select>
            </Form.Item>

            <Form.Item
              label="Квота"
              name="quota"
              rules={[{ required: true, message: "Квота обязательна" }]}
            >
              <InputNumber min={1} style={{ width: '100%' }} />
            </Form.Item>

            <Form.Item
              label="Приоритет категории"
              name="priority"
              rules={[{ required: true, message: "Приоритет обязателен" }]}
            >
              <InputNumber min={1} style={{ width: '100%' }} />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? 'Обновить категорию' : 'Добавить категорию'}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default AdmissionCategoryForm
