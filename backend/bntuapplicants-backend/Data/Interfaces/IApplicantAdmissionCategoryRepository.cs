using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IApplicantAdmissionCategoryRepository
    {
        Task<ApplicantAdmissionCategory?> CreateAsync(ApplicantAdmissionCategory record);
        Task<bool> ExistsAsync(int applicantId, int admissionCategoryId, int? excludeId = null);
        Task<PagedResponse<ApplicantAdmissionCategoryDto>> GetAllByApplicantPagedAsync(
            int applicantId, int page, int pageSize,
            string? sortField = null, string? sortOrder = null,
            string? id = null, string? selectionPriority = null, string? faculty = null, string? department = null,
            string? specialty = null, string? competitionList = null,
            string? category = null);
        Task<List<Applicant>> GetApplicantsByAdmissionCategoryAsync(int admissionCategoryId);
        Task<ApplicantAdmissionCategory?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(ApplicantAdmissionCategory record);
        Task<bool> DeleteAsync(int id);
    }
}
