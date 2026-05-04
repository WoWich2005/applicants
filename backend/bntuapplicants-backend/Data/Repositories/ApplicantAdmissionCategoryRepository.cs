using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class ApplicantAdmissionCategoryRepository : IApplicantAdmissionCategoryRepository
    {
        private readonly string _connectionString;

        public ApplicantAdmissionCategoryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<ApplicantAdmissionCategory?> CreateAsync(ApplicantAdmissionCategory record)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO applicantadmissioncategories (applicantid, admissioncategoryid, selectionpriority)
                    VALUES (@ApplicantId, @AdmissionCategoryId, @SelectionPriority)
                    RETURNING *
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApplicantId", record.ApplicantId);
                    command.Parameters.AddWithValue("@AdmissionCategoryId", record.AdmissionCategoryId);
                    command.Parameters.AddWithValue("@SelectionPriority", record.SelectionPriority);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new ApplicantAdmissionCategory
                            {
                                Id = reader.GetInt32(0),
                                ApplicantId = reader.GetInt32(1),
                                AdmissionCategoryId = reader.GetInt32(2),
                                SelectionPriority = reader.GetInt32(3)
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

                string query = "DELETE FROM applicantadmissioncategories WHERE id = @Id";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }

        public async Task<List<ApplicantAdmissionCategory>> GetAllByApplicantAsync(int applicantId)
        {
            var records = new List<ApplicantAdmissionCategory>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, applicantid, admissioncategoryid, selectionpriority
                    FROM applicantadmissioncategories
                    WHERE applicantid = @ApplicantId
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ApplicantId", applicantId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new ApplicantAdmissionCategory
                        {
                            Id = reader.GetInt32(0),
                            ApplicantId = reader.GetInt32(1),
                            AdmissionCategoryId = reader.GetInt32(2),
                            SelectionPriority = reader.GetInt32(3)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<List<Applicant>> GetApplicantsByAdmissionCategoryAsync(int admissionCategoryId)
        {
            var records = new List<Applicant>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT a.id, a.name, a.externalid
                    FROM applicants a
                    INNER JOIN applicantadmissioncategories aac ON aac.applicantid = a.id
                    WHERE aac.admissioncategoryid = @AdmissionCategoryId
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdmissionCategoryId", admissionCategoryId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new Applicant
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            ExternalId = reader.GetString(2)
                        });
                    }
                }
            }

            return records;
        }

        public async Task<ApplicantAdmissionCategory?> GetByIdAsync(int id)
        {
            ApplicantAdmissionCategory? record = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, applicantid, admissioncategoryid, selectionpriority
                    FROM applicantadmissioncategories
                    WHERE id = @Id
                ";

                var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        record = new ApplicantAdmissionCategory
                        {
                            Id = reader.GetInt32(0),
                            ApplicantId = reader.GetInt32(1),
                            AdmissionCategoryId = reader.GetInt32(2),
                            SelectionPriority = reader.GetInt32(3)
                        };
                    }
                }
            }

            return record;
        }

        public async Task<bool> UpdateAsync(ApplicantAdmissionCategory record)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE applicantadmissioncategories
                    SET applicantid = @ApplicantId,
                        admissioncategoryid = @AdmissionCategoryId,
                        selectionpriority = @SelectionPriority
                    WHERE id = @Id
                ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApplicantId", record.ApplicantId);
                    command.Parameters.AddWithValue("@AdmissionCategoryId", record.AdmissionCategoryId);
                    command.Parameters.AddWithValue("@SelectionPriority", record.SelectionPriority);
                    command.Parameters.AddWithValue("@Id", record.Id);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }
    }
}
