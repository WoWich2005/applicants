using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IEvaluationCriteriaGroupItemRepository
    {
        Task<EvaluationCriteriaGroupItem?> CreateAsync(EvaluationCriteriaGroupItem item);
        Task<List<EvaluationCriteriaGroupItem>> GetAllByGroupAsync(int groupId);
        Task<EvaluationCriteriaGroupItem?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(EvaluationCriteriaGroupItem item);
        Task<bool> DeleteAsync(int id);
    }
}
