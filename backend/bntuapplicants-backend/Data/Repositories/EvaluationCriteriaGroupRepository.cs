using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class EvaluationCriteriaGroupRepository : IEvaluationCriteriaGroupRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public EvaluationCriteriaGroupRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<EvaluationCriteriaGroup?> CreateAsync(EvaluationCriteriaGroup group)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO evaluationcriteriagroups (name)
                VALUES (@Name)
                RETURNING *
            ";

            EvaluationCriteriaGroup? created = null;
            using var tx = await connection.BeginTransactionAsync();

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", group.Name);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    created = new EvaluationCriteriaGroup
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    };
                }
            }

            if (created != null)
            {
                await _auditLogger.LogCreateAsync("evaluation_criteria_group", created.Id.ToString(), created, tx: (NpgsqlTransaction)tx);
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

            string query = "DELETE FROM evaluationcriteriagroups WHERE id = @Id";

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

            await _auditLogger.LogDeleteAsync("evaluation_criteria_group", id.ToString(), before, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        private static readonly Dictionary<string, string> SortColumns = new()
        {
            { "id", "id" },
            { "name", "name" }
        };

        private static string OrderBy(string? field, string? order)
        {
            string col = (field != null && SortColumns.TryGetValue(field, out var c)) ? c : "name";
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        public async Task<PagedResponse<EvaluationCriteriaGroup>> GetPagedAsync(int page, int pageSize, string? search, string? idSearch = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<EvaluationCriteriaGroup>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch) && int.TryParse(idSearch.Trim(), out _);

            var conditions = new List<string>();
            if (hasSearch) conditions.Add("LOWER(name) LIKE @SearchPattern");
            if (hasIdSearch) conditions.Add("CAST(id AS TEXT) LIKE @IdPattern");
            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM evaluationcriteriagroups {whereClause}", connection))
            {
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (var command = new NpgsqlCommand($"SELECT id, name FROM evaluationcriteriagroups {whereClause} {OrderBy(sortField, sortOrder)} LIMIT @PageSize OFFSET @Offset", connection))
            {
                if (hasSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) command.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new EvaluationCriteriaGroup { Id = reader.GetInt32(0), Name = reader.GetString(1) });
            }

            return new PagedResponse<EvaluationCriteriaGroup> { Items = items, Total = total };
        }

        public async Task<List<EvaluationCriteriaGroup>> GetAllAsync()
        {
            var records = new List<EvaluationCriteriaGroup>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT id, name FROM evaluationcriteriagroups";

                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new EvaluationCriteriaGroup
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<EvaluationCriteriaGroup?> GetByIdAsync(int id)
        {
            EvaluationCriteriaGroup? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = "SELECT id, name FROM evaluationcriteriagroups WHERE id = @Id";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new EvaluationCriteriaGroup
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<List<AdmissionCategoryPathDto>> GetAdmissionCategoriesUsingGroupAsync(int groupId)
        {
            var records = new List<AdmissionCategoryPathDto>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT
                        f.name  AS facultyName,
                        d.name  AS departmentName,
                        s.name  AS specialtyName,
                        cl.name AS competitionListName,
                        ac.name AS admissionCategoryName
                    FROM admissioncategories ac
                    INNER JOIN competitionlists cl ON cl.id = ac.competitionlistid
                    INNER JOIN specialties      s  ON s.id  = cl.specialtyid
                    INNER JOIN departments      d  ON d.id  = s.departmentid
                    INNER JOIN faculties        f  ON f.id  = d.facultyid
                    WHERE ac.evaluationcriteriagroupid = @GroupId
                    ORDER BY f.name, d.name, s.name, cl.name, ac.name
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GroupId", groupId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            records.Add(new AdmissionCategoryPathDto
                            {
                                FacultyName = reader.GetString(0),
                                DepartmentName = reader.GetString(1),
                                SpecialtyName = reader.GetString(2),
                                CompetitionListName = reader.GetString(3),
                                AdmissionCategoryName = reader.GetString(4)
                            });
                        }
                    }
                }
            }

            return records;
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = excludeId.HasValue
                ? "SELECT COUNT(*) FROM evaluationcriteriagroups WHERE LOWER(name) = LOWER(@Name) AND id != @ExcludeId"
                : "SELECT COUNT(*) FROM evaluationcriteriagroups WHERE LOWER(name) = LOWER(@Name)";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", name);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> UpdateAsync(EvaluationCriteriaGroup group)
        {
            var before = await GetByIdAsync(group.Id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = @"
                UPDATE evaluationcriteriagroups
                SET name = @Name
                WHERE id = @Id
            ";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", group.Name);
                command.Parameters.AddWithValue("@Id", group.Id);

                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            var diff = JsonDiff.Compute(before, new { group.Id, group.Name });
            await _auditLogger.LogUpdateAsync("evaluation_criteria_group", group.Id.ToString(), diff, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }
    }
}
