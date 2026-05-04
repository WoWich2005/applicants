import { Typography } from "antd"
import HelpButton from "../Buttons/HelpButton"
import styles from "./styles.module.scss"

function Title(props) {
  return (
    <div className={styles.titleWrapper}>
      <Typography.Title className={styles.title}>
        {props.title}
      </Typography.Title>
      {props.helpText && (
        <HelpButton popoverPlacement="bottom">{props.helpText}</HelpButton>
      )}
    </div>
  )
}

export default Title
