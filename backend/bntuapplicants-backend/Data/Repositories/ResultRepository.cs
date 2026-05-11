using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Services.Selection;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class ResultRepository : IResultRepository
    {
        private readonly string _connectionString;

        public ResultRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<PagedResponse<CompetitionListSummaryDto>> GetCompetitionListsPagedAsync(
            int page, int pageSize,
            string? search,
            string? facultySearch, string? departmentSearch, string? specialtySearch,
            int? selectedCount,
            string? sortField, string? sortOrder)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var innerConditions = new List<string>();
            if (!string.IsNullOrWhiteSpace(search))
                innerConditions.Add("cl.name ILIKE @Search");
            if (!string.IsNullOrWhiteSpace(facultySearch))
                innerConditions.Add("f.name ILIKE @FacultySearch");
            if (!string.IsNullOrWhiteSpace(departmentSearch))
                innerConditions.Add("d.name ILIKE @DepartmentSearch");
            if (!string.IsNullOrWhiteSpace(specialtySearch))
                innerConditions.Add("s.name ILIKE @SpecialtySearch");

            string innerWhere = innerConditions.Count > 0 ? "WHERE " + string.Join(" AND ", innerConditions) : "";
            string outerWhere = selectedCount.HasValue ? "WHERE selected_count = @SelectedCount" : "";

            string orderBy = sortField switch
            {
                "name"              => $"cl_name {(sortOrder == "descend" ? "DESC" : "ASC")}",
                "plan"              => $"plan {(sortOrder == "descend" ? "DESC" : "ASC")}",
                "applicationsCount" => $"applications_count {(sortOrder == "descend" ? "DESC" : "ASC")}",
                "selectedCount"     => $"selected_count {(sortOrder == "descend" ? "DESC" : "ASC")}",
                "facultyName"       => $"faculty {(sortOrder == "descend" ? "DESC" : "ASC")}",
                "departmentName"    => $"department {(sortOrder == "descend" ? "DESC" : "ASC")}",
                "specialtyName"     => $"specialty {(sortOrder == "descend" ? "DESC" : "ASC")}",
                _                   => "faculty ASC, department ASC, specialty ASC, cl_name ASC"
            };

            int offset = (page - 1) * pageSize;

            string cte = $@"
                SELECT cl.id, cl.name AS cl_name, f.name AS faculty, d.name AS department,
                       s.name AS specialty, cl.plan,
                       (SELECT COUNT(DISTINCT aac.applicantid)
                        FROM applicantadmissioncategories aac
                        JOIN admissioncategories ac ON aac.admissioncategoryid = ac.id
                        WHERE ac.competitionlistid = cl.id
                          AND NOT EXISTS (
                              SELECT 1 FROM applicant_deletion_requests dr
                              WHERE dr.applicant_id = aac.applicantid
                                AND dr.status IN ('pending','confirmed'))) AS applications_count,
                       (SELECT COUNT(*) FROM selectedapplicants sa
                        JOIN admissioncategories ac ON sa.admissioncategoryid = ac.id
                        WHERE ac.competitionlistid = cl.id) AS selected_count
                FROM competitionlists cl
                JOIN specialties s ON cl.specialtyid = s.id
                JOIN departments d ON s.departmentid = d.id
                JOIN faculties f ON d.facultyid = f.id
                {innerWhere}";

            string countSQL = $"SELECT COUNT(*) FROM ({cte}) sub {outerWhere}";
            string dataSQL  = $"SELECT id, cl_name, faculty, department, specialty, plan, applications_count, selected_count FROM ({cte}) sub {outerWhere} ORDER BY {orderBy} LIMIT @Limit OFFSET @Offset";

            void BindParams(NpgsqlCommand cmd)
            {
                if (!string.IsNullOrWhiteSpace(search))
                    cmd.Parameters.AddWithValue("@Search", $"%{search}%");
                if (!string.IsNullOrWhiteSpace(facultySearch))
                    cmd.Parameters.AddWithValue("@FacultySearch", $"%{facultySearch}%");
                if (!string.IsNullOrWhiteSpace(departmentSearch))
                    cmd.Parameters.AddWithValue("@DepartmentSearch", $"%{departmentSearch}%");
                if (!string.IsNullOrWhiteSpace(specialtySearch))
                    cmd.Parameters.AddWithValue("@SpecialtySearch", $"%{specialtySearch}%");
                if (selectedCount.HasValue)
                    cmd.Parameters.AddWithValue("@SelectedCount", selectedCount.Value);
            }

            int total;
            using (var cmd = new NpgsqlCommand(countSQL, conn))
            {
                BindParams(cmd);
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var items = new List<CompetitionListSummaryDto>();
            using (var cmd = new NpgsqlCommand(dataSQL, conn))
            {
                BindParams(cmd);
                cmd.Parameters.AddWithValue("@Limit", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    items.Add(new CompetitionListSummaryDto
                    {
                        Id = r.GetInt32(0),
                        Name = r.GetString(1),
                        FacultyName = r.GetString(2),
                        DepartmentName = r.GetString(3),
                        SpecialtyName = r.GetString(4),
                        Plan = r.GetInt32(5),
                        ApplicationsCount = Convert.ToInt32(r.GetValue(6)),
                        SelectedCount = Convert.ToInt32(r.GetValue(7))
                    });
            }

            return new PagedResponse<CompetitionListSummaryDto> { Items = items, Total = total };
        }

        public async Task<CompetitionListResultDto?> GetCompetitionListResultAsync(int clId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // CL header
            const string clSQL = @"
                SELECT cl.id, cl.name, f.name, d.name, s.name, cl.plan
                FROM competitionlists cl
                JOIN specialties s ON cl.specialtyid = s.id
                JOIN departments d ON s.departmentid = d.id
                JOIN faculties f ON d.facultyid = f.id
                WHERE cl.id = @ClId";

            CompetitionListResultDto? result = null;
            using (var cmd = new NpgsqlCommand(clSQL, conn))
            {
                cmd.Parameters.AddWithValue("@ClId", clId);
                using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) return null;
                result = new CompetitionListResultDto
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    FacultyName = r.GetString(2),
                    DepartmentName = r.GetString(3),
                    SpecialtyName = r.GetString(4),
                    Plan = r.GetInt32(5)
                };
            }

            // Categories
            const string catSQL = @"
                SELECT id, name, quota, priority, evaluationcriteriagroupid
                FROM admissioncategories
                WHERE competitionlistid = @ClId
                ORDER BY priority ASC";

            var categories = new List<(int Id, string Name, int Quota, int Priority, int GroupId)>();
            using (var cmd = new NpgsqlCommand(catSQL, conn))
            {
                cmd.Parameters.AddWithValue("@ClId", clId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    categories.Add((r.GetInt32(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3), r.GetInt32(4)));
            }

            if (categories.Count == 0)
                return result;

            var catIds = categories.Select(c => c.Id).ToArray();
            var groupIds = categories.Select(c => c.GroupId).Distinct().ToArray();

            // Criteria by group
            const string criteriaSQL = @"
                SELECT ecgi.groupid, ec.id, ec.name
                FROM evaluationcriteriagroupitems ecgi
                JOIN evaluationcriteria ec ON ecgi.criteriaid = ec.id
                WHERE ecgi.groupid = ANY(@GroupIds)
                ORDER BY ecgi.groupid, ecgi.priority ASC";

            var criteriaByGroup = new Dictionary<int, List<CriterionInfoDto>>();
            using (var cmd = new NpgsqlCommand(criteriaSQL, conn))
            {
                cmd.Parameters.AddWithValue("@GroupIds", groupIds);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int gId = r.GetInt32(0);
                    if (!criteriaByGroup.ContainsKey(gId)) criteriaByGroup[gId] = [];
                    criteriaByGroup[gId].Add(new CriterionInfoDto { Id = r.GetInt32(1), Name = r.GetString(2) });
                }
            }

            // Admitted applicants
            const string admittedSQL = @"
                SELECT sa.admissioncategoryid, a.id, a.externalid, a.name
                FROM selectedapplicants sa
                JOIN applicants a ON sa.applicantid = a.id
                JOIN admissioncategories ac ON sa.admissioncategoryid = ac.id
                WHERE ac.competitionlistid = @ClId";

            var admittedByCategory = new Dictionary<int, List<(int Id, string ExtId, string Name)>>();
            var allApplicantIds = new HashSet<int>();
            using (var cmd = new NpgsqlCommand(admittedSQL, conn))
            {
                cmd.Parameters.AddWithValue("@ClId", clId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int catId = r.GetInt32(0);
                    int aId = r.GetInt32(1);
                    if (!admittedByCategory.ContainsKey(catId)) admittedByCategory[catId] = [];
                    admittedByCategory[catId].Add((aId, r.GetString(2), r.GetString(3)));
                    allApplicantIds.Add(aId);
                }
            }

            // Not-admitted applicants (applied but not selected in this CL)
            const string notAdmittedSQL = @"
                SELECT aac.admissioncategoryid, a.id, a.externalid, a.name
                FROM applicantadmissioncategories aac
                JOIN applicants a ON aac.applicantid = a.id
                WHERE aac.admissioncategoryid = ANY(@CatIds)
                  AND NOT EXISTS (
                      SELECT 1 FROM selectedapplicants sa
                      JOIN admissioncategories ac ON sa.admissioncategoryid = ac.id
                      WHERE sa.applicantid = aac.applicantid AND ac.competitionlistid = @ClId)
                  AND NOT EXISTS (
                      SELECT 1 FROM applicant_deletion_requests dr
                      WHERE dr.applicant_id = a.id AND dr.status IN ('pending','confirmed'))";

            var notAdmittedByCategory = new Dictionary<int, List<(int Id, string ExtId, string Name)>>();
            using (var cmd = new NpgsqlCommand(notAdmittedSQL, conn))
            {
                cmd.Parameters.AddWithValue("@CatIds", catIds);
                cmd.Parameters.AddWithValue("@ClId", clId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int catId = r.GetInt32(0);
                    int aId = r.GetInt32(1);
                    if (!notAdmittedByCategory.ContainsKey(catId)) notAdmittedByCategory[catId] = [];
                    notAdmittedByCategory[catId].Add((aId, r.GetString(2), r.GetString(3)));
                    allApplicantIds.Add(aId);
                }
            }

            // Scores for all relevant applicants
            var scores = new Dictionary<int, Dictionary<int, int>>();
            if (allApplicantIds.Count > 0)
            {
                const string scoresSQL = @"
                    SELECT applicantid, evaluationcriteriaid, value
                    FROM applicantevaluationvalues
                    WHERE applicantid = ANY(@AppIds)";

                using var cmd = new NpgsqlCommand(scoresSQL, conn);
                cmd.Parameters.AddWithValue("@AppIds", allApplicantIds.ToArray());
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int aId = r.GetInt32(0), cId = r.GetInt32(1), val = r.GetInt32(2);
                    if (!scores.ContainsKey(aId)) scores[aId] = [];
                    scores[aId][cId] = val;
                }
            }

            // Build result
            foreach (var (catId, catName, quota, priority, groupId) in categories)
            {
                var criteriaList = criteriaByGroup.GetValueOrDefault(groupId) ?? [];
                var criteriaIds = criteriaList.Select(c => c.Id).ToList();

                var cat = new CategoryResultDto
                {
                    Id = catId,
                    Name = catName,
                    Quota = quota,
                    Priority = priority,
                    Criteria = criteriaList
                };

                cat.Admitted = BuildApplicantList(
                    admittedByCategory.GetValueOrDefault(catId) ?? [],
                    criteriaIds, scores);

                cat.NotAdmitted = BuildApplicantList(
                    notAdmittedByCategory.GetValueOrDefault(catId) ?? [],
                    criteriaIds, scores);

                result.Categories.Add(cat);
            }

            // Populate AdmittedTo for all not-admitted applicants
            var allNotAdmittedIds = result.Categories
                .SelectMany(c => c.NotAdmitted)
                .Select(a => a.Id)
                .Distinct()
                .ToArray();

            if (allNotAdmittedIds.Length > 0)
            {
                const string admittedToSQL = @"
                    SELECT sa.applicantid,
                           f2.name || ' / ' || d2.name || ' / ' || sp2.name || ' / ' || cl2.name || ' / ' || ac2.name
                    FROM selectedapplicants sa
                    JOIN admissioncategories ac2 ON sa.admissioncategoryid = ac2.id
                    JOIN competitionlists cl2 ON ac2.competitionlistid = cl2.id
                    JOIN specialties sp2 ON cl2.specialtyid = sp2.id
                    JOIN departments d2 ON sp2.departmentid = d2.id
                    JOIN faculties f2 ON d2.facultyid = f2.id
                    WHERE sa.applicantid = ANY(@AppIds)";

                var admittedToMap = new Dictionary<int, string>();
                using (var cmd = new NpgsqlCommand(admittedToSQL, conn))
                {
                    cmd.Parameters.AddWithValue("@AppIds", allNotAdmittedIds);
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                        admittedToMap[r.GetInt32(0)] = r.GetString(1);
                }

                foreach (var cat in result.Categories)
                    foreach (var a in cat.NotAdmitted)
                        if (admittedToMap.TryGetValue(a.Id, out var v))
                            a.AdmittedTo = v;
            }

            return result;
        }

        public async Task<CompetitionListHeaderDto?> GetCompetitionListHeaderAsync(int clId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string clSQL = @"
                SELECT cl.id, cl.name, f.name, d.name, s.name, cl.plan
                FROM competitionlists cl
                JOIN specialties s ON cl.specialtyid = s.id
                JOIN departments d ON s.departmentid = d.id
                JOIN faculties f ON d.facultyid = f.id
                WHERE cl.id = @ClId";

            CompetitionListHeaderDto? header = null;
            using (var cmd = new NpgsqlCommand(clSQL, conn))
            {
                cmd.Parameters.AddWithValue("@ClId", clId);
                using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) return null;
                header = new CompetitionListHeaderDto
                {
                    Id = r.GetInt32(0), Name = r.GetString(1),
                    FacultyName = r.GetString(2), DepartmentName = r.GetString(3),
                    SpecialtyName = r.GetString(4), Plan = r.GetInt32(5)
                };
            }

            const string catSQL = @"
                SELECT id, name, quota, priority, evaluationcriteriagroupid
                FROM admissioncategories WHERE competitionlistid = @ClId ORDER BY priority ASC";

            var categories = new List<(int Id, string Name, int Quota, int Priority, int GroupId)>();
            using (var cmd = new NpgsqlCommand(catSQL, conn))
            {
                cmd.Parameters.AddWithValue("@ClId", clId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    categories.Add((r.GetInt32(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3), r.GetInt32(4)));
            }

            if (categories.Count == 0) return header;

            var catIds = categories.Select(c => c.Id).ToArray();
            var groupIds = categories.Select(c => c.GroupId).Distinct().ToArray();

            var criteriaByGroup = new Dictionary<int, List<CriterionInfoDto>>();
            using (var cmd = new NpgsqlCommand(@"
                SELECT ecgi.groupid, ec.id, ec.name
                FROM evaluationcriteriagroupitems ecgi
                JOIN evaluationcriteria ec ON ecgi.criteriaid = ec.id
                WHERE ecgi.groupid = ANY(@GroupIds)
                ORDER BY ecgi.groupid, ecgi.priority ASC", conn))
            {
                cmd.Parameters.AddWithValue("@GroupIds", groupIds);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int gId = r.GetInt32(0);
                    if (!criteriaByGroup.ContainsKey(gId)) criteriaByGroup[gId] = [];
                    criteriaByGroup[gId].Add(new CriterionInfoDto { Id = r.GetInt32(1), Name = r.GetString(2) });
                }
            }

            var admittedCounts = new Dictionary<int, int>();
            using (var cmd = new NpgsqlCommand(
                "SELECT admissioncategoryid, COUNT(*) FROM selectedapplicants WHERE admissioncategoryid = ANY(@CatIds) GROUP BY admissioncategoryid", conn))
            {
                cmd.Parameters.AddWithValue("@CatIds", catIds);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    admittedCounts[r.GetInt32(0)] = Convert.ToInt32(r.GetValue(1));
            }

            var notAdmittedCounts = new Dictionary<int, int>();
            using (var cmd = new NpgsqlCommand(@"
                SELECT aac.admissioncategoryid, COUNT(*)
                FROM applicantadmissioncategories aac
                WHERE aac.admissioncategoryid = ANY(@CatIds)
                  AND NOT EXISTS (
                      SELECT 1 FROM selectedapplicants sa
                      JOIN admissioncategories ac ON sa.admissioncategoryid = ac.id
                      WHERE sa.applicantid = aac.applicantid AND ac.competitionlistid = @ClId)
                GROUP BY aac.admissioncategoryid", conn))
            {
                cmd.Parameters.AddWithValue("@CatIds", catIds);
                cmd.Parameters.AddWithValue("@ClId", clId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    notAdmittedCounts[r.GetInt32(0)] = Convert.ToInt32(r.GetValue(1));
            }

            // Load admitted applicants + scores for minScores computation
            var admittedByCategory = new Dictionary<int, List<(int Id, string ExtId, string Name)>>();
            var allAdmittedIds = new HashSet<int>();
            using (var cmd = new NpgsqlCommand(@"
                SELECT sa.admissioncategoryid, a.id, a.externalid, a.name
                FROM selectedapplicants sa
                JOIN applicants a ON sa.applicantid = a.id
                WHERE sa.admissioncategoryid = ANY(@CatIds)", conn))
            {
                cmd.Parameters.AddWithValue("@CatIds", catIds);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int catId = r.GetInt32(0), aId = r.GetInt32(1);
                    if (!admittedByCategory.ContainsKey(catId)) admittedByCategory[catId] = [];
                    admittedByCategory[catId].Add((aId, r.GetString(2), r.GetString(3)));
                    allAdmittedIds.Add(aId);
                }
            }

            var scores = new Dictionary<int, Dictionary<int, int>>();
            if (allAdmittedIds.Count > 0)
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT applicantid, evaluationcriteriaid, value FROM applicantevaluationvalues WHERE applicantid = ANY(@AppIds)", conn);
                cmd.Parameters.AddWithValue("@AppIds", allAdmittedIds.ToArray());
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int aId = r.GetInt32(0), cId = r.GetInt32(1), val = r.GetInt32(2);
                    if (!scores.ContainsKey(aId)) scores[aId] = [];
                    scores[aId][cId] = val;
                }
            }

            foreach (var (catId, catName, quota, _, groupId) in categories)
            {
                var criteriaList = criteriaByGroup.GetValueOrDefault(groupId) ?? [];
                var criteriaIds = criteriaList.Select(c => c.Id).ToList();

                Dictionary<int, int>? minScores = null;
                var admitted = admittedByCategory.GetValueOrDefault(catId) ?? [];
                if (admitted.Count > 0)
                {
                    var sorted = BuildApplicantList(admitted, criteriaIds, scores);
                    var last = sorted[^1];
                    minScores = criteriaIds.ToDictionary(cId => cId, cId => last.Scores.TryGetValue(cId, out int v) ? v : 0);
                }

                header.Categories.Add(new CategoryHeaderDto
                {
                    Id = catId, Name = catName, Quota = quota,
                    AdmittedCount = admittedCounts.GetValueOrDefault(catId, 0),
                    NotAdmittedCount = notAdmittedCounts.GetValueOrDefault(catId, 0),
                    Criteria = criteriaList,
                    MinScores = minScores
                });
            }

            return header;
        }

        public async Task<PagedResponse<ApplicantResultDto>> GetCategoryApplicantsPagedAsync(
            int clId, int catId, bool admitted,
            int page, int pageSize,
            string? idFilter, string? externalIdFilter, string? nameFilter,
            Dictionary<int, string> scoreFilters,
            string? sortField, string? sortOrder)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            int groupId = Convert.ToInt32(await new NpgsqlCommand(
                "SELECT evaluationcriteriagroupid FROM admissioncategories WHERE id = @CatId", conn)
                { Parameters = { new NpgsqlParameter("@CatId", catId) } }.ExecuteScalarAsync());

            var criteriaIds = new List<int>();
            using (var cmd = new NpgsqlCommand(
                "SELECT criteriaid FROM evaluationcriteriagroupitems WHERE groupid = @GroupId ORDER BY priority ASC", conn))
            {
                cmd.Parameters.AddWithValue("@GroupId", groupId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) criteriaIds.Add(r.GetInt32(0));
            }

            string from = admitted
                ? @"FROM selectedapplicants sa
                    JOIN applicants a ON sa.applicantid = a.id
                    JOIN applicantadmissioncategories aac ON aac.applicantid = a.id AND aac.admissioncategoryid = sa.admissioncategoryid"
                : @"FROM applicantadmissioncategories aac
                    JOIN applicants a ON aac.applicantid = a.id";

            string baseWhere = admitted
                ? "WHERE sa.admissioncategoryid = @CatId"
                : @"WHERE aac.admissioncategoryid = @CatId
                    AND NOT EXISTS (
                        SELECT 1 FROM selectedapplicants sa2
                        JOIN admissioncategories ac2 ON sa2.admissioncategoryid = ac2.id
                        WHERE sa2.applicantid = a.id AND ac2.competitionlistid = @ClId)
                    AND NOT EXISTS (
                        SELECT 1 FROM applicant_deletion_requests dr
                        WHERE dr.applicant_id = a.id AND dr.status IN ('pending','confirmed'))";

            var extra = new List<string>();
            if (!string.IsNullOrWhiteSpace(idFilter)) extra.Add("a.id::text ILIKE @IdFilter");
            if (!string.IsNullOrWhiteSpace(externalIdFilter)) extra.Add("a.externalid ILIKE @ExternalIdFilter");
            if (!string.IsNullOrWhiteSpace(nameFilter)) extra.Add("a.name ILIKE @NameFilter");
            foreach (var (critId, _) in scoreFilters)
                extra.Add($"COALESCE((SELECT v.value::text FROM applicantevaluationvalues v WHERE v.applicantid = a.id AND v.evaluationcriteriaid = {critId}), '0') ILIKE @ScoreFilt_{critId}");

            string fullWhere = extra.Count > 0 ? baseWhere + " AND " + string.Join(" AND ", extra) : baseWhere;

            string dir = sortOrder == "descend" ? "DESC" : "ASC";
            string orderBy;
            if (sortField == "id")
                orderBy = $"a.id {dir}";
            else if (sortField == "externalId")
                orderBy = $"a.externalid {dir}, a.id ASC";
            else if (sortField == "name")
                orderBy = $"a.name {dir}, a.id ASC";
            else if (sortField?.StartsWith("score_") == true && int.TryParse(sortField[6..], out int sortCritId))
                orderBy = $"(SELECT COALESCE(v.value,0) FROM applicantevaluationvalues v WHERE v.applicantid = a.id AND v.evaluationcriteriaid = {sortCritId}) {dir} NULLS LAST, a.id ASC";
            else
            {
                var parts = criteriaIds
                    .Select(cId => $"(SELECT COALESCE(v.value,0) FROM applicantevaluationvalues v WHERE v.applicantid = a.id AND v.evaluationcriteriaid = {cId}) DESC")
                    .ToList();
                parts.Add("aac.selectionpriority ASC");
                parts.Add("a.id ASC");
                orderBy = string.Join(", ", parts);
            }

            int offset = (page - 1) * pageSize;

            void BindParams(NpgsqlCommand cmd)
            {
                cmd.Parameters.AddWithValue("@CatId", catId);
                cmd.Parameters.AddWithValue("@ClId", clId);
                if (!string.IsNullOrWhiteSpace(idFilter)) cmd.Parameters.AddWithValue("@IdFilter", $"%{idFilter}%");
                if (!string.IsNullOrWhiteSpace(externalIdFilter)) cmd.Parameters.AddWithValue("@ExternalIdFilter", $"%{externalIdFilter}%");
                if (!string.IsNullOrWhiteSpace(nameFilter)) cmd.Parameters.AddWithValue("@NameFilter", $"%{nameFilter}%");
                foreach (var (critId, val) in scoreFilters)
                    cmd.Parameters.AddWithValue($"@ScoreFilt_{critId}", $"%{val}%");
            }

            int total;
            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) {from} {fullWhere}", conn))
            {
                BindParams(cmd);
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string admittedToSelect = admitted
                ? "NULL"
                : @"(SELECT f2.name || ' / ' || d2.name || ' / ' || sp2.name || ' / ' || cl2.name || ' / ' || ac_any.name
                     FROM selectedapplicants sa_any
                     JOIN admissioncategories ac_any ON sa_any.admissioncategoryid = ac_any.id
                     JOIN competitionlists cl2 ON ac_any.competitionlistid = cl2.id
                     JOIN specialties sp2 ON cl2.specialtyid = sp2.id
                     JOIN departments d2 ON sp2.departmentid = d2.id
                     JOIN faculties f2 ON d2.facultyid = f2.id
                     WHERE sa_any.applicantid = a.id LIMIT 1)";

            var basics = new List<(int Id, string ExtId, string Name, string? AdmittedToCl)>();
            using (var cmd = new NpgsqlCommand($"SELECT a.id, a.externalid, a.name, {admittedToSelect} AS admitted_to_cl_name {from} {fullWhere} ORDER BY {orderBy} LIMIT @Limit OFFSET @Offset", conn))
            {
                BindParams(cmd);
                cmd.Parameters.AddWithValue("@Limit", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    basics.Add((r.GetInt32(0), r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3)));
            }

            var appIds = basics.Select(b => b.Id).ToArray();
            var scores = new Dictionary<int, Dictionary<int, int>>();
            if (appIds.Length > 0 && criteriaIds.Count > 0)
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT applicantid, evaluationcriteriaid, value FROM applicantevaluationvalues WHERE applicantid = ANY(@AppIds) AND evaluationcriteriaid = ANY(@CritIds)", conn);
                cmd.Parameters.AddWithValue("@AppIds", appIds);
                cmd.Parameters.AddWithValue("@CritIds", criteriaIds.ToArray());
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int aId = r.GetInt32(0), cId = r.GetInt32(1), val = r.GetInt32(2);
                    if (!scores.ContainsKey(aId)) scores[aId] = [];
                    scores[aId][cId] = val;
                }
            }

            var items = basics.Select(b => new ApplicantResultDto
            {
                Id = b.Id, ExternalId = b.ExtId, Name = b.Name,
                AdmittedTo = b.AdmittedToCl,
                Scores = criteriaIds.ToDictionary(cId => cId, cId =>
                    scores.TryGetValue(b.Id, out var s) && s.TryGetValue(cId, out int v) ? v : 0)
            }).ToList();

            return new PagedResponse<ApplicantResultDto> { Items = items, Total = total };
        }

        private static List<ApplicantResultDto> BuildApplicantList(
            List<(int Id, string ExtId, string Name)> applicants,
            List<int> criteriaIds,
            Dictionary<int, Dictionary<int, int>> scores)
        {
            var candidates = applicants.Select(a =>
            {
                var scoreMap = scores.GetValueOrDefault(a.Id) ?? [];
                var vector = criteriaIds.Select(cId => scoreMap.TryGetValue(cId, out int v) ? v : 0).ToArray();
                return (Applicant: a, ScoreVector: vector);
            }).ToList();

            // Sort by strength DESC (same comparator logic as CategoryComparer)
            candidates.Sort((x, y) =>
            {
                int len = Math.Min(x.ScoreVector.Length, y.ScoreVector.Length);
                for (int i = 0; i < len; i++)
                {
                    int cmp = y.ScoreVector[i].CompareTo(x.ScoreVector[i]);
                    if (cmp != 0) return cmp;
                }
                return x.Applicant.Id.CompareTo(y.Applicant.Id);
            });

            return candidates.Select(c =>
            {
                var scoreMap = scores.GetValueOrDefault(c.Applicant.Id) ?? [];
                return new ApplicantResultDto
                {
                    Id = c.Applicant.Id,
                    ExternalId = c.Applicant.ExtId,
                    Name = c.Applicant.Name,
                    Scores = criteriaIds.ToDictionary(cId => cId, cId => scoreMap.TryGetValue(cId, out int v) ? v : 0)
                };
            }).ToList();
        }
    }
}
