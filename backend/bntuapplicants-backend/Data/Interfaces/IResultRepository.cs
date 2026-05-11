using bntuapplicants_backend.Dtos.Responses;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IResultRepository
    {
        Task<PagedResponse<CompetitionListSummaryDto>> GetCompetitionListsPagedAsync(
            int page, int pageSize,
            string? search,
            string? facultySearch, string? departmentSearch, string? specialtySearch,
            int? selectedCount,
            string? sortField, string? sortOrder);

        Task<CompetitionListResultDto?> GetCompetitionListResultAsync(int clId);

        Task<CompetitionListHeaderDto?> GetCompetitionListHeaderAsync(int clId);

        Task<PagedResponse<ApplicantResultDto>> GetCategoryApplicantsPagedAsync(
            int clId, int catId, bool admitted,
            int page, int pageSize,
            string? idFilter, string? externalIdFilter, string? nameFilter,
            Dictionary<int, string> scoreFilters,
            string? sortField, string? sortOrder);
    }
}
