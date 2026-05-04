import { useEffect, useState } from 'react'
import { Button, Tag, message } from 'antd'
import Title from '../components/Title'
import CrudTable from '../components/CrudTable'
import UserForm from '../components/Forms/UserForm'
import { usersApi } from '../api/usersApi'
import { facultiesApi } from '../api/facultyApi'
import { specialtiesApi } from '../api/specialtiesApi'
import { departmentsApi } from '../api/departmentsApi'

const ROLES = [
  { value: 'SuperAdmin', label: 'Суперпользователь' },
  { value: 'FacultyManager', label: 'Роль факультета' },
  { value: 'AdmissionsOperator', label: 'Оператор приёмной комиссии' },
  { value: 'DataViewer', label: 'Просмотр данных' },
]

const ROLE_COLORS = {
  SuperAdmin: 'red',
  FacultyManager: 'blue',
  AdmissionsOperator: 'green',
  DataViewer: 'orange',
}

function getRoleLabel(/** @type {string} */ role) {
  return ROLES.find(r => r.value === role)?.label ?? role
}

function Users() {
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
      messageApi.success(user.isActive ? 'Пользователь деактивирован' : 'Пользователь активирован')
    } catch {
      messageApi.error('Ошибка изменения статуса')
    }
  }

  return (
    <>
      {contextHolder}
      <Title title="Пользователи" />
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

        addButtonTitle="Новый пользователь"
        renderEditTitle={(/** @type {any} */ el) => `Редактирование: "${el?.username}"`}
        renderDeleteText={(/** @type {any} */ el) => `Удалить пользователя "${el?.username}"?`}

        extraActions={(/** @type {any} */ user, /** @type {any} */ updateEl) => (
          <Button type="link" onClick={() => handleToggleActive(user, updateEl)}>
            {user.isActive ? 'Деактивировать' : 'Активировать'}
          </Button>
        )}

        columns={[
          {
            title: 'Логин',
            dataIndex: 'username',
            key: 'username',
            withSearch: true,
          },
          {
            title: 'Роль',
            dataIndex: 'role',
            key: 'role',
            filters: ROLES.map(r => ({ text: r.label, value: r.value })),
            filterMultiple: false,
            render: (/** @type {keyof typeof ROLE_COLORS} */ role) => <Tag color={ROLE_COLORS[role]}>{getRoleLabel(role)}</Tag>,
          },
          {
            title: 'Статус',
            dataIndex: 'isActive',
            key: 'isActive',
            filters: [
              { text: 'Активен', value: true },
              { text: 'Неактивен', value: false },
            ],
            filterMultiple: false,
            render: (/** @type {boolean} */ active) => <Tag color={active ? 'success' : 'default'}>{active ? 'Активен' : 'Неактивен'}</Tag>,
          },
        ]}
      />
    </>
  )
}

export default Users
