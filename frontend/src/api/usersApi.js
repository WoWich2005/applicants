import { instance } from './index'

export const usersApi = {
  getPaged: ({ page, pageSize, search, role, isActive, idSearch }) =>
    instance.get('/users', { params: { page, pageSize, search, role, isActive, idSearch } }),
  getById: (id) => instance.get(`/users/${id}`),
  create: (data) => instance.post('/users', data),
  update: (id, data) => instance.put(`/users/${id}`, data),
  toggleActive: (id) => instance.patch(`/users/${id}/toggle-active`),
  delete: (id) => instance.delete(`/users/${id}`),
}
