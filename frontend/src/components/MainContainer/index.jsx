import { Layout } from "antd"
import styles from "./styles.module.scss"
import Sidebar from "../Sidebar"
import ContentHeader from "../ContentHeader"
import MainMenu from "../MainMenu"
import { useLocalStorage } from "../../hooks/useLocalStorage"

const MainContainer = (props) => {
  const [collapsed, setCollapsed] = useLocalStorage("isSidebarCollpased", false)
  return (
    <Layout className={styles.container}>
      <Sidebar collapsed={collapsed} setCollapsed={setCollapsed}>
        <MainMenu collapsed={collapsed} />
      </Sidebar>
      <Layout className={styles.contentWrapper}>
        <ContentHeader />
        <Layout.Content className={styles.content}>
          {props.children}
        </Layout.Content>
      </Layout>
    </Layout>
  )
}

export default MainContainer
