using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IEvaluationCriteriaGroupRepository
    {
        Task<EvaluationCriteriaGroup?> CreateAsync(EvaluationCriteriaGroup group);
        Task<List<EvaluationCriteriaGroup>> GetAllAsync();
        Task<PagedResponse<EvaluationCriteriaGroup>> GetPagedAsync(int page, int pageSize, string? search, string? idSearch = null, string? sortField = null, string? sortOrder = null);
        Task<EvaluationCriteriaGroup?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(EvaluationCriteriaGroup group);
        Task<bool> DeleteAsync(int id);
        Task<List<AdmissionCategoryPathDto>> GetAdmissionCategoriesUsingGroupAsync(int groupId);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    }
}
