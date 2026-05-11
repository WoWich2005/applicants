using Npgsql;
using System.Text.Json;

namespace bntuapplicants_backend.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly string _connectionString;
        private readonly ICurrentUserService _currentUser;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AuditLogger(IConfiguration configuration, ICurrentUserService currentUser)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _currentUser = currentUser;
        }

        public Task LogCreateAsync(string entityType, string entityId, object after, NpgsqlTransaction? tx = null)
        {
            var changes = new { after };
            return WriteAsync("create", entityType, entityId, changes, tx);
        }

        public Task LogUpdateAsync(string entityType, string entityId, object diff, NpgsqlTransaction? tx = null)
        {
            var changes = new { diff };
            return WriteAsync("update", entityType, entityId, changes, tx);
        }

        public Task LogDeleteAsync(string entityType, string entityId, object before, NpgsqlTransaction? tx = null)
        {
            var changes = new { before };
            return WriteAsync("delete", entityType, entityId, changes, tx);
        }

        public Task LogValidateAsync(int applicantId, string? comment, NpgsqlTransaction? tx = null)
        {
            var changes = new { comment };
            return WriteAsync("validate", "applicant", applicantId.ToString(), changes, tx);
        }

        public Task LogInvalidateAsync(int applicantId, bool automatic, string? comment = null, NpgsqlTransaction? tx = null)
        {
            var changes = new { comment, automatic };
            return WriteAsync("invalidate", "applicant", applicantId.ToString(), changes, tx);
        }

        public Task LogDeleteConfirmedAsync(int applicantId, NpgsqlTransaction? tx = null)
        {
            return WriteAsync("delete_confirmed", "applicant", applicantId.ToString(), null, tx);
        }

        public Task LogDeleteRejectedAsync(int applicantId, NpgsqlTransaction? tx = null)
        {
            return WriteAsync("delete_rejected", "applicant", applicantId.ToString(), null, tx);
        }

        public Task LogRecalculationAsync(int selectedCount, NpgsqlTransaction tx)
        {
            var changes = new { selectedCount };
            return WriteAsync("recalculate_all", "selection", "all", changes, tx);
        }

        public async Task ResetValidationIfNeededAsync(int applicantId, NpgsqlTransaction tx)
        {
            var conn = tx.Connection!;

            const string selectSql = "SELECT validated FROM applicants WHERE id = @ApplicantId";
            using var selectCmd = new NpgsqlCommand(selectSql, conn, tx);
            selectCmd.Parameters.AddWithValue("@ApplicantId", applicantId);

            var result = await selectCmd.ExecuteScalarAsync();
            if (result is not true)
                return;

            const string updateSql = "UPDATE applicants SET validated = false WHERE id = @ApplicantId";
            using var updateCmd = new NpgsqlCommand(updateSql, conn, tx);
            updateCmd.Parameters.AddWithValue("@ApplicantId", applicantId);
            await updateCmd.ExecuteNonQueryAsync();

            await LogInvalidateAsync(applicantId, automatic: true, comment: "данные изменены", tx: tx);
        }

        private async Task WriteAsync(string action, string entityType, string entityId, object? changes, NpgsqlTransaction? tx)
        {
            const string sql = @"
                INSERT INTO audit_log (user_id, username, action, entity_type, entity_id, changes)
                VALUES (@UserId, @Username, @Action, @EntityType, @EntityId, @Changes::jsonb)";

            NpgsqlConnection? ownedConnection = null;
            NpgsqlConnection connection;

            if (tx != null)
            {
                connection = tx.Connection!;
            }
            else
            {
                ownedConnection = new NpgsqlConnection(_connectionString);
                await ownedConnection.OpenAsync();
                connection = ownedConnection;
            }

            try
            {
                using var cmd = new NpgsqlCommand(sql, connection, tx);
                cmd.Parameters.AddWithValue("@UserId", (object?)_currentUser.UserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Username", (object?)_currentUser.Username ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@EntityType", entityType);
                cmd.Parameters.AddWithValue("@EntityId", entityId);
                cmd.Parameters.AddWithValue("@Changes", changes != null
                    ? JsonSerializer.Serialize(changes, _jsonOptions)
                    : DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                if (ownedConnection != null)
                    await ownedConnection.DisposeAsync();
            }
        }
    }
}
