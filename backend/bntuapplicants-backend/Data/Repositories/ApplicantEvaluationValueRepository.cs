using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class ApplicantEvaluationValueRepository : IApplicantEvaluationValueRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public ApplicantEvaluationValueRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<ApplicantEvaluationValue?> CreateAsync(ApplicantEvaluationValue record)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO applicantevaluationvalues (applicantid, evaluationcriteriaid, value)
                VALUES (@ApplicantId, @EvaluationCriteriaId, @Value)
                RETURNING *
            ";

            ApplicantEvaluationValue? created = null;
            using var tx = await connection.BeginTransactionAsync();

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@ApplicantId", record.ApplicantId);
                command.Parameters.AddWithValue("@EvaluationCriteriaId", record.EvaluationCriteriaId);
                command.Parameters.AddWithValue("@Value", record.Value);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    created = new ApplicantEvaluationValue
                    {
                        Id = reader.GetInt32(0),
                        ApplicantId = reader.GetInt32(1),
                        EvaluationCriteriaId = reader.GetInt32(2),
                        Value = reader.GetInt32(3)
                    };
                }
            }

            if (created != null)
            {
                await _auditLogger.ResetValidationIfNeededAsync(created.ApplicantId, (NpgsqlTransaction)tx);
                await _auditLogger.LogCreateAsync(
                    "applicant_evaluation_value",
                    created.Id.ToString(),
                    created,
                    tx: (NpgsqlTransaction)tx);
                await tx.CommitAsync();
                return created;
            }

            await tx.RollbackAsync();
            return null;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var before = await GetByIdAsync(id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = "DELETE FROM applicantevaluationvalues WHERE id = @Id";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Id", id);
                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            await _auditLogger.ResetValidationIfNeededAsync(before.ApplicantId, (NpgsqlTransaction)tx);
            await _auditLogger.LogDeleteAsync(
                "applicant_evaluation_value",
                id.ToString(),
                before,
                tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        public async Task<PagedResponse<ApplicantEvaluationValueDto>> GetAllByApplicantPagedAsync(
            int applicantId, int page, int pageSize,
            string? sortField = null, string? sortOrder = null,
            string? id = null, string? criteria = null, string? value = null)
        {
            int offset = (page - 1) * pageSize;

            var filterParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(id) && int.TryParse(id, out int idValue)) filterParts.Add("AND aev.id = @IdValue");
            if (!string.IsNullOrWhiteSpace(criteria)) filterParts.Add("AND LOWER(ec.name) LIKE @CriteriaPattern");
            if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out int valueFilter)) filterParts.Add("AND aev.value = @ValueFilter");
            string whereFilters = string.Join(" ", filterParts);

            string orderByColumn = sortField switch
            {
                "id" => "aev.id",
                "criteriaName" => "ec.name",
                "value" => "aev.value",
                _ => "aev.id"
            };
            string direction = sortOrder == "descend" ? "DESC" : "ASC";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var countSql = $@"
                SELECT COUNT(*) FROM applicantevaluationvalues aev
                INNER JOIN evaluationcriteria ec ON ec.id = aev.evaluationcriteriaid
                WHERE aev.applicantid = @ApplicantId {whereFilters}";

            int total;
            using (var countCmd = new NpgsqlCommand(countSql, conn))
            {
                countCmd.Parameters.AddWithValue("@ApplicantId", applicantId);
                AddEvaluationValueFilterParams(countCmd, id, criteria, value);
                total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            var selectSql = $@"
                SELECT aev.id, aev.applicantid, aev.evaluationcriteriaid, aev.value, ec.name
                FROM applicantevaluationvalues aev
                INNER JOIN evaluationcriteria ec ON ec.id = aev.evaluationcriteriaid
                WHERE aev.applicantid = @ApplicantId {whereFilters}
                ORDER BY {orderByColumn} {direction}
                LIMIT @PageSize OFFSET @Offset";

            var items = new List<ApplicantEvaluationValueDto>();
            using (var cmd = new NpgsqlCommand(selectSql, conn))
            {
                cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
                AddEvaluationValueFilterParams(cmd, id, criteria, value);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new ApplicantEvaluationValueDto
                    {
                        Id = reader.GetInt32(0),
                        ApplicantId = reader.GetInt32(1),
                        EvaluationCriteriaId = reader.GetInt32(2),
                        Value = reader.GetInt32(3),
                        CriteriaName = reader.GetString(4),
                    });
                }
            }

            return new PagedResponse<ApplicantEvaluationValueDto> { Items = items, Total = total };
        }

        private static void AddEvaluationValueFilterParams(NpgsqlCommand cmd, string? id, string? criteria, string? value)
        {
            if (!string.IsNullOrWhiteSpace(id) && int.TryParse(id, out int idValue)) cmd.Parameters.AddWithValue("@IdValue", idValue);
            if (!string.IsNullOrWhiteSpace(criteria)) cmd.Parameters.AddWithValue("@CriteriaPattern", $"%{criteria.ToLower()}%");
            if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out int valueFilter)) cmd.Parameters.AddWithValue("@ValueFilter", valueFilter);
        }

        public async Task<ApplicantEvaluationValue?> GetByIdAsync(int id)
        {
            ApplicantEvaluationValue? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, applicantid, evaluationcriteriaid, value
                    FROM applicantevaluationvalues
                    WHERE id = @Id
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new ApplicantEvaluationValue
                        {
                            Id = reader.GetInt32(0),
                            ApplicantId = reader.GetInt32(1),
                            EvaluationCriteriaId = reader.GetInt32(2),
                            Value = reader.GetInt32(3)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<bool> UpdateAsync(ApplicantEvaluationValue record)
        {
            var before = await GetByIdAsync(record.Id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = @"
                UPDATE applicantevaluationvalues
                SET applicantid = @ApplicantId,
                    evaluationcriteriaid = @EvaluationCriteriaId,
                    value = @Value
                WHERE id = @Id
            ";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@ApplicantId", record.ApplicantId);
                command.Parameters.AddWithValue("@EvaluationCriteriaId", record.EvaluationCriteriaId);
                command.Parameters.AddWithValue("@Value", record.Value);
                command.Parameters.AddWithValue("@Id", record.Id);

                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            var diff = JsonDiff.Compute(before, new { record.Id, record.ApplicantId, record.EvaluationCriteriaId, record.Value });
            await _auditLogger.ResetValidationIfNeededAsync(record.ApplicantId, (NpgsqlTransaction)tx);
            await _auditLogger.LogUpdateAsync(
                "applicant_evaluation_value",
                record.Id.ToString(),
                diff,
                tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }
    }
}
