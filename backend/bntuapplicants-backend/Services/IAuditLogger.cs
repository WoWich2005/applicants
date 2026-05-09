using Npgsql;

namespace bntuapplicants_backend.Services
{
    public interface IAuditLogger
    {
        Task LogCreateAsync(string entityType, string entityId, object after, NpgsqlTransaction? tx = null);
        Task LogUpdateAsync(string entityType, string entityId, object diff, NpgsqlTransaction? tx = null);
        Task LogDeleteAsync(string entityType, string entityId, object before, NpgsqlTransaction? tx = null);
        Task LogValidateAsync(int applicantId, string? comment, NpgsqlTransaction? tx = null);
        Task LogInvalidateAsync(int applicantId, bool automatic, string? comment = null, NpgsqlTransaction? tx = null);
        Task LogDeleteConfirmedAsync(int applicantId, NpgsqlTransaction? tx = null);
        Task LogDeleteRejectedAsync(int applicantId, NpgsqlTransaction? tx = null);
        Task ResetValidationIfNeededAsync(int applicantId, NpgsqlTransaction tx);
    }
}
