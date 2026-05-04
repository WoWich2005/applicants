import { Button, Form, Input, InputNumber, Select, Skeleton, Space, message } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { competitionListsApi } from "../../api/competitionListsApi"
import { specialtiesApi } from "../../api/specialtiesApi"
import { departmentsApi } from "../../api/departmentsApi"
import { facultiesApi } from "../../api/facultyApi"

function CompetitionListForm(/** @type {any} */ props) {
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)

  const [faculties, setFaculties] = useState(/** @type {Array<{id: number, name: string}>} */ ([]))
  const [isFacultiesLoading, setIsFacultiesLoading] = useState(true)

  const [departments, setDepartments] = useState(/** @type {Array<{id: number, name: string, facultyId: number}>} */ ([]))
  const [isDepartmentsLoading, setIsDepartmentsLoading] = useState(false)

  const [specialties, setSpecialties] = useState(/** @type {Array<{id: number, name: string, departmentId: number}>} */ ([]))
  const [isSpecialtiesLoading, setIsSpecialtiesLoading] = useState(false)

  const [selectedFacultyId, setSelectedFacultyId] = useState(/** @type {number | null} */ (null))
  const [selectedDepartmentId, setSelectedDepartmentId] = useState(/** @type {number | null} */ (null))

  useEffect(() => {
    const fetchFaculties = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, facultiesApi.getAll()])
        setFaculties(response.data)
        setIsFacultiesLoading(false)
      } catch {
        messageApi.error("Не удалось получить список факультетов")
      }
    }

    fetchFaculties()
  }, [])

  useEffect(() => {
    if (!props.initialValues?.specialtyId || isFacultiesLoading) return

    const restoreSelections = async () => {
      try {
        const specialty = (await specialtiesApi.getById(props.initialValues.specialtyId)).data
        const department = (await departmentsApi.getById(specialty.departmentId)).data

        const [deptResponse, specResponse] = await Promise.all([
          departmentsApi.getByFacultyId(department.facultyId),
          specialtiesApi.getByDepartmentId(specialty.departmentId),
        ])

        setSelectedFacultyId(department.facultyId)
        setDepartments(deptResponse.data)
        setSelectedDepartmentId(specialty.departmentId)
        setSpecialties(specResponse.data)
        form.setFieldsValue(props.initialValues)
      } catch {
        messageApi.error("Не удалось восстановить данные формы")
      }
    }

    restoreSelections()
  }, [props.initialValues, isFacultiesLoading])

  useEffect(() => {
    if (!props.initialValues?.specialtyId) {
      form.setFieldsValue(props.initialValues)
    }
  }, [props.initialValues, form])

  const handleFacultyChange = async (/** @type {number} */ facultyId) => {
    setSelectedFacultyId(facultyId)
    setSelectedDepartmentId(null)
    setDepartments([])
    setSpecialties([])
    form.setFieldValue("departmentId", undefined)
    form.setFieldValue("specialtyId", undefined)
    setIsDepartmentsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 300))
      const [_, response] = await Promise.all([delayPromise, departmentsApi.getByFacultyId(facultyId)])
      setDepartments(response.data)
    } catch {
      messageApi.error("Не удалось получить список кафедр")
    } finally {
      setIsDepartmentsLoading(false)
    }
  }

  const handleDepartmentChange = async (/** @type {number} */ departmentId) => {
    setSelectedDepartmentId(departmentId)
    setSpecialties([])
    form.setFieldValue("specialtyId", undefined)
    setIsSpecialtiesLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 300))
      const [_, response] = await Promise.all([delayPromise, specialtiesApi.getByDepartmentId(departmentId)])
      setSpecialties(response.data)
    } catch {
      messageApi.error("Не удалось получить список специальностей")
    } finally {
      setIsSpecialtiesLoading(false)
    }
  }

  const onFinish = async (/** @type {any} */ formData) => {
    setIsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 500))

      if (props.elementId) {
        const updatePromise = competitionListsApi.update(props.elementId, formData)
        await Promise.all([delayPromise, updatePromise])

        const updatedEl = (await competitionListsApi.getById(props.elementId)).data
        props.handleRequestResult && props.handleRequestResult(updatedEl)
      } else {
        const createPromise = competitionListsApi.create(formData)
        const [_, createdElResponse] = await Promise.all([delayPromise, createPromise])
        props.handleRequestResult && props.handleRequestResult(createdElResponse.data)
      }

      form.resetFields()
      setSelectedFacultyId(null)
      setSelectedDepartmentId(null)
      setDepartments([])
      setSpecialties([])
    } catch {
      messageApi.error("Ошибка сохранения конкурсного списка на сервере")
    } finally {
      setIsLoading(false)
    }
  }

  if (isFacultiesLoading) {
    return <Skeleton paragraph={{ rows: 5 }} />
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
              label="Название конкурсного списка"
              name="name"
              rules={[{ required: true, message: "Название обязательно для заполнения" }]}
            >
              <Input />
            </Form.Item>

            <Form.Item
              label="План набора"
              name="plan"
              rules={[{ required: true, message: "План набора обязателен" }]}
            >
              <InputNumber min={1} style={{ width: '100%' }} />
            </Form.Item>

            <Form.Item label="Факультет">
              <Select
                value={selectedFacultyId}
                onChange={handleFacultyChange}
                placeholder="Выберите факультет"
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
                <Form.Item label="Кафедра">
                  <Select
                    value={selectedDepartmentId}
                    onChange={handleDepartmentChange}
                    placeholder="Выберите кафедру"
                  >
                    {departments.map(department => (
                      <Select.Option value={department.id} key={department.id}>
                        {department.name}
                      </Select.Option>
                    ))}
                  </Select>
                </Form.Item>
              )
            )}

            {selectedDepartmentId && (
              isSpecialtiesLoading ? (
                <Skeleton paragraph={{ rows: 1 }} active />
              ) : (
                <Form.Item
                  label="Специальность"
                  name="specialtyId"
                  rules={[{ required: true, message: "Специальность обязательна" }]}
                >
                  <Select placeholder="Выберите специальность">
                    {specialties.map(s => (
                      <Select.Option value={s.id} key={s.id}>
                        {s.name}
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
                {props.elementId ? 'Обновить список' : 'Добавить список'}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default CompetitionListForm
