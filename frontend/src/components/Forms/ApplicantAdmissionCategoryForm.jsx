import { Button, Form, InputNumber, Select, Skeleton, Space, message } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { applicantAdmissionCategoriesApi } from "../../api/applicantAdmissionCategoriesApi"
import { admissionCategoriesApi } from "../../api/admissionCategoriesApi"
import { specialtiesApi } from "../../api/specialtiesApi"
import { departmentsApi } from "../../api/departmentsApi"
import { facultiesApi } from "../../api/facultyApi"
import { competitionListsApi } from "../../api/competitionListsApi"
import { useAuth } from "../../contexts/AuthContext"

function ApplicantAdmissionCategoryForm(/** @type {any} */ props) {
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)

  const { auth } = useAuth()
  const isOperator = auth?.role === 'AdmissionsOperator'
  const isFacultyManager = auth?.role === 'FacultyManager'
  const managerFacultyId = isFacultyManager ? auth?.facultyId : null
  const allowedSpecialtyIds = isOperator ? (auth?.specialtyIds ?? []) : null

  const [faculties, setFaculties] = useState(/** @type {Array<{id: number, name: string}>} */ ([]))
  const [isFacultiesLoading, setIsFacultiesLoading] = useState(true)

  const [departments, setDepartments] = useState(/** @type {Array<{id: number, name: string, facultyId: number}>} */ ([]))
  const [isDepartmentsLoading, setIsDepartmentsLoading] = useState(false)

  const [specialties, setSpecialties] = useState(/** @type {Array<{id: number, name: string, departmentId: number}>} */ ([]))
  const [isSpecialtiesLoading, setIsSpecialtiesLoading] = useState(false)

  const [competitionLists, setCompetitionLists] = useState(/** @type {Array<{id: number, name: string, specialtyId: number}>} */ ([]))
  const [isCompetitionListsLoading, setIsCompetitionListsLoading] = useState(false)

  const [categories, setCategories] = useState(/** @type {Array<{id: number, name: string}>} */ ([]))
  const [isCategoriesLoading, setIsCategoriesLoading] = useState(false)

  const [selectedFacultyId, setSelectedFacultyId] = useState(/** @type {number | null} */ (null))
  const [selectedDepartmentId, setSelectedDepartmentId] = useState(/** @type {number | null} */ (null))
  const [selectedSpecialtyId, setSelectedSpecialtyId] = useState(/** @type {number | null} */ (null))
  const [selectedCompetitionListId, setSelectedCompetitionListId] = useState(/** @type {number | null} */ (null))

  // For AdmissionsOperator: maps allowed specialty/department/faculty IDs
  const [allowedSets, setAllowedSets] = useState(/** @type {{specialtyIds: Set<number>, departmentIds: Set<number>, facultyIds: Set<number>} | null} */ (null))
  const [isAllowedSetsLoading, setIsAllowedSetsLoading] = useState(!!(isOperator && allowedSpecialtyIds?.length))

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
    if (!isOperator || !allowedSpecialtyIds?.length) {
      setIsAllowedSetsLoading(false)
      return
    }

    const buildAllowedSets = async () => {
      try {
        const specResponses = await Promise.all(allowedSpecialtyIds.map(id => specialtiesApi.getById(id)))
        const specs = specResponses.map(r => r.data)

        const uniqueDeptIds = [...new Set(specs.map(s => s.departmentId))]
        const deptResponses = await Promise.all(uniqueDeptIds.map(id => departmentsApi.getById(id)))
        const depts = deptResponses.map(r => r.data)

        setAllowedSets({
          specialtyIds: new Set(allowedSpecialtyIds),
          departmentIds: new Set(uniqueDeptIds),
          facultyIds: new Set(depts.map(d => d.facultyId)),
        })
      } catch {
        messageApi.error("Не удалось загрузить данные доступных специальностей")
      } finally {
        setIsAllowedSetsLoading(false)
      }
    }

    buildAllowedSets()
  }, [])

  // FacultyManager: auto-select their faculty and load departments when creating
  useEffect(() => {
    if (isFacultiesLoading || !managerFacultyId || props.initialValues?.admissionCategoryId) return

    setSelectedFacultyId(managerFacultyId)
    setIsDepartmentsLoading(true)
    departmentsApi.getByFacultyId(managerFacultyId)
      .then(r => setDepartments(r.data))
      .catch(() => messageApi.error("Не удалось получить список кафедр"))
      .finally(() => setIsDepartmentsLoading(false))
  }, [isFacultiesLoading])

  useEffect(() => {
    if (!props.initialValues?.admissionCategoryId || isFacultiesLoading) return

    const restoreSelections = async () => {
      try {
        const category = (await admissionCategoriesApi.getById(props.initialValues.admissionCategoryId)).data
        const competitionList = (await competitionListsApi.getById(category.competitionListId)).data
        const specialty = (await specialtiesApi.getById(competitionList.specialtyId)).data
        const department = (await departmentsApi.getById(specialty.departmentId)).data

        const [deptResponse, specResponse, compListsResponse, catResponse] = await Promise.all([
          departmentsApi.getByFacultyId(department.facultyId),
          specialtiesApi.getByDepartmentId(specialty.departmentId),
          competitionListsApi.getBySpecialtyId(competitionList.specialtyId),
          admissionCategoriesApi.getAllByCompetitionList(category.competitionListId),
        ])

        setSelectedFacultyId(department.facultyId)
        setDepartments(deptResponse.data)
        setSelectedDepartmentId(specialty.departmentId)
        setSpecialties(specResponse.data)
        setSelectedSpecialtyId(competitionList.specialtyId)
        setCompetitionLists(compListsResponse.data)
        setSelectedCompetitionListId(category.competitionListId)
        setCategories(catResponse.data)

        form.setFieldsValue({ ...props.initialValues, applicantId: props.applicantId })
      } catch {
        messageApi.error("Не удалось восстановить данные формы")
      }
    }

    restoreSelections()
  }, [props.initialValues, isFacultiesLoading])

  useEffect(() => {
    if (!props.initialValues?.admissionCategoryId) {
      form.setFieldsValue({ ...props.initialValues, applicantId: props.applicantId })
    }
  }, [props.initialValues, props.applicantId, form])

  const handleFacultyChange = async (/** @type {number} */ facultyId) => {
    setSelectedFacultyId(facultyId)
    setSelectedDepartmentId(null)
    setSelectedSpecialtyId(null)
    setSelectedCompetitionListId(null)
    setDepartments([])
    setSpecialties([])
    setCompetitionLists([])
    setCategories([])
    form.setFieldValue("admissionCategoryId", undefined)
    setIsDepartmentsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 300))
      const [_, response] = await Promise.all([delayPromise, departmentsApi.getByFacultyId(facultyId)])
      const loaded = response.data
      setDepartments(allowedSets ? loaded.filter(d => allowedSets.departmentIds.has(d.id)) : loaded)
    } catch {
      messageApi.error("Не удалось получить список кафедр")
    } finally {
      setIsDepartmentsLoading(false)
    }
  }

  const handleDepartmentChange = async (/** @type {number} */ departmentId) => {
    setSelectedDepartmentId(departmentId)
    setSelectedSpecialtyId(null)
    setSelectedCompetitionListId(null)
    setSpecialties([])
    setCompetitionLists([])
    setCategories([])
    form.setFieldValue("admissionCategoryId", undefined)
    setIsSpecialtiesLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 300))
      const [_, response] = await Promise.all([delayPromise, specialtiesApi.getByDepartmentId(departmentId)])
      const loaded = response.data
      setSpecialties(allowedSets ? loaded.filter(s => allowedSets.specialtyIds.has(s.id)) : loaded)
    } catch {
      messageApi.error("Не удалось получить список специальностей")
    } finally {
      setIsSpecialtiesLoading(false)
    }
  }

  const handleSpecialtyChange = async (/** @type {number} */ specialtyId) => {
    setSelectedSpecialtyId(specialtyId)
    setSelectedCompetitionListId(null)
    setCompetitionLists([])
    setCategories([])
    form.setFieldValue("admissionCategoryId", undefined)
    setIsCompetitionListsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 300))
      const [_, response] = await Promise.all([delayPromise, competitionListsApi.getBySpecialtyId(specialtyId)])
      setCompetitionLists(response.data)
    } catch {
      messageApi.error("Не удалось получить список конкурсных списков")
    } finally {
      setIsCompetitionListsLoading(false)
    }
  }

  const handleCompetitionListChange = async (/** @type {number} */ competitionListId) => {
    setSelectedCompetitionListId(competitionListId)
    setCategories([])
    form.setFieldValue("admissionCategoryId", undefined)
    setIsCategoriesLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 300))
      const [_, response] = await Promise.all([delayPromise, admissionCategoriesApi.getAllByCompetitionList(competitionListId)])
      setCategories(response.data)
    } catch {
      messageApi.error("Не удалось получить список категорий приема")
    } finally {
      setIsCategoriesLoading(false)
    }
  }

  const onFinish = async (/** @type {any} */ formData) => {
    formData = { ...formData, applicantId: Number(props.applicantId) }
    setIsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 500))

      if (props.elementId) {
        const updatePromise = applicantAdmissionCategoriesApi.update(props.elementId, formData)
        await Promise.all([delayPromise, updatePromise])

        const updatedEl = (await applicantAdmissionCategoriesApi.getById(props.elementId)).data
        props.handleRequestResult && props.handleRequestResult(updatedEl)
      } else {
        const createPromise = applicantAdmissionCategoriesApi.create(formData)
        const [_, createdElResponse] = await Promise.all([delayPromise, createPromise])
        props.handleRequestResult && props.handleRequestResult(createdElResponse.data)
      }

      form.resetFields()
      setSelectedFacultyId(managerFacultyId ?? null)
      setSelectedDepartmentId(null)
      setSelectedSpecialtyId(null)
      setSelectedCompetitionListId(null)
      if (!managerFacultyId) setDepartments([])
      setSpecialties([])
      setCompetitionLists([])
      setCategories([])
    } catch (err) {
      if ('response' in err && err.response.status === 400) {
        messageApi.error(err.response.data)
      } else if ('response' in err && err.response.status === 403) {
        messageApi.error("Нет доступа к выбранной специальности")
      } else {
        messageApi.error("Ошибка сохранения данных на сервере")
      }
    } finally {
      setIsLoading(false)
    }
  }

  const visibleFaculties = managerFacultyId
    ? faculties.filter(f => f.id === managerFacultyId)
    : allowedSets
      ? faculties.filter(f => allowedSets.facultyIds.has(f.id))
      : faculties

  if (isFacultiesLoading || isAllowedSetsLoading) {
    return <Skeleton paragraph={{ rows: 6 }} />
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
            <Form.Item label="Факультет">
              <Select
                value={selectedFacultyId}
                onChange={handleFacultyChange}
                placeholder="Выберите факультет"
                disabled={isFacultyManager || !!props.readOnly}
              >
                {visibleFaculties.map(faculty => (
                  <Select.Option value={faculty.id} key={faculty.id}>
                    {faculty.name}
                  </Select.Option>
                ))}
              </Select>
            </Form.Item>

            {selectedFacultyId && (
              isDepartmentsLoading ? <Skeleton paragraph={{ rows: 1 }} active /> : (
                <Form.Item label="Кафедра">
                  <Select
                    value={selectedDepartmentId}
                    onChange={handleDepartmentChange}
                    placeholder="Выберите кафедру"
                  >
                    {departments.map(d => (
                      <Select.Option value={d.id} key={d.id}>{d.name}</Select.Option>
                    ))}
                  </Select>
                </Form.Item>
              )
            )}

            {selectedDepartmentId && (
              isSpecialtiesLoading ? <Skeleton paragraph={{ rows: 1 }} active /> : (
                <Form.Item label="Специальность">
                  <Select
                    value={selectedSpecialtyId}
                    onChange={handleSpecialtyChange}
                    placeholder="Выберите специальность"
                  >
                    {specialties.map(s => (
                      <Select.Option value={s.id} key={s.id}>{s.name}</Select.Option>
                    ))}
                  </Select>
                </Form.Item>
              )
            )}

            {selectedSpecialtyId && (
              isCompetitionListsLoading ? <Skeleton paragraph={{ rows: 1 }} active /> : (
                <Form.Item label="Конкурсный список">
                  <Select
                    value={selectedCompetitionListId}
                    onChange={handleCompetitionListChange}
                    placeholder="Выберите конкурсный список"
                  >
                    {competitionLists.map(cl => (
                      <Select.Option value={cl.id} key={cl.id}>{cl.name}</Select.Option>
                    ))}
                  </Select>
                </Form.Item>
              )
            )}

            {selectedCompetitionListId && (
              isCategoriesLoading ? <Skeleton paragraph={{ rows: 1 }} active /> : (
                <Form.Item
                  label="Категория приема"
                  name="admissionCategoryId"
                  rules={[{ required: true, message: "Выберите категорию приема" }]}
                >
                  <Select placeholder="Выберите категорию приема">
                    {categories.map(c => (
                      <Select.Option value={c.id} key={c.id}>{c.name}</Select.Option>
                    ))}
                  </Select>
                </Form.Item>
              )
            )}

            <Form.Item
              label="Приоритет выбора"
              name="selectionPriority"
              rules={[{ required: true, message: "Укажите приоритет" }]}
            >
              <InputNumber className={styles.inputNumberFullWidth} min={1} />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? 'Обновить заявку' : 'Добавить заявку'}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default ApplicantAdmissionCategoryForm
