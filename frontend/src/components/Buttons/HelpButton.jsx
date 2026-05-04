import styles from './styles.module.scss'
import { Button, Popover } from 'antd'
import { QuestionCircleOutlined } from '@ant-design/icons'

function HelpButton(props) {
  return (
    <Popover
      rootClassName={styles.popover}
      content={props.children}
      title={props.popoverTitle}
      placement={props.popoverPlacement}
    >
      <Button
        className={styles.helpButton}
        type="text"
        icon={<QuestionCircleOutlined />}
      />
    </Popover>
  )
}

export default HelpButton
