import { instance } from "."

export const departmentsApi = {
  getAll: () => instance.get(`/departments`),
  getPaged: ({ page, pageSize, filters, sortField, sortOrder }) => instance.get(`/departments/paged`, { params: { page, pageSize, search: filters?.name?.[0], facultyId: filters?.facultyId?.[0], idSearch: filters?.id?.[0], sortField, sortOrder } }),
  getByFacultyId: (facultyId) => instance.get(`/departments/by-faculty/${facultyId}`),
  getById: (id) => instance.get(`/departments/${id}`),
  create: (data) => instance.post("/departments", data),
  update: (id, data) => instance.put(`/departments/${id}`, data),
  delete: (id) => instance.delete(`/departments/${id}`)
}
