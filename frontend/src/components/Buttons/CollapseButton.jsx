import styles from './styles.module.scss'
import {
  MenuFoldOutlined,
  MenuUnfoldOutlined,
} from "@ant-design/icons"
import { Button, ConfigProvider } from 'antd'
import logo from '../../assets/logo.svg'

export default function CollapseButton(props) {
  return (
    <ConfigProvider
      theme={{
        components: {
          Button: {
            textHoverBg: "transparent",
            colorBgTextActive: "transparent"
          }
        }
      }}
    >
      <div className={styles.collapseButtonWrapper}>
        {!props.collapsed && (
          <img src={logo} alt="Логотип" className={styles.logo} />
        )}
        <Button
          type="text"
          className={styles.collapseButton}
          icon={
            props.collapsed ? (
              <MenuUnfoldOutlined className={styles.collapseButtonIcon} />
            ) : (
              <MenuFoldOutlined className={styles.collapseButtonIcon} />
            )
          }
          onClick={() => props.setCollapsed(!props.collapsed)}
        />
      </div>
    </ConfigProvider>
  )
}
