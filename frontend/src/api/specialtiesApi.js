import { instance } from "."

export const specialtiesApi = {
  getAll: () => instance.get(`/specialties`),
  getPaged: ({ page, pageSize, filters, sortField, sortOrder }) => instance.get(`/specialties/paged`, { params: { page, pageSize, search: filters?.name?.[0], facultyId: filters?.facultyId?.[0], departmentId: filters?.departmentId?.[0], idSearch: filters?.id?.[0], sortField, sortOrder } }),
  getByDepartmentId: (departmentId) => instance.get(`/specialties/by-department/${departmentId}`),
  getById: (id) => instance.get(`/specialties/${id}`),
  create: (data) => instance.post("/specialties", data),
  update: (id, data) => instance.put(`/specialties/${id}`, data),
  delete: (id) => instance.delete(`/specialties/${id}`)
}