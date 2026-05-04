import { Breadcrumb, Result, Skeleton, Tabs } from "antd"
import Title from "../components/Title"
import { useParams, useSearchParams, Link } from "react-router"
import { ROUTES } from "../constants/routes"
import { useEffect, useState } from "react"
import ApplicantForm from "../components/Forms/ApplicantForm"
import { applicantsApi } from "../api/applicantsApi"
import { useAuth } from "../contexts/AuthContext"
import CrudTable from "../components/CrudTable"
import useMessage from "antd/es/message/useMessage"
import { applicantAdmissionCategoriesApi } from "../api/applicantAdmissionCategoriesApi"
import ApplicantAdmissionCategoryForm from "../components/Forms/ApplicantAdmissionCategoryForm"
import { admissionCategoriesApi } from "../api/admissionCategoriesApi"
import { applicantEvaluationValuesApi } from "../api/applicantEvaluationValuesApi"
import ApplicantEvaluationValueForm from "../components/Forms/ApplicantEvaluationValueForm"
import { evaluationCriteriaApi } from "../api/evaluationCriteriaApi"
import { competitionListsApi } from "../api/competitionListsApi"
import { specialtiesApi } from "../api/specialtiesApi"
import { departmentsApi } from "../api/departmentsApi"
import { facultiesApi } from "../api/facultyApi"

