using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<Department?> CreateAsync(Department department);
        Task<List<Department>> GetAllAsync();
        Task<PagedResponse<Department>> GetPagedAsync(int page, int pageSize, string? search, string? idSearch = null, string? sortField = null, string? sortOrder = null);
        Task<List<Department>> GetByFacultyIdAsync(int facultyId);
        Task<Department?> GetByIdAsync(int id);
        Task<PagedResponse<Department>> GetPagedByFacultyIdAsync(int facultyId, int page, int pageSize, string? search, string? idSearch = null, string? sortField = null, string? sortOrder = null);
        Task<bool> UpdateAsync(Department department);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int facultyId, int? excludeId = null);
    }
}
