import { SearchOutlined } from "@ant-design/icons"
import { Button, Input, Space, Table } from "antd"
import { useRef, useState } from "react"
import Highlighter from "react-highlight-words"
import { useTranslation } from "react-i18next"

function DataTable(props) {
  const { t } = useTranslation()
  const [searchText, setSearchText] = useState('')
  const [searchedColumn, setSearchedColumn] = useState('')
  const searchInput = useRef(null)

  const handleSearch = (selectedKeys, confirm, dataIndex) => {
    confirm()
    setSearchText(selectedKeys[0])
    setSearchedColumn(dataIndex)
  }

  const handleReset = clearFilters => {
    setSearchText('')
    setSearchedColumn('')
    clearFilters({ confirm: true, closeDropdown: true })
  }

  const getColumnSearchProps = (dataIndex, index) => ({
    filterDropdown: ({ setSelectedKeys, selectedKeys, confirm, clearFilters, close }) => (
      <div style={{ padding: 8 }} onKeyDown={e => e.stopPropagation()}>
        <Input
          ref={searchInput}
          placeholder={t('dataTable.searchPlaceholder')}
          value={selectedKeys[0]}
          onChange={e => setSelectedKeys(e.target.value ? [e.target.value] : [])}
          onPressEnter={() => handleSearch(selectedKeys, confirm, dataIndex)}
          style={{ marginBottom: 8, display: 'block' }}
        />
        <Space>
          <Button
            type="primary"
            onClick={() => handleSearch(selectedKeys, confirm, dataIndex)}
            icon={<SearchOutlined />}
            size="small"
            style={{ width: 90 }}
          >
            {t('dataTable.find')}
          </Button>
          <Button
            onClick={() => clearFilters && handleReset(clearFilters)}
            size="small"
            style={{ width: 90 }}
          >
            {t('dataTable.reset')}
          </Button>
          <Button
            type="link"
            size="small"
            onClick={() => close()}
          >
            {t('dataTable.close')}
          </Button>
        </Space>
      </div>
    ),
    filterIcon: filtered => <SearchOutlined style={{ color: filtered ? '#1677ff' : undefined }} />,
    ...(props.serverSidePagination ? {} : {
      onFilter: (value, record) => {
        return record[dataIndex].toString().toLowerCase().includes(value.toLowerCase())
      }
    }),
    filterDropdownProps: {
      onOpenChange(open) {
        if (open) {
          setTimeout(() => {
            var _a
            return (_a = searchInput.current) === null || _a === void 0 ? void 0 : _a.select()
          }, 100)
        }
      },
    },
    render: (text, el) => {
      const column = props.columns[index]

      if(column.render) {
        text = column.render(text, el)?.toString()
      }else{
        text = text?.toString()
      }

      return searchedColumn === dataIndex ? (
        <Highlighter
          highlightStyle={{ backgroundColor: '#ffc069', padding: 0 }}
          searchWords={[searchText]}
          autoEscape
          textToHighlight={text ? text.toString() : ''}
        />
      ) : (
        text
      )}
  })

  const columns = [
    ...props.columns.map((column, index) => ({
      ...column,
      ...(column.withSearch && getColumnSearchProps(column.key, index))
    }))
  ]

  return (
    <Table
      dataSource={props.dataSource}
      rowKey={props.rowKey}
      columns={columns}
      showSorterTooltip={{ target: 'sorter-icon' }}
      loading={props.loading ?? false}
      pagination={props.pagination}
      onRow={props.onRow}
      onChange={(_, filters, sorter, { action }) => {
        if (action === 'filter' || action === 'sort') {
          props.onTableChange?.(filters, sorter)
        }
      }}
    />
  )
}

export default DataTable
