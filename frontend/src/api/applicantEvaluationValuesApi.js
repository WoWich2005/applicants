import { instance } from "."

export const applicantEvaluationValuesApi = {
  getAllByApplicant: (applicantId, { page = 1, pageSize = 20, filters, sortField, sortOrder } = {}) =>
    instance.get("/applicant_evaluation_values", {
      params: {
        applicantId, page, pageSize, sortField, sortOrder,
        id: filters?.id?.[0],
        criteria: filters?.criteriaName?.[0],
        value: filters?.value?.[0],
      }
    }),
  getById: (id) => instance.get(`/applicant_evaluation_values/${id}`),
  create: (data) => instance.post("/applicant_evaluation_values", data),
  update: (id, data) => instance.put(`/applicant_evaluation_values/${id}`, data),
  delete: (id) => instance.delete(`/applicant_evaluation_values/${id}`)
}
