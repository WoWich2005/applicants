import { instance } from "."

export const selectionApi = {
  recalculateAll: () => instance.post("/selection/recalculate"),

  getCompetitionListsPaged: ({ page, pageSize, search, facultySearch, departmentSearch, specialtySearch, selectedCount, sortField, sortOrder }) =>
    instance.get("/selection/competition-lists", {
      params: { page, pageSize, search, facultySearch, departmentSearch, specialtySearch, selectedCount, sortField, sortOrder }
    }),

  getCompetitionListResult: (id) => instance.get(`/selection/competition-lists/${id}/result`),

  getCompetitionListHeader: (id) => instance.get(`/selection/competition-lists/${id}/header`),

  getCategoryApplicantsPaged: ({ clId, catId, type, page, pageSize, id, externalId, name, sortField, sortOrder, ...scoreFilters }) =>
    instance.get(`/selection/competition-lists/${clId}/categories/${catId}/applicants`, {
      params: { type, page, pageSize, id, externalId, name, sortField, sortOrder, ...scoreFilters }
    }),

  downloadCompetitionListExcel: (id) =>
    instance.get(`/selection/competition-lists/${id}/excel`, { responseType: "blob" }),
}
