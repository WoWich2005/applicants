import { message, Space, Table, Typography } from "antd"
import Title from "../components/Title"
import { useEffect, useState } from "react"
import { facultiesApi } from "../api/facultyApi"
import { instance } from "../api"
import { useTranslation } from "react-i18next"

function Results() {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()

  const [dataSource, setDataSource] = useState([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const fetchData = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, facultiesApi.getAll()])
        setDataSource(response.data)
      } catch (err) {
        messageApi.error(t('results.error'))
        console.log(err)
      } finally {
        setIsLoading(false)
      }
    }

    fetchData()
  }, [])

  const columns = [
    {
      title: t('results.colFaculty'),
      dataIndex: "name",
      key: "name",
    },
    {
      title: t('results.colActions'),
      dataIndex: "control",
      key: "control",
      width: "250px",
      render: (_, el) => {
        return (
          <Space>
            <Typography.Link
              onClick={() => window.open(`http://localhost:5059/get_results/${el.id}`, '_blank', 'noopener,noreferrer')}
            >
              {t('results.downloadExcel')}
            </Typography.Link>
          </Space>
        )
      },
    }
  ]

  return (
    <>
      {contextHolder}

      <Title
        title={t('results.title')}
        helpText={t('results.helpText')}
      />

      <Table
        dataSource={dataSource}
        columns={columns}
        rowKey="id"
        loading={isLoading}
      />
    </>
  )
}

export default Results
