import { Button, message, Modal, Space, Typography } from "antd"
import { useEffect, useState } from "react"
import DeleteModal from "../Modals/DeleteModal"
import AddButton from "../Buttons/AddButton"
import DataTable from "../DataTable"
import { Link } from "react-router"

function CrudTable(props) {
  const [messageApi, contextHolder] = message.useMessage()

  const [isDataLoading, setIsDataLoading] = useState(true)
  const [isDeleteLoading, setIsDeleteLoading] = useState(false)
  const [isBlockersLoading, setIsBlockersLoading] = useState(false)

  const [isCreateElModalOpen, setIsCreateElModalOpen] = useState(false)
  const [isEditElModalOpen, setIsEditElModalOpen] = useState(false)
  const [isDeleteElModalOpen, setIsDeleteElModalOpen] = useState(false)
  const [isDeleteBlockedModalOpen, setIsDeleteBlockedModalOpen] = useState(false)

  const [curEditEl, setCurEditEl] = useState(/** @type {any} */ (null))
  const [curDeleteEl, setCurDeleteEl] = useState(/** @type {any} */ (null))
  const [deleteBlockers, setDeleteBlockers] = useState(/** @type {any} */ (null))

  const [dataSource, setDataSource] = useState(/** @type {any[]} */ ([]))

  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [total, setTotal] = useState(0)
  const [activeFilters, setActiveFilters] = useState(/** @type {Record<string, any>} */ ({}))
  const [sortField, setSortField] = useState(/** @type {string | null} */ (null))
  const [sortOrder, setSortOrder] = useState(/** @type {string | null} */ (null))

  useEffect(() => {
    if (props.serverSidePagination) return

    const fetchData = async () => {
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([delayPromise, props.getAllAsync()])
        setDataSource(response.data)
      } catch (err) {
        messageApi.error("Ошибка получения данных")
        console.log(err)
      } finally {
        setIsDataLoading(false)
      }
    }

    fetchData()
  }, [])

  useEffect(() => {
    if (!props.serverSidePagination) return

    const fetchData = async () => {
      setIsDataLoading(true)
      try {
        const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
        const [_, response] = await Promise.all([
          delayPromise,
          props.getPagedAsync({ page, pageSize, filters: activeFilters, sortField, sortOrder })
        ])
        setDataSource(response.data.items)
        setTotal(response.data.total)
      } catch (err) {
        messageApi.error("Ошибка получения данных")
        console.log(err)
      } finally {
        setIsDataLoading(false)
      }
    }

    fetchData()
  }, [page, pageSize, activeFilters, sortField, sortOrder])

  /**
   * @param {Record<string, any[] | null>} filters
   * @param {any} sorter
   */
  const onTableChange = (filters, sorter) => {
    if (!props.serverSidePagination) return
    setActiveFilters(filters ?? {})
    setSortField(sorter?.field ?? null)
    setSortOrder(sorter?.order ?? null)
    setPage(1)
  }

  const onCreateSuccess = (/** @type {any} */ createdEl) => {
    if (props.serverSidePagination) {
      setTotal(prev => prev + 1)
    }
    setDataSource([...dataSource, createdEl])
    setIsCreateElModalOpen(false)
  }

  const onUpdateSuccess = (updatedEl) => {
    setDataSource(prev =>
      prev.map(item => item.id === updatedEl.id ? updatedEl : item)
    )

    setIsEditElModalOpen(false)
  }

  const handleDeleteEl = async () => {
    setIsDeleteLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 500))
      const deletedId = curDeleteEl?.id
      await Promise.all([delayPromise, props.deleteAsync(deletedId)])

      setIsDeleteElModalOpen(false)

      if (props.serverSidePagination) {
        const remainingCount = dataSource.length - 1
        setDataSource(dataSource.filter(val => val.id != deletedId))
        setTotal(prev => prev - 1)
        if (remainingCount === 0 && page > 1) {
          setPage(prev => prev - 1)
        }
      } else {
        setDataSource(dataSource.filter(val => val.id != deletedId))
      }
    } catch (err) {
      const serverMessage = /** @type {any} */ (err)?.response?.data?.message
      messageApi.error(serverMessage ?? "Ошибка удаления элемента на сервере")
      console.log(err)
    } finally {
      setIsDeleteLoading(false)
    }
  }

  const onUpdateEl = (/** @type {any} */ updatedEl) => {
    setDataSource(prev => prev.map(item => item.id === updatedEl.id ? updatedEl : item))
  }

  const openEditModal = (el) => {
    setCurEditEl(el)
    setIsEditElModalOpen(true)
  }

  const openDeleteModal = async (el) => {
    setCurDeleteEl(el)

    if (props.getDeleteBlockers) {
      setIsBlockersLoading(true)
      try {
        const blockers = await props.getDeleteBlockers(el)
        if (blockers) {
          setDeleteBlockers(blockers)
          setIsDeleteBlockedModalOpen(true)
          return
        }
      } catch {
        messageApi.error("Ошибка при проверке связанных данных")
        return
      } finally {
        setIsBlockersLoading(false)
      }
    }

    setIsDeleteElModalOpen(true)
  }

  const actionColumn = props.readOnly
    ? [{
      title: "Действия",
      dataIndex: "controls",
      key: "control",
      width: 120,
      render: (_, el) => {
        if (props.editType === "page" && props.renderEditUrl) {
          return (
            <Link to={props.renderEditUrl(el)}>
              <Button type="link">Просмотр</Button>
            </Link>
          )
        }
        return (
          <Button type="link" onClick={() => openEditModal(el)}>
            Просмотр
          </Button>
        )
      }
    }]
    : [{
      title: "Действия",
      dataIndex: "controls",
      key: "control",
      width: 250,
      render: (_, el) => {
        return (
          <Space>
            {props.extraActions && props.extraActions(el, onUpdateEl)}

            {(props.editType === undefined || props.editType === "modal") && (
              <Button
                type="link"
                onClick={() => openEditModal(el)}
              >
                Редактировать
              </Button>
            )}

            {(props.editType === "page") && (
              <Link to={props.renderEditUrl(el)}>
                <Button type="link">
                  Редактировать
                </Button>
              </Link>
            )}

            <Button
              type="link"
              onClick={() => openDeleteModal(el)}
              loading={isBlockersLoading && curDeleteEl?.id === el.id}
            >
              Удалить
            </Button>
          </Space>
        )
      }
    }]

  const enrichedColumns = props.columns.map(col => ({
    ...col,
    ...(props.serverSidePagination && (col.filters || col.withSearch) ? { filteredValue: activeFilters[col.key] ?? null } : {})
  }))

  const columns = [...enrichedColumns, ...actionColumn]

  /** @type {import('antd').TablePaginationConfig | undefined} */
  const paginationConfig = props.serverSidePagination ? {
    current: page,
    pageSize: pageSize,
    total: total,
    onChange: (newPage, newPageSize) => {
      setPage(newPage)
      setPageSize(newPageSize)
    },
    showSizeChanger: true,
    showTotal: (t) => `Всего: ${t}`,
  } : undefined

  return (
    <>
      {contextHolder}

      <Modal
        open={isEditElModalOpen}
        title={props.renderEditTitle(curEditEl)}
        footer={null}
        onCancel={() => setIsEditElModalOpen(false)}
      >
        <props.elementForm
          initialValues={curEditEl}
          elementId={curEditEl?.id}
          handleRequestResult={onUpdateSuccess}
          readOnly={props.readOnly}
          buttons={(
            <>
              <Button onClick={() => setIsEditElModalOpen(false)}>{props.readOnly ? 'Закрыть' : 'Отмена'}</Button>
            </>
          )}
          {...props.elementFormProps}
        />
      </Modal>

      <DeleteModal
        open={isDeleteElModalOpen}
        onCancel={() => setIsDeleteElModalOpen(false)}
        warningText={props.renderDeleteText(curDeleteEl)}
        onConfirm={() => handleDeleteEl()}
        loading={isDeleteLoading}
      />

      <Modal
        open={isDeleteBlockedModalOpen}
        title="Невозможно удалить"
        footer={<Button onClick={() => setIsDeleteBlockedModalOpen(false)}>Закрыть</Button>}
        onCancel={() => setIsDeleteBlockedModalOpen(false)}
      >
        {props.renderDeleteBlockersContent && deleteBlockers
          ? props.renderDeleteBlockersContent(curDeleteEl, deleteBlockers)
          : (
            <Typography.Paragraph>
              Удаление невозможно: существуют связанные записи.
            </Typography.Paragraph>
          )
        }
      </Modal>

      {!props.hideAddButton && !props.readOnly && (
        <AddButton
          title={props.addButtonTitle}
          modalContent={(
            <props.elementForm
              handleRequestResult={onCreateSuccess}
              buttons={(
                <>
                  <Button onClick={() => setIsCreateElModalOpen(false)}>Отмена</Button>
                </>
              )}
              {...props.elementFormProps}
            />
          )}
          isModalOpen={isCreateElModalOpen}
          setIsModalOpen={setIsCreateElModalOpen}
        />
      )}

      <DataTable
        dataSource={dataSource}
        rowKey="id"
        withSearch={true}
        loading={isDataLoading}
        columns={columns}
        serverSidePagination={props.serverSidePagination}
        pagination={paginationConfig}
        onTableChange={onTableChange}
        onRow={props.onRow}
      />
    </>
  )
}

export default CrudTable
