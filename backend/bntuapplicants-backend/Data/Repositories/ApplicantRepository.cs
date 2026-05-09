using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class ApplicantRepository : IApplicantRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public ApplicantRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<Applicant?> CreateAsync(Applicant applicant)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO Applicants (Name, Notes, ExternalId)
                VALUES (@Name, @Notes, @ExternalId)
                RETURNING Id, Name, Notes, ExternalId
            ";

            Applicant? created = null;
            using var tx = await connection.BeginTransactionAsync();

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", applicant.Name);
                command.Parameters.AddWithValue("@Notes", (object?)applicant.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@ExternalId", (object?)applicant.ExternalId ?? DBNull.Value);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    created = new Applicant
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                        ExternalId = reader.GetString(3)
                    };
                }
            }

            if (created != null)
            {
                await _auditLogger.LogCreateAsync("applicant", created.Id.ToString(), created, tx: (NpgsqlTransaction)tx);
                await tx.CommitAsync();
                return created;
            }

            await tx.RollbackAsync();
            return null;
        }

        public async Task<(Applicant? Applicant, bool IsDeleted)> FindByExternalIdAsync(string externalId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT a.Id, a.Name, a.Notes, a.ExternalId,
                       EXISTS (SELECT 1 FROM applicant_deletion_requests dr
                               WHERE dr.applicant_id = a.id AND dr.status IN ('pending','confirmed')) AS is_deleted
                FROM Applicants a
                WHERE a.ExternalId = @ExternalId
                LIMIT 1
            ";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@ExternalId", externalId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var applicant = new Applicant
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ExternalId = reader.GetString(3)
                };
                return (applicant, reader.GetBoolean(4));
            }

            return (null, false);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var before = await GetByIdAsync(id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = "DELETE FROM Applicants WHERE Id = @Id";

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

            await _auditLogger.ResetValidationIfNeededAsync(id, (NpgsqlTransaction)tx);
            await _auditLogger.LogDeleteAsync("applicant", id.ToString(), before, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        private static readonly Dictionary<string, string> SortColumns = new()
        {
            { "id", "id" },
            { "name", "name" },
            { "externalId", "externalid" }
        };

        private static string OrderBy(string? field, string? order)
        {
            string col = (field != null && SortColumns.TryGetValue(field, out var c)) ? c : "name";
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        private const string NotSoftDeleted = @"NOT EXISTS (
                SELECT 1 FROM applicant_deletion_requests dr
                WHERE dr.applicant_id = Applicants.id AND dr.status IN ('pending','confirmed'))";

        public async Task<PagedResponse<Applicant>> GetPagedAsync(int page, int pageSize, string? search, string? externalIdSearch = null, string? idSearch = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Applicant>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasNameSearch = !string.IsNullOrWhiteSpace(search);
            bool hasExternalIdSearch = !string.IsNullOrWhiteSpace(externalIdSearch);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch) && int.TryParse(idSearch.Trim(), out _);

            var conditions = new List<string> { NotSoftDeleted };
            if (hasNameSearch) conditions.Add("LOWER(Name) LIKE @SearchPattern");
            if (hasExternalIdSearch) conditions.Add("LOWER(ExternalId) LIKE @ExternalIdPattern");
            if (hasIdSearch) conditions.Add("CAST(Id AS TEXT) LIKE @IdPattern");
            string whereClause = "WHERE " + string.Join(" AND ", conditions);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM Applicants {whereClause}", connection))
            {
                if (hasNameSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasExternalIdSearch) cmd.Parameters.AddWithValue("@ExternalIdPattern", $"%{externalIdSearch!.ToLower()}%");
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT Id, Name, Notes, ExternalId FROM Applicants
                {whereClause}
                {OrderBy(sortField, sortOrder)}
                LIMIT @PageSize OFFSET @Offset
            ";
            using (var command = new NpgsqlCommand(dataQuery, connection))
            {
                if (hasNameSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasExternalIdSearch) command.Parameters.AddWithValue("@ExternalIdPattern", $"%{externalIdSearch!.ToLower()}%");
                if (hasIdSearch) command.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new Applicant
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                        ExternalId = reader.GetString(3)
                    });
            }

            return new PagedResponse<Applicant> { Items = items, Total = total };
        }

        public async Task<List<Applicant>> GetAllAsync()
        {
            var records = new List<Applicant>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = $"SELECT Id, Name, Notes, ExternalId FROM Applicants WHERE {NotSoftDeleted}";
                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new Applicant
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ExternalId = reader.GetString(3)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<Applicant?> GetByIdAsync(int id)
        {
            Applicant? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = $@"
                    SELECT Id, Name, Notes, ExternalId
                    FROM Applicants
                    WHERE Id = @ApplicantId AND {NotSoftDeleted}
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ApplicantId", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new Applicant()
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ExternalId = reader.GetString(3)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<bool> UpdateAsync(Applicant applicant)
        {
            var before = await GetByIdAsync(applicant.Id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = @"
                UPDATE Applicants
                SET Name = @Name, Notes = @Notes, ExternalId = @ExternalId
                WHERE Id = @Id
            ";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", applicant.Name);
                command.Parameters.AddWithValue("@Notes", (object?)applicant.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@ExternalId", applicant.ExternalId);
                command.Parameters.AddWithValue("@Id", applicant.Id);

                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            var diff = JsonDiff.Compute(before, new { applicant.Id, applicant.Name, applicant.Notes, applicant.ExternalId });
            await _auditLogger.ResetValidationIfNeededAsync(applicant.Id, (NpgsqlTransaction)tx);
            await _auditLogger.LogUpdateAsync("applicant", applicant.Id.ToString(), diff, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }
    }
}
