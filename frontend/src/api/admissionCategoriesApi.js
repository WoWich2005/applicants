import { instance } from "."

export const admissionCategoriesApi = {
  getAll: () => instance.get(`/admission_categories`),
  getAllByCompetitionList: (competitionListId) => instance.get(`/admission_categories?competitionListId=${competitionListId}`),
  getPagedByCompetitionList: (competitionListId, { page, pageSize, filters, sortField, sortOrder }) =>
    instance.get(`/admission_categories/paged`, {
      params: {
        competitionListId,
        page,
        pageSize,
        search: filters?.name?.[0],
        groupId: filters?.evaluationCriteriaGroupId?.[0],
        sortField,
        sortOrder
      }
    }),
  getBySpecialtyId: (specialtyId) => instance.get(`/admission_categories/by-specialty/${specialtyId}`),
  getById: (id) => instance.get(`/admission_categories/${id}`),
  create: (data) => instance.post("/admission_categories", data),
  update: (id, data) => instance.put(`/admission_categories/${id}`, data),
  delete: (id) => instance.delete(`/admission_categories/${id}`)
}
