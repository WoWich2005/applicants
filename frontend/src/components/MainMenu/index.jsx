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
  BankOutlined
} from "@ant-design/icons"
import { ConfigProvider, Menu } from 'antd'
import styles from './styles.module.scss'
import { ROUTES } from '../../constants/routes'
import { useLocalStorage } from '../../hooks/useLocalStorage'
import { useAuth } from '../../contexts/AuthContext'

const ALL_ROLES = ['SuperAdmin', 'FacultyManager', 'AdmissionsOperator', 'DataViewer']
const MANAGE_ROLES = ['SuperAdmin', 'FacultyManager']
const APPLICANT_ROLES = ['SuperAdmin', 'FacultyManager', 'AdmissionsOperator']
const VIEW_ALL_ROLES = ['SuperAdmin', 'FacultyManager', 'AdmissionsOperator', 'DataViewer']

function MainMenu({ collapsed }) {
  let [defaultOpenKeys, setDefaultOpenKeys] = useLocalStorage("mainMenuOpenedItems", [])
  const { auth } = useAuth()
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
    hasRole(VIEW_ALL_ROLES) && createMenuItem("Оценочные параметры", ROUTES.EVALUATION_CRITERIA, <FileDoneOutlined />),
    hasRole(VIEW_ALL_ROLES) && createMenuItem("Группы оценочных параметров", ROUTES.EVALUATION_CRITERIA_GROUPS, <FolderOpenOutlined />),
    hasRole(['SuperAdmin']) && createMenuItem("Пользователи", ROUTES.USERS, <TeamOutlined />),
  ].filter(Boolean)

  const menuItems = [
    hasRole(ALL_ROLES) && createMenuItem("Результаты", ROUTES.RESULTS, <DatabaseOutlined />),
    hasRole(ALL_ROLES) && {
      type: "group",
      children: [
        createMenuItem("Абитуриенты", ROUTES.APPLICANTS, <IdcardOutlined />),
      ]
    },
    hasRole(VIEW_ALL_ROLES) && {
      type: "group",
      children: [
        hasRole(VIEW_ALL_ROLES) && createMenuItem("Факультеты", ROUTES.FACULTIES, <BankOutlined />),
        createMenuItem("Кафедры", ROUTES.DEPARTMENTS, <BookOutlined />),
        createMenuItem("Специальности", ROUTES.SPECIALTIES, <ToolOutlined />)
      ].filter(Boolean)
    },
    settingsChildren.length > 0 && {
      type: "group",
      children: [
        createMenuItem("Настройки", "Settings", <SlidersOutlined />, settingsChildren)
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
