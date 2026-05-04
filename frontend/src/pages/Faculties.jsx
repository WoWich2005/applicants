import { List, Typography } from 'antd'
import Title from '../components/Title'
import CrudTable from '../components/CrudTable'
import FacultyForm from '../components/Forms/FacultyForm'
import { facultiesApi } from '../api/facultyApi'
import { departmentsApi } from '../api/departmentsApi'
import { useAuth } from '../contexts/AuthContext'
import { useTranslation } from 'react-i18next'

function Faculties() {
  const { auth } = useAuth()
  const { t } = useTranslation()
  const readOnly = auth?.role === 'DataViewer'

  const getDeleteBlockers = async (/** @type {any} */ faculty) => {
    const response = await departmentsApi.getByFacultyId(faculty.id)
    return response.data.length > 0 ? response.data : null
  }

  const renderDeleteBlockersContent = (/** @type {any} */ faculty, /** @type {any} */ departments) => {
    const visible = departments.slice(0, 5)
    const remaining = departments.length - 5
    return (
      <>
        <Typography.Paragraph>
          {t('faculty.deleteBlocked', { name: faculty?.name })}
        </Typography.Paragraph>
        <List
          size="small"
          dataSource={visible}
          renderItem={(dept) => <List.Item>{dept.name}</List.Item>}
          footer={remaining > 0 ? <Typography.Text type="secondary">{t('faculty.andMoreDepts', { count: remaining })}</Typography.Text> : null}
        />
      </>
    )
  }

  return (
    <>
      <Title
        title={t('faculty.title')}
        helpText={t('faculty.helpText')}
      />

      <CrudTable
        elementForm={FacultyForm}
        readOnly={readOnly}

        serverSidePagination={true}
        getPagedAsync={(/** @type {any} */ params) => facultiesApi.getPaged(params)}
        deleteAsync={(/** @type {any} */ id) => facultiesApi.delete(id)}

        getDeleteBlockers={getDeleteBlockers}
        renderDeleteBlockersContent={renderDeleteBlockersContent}

        addButtonTitle={t('faculty.addButton')}
        renderEditTitle={(/** @type {any} */ el) => t('faculty.editTitle', { name: el?.name })}
        renderDeleteText={(/** @type {any} */ el) => t('faculty.deleteText', { name: el?.name })}

        columns={[
          {
            title: t('faculty.colName'),
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

export default Faculties
