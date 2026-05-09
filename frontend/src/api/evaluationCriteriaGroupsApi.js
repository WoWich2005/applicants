import { instance } from "."

export const evaluationCriteriaGroupsApi = {
  getAll: () => instance.get(`/evaluation_criteria_groups`),
  getPaged: ({ page, pageSize, filters, sortField, sortOrder }) => instance.get(`/evaluation_criteria_groups/paged`, { params: { page, pageSize, search: filters?.name?.[0], idSearch: filters?.id?.[0], sortField, sortOrder } }),
  getById: (id) => instance.get(`/evaluation_criteria_groups/${id}`),
  getDeleteCheck: (id) => instance.get(`/evaluation_criteria_groups/${id}/delete-check`),
  create: (data) => instance.post("/evaluation_criteria_groups", data),
  update: (id, data) => instance.put(`/evaluation_criteria_groups/${id}`, data),
  delete: (id) => instance.delete(`/evaluation_criteria_groups/${id}`)
}
