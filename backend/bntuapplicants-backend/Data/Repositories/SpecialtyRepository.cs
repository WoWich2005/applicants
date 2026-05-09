using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class SpecialtyRepository : ISpecialtyRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public SpecialtyRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<Specialty?> CreateAsync(Specialty specialty)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO Specialties (Name, DepartmentId)
                VALUES (@Name, @DepartmentId)
                RETURNING *
            ";

            Specialty? created = null;
            using var tx = await connection.BeginTransactionAsync();

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", specialty.Name);
                command.Parameters.AddWithValue("@DepartmentId", specialty.DepartmentId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    created = new Specialty
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        DepartmentId = reader.GetInt32(2)
                    };
                }
            }

            if (created != null)
            {
                await _auditLogger.LogCreateAsync("specialty", created.Id.ToString(), created, tx: (NpgsqlTransaction)tx);
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

            string query = "DELETE FROM Specialties WHERE Id = @Id";

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

            await _auditLogger.LogDeleteAsync("specialty", id.ToString(), before, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        private static readonly Dictionary<string, string> SortColumns = new()
        {
            { "id", "s.id" },
            { "name", "s.name" },
            { "departmentId", "d.name" },
            { "facultyId", "f.name" }
        };

        private static string OrderBy(string? field, string? order)
        {
            string col = (field != null && SortColumns.TryGetValue(field, out var c)) ? c : "s.name";
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        public async Task<PagedResponse<Specialty>> GetPagedAsync(int page, int pageSize, string? search, string? idSearch = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Specialty>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch) && int.TryParse(idSearch.Trim(), out _);

            var conditions = new List<string>();
            if (hasSearch) conditions.Add("LOWER(Name) LIKE @SearchPattern");
            if (hasIdSearch) conditions.Add("CAST(Id AS TEXT) LIKE @IdPattern");
            var conditionsAliased = new List<string>();
            if (hasSearch) conditionsAliased.Add("LOWER(s.name) LIKE @SearchPattern");
            if (hasIdSearch) conditionsAliased.Add("CAST(s.id AS TEXT) LIKE @IdPattern");
            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            string whereClauseAliased = conditionsAliased.Count > 0 ? "WHERE " + string.Join(" AND ", conditionsAliased) : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM Specialties {whereClause}", connection))
            {
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT s.id, s.name, s.departmentid
                FROM specialties s
                LEFT JOIN departments d ON d.id = s.departmentid
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
                    items.Add(new Specialty { Id = reader.GetInt32(0), Name = reader.GetString(1), DepartmentId = reader.GetInt32(2) });
            }

            return new PagedResponse<Specialty> { Items = items, Total = total };
        }

        public async Task<List<Specialty>> GetAllAsync()
        {
            var records = new List<Specialty>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT Id, Name, DepartmentId FROM Specialties";
                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new Specialty
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            DepartmentId = reader.GetInt32(2)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<Specialty?> GetByIdAsync(int id)
        {
            Specialty? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT Id, Name, DepartmentId
                    FROM Specialties
                    WHERE Id = @SpecialtyId
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@SpecialtyId", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new Specialty
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            DepartmentId = reader.GetInt32(2)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<bool> ExistsByNameAsync(string name, int departmentId, int? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = excludeId.HasValue
                ? "SELECT COUNT(*) FROM Specialties WHERE LOWER(Name) = LOWER(@Name) AND DepartmentId = @DepartmentId AND Id != @ExcludeId"
                : "SELECT COUNT(*) FROM Specialties WHERE LOWER(Name) = LOWER(@Name) AND DepartmentId = @DepartmentId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@DepartmentId", departmentId);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> UpdateAsync(Specialty specialty)
        {
            var before = await GetByIdAsync(specialty.Id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = @"
                UPDATE Specialties
                SET Name = @Name,
                    DepartmentId = @DepartmentId
                WHERE Id = @Id
            ";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Name", specialty.Name);
                command.Parameters.AddWithValue("@DepartmentId", specialty.DepartmentId);
                command.Parameters.AddWithValue("@Id", specialty.Id);

                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            var diff = JsonDiff.Compute(before, new { specialty.Id, specialty.Name, specialty.DepartmentId });
            await _auditLogger.LogUpdateAsync("specialty", specialty.Id.ToString(), diff, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        public async Task<Dictionary<int, Specialty>> GetSpecialtiesDictionaryAsync()
        {
            var specialties = await GetAllAsync();
            return specialties.ToDictionary(s => s.Id, s => s);
        }

        public async Task<List<Specialty>> GetByDepartmentIdAsync(int departmentId)
        {
            var records = new List<Specialty>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT Id, Name, DepartmentId FROM Specialties WHERE DepartmentId = @DepartmentId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@DepartmentId", departmentId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new Specialty
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DepartmentId = reader.GetInt32(2)
                });
            }

            return records;
        }

        public async Task<PagedResponse<Specialty>> GetPagedByFacultyIdAsync(int facultyId, int page, int pageSize, string? search, string? idSearch = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Specialty>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch) && int.TryParse(idSearch.Trim(), out _);
            string searchClause = hasSearch ? "AND LOWER(s.name) LIKE @SearchPattern" : "";
            string idClause = hasIdSearch ? "AND CAST(s.id AS TEXT) LIKE @IdPattern" : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($@"
                SELECT COUNT(*) FROM specialties s
                JOIN departments d ON s.departmentid = d.id
                WHERE d.facultyid = @FacultyId {searchClause} {idClause}", connection))
            {
                cmd.Parameters.AddWithValue("@FacultyId", facultyId);
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT s.id, s.name, s.departmentid
                FROM specialties s
                JOIN departments d ON d.id = s.departmentid
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
                    items.Add(new Specialty { Id = reader.GetInt32(0), Name = reader.GetString(1), DepartmentId = reader.GetInt32(2) });
            }

            return new PagedResponse<Specialty> { Items = items, Total = total };
        }

        public async Task<PagedResponse<Specialty>> GetPagedByDepartmentIdAsync(int departmentId, int page, int pageSize, string? search, string? idSearch = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Specialty>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch) && int.TryParse(idSearch.Trim(), out _);
            string searchClause = hasSearch ? "AND LOWER(s.name) LIKE @SearchPattern" : "";
            string idClause = hasIdSearch ? "AND CAST(s.id AS TEXT) LIKE @IdPattern" : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM specialties s WHERE s.departmentid = @DepartmentId {searchClause} {idClause}", connection))
            {
                cmd.Parameters.AddWithValue("@DepartmentId", departmentId);
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT s.id, s.name, s.departmentid
                FROM specialties s
                LEFT JOIN departments d ON d.id = s.departmentid
                LEFT JOIN faculties f ON f.id = d.facultyid
                WHERE s.departmentid = @DepartmentId {searchClause} {idClause}
                {OrderBy(sortField, sortOrder)}
                LIMIT @PageSize OFFSET @Offset";

            using (var command = new NpgsqlCommand(dataQuery, connection))
            {
                command.Parameters.AddWithValue("@DepartmentId", departmentId);
                if (hasSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasIdSearch) command.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new Specialty { Id = reader.GetInt32(0), Name = reader.GetString(1), DepartmentId = reader.GetInt32(2) });
            }

            return new PagedResponse<Specialty> { Items = items, Total = total };
        }

        public async Task<List<Specialty>> GetByFacultyIdAsync(int facultyId)
        {
            var records = new List<Specialty>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT s.id, s.name, s.departmentid
                FROM specialties s
                JOIN departments d ON s.departmentid = d.id
                WHERE d.facultyid = @FacultyId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@FacultyId", facultyId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new Specialty
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DepartmentId = reader.GetInt32(2)
                });
            }

            return records;
        }
    }
}
