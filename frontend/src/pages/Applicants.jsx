import Title from "../components/Title"
import CrudTable from "../components/CrudTable"
import ApplicantForm from "../components/Forms/ApplicantForm"
import { applicantsApi } from "../api/applicantsApi"
import { generatePath } from "react-router"
import { ROUTES } from "../constants/routes"
import { useAuth } from "../contexts/AuthContext"

function Applicants() {
  const { auth } = useAuth()
  const readOnly = auth?.role === 'DataViewer'
  return (
    <>
      <Title
        title="Список абитуриентов"
        helpText={
          <>
            Здесь Вы можете изменять данные абитуриентов
          </>
        }
      />

      <CrudTable
        elementForm={ApplicantForm}
        readOnly={readOnly}

        editType="page"
        renderEditUrl={(/** @type {any} */ el) => generatePath(ROUTES.APPLICANT_EDIT, { applicantId: el.id })}

        serverSidePagination={true}
        getPagedAsync={(/** @type {any} */ params) => applicantsApi.getPaged(params)}
        deleteAsync={(/** @type {any} */ id) => applicantsApi.delete(id)}

        hideAddButton={true}
        renderEditTitle={(/** @type {any} */ el) => `Редактирование абитуриента "${el?.name}"`}
        renderDeleteText={(/** @type {any} */ el) => `Удалить абитуриента "${el?.name}"?`}

        onRow={(record) => ({
          title: record.notes ?? '',
        })}
        columns={[
          {
            title: "ID",
            dataIndex: "externalId",
            key: "externalId",
            withSearch: true,
            sorter: true,
          },
          {
            title: "Абитуриент",
            dataIndex: "name",
            key: "name",
            withSearch: true,
            sorter: true
          }
        ]}
      />
    </>
  )
}

export default Applicants
