import { Layout } from "antd"
import CollapseButton from "../Buttons/CollapseButton"
import styles from "./styles.module.scss"

function Sidebar({ collapsed, setCollapsed, children }) {
  return (
    <Layout.Sider
      className={styles.sidebar}
      width="300"
      trigger={null}
      collapsible
      collapsed={collapsed}
    >
      <CollapseButton collapsed={collapsed} setCollapsed={setCollapsed} />
      {children}
    </Layout.Sider>
  )
}

export default Sidebar
