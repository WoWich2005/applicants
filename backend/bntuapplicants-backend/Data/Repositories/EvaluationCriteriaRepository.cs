using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class EvaluationCriteriaRepository : IEvaluationCriteriaRepository
    {
        private readonly string _connectionString;

        public EvaluationCriteriaRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        private static CriteriaType ParseType(string value) => value switch
        {
            "lower_is_better" => CriteriaType.LowerIsBetter,
            _ => CriteriaType.HigherIsBetter
        };

        private static string FormatType(CriteriaType type) => type switch
        {
            CriteriaType.LowerIsBetter => "lower_is_better",
            _ => "higher_is_better"
        };

        public async Task<EvaluationCriteria?> CreateAsync(EvaluationCriteria evaluationCriteria)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO evaluationcriteria (name, minvalue, maxvalue, type)
                    VALUES (@Name, @MinValue, @MaxValue, @Type::criteria_type)
                    RETURNING id, name, minvalue, maxvalue, type::text
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", evaluationCriteria.Name);
                    command.Parameters.AddWithValue("@MinValue", evaluationCriteria.MinValue);
                    command.Parameters.AddWithValue("@MaxValue", evaluationCriteria.MaxValue);
                    command.Parameters.AddWithValue("@Type", FormatType(evaluationCriteria.Type));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new EvaluationCriteria
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                MinValue = reader.GetInt32(2),
                                MaxValue = reader.GetInt32(3),
                                Type = ParseType(reader.GetString(4))
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

                string query = "DELETE FROM evaluationcriteria WHERE id = @Id";

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
            { "name", "name" },
            { "minValue", "minvalue" },
            { "maxValue", "maxvalue" }
        };

        private static string OrderBy(string? field, string? order)
        {
            string col = (field != null && SortColumns.TryGetValue(field, out var c)) ? c : "name";
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        public async Task<PagedResponse<EvaluationCriteria>> GetPagedAsync(int page, int pageSize, string? search, string? type = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<EvaluationCriteria>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasType = !string.IsNullOrWhiteSpace(type);

            var conditions = new List<string>();
            if (hasSearch) conditions.Add("LOWER(name) LIKE @SearchPattern");
            if (hasType) conditions.Add("type = @Type::criteria_type");
            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM evaluationcriteria {whereClause}", connection))
            {
                if (hasSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasType) cmd.Parameters.AddWithValue("@Type", type!);
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (var command = new NpgsqlCommand($"SELECT id, name, minvalue, maxvalue, type::text FROM evaluationcriteria {whereClause} {OrderBy(sortField, sortOrder)} LIMIT @PageSize OFFSET @Offset", connection))
            {
                if (hasSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasType) command.Parameters.AddWithValue("@Type", type!);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new EvaluationCriteria { Id = reader.GetInt32(0), Name = reader.GetString(1), MinValue = reader.GetInt32(2), MaxValue = reader.GetInt32(3), Type = ParseType(reader.GetString(4)) });
            }

            return new PagedResponse<EvaluationCriteria> { Items = items, Total = total };
        }

        public async Task<List<EvaluationCriteria>> GetAllAsync()
        {
            var records = new List<EvaluationCriteria>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT id, name, minvalue, maxvalue, type::text FROM evaluationcriteria";

                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new EvaluationCriteria
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            MinValue = reader.GetInt32(2),
                            MaxValue = reader.GetInt32(3),
                            Type = ParseType(reader.GetString(4))
                        });
                    }
                }
            }

            return records;
        }

        public async Task<EvaluationCriteria?> GetByIdAsync(int id)
        {
            EvaluationCriteria? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, name, minvalue, maxvalue, type::text
                    FROM evaluationcriteria
                    WHERE id = @Id
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new EvaluationCriteria
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            MinValue = reader.GetInt32(2),
                            MaxValue = reader.GetInt32(3),
                            Type = ParseType(reader.GetString(4))
                        };
                    }
                }
            }

            return record;
        }

        public async Task<EvaluationCriteriaDeleteCheckDto> GetDeleteCheckAsync(int criteriaId)
        {
            var result = new EvaluationCriteriaDeleteCheckDto();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string groupsQuery = @"
                    SELECT DISTINCT ecg.id, ecg.name
                    FROM evaluationcriteriagroups ecg
                    INNER JOIN evaluationcriteriagroupitems ecgi ON ecgi.groupid = ecg.id
                    WHERE ecgi.criteriaid = @CriteriaId
                ";

                using (var command = new NpgsqlCommand(groupsQuery, connection))
                {
                    command.Parameters.AddWithValue("@CriteriaId", criteriaId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.UsedInGroups.Add(new EvaluationCriteriaGroup
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }

                string applicantCountQuery = @"
                    SELECT COUNT(DISTINCT applicantid)
                    FROM applicantevaluationvalues
                    WHERE evaluationcriteriaid = @CriteriaId
                ";

                using (var command = new NpgsqlCommand(applicantCountQuery, connection))
                {
                    command.Parameters.AddWithValue("@CriteriaId", criteriaId);
                    result.ApplicantTotalCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                string applicantsQuery = @"
                    SELECT DISTINCT a.id, a.name, a.externalid
                    FROM applicants a
                    INNER JOIN applicantevaluationvalues aev ON aev.applicantid = a.id
                    WHERE aev.evaluationcriteriaid = @CriteriaId
                    ORDER BY a.name
                    LIMIT 5
                ";

                using (var command = new NpgsqlCommand(applicantsQuery, connection))
                {
                    command.Parameters.AddWithValue("@CriteriaId", criteriaId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Applicants.Add(new Applicant
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                ExternalId = reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return result;
        }

        public async Task<EvaluationCriteriaRangeCheckDto> GetRangeCheckAsync(int criteriaId, int minValue, int maxValue)
        {
            var result = new EvaluationCriteriaRangeCheckDto();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string countQuery = @"
                SELECT COUNT(DISTINCT applicantid)
                FROM applicantevaluationvalues
                WHERE evaluationcriteriaid = @CriteriaId
                  AND (value < @MinValue OR value > @MaxValue)
            ";

            using (var cmd = new NpgsqlCommand(countQuery, connection))
            {
                cmd.Parameters.AddWithValue("@CriteriaId", criteriaId);
                cmd.Parameters.AddWithValue("@MinValue", minValue);
                cmd.Parameters.AddWithValue("@MaxValue", maxValue);
                result.ApplicantTotalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string applicantsQuery = @"
                SELECT DISTINCT a.id, a.name, a.externalid
                FROM applicants a
                INNER JOIN applicantevaluationvalues aev ON aev.applicantid = a.id
                WHERE aev.evaluationcriteriaid = @CriteriaId
                  AND (aev.value < @MinValue OR aev.value > @MaxValue)
                ORDER BY a.name
                LIMIT 5
            ";

            using (var cmd = new NpgsqlCommand(applicantsQuery, connection))
            {
                cmd.Parameters.AddWithValue("@CriteriaId", criteriaId);
                cmd.Parameters.AddWithValue("@MinValue", minValue);
                cmd.Parameters.AddWithValue("@MaxValue", maxValue);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Applicants.Add(new Applicant
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        ExternalId = reader.GetString(2)
                    });
                }
            }

            return result;
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = excludeId.HasValue
                ? "SELECT COUNT(*) FROM evaluationcriteria WHERE LOWER(name) = LOWER(@Name) AND id != @ExcludeId"
                : "SELECT COUNT(*) FROM evaluationcriteria WHERE LOWER(name) = LOWER(@Name)";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", name);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> UpdateAsync(EvaluationCriteria evaluationCriteria)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE evaluationcriteria
                    SET name = @Name, minvalue = @MinValue, maxvalue = @MaxValue, type = @Type::criteria_type
                    WHERE id = @Id
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", evaluationCriteria.Name);
                    command.Parameters.AddWithValue("@MinValue", evaluationCriteria.MinValue);
                    command.Parameters.AddWithValue("@MaxValue", evaluationCriteria.MaxValue);
                    command.Parameters.AddWithValue("@Type", FormatType(evaluationCriteria.Type));
                    command.Parameters.AddWithValue("@Id", evaluationCriteria.Id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }
    }
}
