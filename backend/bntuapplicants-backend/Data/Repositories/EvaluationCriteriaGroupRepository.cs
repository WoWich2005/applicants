using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class EvaluationCriteriaGroupRepository : IEvaluationCriteriaGroupRepository
    {
        private readonly string _connectionString;

        public EvaluationCriteriaGroupRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<EvaluationCriteriaGroup?> CreateAsync(EvaluationCriteriaGroup group)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO evaluationcriteriagroups (name)
                    VALUES (@Name)
                    RETURNING *
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", group.Name);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new EvaluationCriteriaGroup
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
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

                string query = "DELETE FROM evaluationcriteriagroups WHERE id = @Id";

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
            { "name", "name" }
        };

        private static string OrderBy(string? field, string? order)
        {
            string col = (field != null && SortColumns.TryGetValue(field, out var c)) ? c : "name";
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        public async Task<PagedResponse<EvaluationCriteriaGroup>> GetPagedAsync(int page, int pageSize, string? search, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<EvaluationCriteriaGroup>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            string whereClause = hasSearch ? "WHERE LOWER(name) LIKE @SearchPattern" : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM evaluationcriteriagroups {whereClause}", connection))
            {
                if (hasSearch)
                    cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (var command = new NpgsqlCommand($"SELECT id, name FROM evaluationcriteriagroups {whereClause} {OrderBy(sortField, sortOrder)} LIMIT @PageSize OFFSET @Offset", connection))
            {
                if (hasSearch)
                    command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
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
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE evaluationcriteriagroups
                    SET name = @Name
                    WHERE id = @Id
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", group.Name);
                    command.Parameters.AddWithValue("@Id", group.Id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }
    }
}
