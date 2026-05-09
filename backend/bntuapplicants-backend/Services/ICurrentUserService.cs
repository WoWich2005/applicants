namespace bntuapplicants_backend.Services
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? Username { get; }
        string? Role { get; }
    }
}
