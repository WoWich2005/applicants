import { useEffect, useState } from 'react'
import { Button, Tag, message } from 'antd'
import Title from '../components/Title'
import CrudTable from '../components/CrudTable'
import UserForm from '../components/Forms/UserForm'
import { usersApi } from '../api/usersApi'
import { facultiesApi } from '../api/facultyApi'
import { specialtiesApi } from '../api/specialtiesApi'
import { departmentsApi } from '../api/departmentsApi'
import { useTranslation } from 'react-i18next'

const ROLE_VALUES = ['SuperAdmin', 'FacultyManager', 'AdmissionsOperator', 'DataViewer']

const ROLE_COLORS = {
  SuperAdmin: 'red',
  FacultyManager: 'blue',
  AdmissionsOperator: 'green',
  DataViewer: 'orange',
}

function Users() {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [faculties, setFaculties] = useState([])
  const [specialties, setSpecialties] = useState([])
  const [departments, setDepartments] = useState([])

  useEffect(() => {
    facultiesApi.getAll().then(r => setFaculties(r.data)).catch(() => {})
    specialtiesApi.getAll().then(r => setSpecialties(r.data)).catch(() => {})
    departmentsApi.getAll().then(r => setDepartments(r.data)).catch(() => {})
  }, [])

  const handleToggleActive = async (/** @type {any} */ user, /** @type {any} */ updateEl) => {
    try {
      await usersApi.toggleActive(user.id)
      const updated = (await usersApi.getById(user.id)).data
      updateEl(updated)
      messageApi.success(user.isActive ? t('users.deactivated') : t('users.activated'))
    } catch {
      messageApi.error(t('users.statusError'))
    }
  }

  const ROLES = ROLE_VALUES.map(value => ({ value, label: t(`users.roles.${value}`) }))

  return (
    <>
      {contextHolder}
      <Title title={t('users.title')} />
      <CrudTable
        elementForm={UserForm}
        elementFormProps={{ faculties, specialties, departments }}

        serverSidePagination={true}
        getPagedAsync={(/** @type {any} */ params) => usersApi.getPaged({
          page: params.page,
          pageSize: params.pageSize,
          search: params.filters?.username?.[0],
          role: params.filters?.role?.[0],
          isActive: params.filters?.isActive?.[0],
        })}
        deleteAsync={(/** @type {any} */ id) => usersApi.delete(id)}

        addButtonTitle={t('users.addButton')}
        renderEditTitle={(/** @type {any} */ el) => t('users.editTitle', { username: el?.username })}
        renderDeleteText={(/** @type {any} */ el) => t('users.deleteText', { username: el?.username })}

        extraActions={(/** @type {any} */ user, /** @type {any} */ updateEl) => (
          <Button type="link" onClick={() => handleToggleActive(user, updateEl)}>
            {user.isActive ? t('users.deactivate') : t('users.activate')}
          </Button>
        )}

        columns={[
          {
            title: t('users.colLogin'),
            dataIndex: 'username',
            key: 'username',
            withSearch: true,
          },
          {
            title: t('users.colRole'),
            dataIndex: 'role',
            key: 'role',
            filters: ROLES.map(r => ({ text: r.label, value: r.value })),
            filterMultiple: false,
            render: (/** @type {keyof typeof ROLE_COLORS} */ role) => (
              <Tag color={ROLE_COLORS[role]}>{t(`users.roles.${role}`, role)}</Tag>
            ),
          },
          {
            title: t('users.colStatus'),
            dataIndex: 'isActive',
            key: 'isActive',
            filters: [
              { text: t('users.statusActive'), value: true },
              { text: t('users.statusInactive'), value: false },
            ],
            filterMultiple: false,
            render: (/** @type {boolean} */ active) => (
              <Tag color={active ? 'success' : 'default'}>
                {active ? t('users.statusActive') : t('users.statusInactive')}
              </Tag>
            ),
          },
        ]}
      />
    </>
  )
}

export default Users
