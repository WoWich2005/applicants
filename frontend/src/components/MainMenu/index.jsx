import { Link, useLocation } from 'react-router'
import {
  ToolOutlined,
  TeamOutlined,
  IdcardOutlined,
  FileDoneOutlined,
  FolderOpenOutlined,
  DatabaseOutlined,
  BookOutlined,
  SlidersOutlined,
  BankOutlined,
  AuditOutlined,
  FileSearchOutlined,
} from "@ant-design/icons"
import { ConfigProvider, Menu } from 'antd'
import styles from './styles.module.scss'
import { ROUTES } from '../../constants/routes'
import { useLocalStorage } from '../../hooks/useLocalStorage'
import { useAuth } from '../../contexts/AuthContext'
import { useTranslation } from 'react-i18next'

const ALL_ROLES = ['SuperAdmin', 'DataAdministrator', 'Auditor', 'AdmissionsOperator', 'DataViewer']
const AUDIT_ROLES = ['SuperAdmin', 'DataAdministrator', 'Auditor']

function MainMenu({ collapsed }) {
  let [defaultOpenKeys, setDefaultOpenKeys] = useLocalStorage("mainMenuOpenedItems", [])
  const { auth } = useAuth()
  const { t } = useTranslation()
  const role = auth?.role
  const location = useLocation()

  const handleOnOpenChange = (openKeys) => {
    if (!collapsed) setDefaultOpenKeys(openKeys)
  }

  const createMenuItem = (label, path, icon, children) => ({
    key: path,
    label: children === undefined ? <Link to={path}>{label}</Link> : label,
    icon,
    children
  })

  const hasRole = (roles) => roles.includes(role)

  const settingsChildren = [
    hasRole(ALL_ROLES) && createMenuItem(t('nav.evaluationCriteria'), ROUTES.EVALUATION_CRITERIA, <FileDoneOutlined />),
    hasRole(ALL_ROLES) && createMenuItem(t('nav.evaluationCriteriaGroups'), ROUTES.EVALUATION_CRITERIA_GROUPS, <FolderOpenOutlined />),
    hasRole(ALL_ROLES) && createMenuItem(t('nav.users'), ROUTES.USERS, <TeamOutlined />),
  ].filter(Boolean)

  const menuItems = [
    hasRole(ALL_ROLES) && createMenuItem(t('nav.results'), ROUTES.RESULTS, <DatabaseOutlined />),
    hasRole(ALL_ROLES) && {
      type: "group",
      children: [
        createMenuItem(t('nav.applicants'), ROUTES.APPLICANTS, <IdcardOutlined />),
      ]
    },
    hasRole(ALL_ROLES) && {
      type: "group",
      children: [
        hasRole(ALL_ROLES) && createMenuItem(t('nav.faculties'), ROUTES.FACULTIES, <BankOutlined />),
        createMenuItem(t('nav.departments'), ROUTES.DEPARTMENTS, <BookOutlined />),
        createMenuItem(t('nav.specialties'), ROUTES.SPECIALTIES, <ToolOutlined />)
      ].filter(Boolean)
    },
    (hasRole(AUDIT_ROLES) || hasRole(ALL_ROLES)) && {
      type: "group",
      children: [
        hasRole(ALL_ROLES) && createMenuItem(t('audit.nav'), ROUTES.AUDIT, <AuditOutlined />),
        hasRole(ALL_ROLES) && createMenuItem(t('audit.logNav'), ROUTES.AUDIT_LOG, <FileSearchOutlined />),
      ].filter(Boolean)
    },
    settingsChildren.length > 0 && {
      type: "group",
      children: [
        createMenuItem(t('nav.settings'), "Settings", <SlidersOutlined />, settingsChildren)
      ]
    },
  ].filter(Boolean)

  return (
    <ConfigProvider
      theme={{
        components: {
          Menu: {
            activeBarBorderWidth: "0",
          }
        }
      }}
    >
      <Menu
        className={styles.menu}
        selectedKeys={[location.pathname]}
        defaultOpenKeys={defaultOpenKeys}
        mode="inline"
        items={menuItems}
        onOpenChange={handleOnOpenChange}
      />
    </ConfigProvider>
  )
}

export default MainMenu
