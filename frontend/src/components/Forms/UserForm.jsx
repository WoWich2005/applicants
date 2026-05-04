import { Button, Form, Input, message, Select, Space } from "antd"
import { useEffect, useState } from "react"
import { usersApi } from "../../api/usersApi"

const ROLES = [
  { value: 'SuperAdmin', label: 'Суперпользователь' },
  { value: 'FacultyManager', label: 'Роль факультета' },
  { value: 'AdmissionsOperator', label: 'Оператор приёмной комиссии' },
  { value: 'ResultViewer', label: 'Просмотр результатов' },
]

function UserForm(props) {
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)
  const [filterFacultyIds, setFilterFacultyIds] = useState(/** @type {number[]} */ ([]))
  const selectedRole = Form.useWatch('role', form)

  const getFilteredSpecialties = (facultyIds) => {
    const specialties = props.specialties ?? []
    if (!facultyIds.length) return specialties

    const departments = props.departments ?? []
    const allowedDeptIds = new Set(
      departments.filter(d => facultyIds.includes(d.facultyId)).map(d => d.id)
    )
    return specialties.filter(s => allowedDeptIds.has(s.departmentId))
  }

  const computeFacultyIdsFromSpecialties = (specialtyIds) => {
    const departments = props.departments ?? []
    const specialties = props.specialties ?? []
    const specDeptIds = new Set(
      specialties.filter(s => specialtyIds.includes(s.id)).map(s => s.departmentId)
    )
    return [...new Set(departments.filter(d => specDeptIds.has(d.id)).map(d => d.facultyId))]
  }

  useEffect(() => {
    if (props.elementId) {
      const currentSpecialtyIds = props.initialValues?.specialtyIds ?? []
      setFilterFacultyIds(computeFacultyIdsFromSpecialties(currentSpecialtyIds))
      form.setFieldsValue({
        username: props.initialValues?.username,
        role: props.initialValues?.role,
        facultyId: props.initialValues?.facultyId,
        specialtyIds: props.initialValues?.specialtyIds,
        facultyAccessIds: props.initialValues?.facultyAccessIds,
      })
    } else {
      form.resetFields()
      setFilterFacultyIds([])
    }
  }, [props.initialValues, props.elementId, form])

  const handleFacultyFilterChange = (ids) => {
    setFilterFacultyIds(ids)
    const current = form.getFieldValue('specialtyIds') ?? []
    const filtered = getFilteredSpecialties(ids)
    const filteredIdSet = new Set(filtered.map(s => s.id))
    form.setFieldValue('specialtyIds', current.filter(id => filteredIdSet.has(id)))
  }

  const onFinish = async (values) => {
    setIsLoading(true)
    try {
      const payload = {
        username: values.username,
        password: values.password || undefined,
        role: values.role,
        facultyId: values.role === 'FacultyManager' ? values.facultyId : null,
        specialtyIds: values.role === 'AdmissionsOperator' ? (values.specialtyIds ?? []) : [],
        facultyAccessIds: values.role === 'ResultViewer' ? (values.facultyAccessIds ?? []) : [],
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
      setFilterFacultyIds([])
    } catch (err) {
      messageApi.error(err.response?.data?.message || 'Ошибка сохранения')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <>
      {contextHolder}
      <Form form={form} layout="vertical" onFinish={onFinish} autoComplete="off">
        <Form.Item name="username" label="Логин" rules={[{ required: true, message: 'Введите логин' }]}>
          <Input />
        </Form.Item>
        <Form.Item
          name="password"
          label="Пароль"
          rules={props.elementId ? [] : [
            { required: true, message: 'Введите пароль' },
            { min: 6, message: 'Минимум 6 символов' },
          ]}
        >
          <Input.Password placeholder={props.elementId ? 'Оставьте пустым чтобы не менять' : ''} />
        </Form.Item>
        <Form.Item name="role" label="Роль" rules={[{ required: true, message: 'Выберите роль' }]}>
          <Select options={ROLES} />
        </Form.Item>

        {selectedRole === 'FacultyManager' && (
          <Form.Item name="facultyId" label="Факультет" rules={[{ required: true, message: 'Выберите факультет' }]}>
            <Select
              options={(props.faculties ?? []).map(f => ({ value: f.id, label: f.name }))}
              placeholder="Выберите факультет"
            />
          </Form.Item>
        )}

        {selectedRole === 'AdmissionsOperator' && (
          <>
            <Form.Item label="Факультеты">
              <Select
                mode="multiple"
                options={(props.faculties ?? []).map(f => ({ value: f.id, label: f.name }))}
                value={filterFacultyIds}
                onChange={handleFacultyFilterChange}
                placeholder="Выберите факультеты"
              />
            </Form.Item>
            <Form.Item name="specialtyIds" label="Доступные специальности">
              <Select
                mode="multiple"
                options={getFilteredSpecialties(filterFacultyIds).map(s => ({ value: s.id, label: s.name }))}
                placeholder={filterFacultyIds.length ? 'Выберите специальности' : 'Сначала выберите факультеты'}
                disabled={!filterFacultyIds.length}
              />
            </Form.Item>
          </>
        )}

        {selectedRole === 'ResultViewer' && (
          <Form.Item name="facultyAccessIds" label="Доступные факультеты для просмотра результатов">
            <Select
              mode="multiple"
              options={(props.faculties ?? []).map(f => ({ value: f.id, label: f.name }))}
              placeholder="Выберите факультеты (пусто = все)"
            />
          </Form.Item>
        )}

        <Space>
          <Button type="primary" htmlType="submit" loading={isLoading}>
            {props.elementId ? 'Сохранить' : 'Создать'}
          </Button>
          {props.buttons}
        </Space>
      </Form>
    </>
  )
}

export default UserForm
