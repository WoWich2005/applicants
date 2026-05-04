import { instance } from "."

export const facultiesApi = {
  getAll: () => instance.get(`/faculties`),
  getPaged: ({ page, pageSize, filters, sortField, sortOrder }) => instance.get(`/faculties/paged`, { params: { page, pageSize, search: filters?.name?.[0], sortField, sortOrder } }),
  getById: (id) => instance.get(`/faculties/${id}`),
  create: (data) => instance.post("/faculties", data),
  update: (id, data) => instance.put(`/faculties/${id}`, data),
  delete: (id) => instance.delete(`/faculties/${id}`)
}
