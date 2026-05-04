using Npgsql;

namespace bntuapplicants_backend.Services
{
    public class SelectionService
    {
        private readonly string _connectionString;

        public SelectionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task RecalculateForApplicantAsync(int applicantId)
        {
            // Phase 1: determine all competition lists that could be involved (read-only, no locks).
            // 2-hop: own CLs + CLs of all applicants sharing any of those CLs.
            var lockIds = await GetAffectedCompetitionListIdsAsync(applicantId);

            // Phase 2: acquire all locks upfront in ascending order, then run algorithm.
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                foreach (var clId in lockIds)
                {
                    using var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key)", conn, tx);
                    lockCmd.Parameters.AddWithValue("@key", (long)clId);
                    await lockCmd.ExecuteNonQueryAsync();
                }

                using var delCmd = new NpgsqlCommand(
                    "DELETE FROM selectedapplicants WHERE applicantid = @Id", conn, tx);
                delCmd.Parameters.AddWithValue("@Id", applicantId);
                await delCmd.ExecuteNonQueryAsync();

                var queue = new Queue<(int ApplicantId, int MinPriority)>();
                queue.Enqueue((applicantId, 1));

                while (queue.Count > 0)
                {
                    var (currentId, minPriority) = queue.Dequeue();
                    await ProcessApplicantAsync(currentId, minPriority, queue, conn, tx);
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Read-only pre-scan: returns all competition list IDs sorted ascending.
        private async Task<List<int>> GetAffectedCompetitionListIdsAsync(int applicantId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Hop 1: CLs of the changed applicant.
            // Hop 2: CLs of all applicants who share any of those CLs (they could be displaced).
            const string sql = @"
                WITH applicant_cls AS (
                    SELECT DISTINCT ac.competitionlistid
                    FROM applicantadmissioncategories aac
                    JOIN admissioncategories ac ON aac.admissioncategoryid = ac.id
                    WHERE aac.applicantid = @ApplicantId
                ),
                sharing_applicants AS (
                    SELECT DISTINCT aac.applicantid
                    FROM applicantadmissioncategories aac
                    JOIN admissioncategories ac ON aac.admissioncategoryid = ac.id
                    WHERE ac.competitionlistid IN (SELECT competitionlistid FROM applicant_cls)
                )
                SELECT DISTINCT ac.competitionlistid
                FROM applicantadmissioncategories aac
                JOIN admissioncategories ac ON aac.admissioncategoryid = ac.id
                WHERE aac.applicantid IN (SELECT applicantid FROM sharing_applicants)
                ORDER BY 1";

            var ids = new List<int>();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                ids.Add(reader.GetInt32(0));

            return ids;
        }

        private async Task ProcessApplicantAsync(
            int applicantId, int minPriority,
            Queue<(int ApplicantId, int MinPriority)> queue,
            NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            var next = await FindNextCategoryAsync(applicantId, minPriority, conn, tx);
            if (next == null) return;

            var (categoryId, selectionPriority, competitionListId, quota, criteriaGroupId) = next.Value;

            using (var insCmd = new NpgsqlCommand(
                "INSERT INTO selectedapplicants (applicantid, admissioncategoryid) VALUES (@Aid, @Cid)",
                conn, tx))
            {
                insCmd.Parameters.AddWithValue("@Aid", applicantId);
                insCmd.Parameters.AddWithValue("@Cid", categoryId);
                await insCmd.ExecuteNonQueryAsync();
            }

            // Quota check: if count > quota, remove worst and add to queue.
            var inCategory = await GetApplicantsInCategoryWithScoresAsync(categoryId, criteriaGroupId, conn, tx);
            if (inCategory.Count > quota)
            {
                var worst = inCategory[^1];
                await RemoveFromSelectedAsync(worst.ApplicantId, categoryId, conn, tx);
                queue.Enqueue((worst.ApplicantId, worst.SelectionPriority + 1));
            }

            // Plan check: if total in competition list > plan, remove worst from lowest-priority category.
            var total = await GetCompetitionListTotalAsync(competitionListId, conn, tx);
            var plan = await GetCompetitionListPlanAsync(competitionListId, conn, tx);
            if (total > plan)
            {
                var worst = await GetWorstInCompetitionListAsync(competitionListId, conn, tx);
                if (worst != null)
                {
                    await RemoveFromSelectedAsync(worst.Value.ApplicantId, worst.Value.CategoryId, conn, tx);
                    queue.Enqueue((worst.Value.ApplicantId, worst.Value.SelectionPriority + 1));
                }
            }
        }

        // Returns the next category to try for the applicant:
        // first by selectionpriority >= minPriority, in a CL where the applicant is not yet selected.
        private async Task<(int CategoryId, int SelectionPriority, int CompetitionListId, int Quota, int CriteriaGroupId)?>
            FindNextCategoryAsync(int applicantId, int minPriority, NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            const string sql = @"
                SELECT aac.admissioncategoryid, aac.selectionpriority,
                       ac.competitionlistid, ac.quota, ac.evaluationcriteriagroupid
                FROM applicantadmissioncategories aac
                JOIN admissioncategories ac ON aac.admissioncategoryid = ac.id
                WHERE aac.applicantid = @ApplicantId
                  AND aac.selectionpriority >= @MinPriority
                  AND ac.competitionlistid NOT IN (
                      SELECT ac2.competitionlistid
                      FROM selectedapplicants sa
                      JOIN admissioncategories ac2 ON sa.admissioncategoryid = ac2.id
                      WHERE sa.applicantid = @ApplicantId
                  )
                ORDER BY aac.selectionpriority ASC
                LIMIT 1";

            using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
            cmd.Parameters.AddWithValue("@MinPriority", minPriority);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
                    reader.GetInt32(3), reader.GetInt32(4));
        }

