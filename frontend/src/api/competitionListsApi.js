import { instance } from "."

export const competitionListsApi = {
  getAll: () => instance.get(`/competition_lists`),
  getBySpecialtyId: (specialtyId) => instance.get(`/competition_lists/by-specialty/${specialtyId}`),
  getPagedBySpecialtyId: (specialtyId, { page, pageSize, filters, sortField, sortOrder }) =>
    instance.get(`/competition_lists/by-specialty/${specialtyId}/paged`, {
      params: { page, pageSize, search: filters?.name?.[0], sortField, sortOrder }
    }),
  getById: (id) => instance.get(`/competition_lists/${id}`),
  create: (data) => instance.post("/competition_lists", data),
  update: (id, data) => instance.put(`/competition_lists/${id}`, data),
  delete: (id) => instance.delete(`/competition_lists/${id}`)
}
