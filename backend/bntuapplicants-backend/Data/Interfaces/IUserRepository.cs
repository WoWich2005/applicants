using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> AnyExistsAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<(List<UserResponseDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, string? role = null, bool? isActive = null, string? idSearch = null, string? sortField = null, string? sortOrder = null);
        Task<UserResponseDto?> GetDetailedByIdAsync(int id);
        Task<User> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> ToggleActiveAsync(int id);
        Task<bool> DeleteAsync(int id);
        Task<bool> ChangePasswordAsync(int id, string newPasswordHash);
        Task<int> CountByRoleAsync(string role);
    }
}
