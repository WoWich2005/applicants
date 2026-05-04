import { Button, Modal, Space, Typography } from "antd"

function DeleteModal(props) {
  return (
    <Modal
      open={props.open}
      title={"Вы уверены?"}
      footer={null}
      onCancel={props.onCancel}
    >
      <Typography.Paragraph>
        {props.warningText}
      </Typography.Paragraph>
        
      <Space>
        <Button
          type="primary"
          onClick={props.onConfirm}
          loading={props.loading}
        >
          Удалить
        </Button>
        <Button onClick={props.onCancel}>Отмена</Button>
      </Space>
    </Modal>
  )
}

export default DeleteModal