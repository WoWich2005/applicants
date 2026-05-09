using Npgsql;
using System.Net;

namespace bntuapplicants_backend.Services
{
    public class AuthLogger : IAuthLogger
    {
        private readonly string _connectionString;

        public AuthLogger(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task LogAsync(string eventType, string username, int? userId = null, string? failureReason = null, string? ipAddress = null, string? userAgent = null)
        {
            const string sql = @"
                INSERT INTO auth_log (user_id, username, event_type, failure_reason, ip_address, user_agent)
                VALUES (@UserId, @Username, @EventType, @FailureReason, @IpAddress, @UserAgent)";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Username", (object?)username ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EventType", eventType);
            cmd.Parameters.AddWithValue("@FailureReason", (object?)failureReason ?? DBNull.Value);

            var ipParam = cmd.Parameters.Add("@IpAddress", NpgsqlTypes.NpgsqlDbType.Inet);
            ipParam.Value = IPAddress.TryParse(ipAddress, out var ip) ? ip : DBNull.Value;

            cmd.Parameters.AddWithValue("@UserAgent", (object?)userAgent ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
