using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class ApplicantEvaluationValueRepository : IApplicantEvaluationValueRepository
    {
        private readonly string _connectionString;

        public ApplicantEvaluationValueRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<ApplicantEvaluationValue?> CreateAsync(ApplicantEvaluationValue record)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO applicantevaluationvalues (applicantid, evaluationcriteriaid, value)
                    VALUES (@ApplicantId, @EvaluationCriteriaId, @Value)
                    RETURNING *
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApplicantId", record.ApplicantId);
                    command.Parameters.AddWithValue("@EvaluationCriteriaId", record.EvaluationCriteriaId);
                    command.Parameters.AddWithValue("@Value", record.Value);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new ApplicantEvaluationValue
                            {
                                Id = reader.GetInt32(0),
                                ApplicantId = reader.GetInt32(1),
                                EvaluationCriteriaId = reader.GetInt32(2),
                                Value = reader.GetInt32(3)
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

                string query = "DELETE FROM applicantevaluationvalues WHERE id = @Id";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }

        public async Task<List<ApplicantEvaluationValue>> GetAllByApplicantAsync(int applicantId)
        {
            var records = new List<ApplicantEvaluationValue>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, applicantid, evaluationcriteriaid, value
                    FROM applicantevaluationvalues
                    WHERE applicantid = @ApplicantId
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ApplicantId", applicantId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new ApplicantEvaluationValue
                        {
                            Id = reader.GetInt32(0),
                            ApplicantId = reader.GetInt32(1),
                            EvaluationCriteriaId = reader.GetInt32(2),
                            Value = reader.GetInt32(3)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<ApplicantEvaluationValue?> GetByIdAsync(int id)
        {
            ApplicantEvaluationValue? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, applicantid, evaluationcriteriaid, value
                    FROM applicantevaluationvalues
                    WHERE id = @Id
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new ApplicantEvaluationValue
                        {
                            Id = reader.GetInt32(0),
                            ApplicantId = reader.GetInt32(1),
                            EvaluationCriteriaId = reader.GetInt32(2),
                            Value = reader.GetInt32(3)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<bool> UpdateAsync(ApplicantEvaluationValue record)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE applicantevaluationvalues
                    SET applicantid = @ApplicantId,
                        evaluationcriteriaid = @EvaluationCriteriaId,
                        value = @Value
                    WHERE id = @Id
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApplicantId", record.ApplicantId);
                    command.Parameters.AddWithValue("@EvaluationCriteriaId", record.EvaluationCriteriaId);
                    command.Parameters.AddWithValue("@Value", record.Value);
                    command.Parameters.AddWithValue("@Id", record.Id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }
    }
}
