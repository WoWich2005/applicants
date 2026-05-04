import { Button, Modal, Space, Typography } from "antd"
import { useTranslation } from "react-i18next"

function DeleteModal(props) {
  const { t } = useTranslation()

  return (
    <Modal
      open={props.open}
      title={t('deleteModal.title')}
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
          {t('common.delete')}
        </Button>
        <Button onClick={props.onCancel}>{t('common.cancel')}</Button>
      </Space>
    </Modal>
  )
}

export default DeleteModal
