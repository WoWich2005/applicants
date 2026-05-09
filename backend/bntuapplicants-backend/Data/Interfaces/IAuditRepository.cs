using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IAuditRepository
    {
        Task<PagedResponse<AuditLogEntry>> GetEntityHistoryAsync(string entityType, string entityId, int page, int pageSize, string? username = null, string? action = null, string? logEntityType = null, DateTime? from = null, DateTime? to = null);
        Task<PagedResponse<AuditLogEntry>> GetAuditLogPagedAsync(int page, int pageSize, string? username = null, string? entityType = null, string? action = null, string? entityId = null, DateTime? from = null, DateTime? to = null);
        Task<PagedResponse<AuthLogEntry>> GetAuthLogPagedAsync(int page, int pageSize, string? userId = null, string? username = null, string? eventType = null, string? ipAddress = null, string? failureReason = null, string? userAgent = null, DateTime? from = null, DateTime? to = null);
        Task<ValidationStatusDto?> GetValidationStatusAsync(int applicantId);
        Task ValidateApplicantAsync(int applicantId);
        Task InvalidateApplicantAsync(int applicantId);
        Task<AlertsSummaryDto> GetAlertsSummaryAsync(int? facultyId, List<int> facultyAccessIds, List<int> specialtyIds);
        Task<PagedResponse<UnvalidatedApplicantDto>> GetUnvalidatedApplicantsAsync(int page, int pageSize, int? facultyId, List<int> facultyAccessIds, List<int> specialtyIds, string? status = null, string? search = null);
        Task<PagedResponse<IncompleteApplicantDto>> GetIncompleteApplicantsAsync(int page, int pageSize, int? facultyId, List<int> facultyAccessIds, List<int> specialtyIds, string? search = null);
        Task<PagedResponse<InvalidCriteriaGroupDto>> GetInvalidCriteriaGroupsAsync(int page, int pageSize, string? search = null);
        Task<PagedResponse<InvalidCompetitionListDto>> GetInvalidAdmissionCategoriesAsync(int page, int pageSize, string? search = null, string? faculty = null, string? department = null, string? specialty = null);
        Task<(bool canValidate, List<string> missingCriteria, bool invalidPriorities)> CheckValidationBlockersAsync(int applicantId);
    }
}
