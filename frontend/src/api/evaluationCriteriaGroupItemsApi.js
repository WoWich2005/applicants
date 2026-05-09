import { instance } from "."

export const evaluationCriteriaGroupItemsApi = {
  getAllByGroup: (groupId) => instance.get(`/evaluation_criteria_group_items?groupId=${groupId}`),
  getPagedByGroup: (groupId, { page = 1, pageSize = 20, filters, sortField, sortOrder } = {}) =>
    instance.get("/evaluation_criteria_group_items/paged", {
      params: {
        groupId, page, pageSize, sortField, sortOrder,
        id: filters?.id?.[0],
        priority: filters?.priority?.[0],
        criteria: filters?.criteriaName?.[0],
      }
    }),
  getById: (id) => instance.get(`/evaluation_criteria_group_items/${id}`),
  create: (data) => instance.post("/evaluation_criteria_group_items", data),
  update: (id, data) => instance.put(`/evaluation_criteria_group_items/${id}`, data),
  delete: (id) => instance.delete(`/evaluation_criteria_group_items/${id}`)
}
