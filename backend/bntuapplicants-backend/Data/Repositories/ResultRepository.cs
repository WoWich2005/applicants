using bntuapplicants_backend.Data.Interfaces;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Data.Repositories
{
    
    public class ResultRepository : IResultRepository
    {
        private readonly string _connectionString;

        public ResultRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<Dictionary<int, List<(string Name, int Points)>>> GetByFacultyIdAsync(int facultyId)
        {
            var result = new Dictionary<int, List<(string Name, int Points)>>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT
                    spec.id                        AS specialtyid,
                    a.name                         AS applicantname,
                    COALESCE(SUM(aev.value), 0)    AS totalpoints
                FROM selectedapplicants sa
                JOIN admissioncategories  ac   ON sa.admissioncategoryid = ac.id
                JOIN competitionlists     cl   ON ac.competitionlistid   = cl.id
                JOIN specialties          spec ON cl.specialtyid         = spec.id
                JOIN departments          dept ON spec.departmentid      = dept.id
                JOIN applicants           a    ON sa.applicantid         = a.id
                LEFT JOIN applicantevaluationvalues aev ON aev.applicantid = sa.applicantid
                WHERE dept.facultyid = @FacultyId
                GROUP BY spec.id, a.id, a.name
                ORDER BY spec.id, totalpoints DESC";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@FacultyId", facultyId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int specId = reader.GetInt32(0);
                string name = reader.GetString(1);
                int points = reader.GetInt32(2);

                if (!result.ContainsKey(specId))
                    result[specId] = new List<(string, int)>();

                result[specId].Add((name, points));
            }

            return result;
        }
    }
}
