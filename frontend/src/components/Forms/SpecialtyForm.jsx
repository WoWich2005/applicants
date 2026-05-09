import { Button, Form, Input, message, Select, Skeleton, Space } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { specialtiesApi } from "../../api/specialtiesApi"
import { departmentsApi } from "../../api/departmentsApi"
import { facultiesApi } from "../../api/facultyApi"
import { useAuth } from "../../contexts/AuthContext"
import { useTranslation } from "react-i18next"

function SpecialtyForm(/** @type {any} */ props) {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)

  const { auth } = useAuth()
  const isFacultyManager = auth?.role === 'FacultyManager'
  const managerFacultyId = isFacultyManager ? auth?.facultyId : null

  const [faculties, setFaculties] = useState(/** @type {Array<{id: number, name: string}>} */ ([]))
  const [isFacultiesLoading, setIsFacultiesLoading] = useState(true)

  const [departments, setDepartments] = useState(/** @type {Array<{id: number, name: string, facultyId: number}>} */ ([]))
  const [isDepartmentsLoading, setIsDepartmentsLoading] = useState(false)

  const [selectedFacultyId, setSelectedFacultyId] = useState(/** @type {number | null} */ (null))

  useEffect(() => {
    const fetchFaculties = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const responsePromise = facultiesApi.getAll()
        const [_, response] = await Promise.all([delayPromise, responsePromise])
        setFaculties(response.data)
        setIsFacultiesLoading(false)
      } catch {
        messageApi.error(t('specialty.form.fetchFacultiesError'))
      }
    }

    fetchFaculties()
  }, [])

  // Restore selections when editing
  useEffect(() => {
    if (!props.initialValues?.departmentId || isFacultiesLoading) return

    const fetchInitialDepartments = async () => {
      try {
        const deptResponse = await departmentsApi.getById(props.initialValues.departmentId)
        const targetDept = deptResponse.data

        const deptsResponse = await departmentsApi.getByFacultyId(targetDept.facultyId)
        const depts = deptsResponse.data

        setSelectedFacultyId(targetDept.facultyId)
        setDepartments(depts)
        form.setFieldsValue(props.initialValues)
      } catch {
        messageApi.error(t('specialty.form.fetchDepartmentError'))
      }
    }

    fetchInitialDepartments()
  }, [props.initialValues, isFacultiesLoading])

  // For new form: set field values; for FacultyManager: auto-load their departments
  useEffect(() => {
    if (isFacultiesLoading) return

    if (!props.initialValues?.departmentId) {
      form.setFieldsValue(props.initialValues)
    }

    if (managerFacultyId && !props.initialValues?.departmentId) {
      setSelectedFacultyId(managerFacultyId)
      setIsDepartmentsLoading(true)
      departmentsApi.getByFacultyId(managerFacultyId)
        .then(r => setDepartments(r.data))
        .catch(() => messageApi.error(t('specialty.form.fetchDepartmentsError')))
        .finally(() => setIsDepartmentsLoading(false))
    }
  }, [isFacultiesLoading])

  const handleFacultyChange = async (/** @type {number} */ facultyId) => {
    setSelectedFacultyId(facultyId)
    form.setFieldValue("departmentId", undefined)
    setDepartments([])
    setIsDepartmentsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 300))
      const responsePromise = departmentsApi.getByFacultyId(facultyId)
      const [_, response] = await Promise.all([delayPromise, responsePromise])
      setDepartments(response.data)
    } catch {
      messageApi.error(t('specialty.form.fetchDepartmentsError'))
    } finally {
      setIsDepartmentsLoading(false)
    }
  }

  const api = specialtiesApi
  const onFinish = async (/** @type {any} */ formData) => {
    setIsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 500))

      if (props.elementId) {
        const updatePromise = api.update(props.elementId, formData)
        await Promise.all([delayPromise, updatePromise])

        const updatedEl = (await api.getById(props.elementId)).data
        props.handleRequestResult && props.handleRequestResult(updatedEl)
      } else {
        const createPromise = api.create(formData)
        const [_, createdElResponse] = await Promise.all([delayPromise, createPromise])
        props.handleRequestResult && props.handleRequestResult(createdElResponse.data)
      }

      form.resetFields()
      setSelectedFacultyId(managerFacultyId ?? null)
      if (!managerFacultyId) setDepartments([])
    } catch (err) {
      const serverMessage = err?.response?.data?.message
      messageApi.error(serverMessage ?? t('specialty.form.saveError'))
    } finally {
      setIsLoading(false)
    }
  }

  if (isFacultiesLoading) {
    return <Skeleton paragraph={{ rows: 4 }} />
  }

  return (
    <>
      {contextHolder}
      <Form
        form={form}
        className={styles.form}
        layout="vertical"
        initialValues={props.initialValues}
        onFinish={onFinish}
        autoComplete="off"
        disabled={!!props.readOnly}
      >
        <div className={styles.formRow}>
          <div className={styles.formColumn}>
            <Form.Item
              label={t('specialty.form.nameLabel')}
              name="name"
              rules={[{ required: true, message: t('specialty.form.nameRequired') }]}
            >
              <Input />
            </Form.Item>

            <Form.Item label={t('specialty.form.facultyLabel')}>
              <Select
                value={selectedFacultyId}
                onChange={handleFacultyChange}
                placeholder={t('specialty.form.facultyPlaceholder')}
                disabled={isFacultyManager || !!props.readOnly}
              >
                {faculties.map(faculty => (
                  <Select.Option value={faculty.id} key={faculty.id}>
                    {faculty.name}
                  </Select.Option>
                ))}
              </Select>
            </Form.Item>

            {selectedFacultyId && (
              isDepartmentsLoading ? (
                <Skeleton paragraph={{ rows: 1 }} active />
              ) : (
                <Form.Item
                  label={t('specialty.form.departmentLabel')}
                  name="departmentId"
                  rules={[{ required: true, message: t('specialty.form.departmentRequired') }]}
                >
                  <Select placeholder={t('specialty.form.departmentPlaceholder')}>
                    {departments.map(department => (
                      <Select.Option value={department.id} key={department.id}>
                        {department.name}
                      </Select.Option>
                    ))}
                  </Select>
                </Form.Item>
              )
            )}
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? t('specialty.form.updateButton') : t('specialty.form.addButton')}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default SpecialtyForm
