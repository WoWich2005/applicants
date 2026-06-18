import { Button, message, Modal, Space, Typography } from "antd"
import { useCallback, useEffect, useRef, useState } from "react"
import DeleteModal from "../Modals/DeleteModal"
import AddButton from "../Buttons/AddButton"
import DataTable from "../DataTable"
import { Link } from "react-router"
import { useTranslation } from "react-i18next"
import { useServerTable } from "../../hooks/useServerTable"

function CrudTable(props) {
  const { t } = useTranslation()
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

  // --- Non-server state ---
  const [localData, setLocalData] = useState(/** @type {any[]} */ ([]))

  useEffect(() => {
    if (!props.serverSidePagination) props.onDataLoaded?.(localData)
  }, [localData]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (props.serverSidePagination) return
    const fetchData = async () => {
      try {
        const response = await props.getAllAsync()
        setLocalData(response.data)
      } catch {
        messageApi.error(t('common.error.fetchData'))
      } finally {
        setIsDataLoading(false)
      }
    }
    fetchData()
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  // --- Server-side state via hook ---
  const getPagedRef = useRef(props.getPagedAsync)
  useEffect(() => { getPagedRef.current = props.getPagedAsync }, [props.getPagedAsync])

  const serverFetchAsync = useCallback(
    (params) => {
      if (!props.serverSidePagination || !getPagedRef.current) return Promise.resolve({ data: { items: [], total: 0 } })
      return getPagedRef.current(params)
    },
    [props.serverSidePagination]
  )

  const {
    data: serverData,
    loading: serverLoading,
    setData: setServerData,
    setTotal: setServerTotal,
    page: serverPage,
    setPage: setServerPage,
    filters: activeFilters,
    setFilters: setServerFilters,
    pagination: paginationConfig,
    onTableChange,
  } = useServerTable(props.serverSidePagination ? serverFetchAsync : null, {
    defaultSortField: props.defaultSortField ?? null,
    defaultSortOrder: props.defaultSortOrder ?? null,
  })

  const dataSource = props.serverSidePagination ? serverData : localData
  const setDataSource = props.serverSidePagination ? setServerData : setLocalData
  const isServerLoading = props.serverSidePagination ? serverLoading : isDataLoading

  // ---

  const onCreateSuccess = (/** @type {any} */ createdEl) => {
    if (props.serverSidePagination) {
      setServerFilters(f => ({ ...f }))
    } else {
      setDataSource(prev => [...prev, createdEl])
    }
    setIsCreateElModalOpen(false)
    props.onDataChange?.()
  }

  const onUpdateSuccess = (/** @type {any} */ updatedEl) => {
    if (props.serverSidePagination) {
      setServerFilters(f => ({ ...f }))
    } else {
      setDataSource(prev =>
        prev.map(item => item.id === updatedEl.id ? updatedEl : item)
      )
    }
    setIsEditElModalOpen(false)
    props.onDataChange?.()
  }

  const handleDeleteEl = async () => {
    setIsDeleteLoading(true)
    try {
      const deletedId = curDeleteEl?.id
      await props.deleteAsync(deletedId)
      setIsDeleteElModalOpen(false)
      props.onDataChange?.()
      const remaining = dataSource.length - 1
      setDataSource(prev => prev.filter(val => val.id != deletedId))
      if (props.serverSidePagination) {
        setServerTotal(prev => prev - 1)
        if (remaining === 0 && serverPage > 1) {
          setServerPage(prev => prev - 1)
        }
      }
    } catch (err) {
      const serverMessage = /** @type {any} */ (err)?.response?.data?.message
      messageApi.error(serverMessage ?? t('common.error.deleteServer'))
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
        messageApi.error(t('common.error.checkRelations'))
        return
      } finally {
        setIsBlockersLoading(false)
      }
    }

    setIsDeleteElModalOpen(true)
  }

  const actionColumn = props.readOnly
    ? [{
      title: t('common.actions'),
      dataIndex: "controls",
      key: "control",
      width: 120,
      render: (_, el) => {
        if (props.editType === "page" && props.renderEditUrl) {
          return (
            <Link to={props.renderEditUrl(el)}>
              <Button type="link">{t('common.view')}</Button>
            </Link>
          )
        }
        return (
          <Button type="link" onClick={() => openEditModal(el)}>
            {t('common.view')}
          </Button>
        )
      }
    }]
    : [{
      title: t('common.actions'),
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
                {t('common.edit')}
              </Button>
            )}

            {(props.editType === "page") && (
              <Link to={props.renderEditUrl(el)}>
                <Button type="link">
                  {t('common.edit')}
                </Button>
              </Link>
            )}

            <Button
              type="link"
              onClick={() => openDeleteModal(el)}
              loading={isBlockersLoading && curDeleteEl?.id === el.id}
            >
              {t('common.delete')}
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
              <Button onClick={() => setIsEditElModalOpen(false)}>
                {props.readOnly ? t('common.close') : t('common.cancel')}
              </Button>
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
        title={t('common.cannotDelete')}
        footer={<Button onClick={() => setIsDeleteBlockedModalOpen(false)}>{t('common.close')}</Button>}
        onCancel={() => setIsDeleteBlockedModalOpen(false)}
      >
        {props.renderDeleteBlockersContent && deleteBlockers
          ? props.renderDeleteBlockersContent(curDeleteEl, deleteBlockers)
          : (
            <Typography.Paragraph>
              {t('common.noRelatedRecords')}
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
                  <Button onClick={() => setIsCreateElModalOpen(false)}>{t('common.cancel')}</Button>
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
        loading={isServerLoading}
        columns={columns}
        serverSidePagination={props.serverSidePagination}
        pagination={props.serverSidePagination ? paginationConfig : undefined}
        onTableChange={onTableChange}
        onRow={props.onRow}
      />
    </>
  )
}

export default CrudTable
