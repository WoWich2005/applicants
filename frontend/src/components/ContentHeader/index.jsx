import { Header } from 'antd/es/layout/layout'
import styles from './styles.module.scss'
import AddButton from '../Buttons/AddButton'
import ApplicantForm from '../Forms/ApplicantForm'
import { useState } from 'react'
import { Avatar, Button, Dropdown, Switch } from 'antd'
import { generatePath, useNavigate } from 'react-router'
import { ROUTES } from '../../constants/routes'
import { MoonOutlined, SunOutlined, UserOutlined } from '@ant-design/icons'
import { useAuth } from '../../contexts/AuthContext'
import ChangePasswordModal from '../Modals/ChangePasswordModal'
import { useTheme } from '../../contexts/ThemeContext'

const APPLICANT_ROLES = ['SuperAdmin', 'FacultyManager', 'AdmissionsOperator']

function ContentHeader() {
  let navigate = useNavigate()
  const { auth, logout } = useAuth()
  const { isDark, toggleTheme } = useTheme()
  const [isCreateElModalOpen, setIsCreateElModalOpen] = useState(false)
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false)

  const items = [
    {
      key: 'AccountLabel',
      label: <span className={styles.userLabel}>{auth?.username ?? 'Пользователь'}</span>,
      disabled: true,
    },
    {
      key: 'divider',
      type: 'divider'
    },
    {
      key: 'ThemeToggle',
      label: (
        <span className={styles.themeToggleItem}>
          {isDark ? <MoonOutlined /> : <SunOutlined />}
          Тёмная тема
          <Switch size="small" checked={isDark} />
        </span>
      ),
      onClick: toggleTheme,
    },
    {
      key: 'ChangePassword',
      label: 'Сменить пароль',
      onClick: () => setIsChangePasswordOpen(true),
    },
    {
      key: 'Logout',
      label: 'Выйти',
      onClick: () => {
        logout()
        navigate(ROUTES.LOGIN, { replace: true })
      },
    },
  ]

  const navigateToApplicant = (applicant) => {
    const url = generatePath(ROUTES.APPLICANT_EDIT, { applicantId: applicant.id })
    navigate(url)
    setIsCreateElModalOpen(false)
  }

  return (
    <Header className={styles.header}>
      {APPLICANT_ROLES.includes(auth?.role) && (
        <AddButton
          title="Новый абитуриент"
          modalContent={(
            <ApplicantForm
              onFinishCallback={() => setIsCreateElModalOpen(false)}
              onCancelCallback={() => setIsCreateElModalOpen(false)}
              handleRequestResult={navigateToApplicant}
              buttons={(
                <>
                  <Button onClick={() => setIsCreateElModalOpen(false)}>Отмена</Button>
                </>
              )}
            />
          )}
          isModalOpen={isCreateElModalOpen}
          setIsModalOpen={setIsCreateElModalOpen}
          withoutContainer={true}
        />
      )}

      <Dropdown
        className={styles.avatar}
        menu={{ items }}
        placement="bottomLeft"
        arrow
        trigger={['click']}
      >
        <Avatar icon={<UserOutlined />} />
      </Dropdown>

      <ChangePasswordModal
        open={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </Header>
  )
}

export default ContentHeader
