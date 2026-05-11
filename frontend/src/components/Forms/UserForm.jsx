import { Button, Form, Input, message, Select, Space } from "antd"
import { useEffect, useState } from "react"
import { usersApi } from "../../api/usersApi"
import { useTranslation } from "react-i18next"

const ROLE_VALUES = ['SuperAdmin', 'DataAdministrator', 'Auditor', 'AdmissionsOperator', 'DataViewer']

function UserForm(props) {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)

  const ROLES = ROLE_VALUES.map(value => ({ value, label: t(`users.roles.${value}`) }))

  useEffect(() => {
    if (props.elementId) {
      form.setFieldsValue({
        username: props.initialValues?.username,
        role: props.initialValues?.role,
      })
    } else {
      form.resetFields()
    }
  }, [props.initialValues, props.elementId, form])

  const onFinish = async (values) => {
    setIsLoading(true)
    try {
      const payload = {
        username: values.username,
        password: values.password || undefined,
        role: values.role,
      }

      if (props.elementId) {
        await usersApi.update(props.elementId, payload)
        const updated = (await usersApi.getById(props.elementId)).data
        props.handleRequestResult && props.handleRequestResult(updated)
      } else {
        const response = await usersApi.create(payload)
        props.handleRequestResult && props.handleRequestResult(response.data)
      }

      form.resetFields()
    } catch (err) {
      messageApi.error(err.response?.data?.message || t('users.form.saveError'))
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <>
      {contextHolder}
      <Form form={form} layout="vertical" onFinish={onFinish} autoComplete="off" disabled={!!props.readOnly}>
        <Form.Item name="username" label={t('users.form.loginLabel')} rules={[{ required: true, message: t('users.form.loginRequired') }]}>
          <Input />
        </Form.Item>
        <Form.Item
          name="password"
          label={t('users.form.passwordLabel')}
          rules={props.elementId ? [] : [
            { required: true, message: t('users.form.passwordRequired') },
            { min: 6, message: t('users.form.passwordMinLength') },
          ]}
        >
          <Input.Password placeholder={props.elementId ? t('users.form.passwordLeaveEmpty') : ''} />
        </Form.Item>
        <Form.Item name="role" label={t('users.form.roleLabel')} rules={[{ required: true, message: t('users.form.roleRequired') }]}>
          <Select options={ROLES} />
        </Form.Item>

        {!props.readOnly && (
          <Space>
            <Button type="primary" htmlType="submit" loading={isLoading}>
              {props.elementId ? t('users.form.saveButton') : t('users.form.createButton')}
            </Button>
            {props.buttons}
          </Space>
        )}
        {props.readOnly && <Space>{props.buttons}</Space>}
      </Form>
    </>
  )
}

export default UserForm