function ApplicantEdit() {
  const { auth } = useAuth()
  const readOnly = auth?.role === 'DataViewer'
  const [messageApi, contextHolder] = useMessage()

  const [searchParams, setSearchParams] = useSearchParams()
  const { applicantId } = useParams()

  const [admissionCategoriesDict, setAdmissionCategoriesDict] = useState(/** @type {Record<number, any>} */ ({}))
  const [competitionListsDict, setCompetitionListsDict] = useState(/** @type {Record<number, any>} */ ({}))
  const [specialtiesDict, setSpecialtiesDict] = useState(/** @type {Record<number, any>} */ ({}))
  const [departmentsDict, setDepartmentsDict] = useState(/** @type {Record<number, any>} */ ({}))
  const [facultiesDict, setFacultiesDict] = useState(/** @type {Record<number, any>} */ ({}))
  const [evaluationCriteriaDict, setEvaluationCriteriaDict] = useState(/** @type {Record<number, any>} */ ({}))

  const [isLoading, setIsLoading] = useState(true)
  const [responseStatus, setResponseStatus] = useState(null)
  const [applicant, setApplicant] = useState({ id: null, name: null })

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [categoriesResponse, criteriaResponse, compListsResponse, specialtiesResponse, departmentsResponse, facultiesResponse] = await Promise.all([
          admissionCategoriesApi.getAll(),
          evaluationCriteriaApi.getAll(),
          competitionListsApi.getAll(),
          specialtiesApi.getAll(),
          departmentsApi.getAll(),
          facultiesApi.getAll(),
        ])

        const toDict = (arr) => arr.reduce((acc, item) => { acc[item.id] = item; return acc }, {})

        setAdmissionCategoriesDict(toDict(categoriesResponse.data))
        setEvaluationCriteriaDict(toDict(criteriaResponse.data))
        setCompetitionListsDict(toDict(compListsResponse.data))
        setSpecialtiesDict(toDict(specialtiesResponse.data))
        setDepartmentsDict(toDict(departmentsResponse.data))
        setFacultiesDict(toDict(facultiesResponse.data))
      } catch {
        messageApi.error("Ошибка получения данных с сервера")
      }
    }

    fetchData()
  }, [])

  useEffect(() => {
    const fetchData = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, applicantsApi.getById(applicantId)])

        setResponseStatus(response.status)
        setApplicant(response.data)
      } catch (err) {
        if (err.response === undefined) {
          setResponseStatus(500)
        } else {
          setResponseStatus(err.response.status)
        }
      } finally {
        setIsLoading(false)
      }
    }

    fetchData()
  }, [applicantId])

  if (isLoading) {
    return <Skeleton paragraph={{ rows: 12 }} />
  }

  if (responseStatus == 404) {
    return (
      <Result
        status="404"
        title="404"
        subTitle="Такого абитуриента не существует"
      />
    )
  } else if (responseStatus == 500) {
    return (
      <Result
        status="500"
        title="500"
        subTitle="Ошибка сервера"
      />
    )
  }

  const onTabChange = (key) => {
    searchParams.set("act", key)
    setSearchParams(searchParams)
  }

  const tabs = [
    {
      key: "data",
      label: "Данные абитуриента",
      children: (
        <ApplicantForm
          initialValues={applicant}
          elementId={applicant?.id}
          handleRequestResult={(updated) => setApplicant(updated)}
          readOnly={readOnly}
        />
      ),
    },
    {
      key: "applications",
      label: "Заявки",
      children: (
        <CrudTable
          elementForm={ApplicantAdmissionCategoryForm}
          elementFormProps={{ applicantId }}
          readOnly={readOnly}

          getAllAsync={() => applicantAdmissionCategoriesApi.getAllByApplicant(applicantId)}
          deleteAsync={(id) => applicantAdmissionCategoriesApi.delete(id)}

          addButtonTitle="Добавить заявку"
          renderEditTitle={() => `Редактирование заявки`}
          renderDeleteText={() => `Удалить заявку?`}

          columns={[
            {
              title: "Приоритет",
              dataIndex: "selectionPriority",
              key: "selectionPriority",
              width: "120px",
              defaultSortOrder: "ascend",
              sorter: (a, b) => a.selectionPriority - b.selectionPriority
            },
            {
              title: "Факультет",
              key: "faculty",
              render: (_, el) => {
                const compList = competitionListsDict[admissionCategoriesDict[el.admissionCategoryId]?.competitionListId]
                const specialty = specialtiesDict[compList?.specialtyId]
                const department = departmentsDict[specialty?.departmentId]
                return facultiesDict[department?.facultyId]?.name ?? ""
              },
              sorter: (a, b) => {
                const getName = (el) => {
                  const compList = competitionListsDict[admissionCategoriesDict[el.admissionCategoryId]?.competitionListId]
                  const specialty = specialtiesDict[compList?.specialtyId]
                  const department = departmentsDict[specialty?.departmentId]
                  return facultiesDict[department?.facultyId]?.name ?? ""
                }
                return getName(a).localeCompare(getName(b))
              }
            },
            {
              title: "Кафедра",
              key: "department",
              render: (_, el) => {
                const compList = competitionListsDict[admissionCategoriesDict[el.admissionCategoryId]?.competitionListId]
                const specialty = specialtiesDict[compList?.specialtyId]
                return departmentsDict[specialty?.departmentId]?.name ?? ""
              },
              sorter: (a, b) => {
                const getName = (el) => {
                  const compList = competitionListsDict[admissionCategoriesDict[el.admissionCategoryId]?.competitionListId]
                  const specialty = specialtiesDict[compList?.specialtyId]
                  return departmentsDict[specialty?.departmentId]?.name ?? ""
                }
                return getName(a).localeCompare(getName(b))
              }
            },
            {
              title: "Специальность",
              key: "specialty",
              render: (_, el) => {
                const compList = competitionListsDict[admissionCategoriesDict[el.admissionCategoryId]?.competitionListId]
                return specialtiesDict[compList?.specialtyId]?.name ?? ""
              },
              sorter: (a, b) => {
                const getName = (el) => {
                  const compList = competitionListsDict[admissionCategoriesDict[el.admissionCategoryId]?.competitionListId]
                  return specialtiesDict[compList?.specialtyId]?.name ?? ""
                }
                return getName(a).localeCompare(getName(b))
              }
            },
            {
              title: "Конкурсный список",
              key: "competitionList",
              render: (_, el) => {
                const compListId = admissionCategoriesDict[el.admissionCategoryId]?.competitionListId
                return competitionListsDict[compListId]?.name ?? ""
              },
              sorter: (a, b) => {
                const getName = (el) => {
                  const compListId = admissionCategoriesDict[el.admissionCategoryId]?.competitionListId
                  return competitionListsDict[compListId]?.name ?? ""
                }
                return getName(a).localeCompare(getName(b))
              }
            },
            {
              title: "Категория приема",
              dataIndex: "admissionCategoryId",
              key: "admissionCategoryId",
              sorter: (a, b) =>
                (admissionCategoriesDict[a.admissionCategoryId]?.name ?? "").localeCompare(
                  admissionCategoriesDict[b.admissionCategoryId]?.name ?? ""
                ),
              render: (_, el) => admissionCategoriesDict[el.admissionCategoryId]?.name ?? el.admissionCategoryId
            }
          ]}
        />
      ),
    },
    {
      key: "evaluation",
      label: "Оценочные параметры",
      children: (
        <CrudTable
          elementForm={ApplicantEvaluationValueForm}
          elementFormProps={{ applicantId }}
          readOnly={readOnly}

          getAllAsync={() => applicantEvaluationValuesApi.getAllByApplicant(applicantId)}
          deleteAsync={(id) => applicantEvaluationValuesApi.delete(id)}

          addButtonTitle="Добавить оценочный параметр"
          renderEditTitle={() => `Редактирование оценочного параметра`}
          renderDeleteText={() => `Удалить оценочный параметр?`}

          columns={[
            {
              title: "Оценочный параметр",
              dataIndex: "evaluationCriteriaId",
              key: "evaluationCriteriaId",
              sorter: (a, b) =>
                (evaluationCriteriaDict[a.evaluationCriteriaId]?.name ?? "").localeCompare(
                  evaluationCriteriaDict[b.evaluationCriteriaId]?.name ?? ""
                ),
              render: (_, el) => evaluationCriteriaDict[el.evaluationCriteriaId]?.name ?? el.evaluationCriteriaId
            },
            {
              title: "Значение",
              dataIndex: "value",
              key: "value",
              width: "150px",
              sorter: (a, b) => a.value - b.value
            }
          ]}
        />
      ),
    },
  ]

  return (
    <>
      {contextHolder}
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <Link to={ROUTES.APPLICANTS}>Абитуриенты</Link> },
          { title: applicant.name },
        ]}
      />
      <Title title={readOnly ? "Просмотр данных абитуриента" : "Редактирование данных абитуриента"} />

      <Tabs
        activeKey={searchParams.get("act") ?? "data"}
        items={tabs}
        onChange={onTabChange}
      />
    </>
  )
}

export default ApplicantEdit
