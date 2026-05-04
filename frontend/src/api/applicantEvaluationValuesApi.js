import { instance } from "."

export const applicantEvaluationValuesApi = {
  getAllByApplicant: (applicantId) => instance.get(`/applicant_evaluation_values?applicantId=${applicantId}`),
  getById: (id) => instance.get(`/applicant_evaluation_values/${id}`),
  create: (data) => instance.post("/applicant_evaluation_values", data),
  update: (id, data) => instance.put(`/applicant_evaluation_values/${id}`, data),
  delete: (id) => instance.delete(`/applicant_evaluation_values/${id}`)
}
