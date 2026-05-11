import { Button, Form, InputNumber, Select, Skeleton, Space, message } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { applicantAdmissionCategoriesApi } from "../../api/applicantAdmissionCategoriesApi"
import { admissionCategoriesApi } from "../../api/admissionCategoriesApi"
import { specialtiesApi } from "../../api/specialtiesApi"
import { departmentsApi } from "../../api/departmentsApi"
import { facultiesApi } from "../../api/facultyApi"
import { competitionListsApi } from "../../api/competitionListsApi"
import { useTranslation } from "react-i18next"

function ApplicantAdmissionCategoryForm(/** @type {any} */ props) {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)

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

  useEffect(() => {
    const fetchFaculties = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, facultiesApi.getAll()])
        setFaculties(response.data)
        setIsFacultiesLoading(false)
      } catch {
        messageApi.error(t('applicantAdmissionCategory.form.fetchFacultiesError'))
      }
    }

    fetchFaculties()
  }, [])

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
        messageApi.error(t('applicantAdmissionCategory.form.restoreError'))
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
      setDepartments(response.data)
    } catch {
      messageApi.error(t('applicantAdmissionCategory.form.fetchDepartmentsError'))
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
      setSpecialties(response.data)
    } catch {
      messageApi.error(t('applicantAdmissionCategory.form.fetchSpecialtiesError'))
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
      messageApi.error(t('applicantAdmissionCategory.form.fetchCompetitionListsError'))
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
      messageApi.error(t('applicantAdmissionCategory.form.fetchCategoriesError'))
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
      setSelectedFacultyId(null)
      setSelectedDepartmentId(null)
      setSelectedSpecialtyId(null)
      setSelectedCompetitionListId(null)
      setDepartments([])
      setSpecialties([])
      setCompetitionLists([])
      setCategories([])
    } catch (/** @type {any} */ err) {
      if (err?.response?.status === 400) {
        messageApi.error(err.response?.data?.message ?? t('applicantAdmissionCategory.form.saveError'))
      } else if (err?.response?.status === 403) {
        messageApi.error(t('applicantAdmissionCategory.form.accessError'))
      } else {
        messageApi.error(t('applicantAdmissionCategory.form.saveError'))
      }
    } finally {
      setIsLoading(false)
    }
  }

  if (isFacultiesLoading) {
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
            <Form.Item label={t('applicantAdmissionCategory.form.facultyLabel')}>
              <Select
                value={selectedFacultyId}
                onChange={handleFacultyChange}
                placeholder={t('applicantAdmissionCategory.form.facultyPlaceholder')}
                disabled={!!props.readOnly}
              >
                {faculties.map(faculty => (
                  <Select.Option value={faculty.id} key={faculty.id}>
                    {faculty.name}
                  </Select.Option>
                ))}
              </Select>
            </Form.Item>

            {selectedFacultyId && (
              isDepartmentsLoading ? <Skeleton paragraph={{ rows: 1 }} active /> : (
                <Form.Item label={t('applicantAdmissionCategory.form.departmentLabel')}>
                  <Select
                    value={selectedDepartmentId}
                    onChange={handleDepartmentChange}
                    placeholder={t('applicantAdmissionCategory.form.departmentPlaceholder')}
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
                <Form.Item label={t('applicantAdmissionCategory.form.specialtyLabel')}>
                  <Select
                    value={selectedSpecialtyId}
                    onChange={handleSpecialtyChange}
                    placeholder={t('applicantAdmissionCategory.form.specialtyPlaceholder')}
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
                <Form.Item label={t('applicantAdmissionCategory.form.competitionListLabel')}>
                  <Select
                    value={selectedCompetitionListId}
                    onChange={handleCompetitionListChange}
                    placeholder={t('applicantAdmissionCategory.form.competitionListPlaceholder')}
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
                  label={t('applicantAdmissionCategory.form.categoryLabel')}
                  name="admissionCategoryId"
                  rules={[{ required: true, message: t('applicantAdmissionCategory.form.categoryRequired') }]}
                >
                  <Select placeholder={t('applicantAdmissionCategory.form.categoryPlaceholder')}>
                    {categories.map(c => (
                      <Select.Option value={c.id} key={c.id}>{c.name}</Select.Option>
                    ))}
                  </Select>
                </Form.Item>
              )
            )}

            <Form.Item
              label={t('applicantAdmissionCategory.form.priorityLabel')}
              name="selectionPriority"
              rules={[{ required: true, message: t('applicantAdmissionCategory.form.priorityRequired') }]}
            >
              <InputNumber className={styles.inputNumberFullWidth} min={1} />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? t('applicantAdmissionCategory.form.updateButton') : t('applicantAdmissionCategory.form.addButton')}
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
