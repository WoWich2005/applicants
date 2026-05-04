import { useState } from 'react'
import { Button, Card, Form, Input, message, Typography } from 'antd'
import { UserOutlined, LockOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router'
import { authApi } from '../api/authApi'
import { useAuth } from '../contexts/AuthContext'
import { ROUTES } from '../constants/routes'
import styles from './Login.module.scss'

function Login() {
  const { login } = useAuth()
  const navigate = useNavigate()
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
      const msg = err.response?.data?.message || 'Ошибка входа'
      messageApi.error(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={styles.wrapper}>
      {contextHolder}
      <Card className={styles.card}>
        <Typography.Title level={3} className={styles.title}>
          Система приёма абитуриентов
        </Typography.Title>
        <Form form={form} onFinish={onFinish} layout="vertical" size="large">
          <Form.Item name="username" rules={[{ required: true, message: 'Введите логин' }]}>
            <Input prefix={<UserOutlined />} placeholder="Логин" />
          </Form.Item>
          <Form.Item name="password" rules={[{ required: true, message: 'Введите пароль' }]}>
            <Input.Password prefix={<LockOutlined />} placeholder="Пароль" />
          </Form.Item>
          <Form.Item>
            <Button type="primary" htmlType="submit" block loading={loading}>
              Войти
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  )
}

export default Login
