import { Button, Form, Input, message, Space, Typography } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { applicantsApi } from "../../api/applicantsApi"
import { auditApi } from "../../api/auditApi"
import { useTranslation } from "react-i18next"
import { usePermissions } from "../../hooks/usePermissions"

const { TextArea } = Input

function ApplicantForm(props) {
  const { t } = useTranslation()
  const { canWriteAudit } = usePermissions()
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)
  const [deletedConflict, setDeletedConflict] = useState(null)
  const [isRestoring, setIsRestoring] = useState(false)

  const api = applicantsApi

  const onFinish = async (formData) => {
    setIsLoading(true)

    try {
      const delayPromise = new Promise(resolve => setTimeout(resolve, 500))

      if(props.elementId) {
        const updatePromise = api.update(props.elementId, formData)
        await Promise.all([delayPromise, updatePromise])

        const updatedEl = (await api.getById(props.elementId)).data

        props.handleRequestResult && props.handleRequestResult(updatedEl)
      }else{
        const createPromise = api.create(formData)
        const [_, createdElResponse] = await Promise.all([delayPromise, createPromise])

        props.handleRequestResult && props.handleRequestResult(createdElResponse.data)
      }

      form.resetFields()
    } catch (err) {
      const data = err?.response?.data
      if (err?.response?.status === 409 && data?.isDeleted && data?.deletedApplicant) {
        setDeletedConflict(data.deletedApplicant)
      } else {
        messageApi.error(data?.message ?? t('applicant.form.saveError'))
      }
    } finally {
      setIsLoading(false)
    }
  }

  const handleRestore = async () => {
    setIsRestoring(true)
    try {
      await auditApi.rejectDeletion(deletedConflict.id)
      const restored = (await api.getById(deletedConflict.id)).data
      messageApi.success(t('applicant.form.deletedConflict.restoreSuccess'))
      setDeletedConflict(null)
      form.resetFields()
      props.handleRequestResult && props.handleRequestResult(restored)
    } catch {
      messageApi.error(t('applicant.form.deletedConflict.restoreError'))
    } finally {
      setIsRestoring(false)
    }
  }

  const handleExternalIdChange = () => {
    if (deletedConflict) setDeletedConflict(null)
  }

  useEffect(() => {
    form.setFieldsValue(props.initialValues)
  }, [props.initialValues, form])

  if (deletedConflict) {
    return (
      <>
        {contextHolder}
        <Form layout="vertical" className={styles.form}>
          <Typography.Text style={{ display: 'block', marginBottom: 16 }}>
            {t('applicant.form.deletedConflict.description')}
          </Typography.Text>
          <div className={styles.formRow}>
            <div className={styles.formColumn}>
              <Form.Item label={t('applicant.form.idLabel')}>
                <Input readOnly value={deletedConflict.externalId} />
              </Form.Item>
            </div>
          </div>
          <div className={styles.formRow}>
            <div className={styles.formColumn}>
              <Form.Item label={t('applicant.form.nameLabel')}>
                <Input readOnly value={deletedConflict.name} />
              </Form.Item>
            </div>
          </div>
          <div className={styles.formRow}>
            <div className={styles.formColumn}>
              <Form.Item label={t('applicant.form.notesLabel')}>
                <TextArea rows={4} readOnly value={deletedConflict.notes ?? ''} />
              </Form.Item>
            </div>
          </div>
          {!canWriteAudit && (
            <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
              {t('applicant.form.deletedConflict.noPermission')}
            </Typography.Text>
          )}
          <div className={styles.formRow}>
            <Space>
              {canWriteAudit && (
                <Button type="primary" onClick={handleRestore} loading={isRestoring}>
                  {t('applicant.form.deletedConflict.restoreButton')}
                </Button>
              )}
              {props.buttons}
            </Space>
          </div>
        </Form>
      </>
    )
  }

  return (
    <>
      {contextHolder}
      <Form
        form={form}
        className={styles.form}
        layout="vertical"
        initialValues={props.initialValues}
        onFinish={onFinish}
        autoComplete="off"
      >
        <div className={styles.formRow}>
          <div className={styles.formColumn}>
            <Form.Item
              label={t('applicant.form.idLabel')}
              name="externalId"
              rules={[
                {
                  required: true,
                  message: t('applicant.form.idRequired'),
                },
                {
                  max: 255,
                  message: t('applicant.form.idMaxLength'),
                }
              ]}
            >
              <Input readOnly={!!props.readOnly} onChange={handleExternalIdChange} />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <div className={styles.formColumn}>
            <Form.Item
              label={t('applicant.form.nameLabel')}
              name="name"
              rules={[
                {
                  required: true,
                  message: t('applicant.form.nameRequired'),
                }
              ]}
            >
              <Input readOnly={!!props.readOnly} />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <div className={styles.formColumn}>
            <Form.Item
              label={t('applicant.form.notesLabel')}
              name="notes"
            >
              <TextArea rows={4} readOnly={!!props.readOnly} />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <Space>
            {!props.readOnly && (
              <Button type="primary" htmlType="submit" loading={isLoading}>
                {props.elementId ? t('applicant.form.updateButton') : t('applicant.form.addButton')}
              </Button>
            )}
            {props.buttons}
          </Space>
        </div>
      </Form>
    </>
  )
}

export default ApplicantForm
