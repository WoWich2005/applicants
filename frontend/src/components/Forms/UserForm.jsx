import { Button, Form, Input, message, Select, Space } from "antd"
import { useEffect, useState } from "react"
import { usersApi } from "../../api/usersApi"
import { useTranslation } from "react-i18next"

const ROLE_VALUES = ['SuperAdmin', 'FacultyManager', 'AdmissionsOperator', 'DataViewer']

function UserForm(props) {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)
  const [filterFacultyIds, setFilterFacultyIds] = useState(/** @type {number[]} */ ([]))
  const selectedRole = Form.useWatch('role', form)

  const ROLES = ROLE_VALUES.map(value => ({ value, label: t(`users.roles.${value}`) }))

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
        facultyAccessIds: values.role === 'DataViewer' ? (values.facultyAccessIds ?? []) : [],
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
      messageApi.error(err.response?.data?.message || t('users.form.saveError'))
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <>
      {contextHolder}
      <Form form={form} layout="vertical" onFinish={onFinish} autoComplete="off">
        <Form.Item name="username" label={t('users.form.loginLabel')} rules={[{ required: true, message: t('users.form.loginRequired') }]}>
          <Input />
        </Form.Item>
        <Form.Item
          name="password"
          label={t('users.form.passwordLabel')}
          rules={props.elementId ? [] : [
            { required: true, message: t('users.form.passwordRequired') },
            { min: 6, message: t('users.form.passwordMinLength') },
          ]}
        >
          <Input.Password placeholder={props.elementId ? t('users.form.passwordLeaveEmpty') : ''} />
        </Form.Item>
        <Form.Item name="role" label={t('users.form.roleLabel')} rules={[{ required: true, message: t('users.form.roleRequired') }]}>
          <Select options={ROLES} />
        </Form.Item>

        {selectedRole === 'FacultyManager' && (
          <Form.Item name="facultyId" label={t('users.form.facultyLabel')} rules={[{ required: true, message: t('users.form.facultyRequired') }]}>
            <Select
              options={(props.faculties ?? []).map(f => ({ value: f.id, label: f.name }))}
              placeholder={t('users.form.facultyPlaceholder')}
            />
          </Form.Item>
        )}

        {selectedRole === 'AdmissionsOperator' && (
          <>
            <Form.Item label={t('users.form.filterFacultiesLabel')}>
              <Select
                mode="multiple"
                options={(props.faculties ?? []).map(f => ({ value: f.id, label: f.name }))}
                value={filterFacultyIds}
                onChange={handleFacultyFilterChange}
                placeholder={t('users.form.filterFacultiesPlaceholder')}
              />
            </Form.Item>
            <Form.Item name="specialtyIds" label={t('users.form.specialtiesLabel')}>
              <Select
                mode="multiple"
                options={getFilteredSpecialties(filterFacultyIds).map(s => ({ value: s.id, label: s.name }))}
                placeholder={filterFacultyIds.length ? t('users.form.specialtiesPlaceholder') : t('users.form.specialtiesDisabledPlaceholder')}
                disabled={!filterFacultyIds.length}
              />
            </Form.Item>
          </>
        )}

        {selectedRole === 'DataViewer' && (
          <Form.Item name="facultyAccessIds" label={t('users.form.facultyAccessLabel')}>
            <Select
              mode="multiple"
              options={(props.faculties ?? []).map(f => ({ value: f.id, label: f.name }))}
              placeholder={t('users.form.facultyAccessPlaceholder')}
            />
          </Form.Item>
        )}

        <Space>
          <Button type="primary" htmlType="submit" loading={isLoading}>
            {props.elementId ? t('users.form.saveButton') : t('users.form.createButton')}
          </Button>
          {props.buttons}
        </Space>
      </Form>
    </>
  )
}

export default UserForm
