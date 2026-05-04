using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface ISpecialtyRepository
    {
        Task<Specialty?> CreateAsync(Specialty specialty);
        Task<List<Specialty>> GetAllAsync();
        Task<PagedResponse<Specialty>> GetPagedAsync(int page, int pageSize, string? search, string? sortField = null, string? sortOrder = null);
        Task<Specialty?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(Specialty specialty);
        Task<bool> DeleteAsync(int id);
        Task<Dictionary<int, Specialty>> GetSpecialtiesDictionaryAsync();
        Task<List<Specialty>> GetByFacultyIdAsync(int facultyId);
        Task<List<Specialty>> GetByDepartmentIdAsync(int departmentId);
        Task<PagedResponse<Specialty>> GetPagedByFacultyIdAsync(int facultyId, int page, int pageSize, string? search, string? sortField = null, string? sortOrder = null);
        Task<PagedResponse<Specialty>> GetPagedByDepartmentIdAsync(int departmentId, int page, int pageSize, string? search, string? sortField = null, string? sortOrder = null);
        Task<bool> ExistsByNameAsync(string name, int departmentId, int? excludeId = null);
    }
}
