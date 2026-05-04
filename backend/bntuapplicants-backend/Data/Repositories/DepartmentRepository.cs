using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly string _connectionString;

        public DepartmentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<Department?> CreateAsync(Department department)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO departments (name, facultyid)
                    VALUES (@Name, @FacultyId)
                    RETURNING *
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", department.Name);
                    command.Parameters.AddWithValue("@FacultyId", department.FacultyId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Department
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                FacultyId = reader.GetInt32(2)
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

                string query = "DELETE FROM departments WHERE id = @Id";

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
            { "name", "d.name" },
            { "facultyId", "f.name" }
        };

        private static string OrderBy(string? field, string? order)
        {
            string col = (field != null && SortColumns.TryGetValue(field, out var c)) ? c : "d.name";
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        public async Task<PagedResponse<Department>> GetPagedAsync(int page, int pageSize, string? search, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Department>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            string whereClause = hasSearch ? "WHERE LOWER(name) LIKE @SearchPattern" : "";
            string whereClauseAliased = hasSearch ? "WHERE LOWER(d.name) LIKE @SearchPattern" : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM departments {whereClause}", connection))
            {
                if (hasSearch)
                    cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
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
                if (hasSearch)
                    command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
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

        public async Task<PagedResponse<Department>> GetPagedByFacultyIdAsync(int facultyId, int page, int pageSize, string? search, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Department>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            string searchClause = hasSearch ? "AND LOWER(d.name) LIKE @SearchPattern" : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM departments d WHERE d.facultyid = @FacultyId {searchClause}", connection))
            {
                cmd.Parameters.AddWithValue("@FacultyId", facultyId);
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT d.id, d.name, d.facultyid
                FROM departments d
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
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE departments
                    SET name = @Name, facultyid = @FacultyId
                    WHERE id = @Id
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", department.Name);
                    command.Parameters.AddWithValue("@FacultyId", department.FacultyId);
                    command.Parameters.AddWithValue("@Id", department.Id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }
    }
}
