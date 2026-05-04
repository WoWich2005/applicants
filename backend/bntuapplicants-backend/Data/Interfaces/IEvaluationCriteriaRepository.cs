using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IEvaluationCriteriaRepository
    {
        Task<EvaluationCriteria?> CreateAsync(EvaluationCriteria evaluationCriteria);
        Task<List<EvaluationCriteria>> GetAllAsync();
        Task<PagedResponse<EvaluationCriteria>> GetPagedAsync(int page, int pageSize, string? search, string? type = null, string? sortField = null, string? sortOrder = null);
        Task<EvaluationCriteria?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(EvaluationCriteria evaluationCriteria);
        Task<bool> DeleteAsync(int id);
        Task<EvaluationCriteriaDeleteCheckDto> GetDeleteCheckAsync(int criteriaId);
        Task<EvaluationCriteriaRangeCheckDto> GetRangeCheckAsync(int criteriaId, int minValue, int maxValue);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    }
}
