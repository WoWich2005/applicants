import { Button, Form, Input, message, Modal } from 'antd'
import { useState } from 'react'
import { authApi } from '../../api/authApi'
import { useTranslation } from 'react-i18next'

function ChangePasswordModal({ open, onClose }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const [loading, setLoading] = useState(false)
  const [messageApi, contextHolder] = message.useMessage()

  const onFinish = async (values) => {
    setLoading(true)
    try {
      await authApi.changePassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      })
      messageApi.success(t('auth.changePassword.success'))
      form.resetFields()
      onClose()
    } catch (err) {
      messageApi.error(err.response?.data?.message || t('auth.changePassword.error'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      {contextHolder}
      <Modal
        open={open}
        title={t('auth.changePassword.title')}
        footer={null}
        onCancel={onClose}
        destroyOnClose
      >
        <Form form={form} layout="vertical" onFinish={onFinish} style={{ marginTop: 16 }}>
          <Form.Item
            name="currentPassword"
            label={t('auth.changePassword.currentPassword')}
            rules={[{ required: true, message: t('auth.changePassword.currentPasswordRequired') }]}
          >
            <Input.Password />
          </Form.Item>
          <Form.Item
            name="newPassword"
            label={t('auth.changePassword.newPassword')}
            rules={[
              { required: true, message: t('auth.changePassword.newPasswordRequired') },
              { min: 6, message: t('auth.changePassword.minLength') },
            ]}
          >
            <Input.Password />
          </Form.Item>
          <Form.Item
            name="confirmPassword"
            label={t('auth.changePassword.confirmPassword')}
            dependencies={['newPassword']}
            rules={[
              { required: true, message: t('auth.changePassword.confirmRequired') },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('newPassword') === value)
                    return Promise.resolve()
                  return Promise.reject(new Error(t('auth.changePassword.passwordMismatch')))
                },
              }),
            ]}
          >
            <Input.Password />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0, textAlign: 'right' }}>
            <Button onClick={onClose} style={{ marginRight: 8 }}>{t('common.cancel')}</Button>
            <Button type="primary" htmlType="submit" loading={loading}>
              {t('auth.changePassword.save')}
            </Button>
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}

export default ChangePasswordModal
