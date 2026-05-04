import { List, Typography } from 'antd'
import Title from '../components/Title'
import CrudTable from '../components/CrudTable'
import { specialtiesApi } from '../api/specialtiesApi'
import { competitionListsApi } from '../api/competitionListsApi'
import { useEffect, useState } from 'react'
import { departmentsApi } from '../api/departmentsApi'
import { facultiesApi } from '../api/facultyApi'
import useMessage from 'antd/es/message/useMessage'
import SpecialtyForm from '../components/Forms/SpecialtyForm'
import { generatePath } from 'react-router'
import { ROUTES } from '../constants/routes'
import { useAuth } from '../contexts/AuthContext'

function Specialties() {
  const { auth } = useAuth()
  const readOnly = auth?.role === 'DataViewer'
  const [messageApi, contextHolder] = useMessage()
  const [departments, setDepartments] = useState(/** @type {Record<number, any>} */ ({}))
  const [faculties, setFaculties] = useState(/** @type {Record<number, any>} */ ({}))

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [departmentsResponse, facultiesResponse] = await Promise.all([
          departmentsApi.getAll(),
          facultiesApi.getAll(),
        ])
        setDepartments(departmentsResponse.data.reduce((acc, department) => {
          acc[department.id] = department
          return acc
        }, {}))
        setFaculties(facultiesResponse.data.reduce((acc, faculty) => {
          acc[faculty.id] = faculty
          return acc
        }, {}))
      } catch {
        messageApi.error("Ошибка получения данных с сервера")
      }
    }

    fetchData()
  }, [])

  const getDeleteBlockers = async (/** @type {any} */ specialty) => {
    const response = await competitionListsApi.getBySpecialtyId(specialty.id)
    return response.data.length > 0 ? response.data : null
  }

  const renderDeleteBlockersContent = (/** @type {any} */ specialty, /** @type {any} */ competitionLists) => {
    const visible = competitionLists.slice(0, 5)
    const remaining = competitionLists.length - 5
    return (
      <>
        <Typography.Paragraph>
          Невозможно удалить специальность <strong>"{specialty?.name}"</strong>, так как к ней привязаны конкурсные списки.
          Сначала удалите следующие конкурсные списки:
        </Typography.Paragraph>
        <List
          size="small"
          dataSource={visible}
          renderItem={(list) => <List.Item>{list.name}</List.Item>}
          footer={remaining > 0 ? <Typography.Text type="secondary">и ещё {remaining} конкурсных списков</Typography.Text> : null}
        />
      </>
    )
  }

  return (
    <>
      {contextHolder}
      <Title
        title="Специальности"
        helpText={<>Здесь Вы можете создать специальности для конкретной кафедры</>}
      />

      <CrudTable
        elementForm={SpecialtyForm}
        readOnly={readOnly}

        editType="page"
        renderEditUrl={(/** @type {any} */ el) => generatePath(ROUTES.SPECIALTY_EDIT, { specialtyId: el.id })}

        serverSidePagination={true}
        getPagedAsync={(/** @type {any} */ params) => specialtiesApi.getPaged(params)}
        deleteAsync={(/** @type {any} */ id) => specialtiesApi.delete(id)}

        getDeleteBlockers={getDeleteBlockers}
        renderDeleteBlockersContent={renderDeleteBlockersContent}

        addButtonTitle="Новая специальность"
        renderEditTitle={(/** @type {any} */ el) => `Редактирование специальности "${el?.name}"`}
        renderDeleteText={(/** @type {any} */ el) => `Удалить специальность "${el?.name}"?`}

        columns={[
          {
            title: "Имя специальности",
            dataIndex: "name",
            key: "name",
            withSearch: true,
            sorter: true
          },
          {
            title: "Факультет",
            dataIndex: 'departmentId',
            key: 'facultyId',
            width: "200px",
            sorter: true,
            filters: Object.values(faculties).map(f => ({ text: f.name, value: f.id })),
            filterMultiple: false,
            filterSearch: true,
            render: (/** @type {any} */ _, /** @type {any} */ el) => faculties[departments[el.departmentId]?.facultyId]?.name ?? ''
          },
          {
            title: "Кафедра",
            dataIndex: 'departmentId',
            key: 'departmentId',
            width: "200px",
            sorter: true,
            filters: Object.values(departments).map(d => ({ text: d.name, value: d.id })),
            filterMultiple: false,
            filterSearch: true,
            render: (/** @type {any} */ _, /** @type {any} */ el) => departments[el.departmentId]?.name ?? ''
          },
        ]}
      />
    </>
  )
}

export default Specialties
