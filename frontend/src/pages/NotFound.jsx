import { Result, Button } from 'antd'
import { useNavigate } from 'react-router'
import { useTranslation } from 'react-i18next'
import { ROUTES } from '../constants/routes'

export default function NotFound() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  return (
    <Result
      status="404"
      title="404"
      subTitle={t('notFound.subTitle')}
      extra={
        <Button type="primary" onClick={() => navigate(ROUTES.RESULTS)}>
          {t('notFound.backHome')}
        </Button>
      }
    />
  )
}
