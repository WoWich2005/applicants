using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IApplicantEvaluationValueRepository
    {
        Task<ApplicantEvaluationValue?> CreateAsync(ApplicantEvaluationValue record);
        Task<bool> ExistsForApplicantAsync(int applicantId, int evaluationCriteriaId, int? excludeId = null);
        Task<PagedResponse<ApplicantEvaluationValueDto>> GetAllByApplicantPagedAsync(
            int applicantId, int page, int pageSize,
            string? sortField = null, string? sortOrder = null,
            string? id = null, string? criteria = null, string? value = null);
        Task<ApplicantEvaluationValue?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(ApplicantEvaluationValue record);
        Task<bool> DeleteAsync(int id);
    }
}
