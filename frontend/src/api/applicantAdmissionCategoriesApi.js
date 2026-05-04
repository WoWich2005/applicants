import { instance } from "."

export const applicantAdmissionCategoriesApi = {
  getAllByApplicant: (applicantId) => instance.get(`/applicant_admission_categories?applicantId=${applicantId}`),
  getApplicantsByCategory: (admissionCategoryId) => instance.get(`/applicant_admission_categories/by-category/${admissionCategoryId}`),
  getById: (id) => instance.get(`/applicant_admission_categories/${id}`),
  create: (data) => instance.post("/applicant_admission_categories", data),
  update: (id, data) => instance.put(`/applicant_admission_categories/${id}`, data),
  delete: (id) => instance.delete(`/applicant_admission_categories/${id}`)
}
