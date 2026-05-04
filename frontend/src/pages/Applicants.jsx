import Title from "../components/Title"
import CrudTable from "../components/CrudTable"
import ApplicantForm from "../components/Forms/ApplicantForm"
import { applicantsApi } from "../api/applicantsApi"
import { generatePath } from "react-router"
import { ROUTES } from "../constants/routes"
import { useAuth } from "../contexts/AuthContext"
import { useTranslation } from "react-i18next"

function Applicants() {
  const { auth } = useAuth()
  const { t } = useTranslation()
  const readOnly = auth?.role === 'DataViewer'

  return (
    <>
      <Title
        title={t('applicant.listTitle')}
        helpText={t('applicant.listHelpText')}
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
        renderEditTitle={(/** @type {any} */ el) => t('applicant.editTitle', { name: el?.name })}
        renderDeleteText={(/** @type {any} */ el) => t('applicant.deleteText', { name: el?.name })}

        onRow={(record) => ({
          title: record.notes ?? '',
        })}
        columns={[
          {
            title: t('applicant.colId'),
            dataIndex: "externalId",
            key: "externalId",
            withSearch: true,
            sorter: true,
          },
          {
            title: t('applicant.colName'),
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
