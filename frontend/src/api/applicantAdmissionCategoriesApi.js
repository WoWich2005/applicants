import { instance } from "."

export const applicantAdmissionCategoriesApi = {
  getAllByApplicant: (applicantId, { page = 1, pageSize = 20, filters, sortField, sortOrder } = {}) =>
    instance.get("/applicant_admission_categories", {
      params: {
        applicantId, page, pageSize, sortField, sortOrder,
        id: filters?.id?.[0],
        selectionPriority: filters?.selectionPriority?.[0],
        faculty: filters?.facultyName?.[0],
        department: filters?.departmentName?.[0],
        specialty: filters?.specialtyName?.[0],
        competitionList: filters?.competitionListName?.[0],
        category: filters?.categoryName?.[0],
      }
    }),
  getApplicantsByCategory: (admissionCategoryId) => instance.get(`/applicant_admission_categories/by-category/${admissionCategoryId}`),
  getById: (id) => instance.get(`/applicant_admission_categories/${id}`),
  create: (data) => instance.post("/applicant_admission_categories", data),
  update: (id, data) => instance.put(`/applicant_admission_categories/${id}`, data),
  delete: (id) => instance.delete(`/applicant_admission_categories/${id}`)
}