        // Returns applicants in a category sorted by score DESC (lexicographic by criteria priority), then selectionpriority ASC.
        // Last entry = worst (candidate for removal when quota or plan is exceeded).
        private async Task<List<ApplicantScoreEntry>> GetApplicantsInCategoryWithScoresAsync(
            int categoryId, int criteriaGroupId, NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            // Criteria for this group, ordered by priority ASC (defines sort key order).
            var criteria = new List<int>();
            const string criteriaSQL = @"
                SELECT criteriaid FROM evaluationcriteriagroupitems
                WHERE groupid = @GroupId ORDER BY priority ASC";

            using (var cmd = new NpgsqlCommand(criteriaSQL, conn, tx))
            {
                cmd.Parameters.AddWithValue("@GroupId", criteriaGroupId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    criteria.Add(reader.GetInt32(0));
            }

            // Applicants currently selected in this category, with their selectionpriority for it.
            var applicants = new List<(int ApplicantId, int SelectionPriority)>();
            const string appSQL = @"
                SELECT sa.applicantid, aac.selectionpriority
                FROM selectedapplicants sa
                JOIN applicantadmissioncategories aac
                    ON aac.applicantid = sa.applicantid AND aac.admissioncategoryid = sa.admissioncategoryid
                WHERE sa.admissioncategoryid = @CategoryId";

            using (var cmd = new NpgsqlCommand(appSQL, conn, tx))
            {
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    applicants.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }

            if (applicants.Count == 0)
                return [];

            if (criteria.Count == 0)
                return applicants
                    .Select(a => new ApplicantScoreEntry(a.ApplicantId, a.SelectionPriority, []))
                    .ToList();

            var criteriaIndex = criteria.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
            var scores = applicants.ToDictionary(a => a.ApplicantId, _ => new int[criteria.Count]);

            const string valSQL = @"
                SELECT applicantid, evaluationcriteriaid, value
                FROM applicantevaluationvalues
                WHERE applicantid = ANY(@AppIds) AND evaluationcriteriaid = ANY(@CritIds)";

            using (var cmd = new NpgsqlCommand(valSQL, conn, tx))
            {
                cmd.Parameters.AddWithValue("@AppIds", applicants.Select(a => a.ApplicantId).ToArray());
                cmd.Parameters.AddWithValue("@CritIds", criteria.ToArray());
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int appId = reader.GetInt32(0);
                    int critId = reader.GetInt32(1);
                    int value = reader.GetInt32(2);
                    if (criteriaIndex.TryGetValue(critId, out int idx))
                        scores[appId][idx] = value;
                }
            }

            var result = applicants
                .Select(a => new ApplicantScoreEntry(a.ApplicantId, a.SelectionPriority, scores[a.ApplicantId]))
                .ToList();

            // Sort: score vector DESC (lexicographic), selectionpriority ASC as tiebreaker.
            result.Sort((a, b) =>
            {
                for (int i = 0; i < Math.Min(a.ScoreVector.Length, b.ScoreVector.Length); i++)
                {
                    int cmp = b.ScoreVector[i].CompareTo(a.ScoreVector[i]);
                    if (cmp != 0) return cmp;
                }
                return a.SelectionPriority.CompareTo(b.SelectionPriority);
            });

            return result;
        }

        // Finds the worst applicant in the lowest-priority non-empty category of a competition list.
        private async Task<(int ApplicantId, int CategoryId, int SelectionPriority)?> GetWorstInCompetitionListAsync(
            int competitionListId, NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            // Categories ordered by priority DESC: highest number = lowest priority, checked first.
            const string catSQL = @"
                SELECT id, evaluationcriteriagroupid
                FROM admissioncategories
                WHERE competitionlistid = @ClId
                ORDER BY priority DESC";

            var categories = new List<(int Id, int CriteriaGroupId)>();
            using (var cmd = new NpgsqlCommand(catSQL, conn, tx))
            {
                cmd.Parameters.AddWithValue("@ClId", competitionListId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    categories.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }

            foreach (var (catId, groupId) in categories)
            {
                var applicants = await GetApplicantsInCategoryWithScoresAsync(catId, groupId, conn, tx);
                if (applicants.Count > 0)
                {
                    var worst = applicants[^1];
                    return (worst.ApplicantId, catId, worst.SelectionPriority);
                }
            }

            return null;
        }

        private async Task<int> GetCompetitionListTotalAsync(
            int competitionListId, NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM selectedapplicants sa
                JOIN admissioncategories ac ON sa.admissioncategoryid = ac.id
                WHERE ac.competitionlistid = @ClId";

            using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ClId", competitionListId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<int> GetCompetitionListPlanAsync(
            int competitionListId, NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT plan FROM competitionlists WHERE id = @Id", conn, tx);
            cmd.Parameters.AddWithValue("@Id", competitionListId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task RemoveFromSelectedAsync(
            int applicantId, int categoryId, NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            using var cmd = new NpgsqlCommand(
                "DELETE FROM selectedapplicants WHERE applicantid = @Aid AND admissioncategoryid = @Cid",
                conn, tx);
            cmd.Parameters.AddWithValue("@Aid", applicantId);
            cmd.Parameters.AddWithValue("@Cid", categoryId);
            await cmd.ExecuteNonQueryAsync();
        }

        private record ApplicantScoreEntry(int ApplicantId, int SelectionPriority, int[] ScoreVector);
    }
}
