using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class EvaluationCriteriaGroupItemRepository : IEvaluationCriteriaGroupItemRepository
    {
        private readonly string _connectionString;

        public EvaluationCriteriaGroupItemRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<EvaluationCriteriaGroupItem?> CreateAsync(EvaluationCriteriaGroupItem item)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO evaluationcriteriagroupitems (groupid, criteriaid, priority)
                    VALUES (@GroupId, @CriteriaId, @Priority)
                    RETURNING *
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GroupId", item.GroupId);
                    command.Parameters.AddWithValue("@CriteriaId", item.CriteriaId);
                    command.Parameters.AddWithValue("@Priority", item.Priority);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new EvaluationCriteriaGroupItem
                            {
                                Id = reader.GetInt32(0),
                                GroupId = reader.GetInt32(1),
                                CriteriaId = reader.GetInt32(2),
                                Priority = reader.GetInt32(3)
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

                string query = "DELETE FROM evaluationcriteriagroupitems WHERE id = @Id";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }

        public async Task<List<EvaluationCriteriaGroupItem>> GetAllByGroupAsync(int groupId)
        {
            var records = new List<EvaluationCriteriaGroupItem>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, groupid, criteriaid, priority
                    FROM evaluationcriteriagroupitems
                    WHERE groupid = @GroupId
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new EvaluationCriteriaGroupItem
                        {
                            Id = reader.GetInt32(0),
                            GroupId = reader.GetInt32(1),
                            CriteriaId = reader.GetInt32(2),
                            Priority = reader.GetInt32(3)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<EvaluationCriteriaGroupItem?> GetByIdAsync(int id)
        {
            EvaluationCriteriaGroupItem? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, groupid, criteriaid, priority
                    FROM evaluationcriteriagroupitems
                    WHERE id = @Id
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new EvaluationCriteriaGroupItem
                        {
                            Id = reader.GetInt32(0),
                            GroupId = reader.GetInt32(1),
                            CriteriaId = reader.GetInt32(2),
                            Priority = reader.GetInt32(3)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<bool> UpdateAsync(EvaluationCriteriaGroupItem item)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE evaluationcriteriagroupitems
                    SET groupid = @GroupId, criteriaid = @CriteriaId, priority = @Priority
                    WHERE id = @Id
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GroupId", item.GroupId);
                    command.Parameters.AddWithValue("@CriteriaId", item.CriteriaId);
                    command.Parameters.AddWithValue("@Priority", item.Priority);
                    command.Parameters.AddWithValue("@Id", item.Id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }
    }
}
