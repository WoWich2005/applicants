using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class ApplicantDeletionRequestRepository : IApplicantDeletionRequestRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public ApplicantDeletionRequestRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<bool> RequestAsync(int applicantId, int? requestedByUserId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            const string upsertSql = @"
                INSERT INTO applicant_deletion_requests
                    (applicant_id, requested_by_user_id, status)
                VALUES (@ApplicantId, @UserId, 'pending')
                ON CONFLICT (applicant_id) DO UPDATE
                    SET requested_by_user_id = EXCLUDED.requested_by_user_id,
                        status               = 'pending'
                WHERE applicant_deletion_requests.status = 'rejected'";

            int affected;
            using (var cmd = new NpgsqlCommand(upsertSql, connection, (NpgsqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
                cmd.Parameters.AddWithValue("@UserId", (object?)requestedByUserId ?? DBNull.Value);
                affected = await cmd.ExecuteNonQueryAsync();
            }

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return false;
            }

            await _auditLogger.LogDeleteAsync("applicant", applicantId.ToString(), new { applicantId }, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        public async Task<PagedResponse<PendingDeletionDto>> GetPagedAsync(int page, int pageSize, string? search, string? requestedBySearch = null, string? status = null, string? idSearch = null)
        {
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasRequestedBySearch = !string.IsNullOrWhiteSpace(requestedBySearch);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch);
            bool hasStatus = !string.IsNullOrWhiteSpace(status);

            string searchCondition = hasSearch
                ? "AND (LOWER(a.name) LIKE @Search OR LOWER(a.externalid) LIKE @Search)"
                : "";
            string requestedByCondition = hasRequestedBySearch
                ? "AND LOWER(u.username) LIKE @RequestedBySearch"
                : "";
            string idCondition = hasIdSearch
                ? "AND CAST(dr.applicant_id AS TEXT) LIKE @IdSearch"
                : "";
            string statusCondition = hasStatus
                ? "AND dr.status = @Status"
                : "AND dr.status IN ('pending', 'confirmed')";

            string countSql = $@"
                SELECT COUNT(*)
                FROM applicant_deletion_requests dr
                JOIN applicants a ON a.id = dr.applicant_id
                LEFT JOIN users u ON u.id = dr.requested_by_user_id
                WHERE 1=1
                {statusCondition}
                {searchCondition}
                {requestedByCondition}
                {idCondition}";

            string dataSql = $@"
                SELECT dr.applicant_id, a.name, a.externalid,
                       dr.requested_by_user_id, u.username,
                       dr.status
                FROM applicant_deletion_requests dr
                JOIN applicants a ON a.id = dr.applicant_id
                LEFT JOIN users u ON u.id = dr.requested_by_user_id
                WHERE 1=1
                {statusCondition}
                {searchCondition}
                {requestedByCondition}
                {idCondition}
                ORDER BY a.name ASC
                LIMIT @PageSize OFFSET @Offset";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            void BindParams(NpgsqlCommand cmd)
            {
                if (hasStatus) cmd.Parameters.AddWithValue("@Status", status!);
                if (hasSearch) cmd.Parameters.AddWithValue("@Search", $"%{search!.ToLower()}%");
                if (hasRequestedBySearch) cmd.Parameters.AddWithValue("@RequestedBySearch", $"%{requestedBySearch!.ToLower()}%");
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdSearch", $"%{idSearch}%");
            }

            int total;
            using (var cmd = new NpgsqlCommand(countSql, connection))
            {
                BindParams(cmd);
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var items = new List<PendingDeletionDto>();
            using (var cmd = new NpgsqlCommand(dataSql, connection))
            {
                BindParams(cmd);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new PendingDeletionDto
                    {
                        ApplicantId = reader.GetInt32(0),
                        ApplicantName = reader.GetString(1),
                        ApplicantExternalId = reader.IsDBNull(2) ? null : reader.GetString(2),
                        RequestedByUserId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        RequestedByUsername = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Status = reader.GetString(5)
                    });
                }
            }

            return new PagedResponse<PendingDeletionDto> { Items = items, Total = total };
        }

        public async Task<bool> ConfirmAsync(int applicantId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            const string sql = @"
                UPDATE applicant_deletion_requests
                SET status = 'confirmed'
                WHERE applicant_id = @ApplicantId AND status = 'pending'";

            int affected;
            using (var cmd = new NpgsqlCommand(sql, connection, (NpgsqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
                affected = await cmd.ExecuteNonQueryAsync();
            }

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return false;
            }

            await _auditLogger.LogDeleteConfirmedAsync(applicantId, (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        public async Task<bool> RejectAsync(int applicantId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            const string sql = @"
                UPDATE applicant_deletion_requests
                SET status = 'rejected'
                WHERE applicant_id = @ApplicantId AND status IN ('pending', 'confirmed')";

            int affected;
            using (var cmd = new NpgsqlCommand(sql, connection, (NpgsqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
                affected = await cmd.ExecuteNonQueryAsync();
            }

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return false;
            }

            await _auditLogger.ResetValidationIfNeededAsync(applicantId, (NpgsqlTransaction)tx);
            await _auditLogger.LogDeleteRejectedAsync(applicantId, (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }
    }
}
