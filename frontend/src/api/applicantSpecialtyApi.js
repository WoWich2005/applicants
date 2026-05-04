import { instance } from "."

export const applicantSpecialtyApi = {
  getAllByApplicant: (applicantId) => instance.get(`/applicant_specialty?applicantId=${applicantId}`),
  getById: (id) => instance.get(`/applicant_specialty/${id}`),
  create: (data) => instance.post("/applicant_specialty", data),
  update: (id, data) => instance.put(`/applicant_specialty/${id}`, data),
  delete: (id) => instance.delete(`/applicant_specialty/${id}`)
}
