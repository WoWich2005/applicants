import { instance } from "."

export const applicantsApi = {
  getAll: () => instance.get(`/applicants`),
  getPaged: ({ page, pageSize, filters, sortField, sortOrder }) => instance.get(`/applicants/paged`, { params: { page, pageSize, search: filters?.name?.[0], externalIdSearch: filters?.externalId?.[0], idSearch: filters?.id?.[0], sortField, sortOrder } }),
  getById: (id) => instance.get(`/applicants/${id}`),
  create: (data) => instance.post("/applicants", data),
  update: (id, data) => instance.put(`/applicants/${id}`, data),
  delete: (id) => instance.delete(`/applicants/${id}`)
}
