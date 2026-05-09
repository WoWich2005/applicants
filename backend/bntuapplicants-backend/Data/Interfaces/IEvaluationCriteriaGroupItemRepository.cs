using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IEvaluationCriteriaGroupItemRepository
    {
        Task<EvaluationCriteriaGroupItem?> CreateAsync(EvaluationCriteriaGroupItem item);
        Task<List<EvaluationCriteriaGroupItem>> GetAllByGroupAsync(int groupId);
        Task<PagedResponse<EvaluationCriteriaGroupItemDto>> GetAllByGroupPagedAsync(
            int groupId, int page, int pageSize,
            string? sortField = null, string? sortOrder = null,
            string? id = null, string? priority = null, string? criteria = null);
        Task<EvaluationCriteriaGroupItem?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(EvaluationCriteriaGroupItem item);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsInGroupAsync(int groupId, int criteriaId, int? excludeId = null);
    }
}
