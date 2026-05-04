using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IFacultyRepository
    {
        Task<Faculty?> CreateAsync(Faculty faculty);
        Task<List<Faculty>> GetAllAsync();
        Task<PagedResponse<Faculty>> GetPagedAsync(int page, int pageSize, string? search, string? sortField = null, string? sortOrder = null);
        Task<Faculty?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(Faculty parameter);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    }
}
