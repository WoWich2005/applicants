import { Breadcrumb, message, Space, Table, Typography } from "antd"
import Title from "../components/Title"
import { useEffect, useState } from "react"
import { facultiesApi } from "../api/facultyApi"
import { instance } from "../api"

function Results() {
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
        messageApi.error("Ошибка получения данных")
        console.log(err)
      } finally {
        setIsLoading(false)
      }
    }
  
    fetchData()
  }, [])

  const columns = [
    {
      title: "Факультет",
      dataIndex: "name",
      key: "name",
    },
    {
      title: "Действия",
      dataIndex: "control",
      key: "control",
      width: "250px",
      render: (_, el) => {
        return (
          <Space>
            <Typography.Link
              onClick={() => window.open(`http://localhost:5059/get_results/${el.id}`, '_blank', 'noopener,noreferrer')}
            >
              Скачать Excel
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
        title="Результаты"
        helpText={
          <>
            На данной странице Вы можете скачать результаты в формате таблицы
            Excel
          </>
        }
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
