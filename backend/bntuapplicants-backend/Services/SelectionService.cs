using bntuapplicants_backend.Data.Sql;
using bntuapplicants_backend.Services.Selection;
using Npgsql;

namespace bntuapplicants_backend.Services
{
    public class SelectionService
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public SelectionService(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task RecalculateAllAsync()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                using var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(0)", conn, tx);
                await lockCmd.ExecuteNonQueryAsync();

                var snapshot = await LoadSnapshotAsync(conn, tx);
                var result = FullRecalculationCore.Run(snapshot);

                using var delCmd = new NpgsqlCommand("DELETE FROM selectedapplicants", conn, tx);
                await delCmd.ExecuteNonQueryAsync();

                if (result.Count > 0)
                {
                    using var writer = await conn.BeginBinaryImportAsync(
                        "COPY selectedapplicants (applicantid, admissioncategoryid) FROM STDIN (FORMAT BINARY)");
                    foreach (var sel in result)
                    {
                        await writer.StartRowAsync();
                        await writer.WriteAsync(sel.ApplicantId, NpgsqlTypes.NpgsqlDbType.Integer);
                        await writer.WriteAsync(sel.CategoryId, NpgsqlTypes.NpgsqlDbType.Integer);
                    }
                    await writer.CompleteAsync();
                }

                await _auditLogger.LogRecalculationAsync(result.Count, tx);
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<FullRecalculationCore.Snapshot> LoadSnapshotAsync(NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            // Applications (excluding soft-deleted applicants)
            const string appSQL = $@"
                SELECT aac.applicantid, aac.admissioncategoryid, aac.selectionpriority
                FROM applicantadmissioncategories aac
                JOIN applicants ON aac.applicantid = applicants.id
                WHERE {SoftDelete.NotSoftDeleted}";

            var applications = new List<FullRecalculationCore.Application>();
            using (var cmd = new NpgsqlCommand(appSQL, conn, tx))
            using (var r = await cmd.ExecuteReaderAsync())
                while (await r.ReadAsync())
                    applications.Add(new FullRecalculationCore.Application(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2)));

            // Categories
            const string catSQL = @"
                SELECT id, competitionlistid, quota, priority, evaluationcriteriagroupid
                FROM admissioncategories";

            var categories = new List<FullRecalculationCore.Category>();
            using (var cmd = new NpgsqlCommand(catSQL, conn, tx))
            using (var r = await cmd.ExecuteReaderAsync())
                while (await r.ReadAsync())
                    categories.Add(new FullRecalculationCore.Category(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.GetInt32(4)));

            // Competition list plans
            const string clSQL = "SELECT id, plan FROM competitionlists";
            var clPlans = new Dictionary<int, int>();
            using (var cmd = new NpgsqlCommand(clSQL, conn, tx))
            using (var r = await cmd.ExecuteReaderAsync())
                while (await r.ReadAsync())
                    clPlans[r.GetInt32(0)] = r.GetInt32(1);

            // Criteria by group
            const string criteriaSQL = @"
                SELECT groupid, criteriaid FROM evaluationcriteriagroupitems ORDER BY groupid, priority ASC";

            var criteriaByGroup = new Dictionary<int, List<int>>();
            using (var cmd = new NpgsqlCommand(criteriaSQL, conn, tx))
            using (var r = await cmd.ExecuteReaderAsync())
                while (await r.ReadAsync())
                {
                    int gId = r.GetInt32(0), cId = r.GetInt32(1);
                    if (!criteriaByGroup.ContainsKey(gId)) criteriaByGroup[gId] = new List<int>();
                    criteriaByGroup[gId].Add(cId);
                }

            // Scores
            const string scoreSQL = $@"
                SELECT aev.applicantid, aev.evaluationcriteriaid, aev.value
                FROM applicantevaluationvalues aev
                JOIN applicants ON aev.applicantid = applicants.id
                WHERE {SoftDelete.NotSoftDeleted}";

            var scores = new Dictionary<int, Dictionary<int, int>>();
            using (var cmd = new NpgsqlCommand(scoreSQL, conn, tx))
            using (var r = await cmd.ExecuteReaderAsync())
                while (await r.ReadAsync())
                {
                    int aId = r.GetInt32(0), cId = r.GetInt32(1), val = r.GetInt32(2);
                    if (!scores.ContainsKey(aId)) scores[aId] = new Dictionary<int, int>();
                    scores[aId][cId] = val;
                }

            // Criteria direction
            const string criteriaTypeSQL = "SELECT id, type FROM evaluationcriteria";
            var criteriaHigherIsBetter = new Dictionary<int, bool>();
            using (var cmd = new NpgsqlCommand(criteriaTypeSQL, conn, tx))
            using (var r = await cmd.ExecuteReaderAsync())
                while (await r.ReadAsync())
                    criteriaHigherIsBetter[r.GetInt32(0)] = r.GetString(1) == "higher_is_better";

            return new FullRecalculationCore.Snapshot(
                applications,
                categories,
                clPlans,
                criteriaByGroup.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<int>)kv.Value),
                scores.ToDictionary(kv => kv.Key, kv => (IReadOnlyDictionary<int, int>)kv.Value),
                criteriaHigherIsBetter
            );
        }

    }
}
