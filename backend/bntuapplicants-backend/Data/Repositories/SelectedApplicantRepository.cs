using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Models;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class SelectedApplicantRepository : ISelectedApplicantRepository
    {
        private readonly string _connectionString;

        public SelectedApplicantRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<SelectedApplicant>> GetByCompetitionListIdAsync(int competitionListId)
        {
            var records = new List<SelectedApplicant>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT sa.id, sa.applicantid, sa.admissioncategoryid
                FROM selectedapplicants sa
                JOIN admissioncategories ac ON sa.admissioncategoryid = ac.id
                WHERE ac.competitionlistid = @CompListId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@CompListId", competitionListId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new SelectedApplicant
                {
                    Id = reader.GetInt32(0),
                    ApplicantId = reader.GetInt32(1),
                    AdmissionCategoryId = reader.GetInt32(2)
                });
            }

            return records;
        }

        public async Task<List<SelectedApplicant>> GetByApplicantIdAsync(int applicantId)
        {
            var records = new List<SelectedApplicant>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT id, applicantid, admissioncategoryid
                FROM selectedapplicants
                WHERE applicantid = @ApplicantId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@ApplicantId", applicantId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new SelectedApplicant
                {
                    Id = reader.GetInt32(0),
                    ApplicantId = reader.GetInt32(1),
                    AdmissionCategoryId = reader.GetInt32(2)
                });
            }

            return records;
        }
    }
}
