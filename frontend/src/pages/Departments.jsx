import { List, Typography } from 'antd'
import Title from '../components/Title'
import CrudTable from '../components/CrudTable'
import DepartmentForm from '../components/Forms/DepartmentForm'
import { departmentsApi } from '../api/departmentsApi'
import { facultiesApi } from '../api/facultyApi'
import { specialtiesApi } from '../api/specialtiesApi'
import { useEffect, useState } from 'react'
import useMessage from 'antd/es/message/useMessage'
import { useAuth } from '../contexts/AuthContext'

function Departments() {
  const { auth } = useAuth()
  const readOnly = auth?.role === 'DataViewer'
  const [messageApi, contextHolder] = useMessage()
  const [faculties, setFaculties] = useState({})

  useEffect(() => {
    const fetchData = async () => {
      try {
        const response = await facultiesApi.getAll()
        setFaculties(response.data.reduce((acc, faculty) => {
          acc[faculty.id] = faculty
          return acc
        }, {}))
      } catch {
        messageApi.error("Ошибка получения данных с сервера")
      }
    }

    fetchData()
  }, [])

  const getDeleteBlockers = async (/** @type {any} */ department) => {
    const response = await specialtiesApi.getByDepartmentId(department.id)
    return response.data.length > 0 ? response.data : null
  }

  const renderDeleteBlockersContent = (/** @type {any} */ department, /** @type {any} */ specialties) => {
    const visible = specialties.slice(0, 5)
    const remaining = specialties.length - 5
    return (
      <>
        <Typography.Paragraph>
          Невозможно удалить кафедру <strong>"{department?.name}"</strong>, так как к ней привязаны специальности.
          Сначала удалите следующие специальности:
        </Typography.Paragraph>
        <List
          size="small"
          dataSource={visible}
          renderItem={(specialty) => <List.Item>{specialty.name}</List.Item>}
          footer={remaining > 0 ? <Typography.Text type="secondary">и ещё {remaining} специальностей</Typography.Text> : null}
        />
      </>
    )
  }

  return (
    <>
      {contextHolder}
      <Title
        title="Кафедры"
        helpText={<>Здесь Вы можете создать кафедры для конкретного факультета</>}
      />

      <CrudTable
        elementForm={DepartmentForm}
        readOnly={readOnly}

        serverSidePagination={true}
        getPagedAsync={(/** @type {any} */ params) => departmentsApi.getPaged(params)}
        deleteAsync={(/** @type {any} */ id) => departmentsApi.delete(id)}

        getDeleteBlockers={getDeleteBlockers}
        renderDeleteBlockersContent={renderDeleteBlockersContent}

        addButtonTitle="Новая кафедра"
        renderEditTitle={(/** @type {any} */ el) => `Редактирование кафедры "${el?.name}"`}
        renderDeleteText={(/** @type {any} */ el) => `Удалить кафедру "${el?.name}"?`}

        columns={[
          {
            title: "Название кафедры",
            dataIndex: "name",
            key: "name",
            withSearch: true,
            sorter: true
          },
          {
            title: "Факультет",
            dataIndex: "facultyId",
            key: "facultyId",
            sorter: true,
            filters: Object.values(faculties).map(f => ({ text: f.name, value: f.id })),
            filterMultiple: false,
            filterSearch: true,
            render: (/** @type {any} */ _, /** @type {any} */ el) => faculties[el.facultyId]?.name ?? el.facultyId
          }
        ]}
      />
    </>
  )
}

export default Departments
