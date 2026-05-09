namespace bntuapplicants_backend.Services
{
    public interface IAuthLogger
    {
        Task LogAsync(string eventType, string username, int? userId = null, string? failureReason = null, string? ipAddress = null, string? userAgent = null);
    }
}
