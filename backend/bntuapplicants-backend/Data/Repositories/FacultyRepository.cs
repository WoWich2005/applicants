using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class FacultyRepository : IFacultyRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public FacultyRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<Faculty?> CreateAsync(Faculty faculty)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO Faculties (Name)
                VALUES (@Name)
                RETURNING *
            ";

            Faculty? created = null;
            using var tx = await connection.BeginTransactionAsync();

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", faculty.Name);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    created = new Faculty
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    };
                }
            }

            if (created != null)
            {
                await _auditLogger.LogCreateAsync("faculty", created.Id.ToString(), created, tx: (NpgsqlTransaction)tx);
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

            string query = "DELETE FROM Faculties WHERE Id = @Id";

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

            await _auditLogger.LogDeleteAsync("faculty", id.ToString(), before, tx: (NpgsqlTransaction)tx);
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

        public async Task<PagedResponse<Faculty>> GetPagedAsync(int page, int pageSize, string? search, string? idSearch = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Faculty>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch) && int.TryParse(idSearch.Trim(), out _);

            var conditions = new List<string>();
            if (hasSearch) conditions.Add("LOWER(Name) LIKE @SearchPattern");
            if (hasIdSearch) conditions.Add("CAST(Id AS TEXT) LIKE @IdPattern");
            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM Faculties {whereClause}", connection))
            {
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (var command = new NpgsqlCommand($"SELECT Id, Name FROM Faculties {whereClause} {OrderBy(sortField, sortOrder)} LIMIT @PageSize OFFSET @Offset", connection))
            {
                if (hasSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) command.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new Faculty { Id = reader.GetInt32(0), Name = reader.GetString(1) });
            }

            return new PagedResponse<Faculty> { Items = items, Total = total };
        }

        public async Task<List<Faculty>> GetAllAsync()
        {
            var records = new List<Faculty>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT Id, Name FROM Faculties";
                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new Faculty
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<Faculty?> GetByIdAsync(int id)
        {
            Faculty? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT Id, Name
                    FROM Faculties
                    WHERE Id = @FacultyId
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FacultyId", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            record = new Faculty()
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            };
                        }
                    }
                }
            }

            return record;
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = excludeId.HasValue
                ? "SELECT COUNT(*) FROM Faculties WHERE LOWER(Name) = LOWER(@Name) AND Id != @ExcludeId"
                : "SELECT COUNT(*) FROM Faculties WHERE LOWER(Name) = LOWER(@Name)";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", name);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> UpdateAsync(Faculty faculty)
        {
            var before = await GetByIdAsync(faculty.Id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = @"
                UPDATE Faculties
                SET Name = @Name
                WHERE Id = @Id
            ";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", faculty.Name);
                command.Parameters.AddWithValue("@Id", faculty.Id);

                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            var diff = JsonDiff.Compute(before, new { faculty.Id, faculty.Name });
            await _auditLogger.LogUpdateAsync("faculty", faculty.Id.ToString(), diff, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }
    }
}
