using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class CompetitionListRepository : ICompetitionListRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public CompetitionListRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<CompetitionList?> CreateAsync(CompetitionList competitionList)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO competitionlists (name, plan, specialtyid)
                VALUES (@Name, @Plan, @SpecialtyId)
                RETURNING *
            ";

            CompetitionList? created = null;
            using var tx = await connection.BeginTransactionAsync();

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", competitionList.Name);
                command.Parameters.AddWithValue("@Plan", competitionList.Plan);
                command.Parameters.AddWithValue("@SpecialtyId", competitionList.SpecialtyId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    created = new CompetitionList
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Plan = reader.GetInt32(2),
                        SpecialtyId = reader.GetInt32(3)
                    };
                }
            }

            if (created != null)
            {
                await _auditLogger.LogCreateAsync("competition_list", created.Id.ToString(), created, tx: (NpgsqlTransaction)tx);
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

            string query = "DELETE FROM competitionlists WHERE id = @Id";

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

            await _auditLogger.LogDeleteAsync("competition_list", id.ToString(), before, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        public async Task<List<CompetitionList>> GetAllAsync()
        {
            var records = new List<CompetitionList>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT id, name, plan, specialtyid FROM competitionlists";

                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new CompetitionList
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Plan = reader.GetInt32(2),
                            SpecialtyId = reader.GetInt32(3)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<List<CompetitionList>> GetBySpecialtyIdAsync(int specialtyId)
        {
            var records = new List<CompetitionList>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT id, name, plan, specialtyid FROM competitionlists WHERE specialtyid = @SpecialtyId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@SpecialtyId", specialtyId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new CompetitionList
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Plan = reader.GetInt32(2),
                    SpecialtyId = reader.GetInt32(3)
                });
            }

            return records;
        }

        private static readonly Dictionary<string, string> SortColumns = new()
        {
            { "id", "id" },
            { "name", "name" },
            { "plan", "plan" }
        };

        private static string OrderBy(string? field, string? order)
        {
            string col = (field != null && SortColumns.TryGetValue(field, out var c)) ? c : "name";
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        public async Task<PagedResponse<CompetitionList>> GetPagedBySpecialtyAsync(int specialtyId, int page, int pageSize, string? search, string? idSearch, string? planSearch, string? sortField, string? sortOrder)
        {
            var items = new List<CompetitionList>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch) && int.TryParse(idSearch.Trim(), out _);
            int planValue = 0;
            bool hasPlanSearch = !string.IsNullOrWhiteSpace(planSearch) && int.TryParse(planSearch.Trim(), out planValue);

            var extraConditions = new List<string>();
            if (hasSearch) extraConditions.Add("LOWER(name) LIKE @SearchPattern");
            if (hasIdSearch) extraConditions.Add("CAST(id AS TEXT) LIKE @IdPattern");
            if (hasPlanSearch) extraConditions.Add("plan = @PlanValue");
            string whereClause = extraConditions.Count > 0
                ? "WHERE specialtyid = @SpecialtyId AND " + string.Join(" AND ", extraConditions)
                : "WHERE specialtyid = @SpecialtyId";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            void AddParams(NpgsqlCommand cmd)
            {
                cmd.Parameters.AddWithValue("@SpecialtyId", specialtyId);
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                if (hasPlanSearch) cmd.Parameters.AddWithValue("@PlanValue", planValue);
            }

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM competitionlists {whereClause}", connection))
            {
                AddParams(cmd);
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (var command = new NpgsqlCommand($"SELECT id, name, plan, specialtyid FROM competitionlists {whereClause} {OrderBy(sortField, sortOrder)} LIMIT @PageSize OFFSET @Offset", connection))
            {
                AddParams(command);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new CompetitionList { Id = reader.GetInt32(0), Name = reader.GetString(1), Plan = reader.GetInt32(2), SpecialtyId = reader.GetInt32(3) });
            }

            return new PagedResponse<CompetitionList> { Items = items, Total = total };
        }

        public async Task<CompetitionList?> GetByIdAsync(int id)
        {
            CompetitionList? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = "SELECT id, name, plan, specialtyid FROM competitionlists WHERE id = @Id";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new CompetitionList
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Plan = reader.GetInt32(2),
                            SpecialtyId = reader.GetInt32(3)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<bool> ExistsByNameAsync(string name, int specialtyId, int? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = excludeId.HasValue
                ? "SELECT COUNT(*) FROM competitionlists WHERE LOWER(name) = LOWER(@Name) AND specialtyid = @SpecialtyId AND id != @ExcludeId"
                : "SELECT COUNT(*) FROM competitionlists WHERE LOWER(name) = LOWER(@Name) AND specialtyid = @SpecialtyId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@SpecialtyId", specialtyId);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> UpdateAsync(CompetitionList competitionList)
        {
            var before = await GetByIdAsync(competitionList.Id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = @"
                UPDATE competitionlists
                SET name = @Name, plan = @Plan, specialtyid = @SpecialtyId
                WHERE id = @Id
            ";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", competitionList.Name);
                command.Parameters.AddWithValue("@Plan", competitionList.Plan);
                command.Parameters.AddWithValue("@SpecialtyId", competitionList.SpecialtyId);
                command.Parameters.AddWithValue("@Id", competitionList.Id);

                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            var diff = JsonDiff.Compute(before, new { competitionList.Id, competitionList.Name, competitionList.Plan, competitionList.SpecialtyId });
            await _auditLogger.LogUpdateAsync("competition_list", competitionList.Id.ToString(), diff, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }
    }
}
