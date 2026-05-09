using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface ICompetitionListRepository
    {
        Task<CompetitionList?> CreateAsync(CompetitionList competitionList);
        Task<List<CompetitionList>> GetAllAsync();
        Task<List<CompetitionList>> GetBySpecialtyIdAsync(int specialtyId);
        Task<PagedResponse<CompetitionList>> GetPagedBySpecialtyAsync(int specialtyId, int page, int pageSize, string? search, string? idSearch, string? planSearch, string? sortField, string? sortOrder);
        Task<CompetitionList?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(CompetitionList competitionList);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int specialtyId, int? excludeId = null);
    }
}
