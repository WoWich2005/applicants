using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class SpecialtyRepository : ISpecialtyRepository
    {
        private readonly string _connectionString;

        public SpecialtyRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<Specialty?> CreateAsync(Specialty specialty)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO Specialties (Name, DepartmentId)
                    VALUES (@Name, @DepartmentId)
                    RETURNING *
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", specialty.Name);
                    command.Parameters.AddWithValue("@DepartmentId", specialty.DepartmentId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Specialty
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                DepartmentId = reader.GetInt32(2)
                            };
                        }
                    }
                }
            }

            return null;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "DELETE FROM Specialties WHERE Id = @Id";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }

        private static readonly Dictionary<string, string> SortColumns = new()
        {
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

        public async Task<PagedResponse<Specialty>> GetPagedAsync(int page, int pageSize, string? search, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Specialty>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            string whereClause = hasSearch ? "WHERE LOWER(Name) LIKE @SearchPattern" : "";
            string whereClauseAliased = hasSearch ? "WHERE LOWER(s.name) LIKE @SearchPattern" : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM Specialties {whereClause}", connection))
            {
                if (hasSearch)
                    cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
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
                if (hasSearch)
                    command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
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
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE Specialties
                    SET Name = @Name,
                        DepartmentId = @DepartmentId
                    WHERE Id = @Id
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", specialty.Name);
                    command.Parameters.AddWithValue("@DepartmentId", specialty.DepartmentId);
                    command.Parameters.AddWithValue("@Id", specialty.Id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
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

        public async Task<PagedResponse<Specialty>> GetPagedByFacultyIdAsync(int facultyId, int page, int pageSize, string? search, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Specialty>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            string searchClause = hasSearch ? "AND LOWER(s.name) LIKE @SearchPattern" : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($@"
                SELECT COUNT(*) FROM specialties s
                JOIN departments d ON s.departmentid = d.id
                WHERE d.facultyid = @FacultyId {searchClause}", connection))
            {
                cmd.Parameters.AddWithValue("@FacultyId", facultyId);
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT s.id, s.name, s.departmentid
                FROM specialties s
                JOIN departments d ON d.id = s.departmentid
                LEFT JOIN faculties f ON f.id = d.facultyid
                WHERE d.facultyid = @FacultyId {searchClause}
                {OrderBy(sortField, sortOrder)}
                LIMIT @PageSize OFFSET @Offset";

            using (var command = new NpgsqlCommand(dataQuery, connection))
            {
                command.Parameters.AddWithValue("@FacultyId", facultyId);
                if (hasSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new Specialty { Id = reader.GetInt32(0), Name = reader.GetString(1), DepartmentId = reader.GetInt32(2) });
            }

            return new PagedResponse<Specialty> { Items = items, Total = total };
        }

        public async Task<PagedResponse<Specialty>> GetPagedByDepartmentIdAsync(int departmentId, int page, int pageSize, string? search, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Specialty>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            string searchClause = hasSearch ? "AND LOWER(s.name) LIKE @SearchPattern" : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM specialties s WHERE s.departmentid = @DepartmentId {searchClause}", connection))
            {
                cmd.Parameters.AddWithValue("@DepartmentId", departmentId);
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT s.id, s.name, s.departmentid
                FROM specialties s
                LEFT JOIN departments d ON d.id = s.departmentid
                LEFT JOIN faculties f ON f.id = d.facultyid
                WHERE s.departmentid = @DepartmentId {searchClause}
                {OrderBy(sortField, sortOrder)}
                LIMIT @PageSize OFFSET @Offset";

            using (var command = new NpgsqlCommand(dataQuery, connection))
            {
                command.Parameters.AddWithValue("@DepartmentId", departmentId);
                if (hasSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
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
