import styles from './styles.module.scss'
import { Button, Modal } from 'antd'
import { PlusCircleOutlined } from '@ant-design/icons'

function AddButton(props) {
  return (
    <div className={props.withoutContainer ? '' : styles.buttonWrapperRight}>
      <Button
        className={styles.button}
        type="primary"
        icon={<PlusCircleOutlined />}
        onClick={() => props.setIsModalOpen(true)}
      >
        <span>{props.title}</span>
      </Button>

      <Modal
        open={props.isModalOpen}
        title={props.title}
        footer={null}
        onCancel={() => props.setIsModalOpen(false)}
        destroyOnClose
      >
        {props.modalContent}
      </Modal>
    </div>
  )
}

export default AddButton