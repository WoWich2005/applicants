using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class AdmissionCategoryRepository : IAdmissionCategoryRepository
    {
        private readonly string _connectionString;

        public AdmissionCategoryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<AdmissionCategory?> CreateAsync(AdmissionCategory category)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO admissioncategories (name, competitionlistid, evaluationcriteriagroupid, quota, priority)
                    VALUES (@Name, @CompetitionListId, @EvaluationCriteriaGroupId, @Quota, @Priority)
                    RETURNING *
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", category.Name);
                    command.Parameters.AddWithValue("@CompetitionListId", category.CompetitionListId);
                    command.Parameters.AddWithValue("@EvaluationCriteriaGroupId", category.EvaluationCriteriaGroupId);
                    command.Parameters.AddWithValue("@Quota", category.Quota);
                    command.Parameters.AddWithValue("@Priority", category.Priority);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new AdmissionCategory
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                CompetitionListId = reader.GetInt32(2),
                                EvaluationCriteriaGroupId = reader.GetInt32(3),
                                Quota = reader.GetInt32(4),
                                Priority = reader.GetInt32(5)
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

                string query = "DELETE FROM admissioncategories WHERE id = @Id";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }

        public async Task<List<AdmissionCategory>> GetAllAsync()
        {
            var records = new List<AdmissionCategory>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT id, name, competitionlistid, evaluationcriteriagroupid, quota, priority FROM admissioncategories";

                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new AdmissionCategory
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            CompetitionListId = reader.GetInt32(2),
                            EvaluationCriteriaGroupId = reader.GetInt32(3),
                            Quota = reader.GetInt32(4),
                            Priority = reader.GetInt32(5)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<List<AdmissionCategory>> GetAllByCompetitionListAsync(int competitionListId)
        {
            var records = new List<AdmissionCategory>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, name, competitionlistid, evaluationcriteriagroupid, quota, priority
                    FROM admissioncategories
                    WHERE competitionlistid = @CompetitionListId
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@CompetitionListId", competitionListId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new AdmissionCategory
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            CompetitionListId = reader.GetInt32(2),
                            EvaluationCriteriaGroupId = reader.GetInt32(3),
                            Quota = reader.GetInt32(4),
                            Priority = reader.GetInt32(5)
                        });
                    }
                }
            }

            return records;
        }

        private static string OrderByCategory(string? field, string? order)
        {
            string col = field switch
            {
                "name" => "ac.name",
                "quota" => "ac.quota",
                "evaluationCriteriaGroupId" => "ecg.name",
                _ => "ac.priority"
            };
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        public async Task<PagedResponse<AdmissionCategory>> GetPagedByCompetitionListAsync(
            int competitionListId, int page, int pageSize, string? search, int? groupId, string? sortField, string? sortOrder)
        {
            var items = new List<AdmissionCategory>();
            int total = 0;
            int offset = (page - 1) * pageSize;

            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasGroupFilter = groupId.HasValue;

            var conditions = new List<string> { "ac.competitionlistid = @CompetitionListId" };
            if (hasSearch) conditions.Add("LOWER(ac.name) LIKE @SearchPattern");
            if (hasGroupFilter) conditions.Add("ac.evaluationcriteriagroupid = @GroupId");
            string whereClause = "WHERE " + string.Join(" AND ", conditions);

            const string baseFrom = @"
                FROM admissioncategories ac
                LEFT JOIN evaluationcriteriagroups ecg ON ecg.id = ac.evaluationcriteriagroupid";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) {baseFrom} {whereClause}", connection))
            {
                cmd.Parameters.AddWithValue("@CompetitionListId", competitionListId);
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasGroupFilter) cmd.Parameters.AddWithValue("@GroupId", groupId!.Value);
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT ac.id, ac.name, ac.competitionlistid, ac.evaluationcriteriagroupid, ac.quota, ac.priority
                {baseFrom} {whereClause} {OrderByCategory(sortField, sortOrder)} LIMIT @PageSize OFFSET @Offset";

            using (var command = new NpgsqlCommand(dataQuery, connection))
            {
                command.Parameters.AddWithValue("@CompetitionListId", competitionListId);
                if (hasSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasGroupFilter) command.Parameters.AddWithValue("@GroupId", groupId!.Value);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new AdmissionCategory
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        CompetitionListId = reader.GetInt32(2),
                        EvaluationCriteriaGroupId = reader.GetInt32(3),
                        Quota = reader.GetInt32(4),
                        Priority = reader.GetInt32(5)
                    });
            }

            return new PagedResponse<AdmissionCategory> { Items = items, Total = total };
        }

        public async Task<List<AdmissionCategory>> GetAllBySpecialtyIdAsync(int specialtyId)
        {
            var records = new List<AdmissionCategory>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT ac.id, ac.name, ac.competitionlistid, ac.evaluationcriteriagroupid, ac.quota, ac.priority
                FROM admissioncategories ac
                JOIN competitionlists cl ON ac.competitionlistid = cl.id
                WHERE cl.specialtyid = @SpecialtyId
            ";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@SpecialtyId", specialtyId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new AdmissionCategory
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CompetitionListId = reader.GetInt32(2),
                    EvaluationCriteriaGroupId = reader.GetInt32(3),
                    Quota = reader.GetInt32(4),
                    Priority = reader.GetInt32(5)
                });
            }

            return records;
        }

        public async Task<AdmissionCategory?> GetByIdAsync(int id)
        {
            AdmissionCategory? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, name, competitionlistid, evaluationcriteriagroupid, quota, priority
                    FROM admissioncategories
                    WHERE id = @Id
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new AdmissionCategory
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            CompetitionListId = reader.GetInt32(2),
                            EvaluationCriteriaGroupId = reader.GetInt32(3),
                            Quota = reader.GetInt32(4),
                            Priority = reader.GetInt32(5)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<bool> ExistsByNameAsync(string name, int competitionListId, int? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = excludeId.HasValue
                ? "SELECT COUNT(*) FROM admissioncategories WHERE LOWER(name) = LOWER(@Name) AND competitionlistid = @CompetitionListId AND id != @ExcludeId"
                : "SELECT COUNT(*) FROM admissioncategories WHERE LOWER(name) = LOWER(@Name) AND competitionlistid = @CompetitionListId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@CompetitionListId", competitionListId);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> UpdateAsync(AdmissionCategory category)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE admissioncategories
                    SET name = @Name,
                        competitionlistid = @CompetitionListId,
                        evaluationcriteriagroupid = @EvaluationCriteriaGroupId,
                        quota = @Quota,
                        priority = @Priority
                    WHERE id = @Id
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", category.Name);
                    command.Parameters.AddWithValue("@CompetitionListId", category.CompetitionListId);
                    command.Parameters.AddWithValue("@EvaluationCriteriaGroupId", category.EvaluationCriteriaGroupId);
                    command.Parameters.AddWithValue("@Quota", category.Quota);
                    command.Parameters.AddWithValue("@Priority", category.Priority);
                    command.Parameters.AddWithValue("@Id", category.Id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }
    }
}
