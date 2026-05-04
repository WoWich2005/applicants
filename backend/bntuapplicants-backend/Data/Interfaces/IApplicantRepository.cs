using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IApplicantRepository
    {
        Task<Applicant?> CreateAsync(Applicant applicant);
        Task<List<Applicant>> GetAllAsync();
        Task<PagedResponse<Applicant>> GetPagedAsync(int page, int pageSize, string? search, string? externalIdSearch = null, string? sortField = null, string? sortOrder = null);
        Task<Applicant?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(Applicant applicant);
        Task<bool> DeleteAsync(int id);
    }
}
