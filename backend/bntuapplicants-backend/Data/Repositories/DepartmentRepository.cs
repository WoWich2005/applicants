using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public DepartmentRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<Department?> CreateAsync(Department department)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO departments (name, facultyid)
                VALUES (@Name, @FacultyId)
                RETURNING *
            ";

            Department? created = null;
            using var tx = await connection.BeginTransactionAsync();

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", department.Name);
                command.Parameters.AddWithValue("@FacultyId", department.FacultyId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    created = new Department
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        FacultyId = reader.GetInt32(2)
                    };
                }
            }

            if (created != null)
            {
                await _auditLogger.LogCreateAsync("department", created.Id.ToString(), created, tx: (NpgsqlTransaction)tx);
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

            string query = "DELETE FROM departments WHERE id = @Id";

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

            await _auditLogger.LogDeleteAsync("department", id.ToString(), before, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        private static readonly Dictionary<string, string> SortColumns = new()
        {
            { "id", "d.id" },
            { "name", "d.name" },
            { "facultyId", "f.name" }
        };

        private static string OrderBy(string? field, string? order)
        {
            string col = (field != null && SortColumns.TryGetValue(field, out var c)) ? c : "d.name";
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        public async Task<PagedResponse<Department>> GetPagedAsync(int page, int pageSize, string? search, string? idSearch = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Department>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch) && int.TryParse(idSearch.Trim(), out _);

            var conditions = new List<string>();
            if (hasSearch) conditions.Add("LOWER(name) LIKE @SearchPattern");
            if (hasIdSearch) conditions.Add("CAST(id AS TEXT) LIKE @IdPattern");
            var conditionsAliased = new List<string>();
            if (hasSearch) conditionsAliased.Add("LOWER(d.name) LIKE @SearchPattern");
            if (hasIdSearch) conditionsAliased.Add("CAST(d.id AS TEXT) LIKE @IdPattern");
            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            string whereClauseAliased = conditionsAliased.Count > 0 ? "WHERE " + string.Join(" AND ", conditionsAliased) : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM departments {whereClause}", connection))
            {
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT d.id, d.name, d.facultyid
                FROM departments d
                LEFT JOIN faculties f ON f.id = d.facultyid
                {whereClauseAliased}
                {OrderBy(sortField, sortOrder)}
                LIMIT @PageSize OFFSET @Offset
            ";
            using (var command = new NpgsqlCommand(dataQuery, connection))
            {
                if (hasSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) command.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new Department { Id = reader.GetInt32(0), Name = reader.GetString(1), FacultyId = reader.GetInt32(2) });
            }

            return new PagedResponse<Department> { Items = items, Total = total };
        }

        public async Task<List<Department>> GetAllAsync()
        {
            var records = new List<Department>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT id, name, facultyid FROM departments";

                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new Department
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            FacultyId = reader.GetInt32(2)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<PagedResponse<Department>> GetPagedByFacultyIdAsync(int facultyId, int page, int pageSize, string? search, string? idSearch = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Department>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch) && int.TryParse(idSearch.Trim(), out _);
            string searchClause = hasSearch ? "AND LOWER(d.name) LIKE @SearchPattern" : "";
            string idClause = hasIdSearch ? "AND CAST(d.id AS TEXT) LIKE @IdPattern" : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM departments d WHERE d.facultyid = @FacultyId {searchClause} {idClause}", connection))
            {
                cmd.Parameters.AddWithValue("@FacultyId", facultyId);
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT d.id, d.name, d.facultyid
                FROM departments d
                LEFT JOIN faculties f ON f.id = d.facultyid
                WHERE d.facultyid = @FacultyId {searchClause} {idClause}
                {OrderBy(sortField, sortOrder)}
                LIMIT @PageSize OFFSET @Offset";

            using (var command = new NpgsqlCommand(dataQuery, connection))
            {
                command.Parameters.AddWithValue("@FacultyId", facultyId);
                if (hasSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) command.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new Department { Id = reader.GetInt32(0), Name = reader.GetString(1), FacultyId = reader.GetInt32(2) });
            }

            return new PagedResponse<Department> { Items = items, Total = total };
        }

        public async Task<List<Department>> GetByFacultyIdAsync(int facultyId)
        {
            var records = new List<Department>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT id, name, facultyid FROM departments WHERE facultyid = @FacultyId";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FacultyId", facultyId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            records.Add(new Department
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                FacultyId = reader.GetInt32(2)
                            });
                        }
                    }
                }
            }

            return records;
        }

        public async Task<Department?> GetByIdAsync(int id)
        {
            Department? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = "SELECT id, name, facultyid FROM departments WHERE id = @Id";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new Department
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            FacultyId = reader.GetInt32(2)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<bool> ExistsByNameAsync(string name, int facultyId, int? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = excludeId.HasValue
                ? "SELECT COUNT(*) FROM departments WHERE LOWER(name) = LOWER(@Name) AND facultyid = @FacultyId AND id != @ExcludeId"
                : "SELECT COUNT(*) FROM departments WHERE LOWER(name) = LOWER(@Name) AND facultyid = @FacultyId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@FacultyId", facultyId);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> UpdateAsync(Department department)
        {
            var before = await GetByIdAsync(department.Id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = @"
                UPDATE departments
                SET name = @Name, facultyid = @FacultyId
                WHERE id = @Id
            ";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", department.Name);
                command.Parameters.AddWithValue("@FacultyId", department.FacultyId);
                command.Parameters.AddWithValue("@Id", department.Id);

                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            var diff = JsonDiff.Compute(before, new { department.Id, department.Name, department.FacultyId });
            await _auditLogger.LogUpdateAsync("department", department.Id.ToString(), diff, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }
    }
}
