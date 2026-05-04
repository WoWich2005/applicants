using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IAdmissionCategoryRepository
    {
        Task<AdmissionCategory?> CreateAsync(AdmissionCategory category);
        Task<List<AdmissionCategory>> GetAllAsync();
        Task<List<AdmissionCategory>> GetAllByCompetitionListAsync(int competitionListId);
        Task<PagedResponse<AdmissionCategory>> GetPagedByCompetitionListAsync(int competitionListId, int page, int pageSize, string? search, int? groupId, string? sortField, string? sortOrder);
        Task<List<AdmissionCategory>> GetAllBySpecialtyIdAsync(int specialtyId);
        Task<AdmissionCategory?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(AdmissionCategory category);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int competitionListId, int? excludeId = null);
    }
}
