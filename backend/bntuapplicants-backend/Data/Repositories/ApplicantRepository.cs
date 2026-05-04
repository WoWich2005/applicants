using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{

    public class ApplicantRepository : IApplicantRepository
    {
        private readonly string _connectionString;

        public ApplicantRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<Applicant?> CreateAsync(Applicant applicant)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO Applicants (Name, Notes, ExternalId)
                    VALUES (@Name, @Notes, @ExternalId)
                    RETURNING Id, Name, Notes, ExternalId
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", applicant.Name);
                    command.Parameters.AddWithValue("@Notes", (object?)applicant.Notes ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ExternalId", (object?)applicant.ExternalId ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Applicant
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                                ExternalId = reader.GetString(3)
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

                string query = "DELETE FROM Applicants WHERE Id = @Id";

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
            { "externalId", "externalid" }
        };

        private static string OrderBy(string? field, string? order)
        {
            string col = (field != null && SortColumns.TryGetValue(field, out var c)) ? c : "name";
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        public async Task<PagedResponse<Applicant>> GetPagedAsync(int page, int pageSize, string? search, string? externalIdSearch = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<Applicant>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasNameSearch = !string.IsNullOrWhiteSpace(search);
            bool hasExternalIdSearch = !string.IsNullOrWhiteSpace(externalIdSearch);

            var conditions = new List<string>();
            if (hasNameSearch) conditions.Add("LOWER(Name) LIKE @SearchPattern");
            if (hasExternalIdSearch) conditions.Add("LOWER(ExternalId) LIKE @ExternalIdPattern");
            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM Applicants {whereClause}", connection))
            {
                if (hasNameSearch) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasExternalIdSearch) cmd.Parameters.AddWithValue("@ExternalIdPattern", $"%{externalIdSearch!.ToLower()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string dataQuery = $@"
                SELECT Id, Name, Notes, ExternalId FROM Applicants
                {whereClause}
                {OrderBy(sortField, sortOrder)}
                LIMIT @PageSize OFFSET @Offset
            ";
            using (var command = new NpgsqlCommand(dataQuery, connection))
            {
                if (hasNameSearch) command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                if (hasExternalIdSearch) command.Parameters.AddWithValue("@ExternalIdPattern", $"%{externalIdSearch!.ToLower()}%");
                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(new Applicant
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                        ExternalId = reader.GetString(3)
                    });
            }

            return new PagedResponse<Applicant> { Items = items, Total = total };
        }

        public async Task<List<Applicant>> GetAllAsync()
        {
            var records = new List<Applicant>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT Id, Name, Notes, ExternalId FROM Applicants";
                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new Applicant
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ExternalId = reader.GetString(3)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<Applicant?> GetByIdAsync(int id)
        {
            Applicant? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT Id, Name, Notes, ExternalId
                    FROM Applicants
                    WHERE Id = @ApplicantId
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ApplicantId", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new Applicant()
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ExternalId = reader.GetString(3)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<bool> UpdateAsync(Applicant applicant)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE Applicants
                    SET Name = @Name, Notes = @Notes, ExternalId = @ExternalId
                    WHERE Id = @Id
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", applicant.Name);
                    command.Parameters.AddWithValue("@Notes", (object?)applicant.Notes ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ExternalId", applicant.ExternalId);
                    command.Parameters.AddWithValue("@Id", applicant.Id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }
    }
}
