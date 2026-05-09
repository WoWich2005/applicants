using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class ApplicantAdmissionCategoryRepository : IApplicantAdmissionCategoryRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public ApplicantAdmissionCategoryRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<ApplicantAdmissionCategory?> CreateAsync(ApplicantAdmissionCategory record)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO applicantadmissioncategories (applicantid, admissioncategoryid, selectionpriority)
                VALUES (@ApplicantId, @AdmissionCategoryId, @SelectionPriority)
                RETURNING *
            ";

            ApplicantAdmissionCategory? created = null;
            using var tx = await connection.BeginTransactionAsync();

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@ApplicantId", record.ApplicantId);
                command.Parameters.AddWithValue("@AdmissionCategoryId", record.AdmissionCategoryId);
                command.Parameters.AddWithValue("@SelectionPriority", record.SelectionPriority);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    created = new ApplicantAdmissionCategory
                    {
                        Id = reader.GetInt32(0),
                        ApplicantId = reader.GetInt32(1),
                        AdmissionCategoryId = reader.GetInt32(2),
                        SelectionPriority = reader.GetInt32(3)
                    };
                }
            }

            if (created != null)
            {
                await _auditLogger.ResetValidationIfNeededAsync(created.ApplicantId, (NpgsqlTransaction)tx);
                await _auditLogger.LogCreateAsync(
                    "applicant_admission_category",
                    created.Id.ToString(),
                    created,
                    tx: (NpgsqlTransaction)tx);
                await tx.CommitAsync();
                return created;
            }

            await tx.RollbackAsync();
            return null;
        }

        public async Task<bool> ExistsAsync(int applicantId, int admissionCategoryId, int? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = excludeId.HasValue
                ? "SELECT 1 FROM applicantadmissioncategories WHERE applicantid = @ApplicantId AND admissioncategoryid = @AdmissionCategoryId AND id != @ExcludeId LIMIT 1"
                : "SELECT 1 FROM applicantadmissioncategories WHERE applicantid = @ApplicantId AND admissioncategoryid = @AdmissionCategoryId LIMIT 1";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@ApplicantId", applicantId);
            command.Parameters.AddWithValue("@AdmissionCategoryId", admissionCategoryId);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

            var result = await command.ExecuteScalarAsync();
            return result != null;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var before = await GetByIdAsync(id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = "DELETE FROM applicantadmissioncategories WHERE id = @Id";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@Id", id);
                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            await _auditLogger.ResetValidationIfNeededAsync(before.ApplicantId, (NpgsqlTransaction)tx);
            await _auditLogger.LogDeleteAsync(
                "applicant_admission_category",
                id.ToString(),
                before,
                tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        public async Task<PagedResponse<ApplicantAdmissionCategoryDto>> GetAllByApplicantPagedAsync(
            int applicantId, int page, int pageSize,
            string? sortField = null, string? sortOrder = null,
            string? id = null, string? selectionPriority = null, string? faculty = null, string? department = null,
            string? specialty = null, string? competitionList = null,
            string? category = null)
        {
            int offset = (page - 1) * pageSize;

            var filterParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(id) && int.TryParse(id, out int idValue)) filterParts.Add("AND aac.id = @IdValue");
            if (!string.IsNullOrWhiteSpace(selectionPriority) && int.TryParse(selectionPriority, out int priorityValue)) filterParts.Add("AND aac.selectionpriority = @PriorityValue");
            if (!string.IsNullOrWhiteSpace(faculty)) filterParts.Add("AND LOWER(f.name) LIKE @FacultyPattern");
            if (!string.IsNullOrWhiteSpace(department)) filterParts.Add("AND LOWER(d.name) LIKE @DepartmentPattern");
            if (!string.IsNullOrWhiteSpace(specialty)) filterParts.Add("AND LOWER(s.name) LIKE @SpecialtyPattern");
            if (!string.IsNullOrWhiteSpace(competitionList)) filterParts.Add("AND LOWER(cl.name) LIKE @CompetitionListPattern");
            if (!string.IsNullOrWhiteSpace(category)) filterParts.Add("AND LOWER(ac.name) LIKE @CategoryPattern");
            string whereFilters = string.Join(" ", filterParts);

            string orderByColumn = sortField switch
            {
                "id" => "aac.id",
                "selectionPriority" => "aac.selectionpriority",
                "facultyName" => "f.name",
                "departmentName" => "d.name",
                "specialtyName" => "s.name",
                "competitionListName" => "cl.name",
                "categoryName" => "ac.name",
                _ => "aac.selectionpriority"
            };
            string direction = sortOrder == "descend" ? "DESC" : "ASC";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var countSql = $@"
                SELECT COUNT(*) FROM applicantadmissioncategories aac
                INNER JOIN admissioncategories ac ON ac.id = aac.admissioncategoryid
                INNER JOIN competitionlists cl ON cl.id = ac.competitionlistid
                INNER JOIN specialties s ON s.id = cl.specialtyid
                INNER JOIN departments d ON d.id = s.departmentid
                INNER JOIN faculties f ON f.id = d.facultyid
                WHERE aac.applicantid = @ApplicantId {whereFilters}";

            int total;
            using (var countCmd = new NpgsqlCommand(countSql, conn))
            {
                countCmd.Parameters.AddWithValue("@ApplicantId", applicantId);
                AddApplicantAdmissionCategoryFilterParams(countCmd, id, selectionPriority, faculty, department, specialty, competitionList, category);
                total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            var selectSql = $@"
                SELECT aac.id, aac.applicantid, aac.admissioncategoryid, aac.selectionpriority,
                       f.name, d.name, s.name, cl.name, ac.name
                FROM applicantadmissioncategories aac
                INNER JOIN admissioncategories ac ON ac.id = aac.admissioncategoryid
                INNER JOIN competitionlists cl ON cl.id = ac.competitionlistid
                INNER JOIN specialties s ON s.id = cl.specialtyid
                INNER JOIN departments d ON d.id = s.departmentid
                INNER JOIN faculties f ON f.id = d.facultyid
                WHERE aac.applicantid = @ApplicantId {whereFilters}
                ORDER BY {orderByColumn} {direction}
                LIMIT @PageSize OFFSET @Offset";

            var items = new List<ApplicantAdmissionCategoryDto>();
            using (var cmd = new NpgsqlCommand(selectSql, conn))
            {
                cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
                AddApplicantAdmissionCategoryFilterParams(cmd, id, selectionPriority, faculty, department, specialty, competitionList, category);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new ApplicantAdmissionCategoryDto
                    {
                        Id = reader.GetInt32(0),
                        ApplicantId = reader.GetInt32(1),
                        AdmissionCategoryId = reader.GetInt32(2),
                        SelectionPriority = reader.GetInt32(3),
                        FacultyName = reader.GetString(4),
                        DepartmentName = reader.GetString(5),
                        SpecialtyName = reader.GetString(6),
                        CompetitionListName = reader.GetString(7),
                        CategoryName = reader.GetString(8),
                    });
                }
            }

            return new PagedResponse<ApplicantAdmissionCategoryDto> { Items = items, Total = total };
        }

        private static void AddApplicantAdmissionCategoryFilterParams(
            NpgsqlCommand cmd, string? id, string? selectionPriority, string? faculty, string? department, string? specialty, string? competitionList, string? category)
        {
            if (!string.IsNullOrWhiteSpace(id) && int.TryParse(id, out int idValue)) cmd.Parameters.AddWithValue("@IdValue", idValue);
            if (!string.IsNullOrWhiteSpace(selectionPriority) && int.TryParse(selectionPriority, out int priorityValue)) cmd.Parameters.AddWithValue("@PriorityValue", priorityValue);
            if (!string.IsNullOrWhiteSpace(faculty)) cmd.Parameters.AddWithValue("@FacultyPattern", $"%{faculty.ToLower()}%");
            if (!string.IsNullOrWhiteSpace(department)) cmd.Parameters.AddWithValue("@DepartmentPattern", $"%{department.ToLower()}%");
            if (!string.IsNullOrWhiteSpace(specialty)) cmd.Parameters.AddWithValue("@SpecialtyPattern", $"%{specialty.ToLower()}%");
            if (!string.IsNullOrWhiteSpace(competitionList)) cmd.Parameters.AddWithValue("@CompetitionListPattern", $"%{competitionList.ToLower()}%");
            if (!string.IsNullOrWhiteSpace(category)) cmd.Parameters.AddWithValue("@CategoryPattern", $"%{category.ToLower()}%");
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
            var before = await GetByIdAsync(record.Id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = @"
                UPDATE applicantadmissioncategories
                SET applicantid = @ApplicantId,
                    admissioncategoryid = @AdmissionCategoryId,
                    selectionpriority = @SelectionPriority
                WHERE id = @Id
            ";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@ApplicantId", record.ApplicantId);
                command.Parameters.AddWithValue("@AdmissionCategoryId", record.AdmissionCategoryId);
                command.Parameters.AddWithValue("@SelectionPriority", record.SelectionPriority);
                command.Parameters.AddWithValue("@Id", record.Id);

                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            var diff = JsonDiff.Compute(before, new { record.Id, record.ApplicantId, record.AdmissionCategoryId, record.SelectionPriority });
            await _auditLogger.ResetValidationIfNeededAsync(record.ApplicantId, (NpgsqlTransaction)tx);
            await _auditLogger.LogUpdateAsync(
                "applicant_admission_category",
                record.Id.ToString(),
                diff,
                tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }
    }
}
