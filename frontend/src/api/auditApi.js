// @ts-nocheck
import { instance } from "."

export const auditApi = {
  getEntityHistory: (entityType, entityId, page = 1, pageSize = 10, { username, action, logEntityType, from, to, sortOrder } = {}) =>
    instance.get("/audit/entity-history", { params: { entityType, entityId, page, pageSize, username, action, logEntityType, from, to, sortOrder } }),

  getAuditLog: ({ page = 1, pageSize = 20, filters, from, to, sortOrder } = {}) =>
    instance.get("/audit/log", { params: { page, pageSize, username: filters?.username?.[0], entityType: filters?.entityType?.[0], action: filters?.action?.[0], entityId: filters?.entityId?.[0], from, to, sortOrder } }),

  getAuthLog: ({ page = 1, pageSize = 20, filters, from, to, sortOrder } = {}) =>
    instance.get("/audit/auth-log", { params: { page, pageSize, userId: filters?.userId?.[0], username: filters?.username?.[0], eventType: filters?.eventType?.[0], ipAddress: filters?.ipAddress?.[0], failureReason: filters?.failureReason?.[0], userAgent: filters?.userAgent?.[0], from, to, sortOrder } }),

  getValidationStatus: (applicantId) =>
    instance.get(`/audit/validation/${applicantId}`),

  validate: (applicantId, data) =>
    instance.post(`/audit/validation/${applicantId}`, data),

  invalidate: (applicantId, data) =>
    instance.delete(`/audit/validation/${applicantId}`, { data }),

  getAlertsSummary: () =>
    instance.get("/audit/alerts-summary"),

  getUnvalidated: ({ page = 1, pageSize = 20, filters } = {}) =>
    instance.get("/audit/unvalidated", { params: { page, pageSize, status: filters?.validated?.[0], search: filters?.name?.[0] ?? filters?.externalId?.[0] } }),

  getIncomplete: ({ page = 1, pageSize = 20, filters } = {}) =>
    instance.get("/audit/incomplete", { params: { page, pageSize, search: filters?.name?.[0] ?? filters?.externalId?.[0] } }),

  getIncompleteForApplicant: (applicantId) =>
    instance.get(`/audit/incomplete/${applicantId}`),

  getInvalidCriteriaGroups: ({ page = 1, pageSize = 20, filters } = {}) =>
    instance.get("/audit/invalid-criteria-groups", { params: { page, pageSize, search: filters?.name?.[0] } }),

  getInvalidAdmissionCategories: ({ page = 1, pageSize = 20, filters } = {}) =>
    instance.get("/audit/invalid-admission-categories", { params: { page, pageSize, search: filters?.name?.[0], faculty: filters?.facultyName?.[0], department: filters?.departmentName?.[0], specialty: filters?.specialtyName?.[0] } }),

  getPendingDeletions: ({ page = 1, pageSize = 20, filters } = {}) =>
    instance.get("/audit/pending-deletions", { params: {
      page,
      pageSize,
      search: filters?.name?.[0] ?? filters?.externalId?.[0],
      requestedBySearch: filters?.requestedByUsername?.[0],
      status: filters?.status?.[0],
      idSearch: filters?.applicantId?.[0],
    }}),

  confirmDeletion: (applicantId) =>
    instance.post(`/audit/pending-deletions/${applicantId}/confirm`),

  rejectDeletion: (applicantId) =>
    instance.post(`/audit/pending-deletions/${applicantId}/reject`),
}
