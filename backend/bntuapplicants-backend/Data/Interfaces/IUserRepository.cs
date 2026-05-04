using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> AnyExistsAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<(List<UserResponseDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, string? role = null, bool? isActive = null);
        Task<UserResponseDto?> GetDetailedByIdAsync(int id);
        Task<User> CreateAsync(User user, List<int> specialtyIds, List<int> facultyAccessIds);
        Task<bool> UpdateAsync(User user, List<int> specialtyIds, List<int> facultyAccessIds);
        Task<bool> ToggleActiveAsync(int id);
        Task<bool> DeleteAsync(int id);
        Task<bool> ChangePasswordAsync(int id, string newPasswordHash);
        Task<List<int>> GetSpecialtyIdsAsync(int userId);
        Task<List<int>> GetFacultyAccessIdsAsync(int userId);
        Task<int> CountByRoleAsync(string role);
    }
}
