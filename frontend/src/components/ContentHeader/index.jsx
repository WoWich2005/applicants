import { Header } from 'antd/es/layout/layout'
import styles from './styles.module.scss'
import AddButton from '../Buttons/AddButton'
import ApplicantForm from '../Forms/ApplicantForm'
import { useState } from 'react'
import { Avatar, Button, Dropdown, Tooltip } from 'antd'
import { generatePath, useNavigate } from 'react-router'
import { ROUTES } from '../../constants/routes'
import { DownOutlined, MoonOutlined, SunOutlined, UserOutlined } from '@ant-design/icons'
import { useAuth } from '../../contexts/AuthContext'
import ChangePasswordModal from '../Modals/ChangePasswordModal'
import { useTheme } from '../../contexts/ThemeContext'
import { useLanguage } from '../../contexts/LanguageContext'
import { useTranslation } from 'react-i18next'

const APPLICANT_ROLES = ['SuperAdmin', 'FacultyManager', 'AdmissionsOperator']

function ContentHeader() {
  let navigate = useNavigate()
  const { auth, logout } = useAuth()
  const { isDark, toggleTheme } = useTheme()
  const { language, setLanguage } = useLanguage()
  const { t } = useTranslation()
  const [isCreateElModalOpen, setIsCreateElModalOpen] = useState(false)
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false)

  const items = [
    {
      key: 'AccountLabel',
      label: <span className={styles.userLabel}>{auth?.username ?? t('header.defaultUser')}</span>,
      disabled: true,
    },
    {
      key: 'divider',
      type: 'divider'
    },
    {
      key: 'ChangePassword',
      label: t('nav.changePassword'),
      onClick: () => setIsChangePasswordOpen(true),
    },
    {
      key: 'Logout',
      label: t('nav.logout'),
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
          title={t('header.newApplicant')}
          modalContent={(
            <ApplicantForm
              onFinishCallback={() => setIsCreateElModalOpen(false)}
              onCancelCallback={() => setIsCreateElModalOpen(false)}
              handleRequestResult={navigateToApplicant}
              buttons={(
                <>
                  <Button onClick={() => setIsCreateElModalOpen(false)}>{t('common.cancel')}</Button>
                </>
              )}
            />
          )}
          isModalOpen={isCreateElModalOpen}
          setIsModalOpen={setIsCreateElModalOpen}
          withoutContainer={true}
        />
      )}

      <div className={styles.rightControls}>
        <Dropdown
          menu={{
            items: [
              { key: 'ru', label: 'Русский', onClick: () => setLanguage('ru') },
              { key: 'en', label: 'English', onClick: () => setLanguage('en') },
            ],
            selectedKeys: [language],
          }}
          placement="bottomRight"
          trigger={['click']}
        >
          <Button size="small" type="text">
            {language.toUpperCase()} <DownOutlined />
          </Button>
        </Dropdown>

        <Tooltip title={isDark ? t('nav.lightTheme') : t('nav.darkTheme')}>
          <Button
            size="small"
            type="text"
            icon={isDark ? <MoonOutlined /> : <SunOutlined />}
            onClick={toggleTheme}
          />
        </Tooltip>

        <Dropdown
          menu={{ items }}
          placement="bottomLeft"
          arrow
          trigger={['click']}
        >
          <Avatar className={styles.avatar} icon={<UserOutlined />} />
        </Dropdown>
      </div>

      <ChangePasswordModal
        open={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </Header>
  )
}

export default ContentHeader
