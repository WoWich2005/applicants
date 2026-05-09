import { Button, Form, Input, InputNumber, Select, Skeleton, Space, message } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { admissionCategoriesApi } from "../../api/admissionCategoriesApi"
import { evaluationCriteriaGroupsApi } from "../../api/evaluationCriteriaGroupsApi"
import { useTranslation } from "react-i18next"

function AdmissionCategoryForm(props) {
  const { t } = useTranslation()
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
        messageApi.error(t('admissionCategory.form.fetchGroupsError'))
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
      const serverMessage = err?.response?.data?.message
      messageApi.error(serverMessage ?? t('admissionCategory.form.saveError'))
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
              label={t('admissionCategory.form.nameLabel')}
              name="name"
              rules={[{ required: true, message: t('admissionCategory.form.nameRequired') }]}
            >
              <Input />
            </Form.Item>

            <Form.Item
              label={t('admissionCategory.form.groupLabel')}
              name="evaluationCriteriaGroupId"
              rules={[{ required: true, message: t('admissionCategory.form.groupRequired') }]}
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
              label={t('admissionCategory.form.quotaLabel')}
              name="quota"
              rules={[{ required: true, message: t('admissionCategory.form.quotaRequired') }]}
            >
              <InputNumber min={1} style={{ width: '100%' }} />
            </Form.Item>

            <Form.Item
              label={t('admissionCategory.form.priorityLabel')}
              name="priority"
              rules={[{ required: true, message: t('admissionCategory.form.priorityRequired') }]}
            >
              <InputNumber min={1} style={{ width: '100%' }} />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? t('admissionCategory.form.updateButton') : t('admissionCategory.form.addButton')}
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
