import { instance } from "."

export const evaluationCriteriaApi = {
  getAll: () => instance.get(`/evaluation_criteria`),
  getPaged: ({ page, pageSize, filters, sortField, sortOrder }) => instance.get(`/evaluation_criteria/paged`, { params: { page, pageSize, search: filters?.name?.[0], type: filters?.type?.[0], sortField, sortOrder } }),
  getById: (id) => instance.get(`/evaluation_criteria/${id}`),
  getDeleteCheck: (id) => instance.get(`/evaluation_criteria/${id}/delete-check`),
  create: (data) => instance.post("/evaluation_criteria", data),
  update: (id, data) => instance.put(`/evaluation_criteria/${id}`, data),
  delete: (id) => instance.delete(`/evaluation_criteria/${id}`)
}
