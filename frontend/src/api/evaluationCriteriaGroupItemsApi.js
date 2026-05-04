import { instance } from "."

export const evaluationCriteriaGroupItemsApi = {
  getAllByGroup: (groupId) => instance.get(`/evaluation_criteria_group_items?groupId=${groupId}`),
  getById: (id) => instance.get(`/evaluation_criteria_group_items/${id}`),
  create: (data) => instance.post("/evaluation_criteria_group_items", data),
  update: (id, data) => instance.put(`/evaluation_criteria_group_items/${id}`, data),
  delete: (id) => instance.delete(`/evaluation_criteria_group_items/${id}`)
}
