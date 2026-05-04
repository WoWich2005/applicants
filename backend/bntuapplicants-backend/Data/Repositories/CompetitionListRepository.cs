using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class CompetitionListRepository : ICompetitionListRepository
    {
        private readonly string _connectionString;

        public CompetitionListRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<CompetitionList?> CreateAsync(CompetitionList competitionList)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO competitionlists (name, plan, specialtyid)
                    VALUES (@Name, @Plan, @SpecialtyId)
                    RETURNING *
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", competitionList.Name);
                    command.Parameters.AddWithValue("@Plan", competitionList.Plan);
                    command.Parameters.AddWithValue("@SpecialtyId", competitionList.SpecialtyId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new CompetitionList
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Plan = reader.GetInt32(2),
                                SpecialtyId = reader.GetInt32(3)
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

                string query = "DELETE FROM competitionlists WHERE id = @Id";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
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
            { "name", "name" },
            { "plan", "plan" }
        };

        private static string OrderBy(string? field, string? order)
        {
            string col = (field != null && SortColumns.TryGetValue(field, out var c)) ? c : "name";
            string dir = order == "descend" ? "DESC" : "ASC";
            return $"ORDER BY {col} {dir}";
        }

        public async Task<PagedResponse<CompetitionList>> GetPagedBySpecialtyAsync(int specialtyId, int page, int pageSize, string? search, string? sortField, string? sortOrder)
        {
            var items = new List<CompetitionList>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            string whereClause = hasSearch
                ? "WHERE specialtyid = @SpecialtyId AND LOWER(name) LIKE @SearchPattern"
                : "WHERE specialtyid = @SpecialtyId";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM competitionlists {whereClause}", connection))
            {
                cmd.Parameters.AddWithValue("@SpecialtyId", specialtyId);
                if (hasSearch)
                    cmd.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (var command = new NpgsqlCommand($"SELECT id, name, plan, specialtyid FROM competitionlists {whereClause} {OrderBy(sortField, sortOrder)} LIMIT @PageSize OFFSET @Offset", connection))
            {
                command.Parameters.AddWithValue("@SpecialtyId", specialtyId);
                if (hasSearch)
                    command.Parameters.AddWithValue("@SearchPattern", $"%{search!.ToLower()}%");
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
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE competitionlists
                    SET name = @Name, plan = @Plan, specialtyid = @SpecialtyId
                    WHERE id = @Id
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", competitionList.Name);
                    command.Parameters.AddWithValue("@Plan", competitionList.Plan);
                    command.Parameters.AddWithValue("@SpecialtyId", competitionList.SpecialtyId);
                    command.Parameters.AddWithValue("@Id", competitionList.Id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }
    }
}
