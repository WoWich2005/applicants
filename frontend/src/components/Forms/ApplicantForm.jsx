import { Button, Form, Input, message, Space } from "antd"
import styles from "./styles.module.scss"
import { useEffect, useState } from "react"
import { applicantsApi } from "../../api/applicantsApi"
import { useTranslation } from "react-i18next"

const { TextArea } = Input

function ApplicantForm(props) {
  const { t } = useTranslation()
  const [messageApi, contextHolder] = message.useMessage()
  const [form] = Form.useForm()
  const [isLoading, setIsLoading] = useState(false)

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
      const serverMessage = err?.response?.data?.message
      messageApi.error(serverMessage ?? t('applicant.form.saveError'))
      console.log(err)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    form.setFieldsValue(props.initialValues)
  }, [props.initialValues, form])

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
        disabled={!!props.readOnly}
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
              <Input />
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
              <Input />
            </Form.Item>
          </div>
        </div>
        <div className={styles.formRow}>
          <div className={styles.formColumn}>
            <Form.Item
              label={t('applicant.form.notesLabel')}
              name="notes"
            >
              <TextArea rows={4} />
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
