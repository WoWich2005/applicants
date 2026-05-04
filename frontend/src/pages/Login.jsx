import { useState } from 'react'
import { Button, Card, Form, Input, message, Radio, Typography } from 'antd'
import { UserOutlined, LockOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router'
import { authApi } from '../api/authApi'
import { useAuth } from '../contexts/AuthContext'
import { ROUTES } from '../constants/routes'
import { useLanguage } from '../contexts/LanguageContext'
import { useTranslation } from 'react-i18next'
import styles from './Login.module.scss'

function Login() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const { language, setLanguage } = useLanguage()
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const [messageApi, contextHolder] = message.useMessage()
  const [loading, setLoading] = useState(false)

  const onFinish = async (values) => {
    setLoading(true)
    try {
      const response = await authApi.login(values)
      login(response.data)
      navigate(ROUTES.RESULTS, { replace: true })
    } catch (err) {
      const msg = err.response?.data?.message || t('auth.error')
      messageApi.error(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={styles.wrapper}>
      {contextHolder}
      <Radio.Group
        value={language}
        onChange={(e) => setLanguage(e.target.value)}
        optionType="button"
        buttonStyle="solid"
        options={[
          { label: 'RU', value: 'ru' },
          { label: 'EN', value: 'en' },
        ]}
        className={styles.langSwitcher}
      />
      <Card className={styles.card}>
        <Typography.Title level={3} className={styles.title}>
          {t('auth.title')}
        </Typography.Title>
        <Form form={form} onFinish={onFinish} layout="vertical" size="large">
          <Form.Item name="username" rules={[{ required: true, message: t('auth.loginRequired') }]}>
            <Input prefix={<UserOutlined />} placeholder={t('auth.loginLabel')} />
          </Form.Item>
          <Form.Item name="password" rules={[{ required: true, message: t('auth.passwordRequired') }]}>
            <Input.Password prefix={<LockOutlined />} placeholder={t('auth.passwordLabel')} />
          </Form.Item>
          <Form.Item>
            <Button type="primary" htmlType="submit" block loading={loading}>
              {t('auth.submit')}
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  )
}

export default Login
