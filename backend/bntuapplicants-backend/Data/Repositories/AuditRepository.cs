using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly string _connectionString;

        private const string NotSoftDeleted = @"NOT EXISTS (
            SELECT 1 FROM applicant_deletion_requests dr
            WHERE dr.applicant_id = a.id AND dr.status IN ('pending','confirmed'))";

        public AuditRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<PagedResponse<AuditLogEntry>> GetEntityHistoryAsync(string entityType, string entityId, int page, int pageSize, string? username = null, string? action = null, string? logEntityType = null, DateTime? from = null, DateTime? to = null, string? sortOrder = null)
        {
            int offset = (page - 1) * pageSize;
            var items = new List<AuditLogEntry>();
            int total = 0;

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            string baseClause;
            if (entityType == "applicant")
            {
                baseClause = @"(
                    (entity_type = @EntityType AND entity_id = @EntityId)
                    OR (entity_type = 'applicant_evaluation_value'
                        AND entity_id IN (
                            SELECT id::text FROM applicantevaluationvalues WHERE applicantid = @ApplicantIdInt
                            UNION
                            SELECT al2.entity_id FROM audit_log al2
                            WHERE al2.entity_type = 'applicant_evaluation_value'
                            AND (al2.changes->'after'->>'applicantId' = @ApplicantIdStr
                                OR al2.changes->'before'->>'applicantId' = @ApplicantIdStr)
                        ))
                    OR (entity_type = 'applicant_admission_category'
                        AND entity_id IN (
                            SELECT id::text FROM applicantadmissioncategories WHERE applicantid = @ApplicantIdInt
                            UNION
                            SELECT al3.entity_id FROM audit_log al3
                            WHERE al3.entity_type = 'applicant_admission_category'
                            AND (al3.changes->'after'->>'applicantId' = @ApplicantIdStr
                                OR al3.changes->'before'->>'applicantId' = @ApplicantIdStr)
                        ))
                )";
            }
            else if (entityType == "specialty")
            {
                baseClause = @"(
                    (entity_type = @EntityType AND entity_id = @EntityId)
                    OR (entity_type = 'competition_list'
                        AND entity_id IN (
                            SELECT id::text FROM competitionlists WHERE specialtyid = @SpecialtyIdInt
                            UNION
                            SELECT al2.entity_id FROM audit_log al2
                            WHERE al2.entity_type = 'competition_list'
                            AND (al2.changes->'after'->>'specialtyId' = @SpecialtyIdStr
                                OR al2.changes->'before'->>'specialtyId' = @SpecialtyIdStr)
                        ))
                )";
            }
            else if (entityType == "competition_list")
            {
                baseClause = @"(
                    (entity_type = @EntityType AND entity_id = @EntityId)
                    OR (entity_type = 'admission_category'
                        AND entity_id IN (
                            SELECT id::text FROM admissioncategories WHERE competitionlistid = @CompetitionListIdInt
                            UNION
                            SELECT al2.entity_id FROM audit_log al2
                            WHERE al2.entity_type = 'admission_category'
                            AND (al2.changes->'after'->>'competitionListId' = @CompetitionListIdStr
                                OR al2.changes->'before'->>'competitionListId' = @CompetitionListIdStr)
                        ))
                )";
            }
            else if (entityType == "evaluation_criteria_group")
            {
                baseClause = @"(
                    (entity_type = @EntityType AND entity_id = @EntityId)
                    OR (entity_type = 'evaluation_criteria_group_item'
                        AND entity_id IN (
                            SELECT id::text FROM evaluationcriteriagroupitems WHERE groupid = @GroupIdInt
                            UNION
                            SELECT al2.entity_id FROM audit_log al2
                            WHERE al2.entity_type = 'evaluation_criteria_group_item'
                            AND (al2.changes->'after'->>'groupId' = @GroupIdStr
                                OR al2.changes->'before'->>'groupId' = @GroupIdStr)
                        ))
                )";
            }
            else
            {
                baseClause = "entity_type = @EntityType AND entity_id = @EntityId";
            }

            var extraConditions = new List<string>();
            if (!string.IsNullOrEmpty(username)) extraConditions.Add("LOWER(username) LIKE @UsernamePattern");
            if (!string.IsNullOrEmpty(action)) extraConditions.Add("action = @Action");
            if (!string.IsNullOrEmpty(logEntityType)) extraConditions.Add("entity_type = @LogEntityType");
            if (from.HasValue) extraConditions.Add("created_at >= @From");
            if (to.HasValue) extraConditions.Add("created_at <= @To");

            string whereClause = extraConditions.Count > 0
                ? baseClause + " AND " + string.Join(" AND ", extraConditions)
                : baseClause;
            string orderDir = sortOrder == "ascend" ? "ASC" : "DESC";

            void AddParams(NpgsqlCommand cmd)
            {
                cmd.Parameters.AddWithValue("@EntityType", entityType);
                cmd.Parameters.AddWithValue("@EntityId", entityId);
                if (entityType == "applicant")
                {
                    cmd.Parameters.AddWithValue("@ApplicantIdInt", int.Parse(entityId));
                    cmd.Parameters.AddWithValue("@ApplicantIdStr", entityId);
                }
                if (entityType == "specialty")
                {
                    cmd.Parameters.AddWithValue("@SpecialtyIdInt", int.Parse(entityId));
                    cmd.Parameters.AddWithValue("@SpecialtyIdStr", entityId);
                }
                if (entityType == "competition_list")
                {
                    cmd.Parameters.AddWithValue("@CompetitionListIdInt", int.Parse(entityId));
                    cmd.Parameters.AddWithValue("@CompetitionListIdStr", entityId);
                }
                if (entityType == "evaluation_criteria_group")
                {
                    cmd.Parameters.AddWithValue("@GroupIdInt", int.Parse(entityId));
                    cmd.Parameters.AddWithValue("@GroupIdStr", entityId);
                }
                if (!string.IsNullOrEmpty(username))
                    cmd.Parameters.AddWithValue("@UsernamePattern", $"%{username.ToLower()}%");
                if (!string.IsNullOrEmpty(action))
                    cmd.Parameters.AddWithValue("@Action", action);
                if (!string.IsNullOrEmpty(logEntityType))
                    cmd.Parameters.AddWithValue("@LogEntityType", logEntityType);
                if (from.HasValue)
                    cmd.Parameters.AddWithValue("@From", from.Value);
                if (to.HasValue)
                    cmd.Parameters.AddWithValue("@To", to.Value);
            }

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM audit_log WHERE {whereClause}", conn))
            {
                AddParams(cmd);
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var selectSql = $@"
                SELECT id, created_at, user_id, username, action, entity_type, entity_id, changes::text
                FROM audit_log
                WHERE {whereClause}
                ORDER BY created_at {orderDir}
                LIMIT @PageSize OFFSET @Offset";

            using var selectCmd = new NpgsqlCommand(selectSql, conn);
            AddParams(selectCmd);
            selectCmd.Parameters.AddWithValue("@PageSize", pageSize);
            selectCmd.Parameters.AddWithValue("@Offset", offset);

            using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                items.Add(MapAuditEntry(reader));

            return new PagedResponse<AuditLogEntry> { Items = items, Total = total };
        }

        public async Task<PagedResponse<AuditLogEntry>> GetAuditLogPagedAsync(int page, int pageSize, string? username = null, string? entityType = null, string? action = null, string? entityId = null, DateTime? from = null, DateTime? to = null, string? sortOrder = null)
        {
            int offset = (page - 1) * pageSize;
            var items = new List<AuditLogEntry>();
            int total = 0;

            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(username)) conditions.Add("LOWER(username) LIKE @UsernamePattern");
            if (!string.IsNullOrEmpty(entityType)) conditions.Add("entity_type = @EntityType");
            if (!string.IsNullOrEmpty(action)) conditions.Add("action = @Action");
            if (!string.IsNullOrEmpty(entityId)) conditions.Add("entity_id = @EntityId");
            if (from.HasValue) conditions.Add("created_at >= @From");
            if (to.HasValue) conditions.Add("created_at <= @To");

            string where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            string orderDir = sortOrder == "ascend" ? "ASC" : "DESC";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM audit_log {where}", conn))
            {
                AddFilterParams(cmd, username, entityType, action, entityId, from, to);
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var selectSql = $@"
                SELECT id, created_at, user_id, username, action, entity_type, entity_id, changes::text
                FROM audit_log {where}
                ORDER BY created_at {orderDir}
                LIMIT @PageSize OFFSET @Offset";

            using var selectCmd = new NpgsqlCommand(selectSql, conn);
            AddFilterParams(selectCmd, username, entityType, action, entityId, from, to);
            selectCmd.Parameters.AddWithValue("@PageSize", pageSize);
            selectCmd.Parameters.AddWithValue("@Offset", offset);

            using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                items.Add(MapAuditEntry(reader));

            return new PagedResponse<AuditLogEntry> { Items = items, Total = total };
        }

        public async Task<PagedResponse<AuthLogEntry>> GetAuthLogPagedAsync(int page, int pageSize, string? userId = null, string? username = null, string? eventType = null, string? ipAddress = null, string? failureReason = null, string? userAgent = null, DateTime? from = null, DateTime? to = null, string? sortOrder = null)
        {
            int offset = (page - 1) * pageSize;
            var items = new List<AuthLogEntry>();
            int total = 0;

            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(userId)) conditions.Add("CAST(user_id AS TEXT) ILIKE @UserId");
            if (!string.IsNullOrEmpty(username)) conditions.Add("username ILIKE @Username");
            if (!string.IsNullOrEmpty(eventType)) conditions.Add("event_type = @EventType");
            if (!string.IsNullOrEmpty(ipAddress)) conditions.Add("host(ip_address) ILIKE @IpAddress");
            if (!string.IsNullOrEmpty(failureReason)) conditions.Add("failure_reason = @FailureReason");
            if (!string.IsNullOrEmpty(userAgent)) conditions.Add("user_agent ILIKE @UserAgent");
            if (from.HasValue) conditions.Add("created_at >= @From");
            if (to.HasValue) conditions.Add("created_at <= @To");

            string where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            string orderDir = sortOrder == "ascend" ? "ASC" : "DESC";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            void AddParams(NpgsqlCommand cmd)
            {
                if (!string.IsNullOrEmpty(userId)) cmd.Parameters.AddWithValue("@UserId", $"%{userId}%");
                if (!string.IsNullOrEmpty(username)) cmd.Parameters.AddWithValue("@Username", $"%{username}%");
                if (!string.IsNullOrEmpty(eventType)) cmd.Parameters.AddWithValue("@EventType", eventType);
                if (!string.IsNullOrEmpty(ipAddress)) cmd.Parameters.AddWithValue("@IpAddress", $"%{ipAddress}%");
                if (!string.IsNullOrEmpty(failureReason)) cmd.Parameters.AddWithValue("@FailureReason", failureReason);
                if (!string.IsNullOrEmpty(userAgent)) cmd.Parameters.AddWithValue("@UserAgent", $"%{userAgent}%");
                if (from.HasValue) cmd.Parameters.AddWithValue("@From", from.Value);
                if (to.HasValue) cmd.Parameters.AddWithValue("@To", to.Value);
            }

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM auth_log {where}", conn))
            {
                AddParams(cmd);
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var selectSql = $@"
                SELECT id, created_at, user_id, username, event_type, failure_reason, host(ip_address), user_agent
                FROM auth_log {where}
                ORDER BY created_at {orderDir}
                LIMIT @PageSize OFFSET @Offset";

            using var selectCmd = new NpgsqlCommand(selectSql, conn);
            AddParams(selectCmd);
            selectCmd.Parameters.AddWithValue("@PageSize", pageSize);
            selectCmd.Parameters.AddWithValue("@Offset", offset);

            using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new AuthLogEntry
                {
                    Id = reader.GetInt64(0),
                    CreatedAt = reader.GetDateTime(1),
                    UserId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Username = reader.IsDBNull(3) ? null : reader.GetString(3),
                    EventType = reader.GetString(4),
                    FailureReason = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IpAddress = reader.IsDBNull(6) ? null : reader.GetString(6),
                    UserAgent = reader.IsDBNull(7) ? null : reader.GetString(7),
                });
            }

            return new PagedResponse<AuthLogEntry> { Items = items, Total = total };
        }

        public async Task<ValidationStatusDto?> GetValidationStatusAsync(int applicantId)
        {
            const string sql = "SELECT id, validated FROM applicants WHERE id = @ApplicantId";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ApplicantId", applicantId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new ValidationStatusDto
            {
                ApplicantId = reader.GetInt32(0),
                Validated = reader.GetBoolean(1),
            };
        }

        public async Task ValidateApplicantAsync(int applicantId)
        {
            const string sql = "UPDATE applicants SET validated = true WHERE id = @ApplicantId";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InvalidateApplicantAsync(int applicantId)
        {
            const string sql = "UPDATE applicants SET validated = false WHERE id = @ApplicantId";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<(bool canValidate, List<string> missingCriteria, bool invalidPriorities)> CheckValidationBlockersAsync(int applicantId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Missing criteria
            const string missingCriteriaSql = @"
                SELECT ec.name
                FROM (
                    SELECT DISTINCT ecgi.criteriaid
                    FROM applicantadmissioncategories aac
                    JOIN admissioncategories ac ON ac.id = aac.admissioncategoryid
                    JOIN evaluationcriteriagroupitems ecgi ON ecgi.groupid = ac.evaluationcriteriagroupid
                    WHERE aac.applicantid = @ApplicantId
                    EXCEPT
                    SELECT aev.evaluationcriteriaid
                    FROM applicantevaluationvalues aev
                    WHERE aev.applicantid = @ApplicantId
                ) missing
                JOIN evaluationcriteria ec ON ec.id = missing.criteriaid";

            var missingCriteria = new List<string>();
            using (var cmd = new NpgsqlCommand(missingCriteriaSql, conn))
            {
                cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    missingCriteria.Add(reader.GetString(0));
            }

            // Priority check
            const string prioritySql = @"
                SELECT COUNT(*) AS total, COALESCE(MAX(selectionpriority), 0) AS max_prio, COALESCE(MIN(selectionpriority), 0) AS min_prio
                FROM applicantadmissioncategories
                WHERE applicantid = @ApplicantId";

            bool invalidPriorities = false;
            using (var cmd = new NpgsqlCommand(prioritySql, conn))
            {
                cmd.Parameters.AddWithValue("@ApplicantId", applicantId);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    long total = reader.GetInt64(0);
                    long maxPrio = reader.GetInt64(1);
                    long minPrio = reader.GetInt64(2);
                    invalidPriorities = total > 0 && (maxPrio != total || minPrio != 1);
                }
            }

            return (!missingCriteria.Any() && !invalidPriorities, missingCriteria, invalidPriorities);
        }

        public async Task<AlertsSummaryDto> GetAlertsSummaryAsync()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = $@"
                SELECT
                    (SELECT COUNT(DISTINCT a.id)
                     FROM applicants a
                     WHERE a.validated = false AND {NotSoftDeleted}
                    ) AS unvalidated_count,

                    (SELECT COUNT(DISTINCT a.id)
                     FROM applicants a
                     WHERE EXISTS (
                         SELECT 1
                         FROM applicantadmissioncategories aac3
                         JOIN admissioncategories ac3 ON ac3.id = aac3.admissioncategoryid
                         JOIN evaluationcriteriagroupitems ecgi ON ecgi.groupid = ac3.evaluationcriteriagroupid
                         WHERE aac3.applicantid = a.id
                         AND ecgi.criteriaid NOT IN (
                             SELECT aev.evaluationcriteriaid FROM applicantevaluationvalues aev WHERE aev.applicantid = a.id
                         )
                     )
                    ) AS missing_criteria_count,

                    (SELECT COUNT(DISTINCT a.id)
                     FROM applicants a
                     WHERE EXISTS (
                         SELECT 1 FROM applicantadmissioncategories sub
                         WHERE sub.applicantid = a.id
                         GROUP BY sub.applicantid
                         HAVING COUNT(*) > 0
                           AND (MAX(sub.selectionpriority) != COUNT(*) OR MIN(sub.selectionpriority) != 1)
                     )
                    ) AS invalid_priorities_count,

                    (SELECT COUNT(DISTINCT a.id)
                     FROM applicants a
                     WHERE (
                         EXISTS (
                             SELECT 1
                             FROM applicantadmissioncategories aac6
                             JOIN admissioncategories ac6 ON ac6.id = aac6.admissioncategoryid
                             JOIN evaluationcriteriagroupitems ecgi6 ON ecgi6.groupid = ac6.evaluationcriteriagroupid
                             WHERE aac6.applicantid = a.id
                             AND ecgi6.criteriaid NOT IN (
                                 SELECT aev6.evaluationcriteriaid FROM applicantevaluationvalues aev6 WHERE aev6.applicantid = a.id
                             )
                         )
                         OR EXISTS (
                             SELECT 1 FROM applicantadmissioncategories sub6
                             WHERE sub6.applicantid = a.id
                             GROUP BY sub6.applicantid
                             HAVING MAX(sub6.selectionpriority) != COUNT(*) OR MIN(sub6.selectionpriority) != 1
                         )
                     )
                    ) AS incomplete_count,

                    (SELECT COUNT(*) FROM (
                        SELECT g.id FROM evaluationcriteriagroups g
                        INNER JOIN evaluationcriteriagroupitems i ON i.groupid = g.id
                        GROUP BY g.id
                        HAVING MIN(i.priority) != 1
                            OR MAX(i.priority) != COUNT(*)
                            OR COUNT(DISTINCT i.priority) != COUNT(*)
                    ) sub
                    ) AS invalid_criteria_groups_count,

                    (SELECT COUNT(*) FROM (
                        SELECT cl.id FROM competitionlists cl
                        INNER JOIN admissioncategories ac ON ac.competitionlistid = cl.id
                        GROUP BY cl.id
                        HAVING MIN(ac.priority) != 1
                            OR MAX(ac.priority) != COUNT(*)
                            OR COUNT(DISTINCT ac.priority) != COUNT(*)
                    ) sub
                    ) AS invalid_admission_categories_count,

                    (SELECT COUNT(*) FROM applicant_deletion_requests WHERE status = 'pending'
                    ) AS pending_deletions_count";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new AlertsSummaryDto
                {
                    UnvalidatedCount = Convert.ToInt32(reader.GetInt64(0)),
                    MissingCriteriaCount = Convert.ToInt32(reader.GetInt64(1)),
                    InvalidPrioritiesCount = Convert.ToInt32(reader.GetInt64(2)),
                    IncompleteCount = Convert.ToInt32(reader.GetInt64(3)),
                    InvalidCriteriaGroupsCount = Convert.ToInt32(reader.GetInt64(4)),
                    InvalidAdmissionCategoriesCount = Convert.ToInt32(reader.GetInt64(5)),
                    PendingDeletionsCount = Convert.ToInt32(reader.GetInt64(6))
                };
            }

            return new AlertsSummaryDto();
        }

        public async Task<PagedResponse<UnvalidatedApplicantDto>> GetUnvalidatedApplicantsAsync(int page, int pageSize, string? status = null, string? search = null)
        {
            int offset = (page - 1) * pageSize;
            var items = new List<UnvalidatedApplicantDto>();
            int total = 0;

            string statusFilter = status == "validated" ? "AND a.validated = true"
                : status == "unvalidated" ? "AND a.validated = false"
                : "";
            string searchFilter = !string.IsNullOrWhiteSpace(search) ? "AND (LOWER(a.name) LIKE @SearchPattern OR LOWER(a.externalid) LIKE @SearchPattern)" : "";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var countSql = $@"SELECT COUNT(DISTINCT a.id)
                FROM applicants a
                WHERE {NotSoftDeleted} {statusFilter} {searchFilter}";

            using (var cmd = new NpgsqlCommand(countSql, conn))
            {
                if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search.ToLower()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var selectSql = $@"
                SELECT DISTINCT a.id, a.externalid, a.name, a.validated
                FROM applicants a
                WHERE {NotSoftDeleted} {statusFilter} {searchFilter}
                ORDER BY a.name
                LIMIT @PageSize OFFSET @Offset";

            using var selectCmd = new NpgsqlCommand(selectSql, conn);
            if (!string.IsNullOrWhiteSpace(search)) selectCmd.Parameters.AddWithValue("@SearchPattern", $"%{search.ToLower()}%");
            selectCmd.Parameters.AddWithValue("@PageSize", pageSize);
            selectCmd.Parameters.AddWithValue("@Offset", offset);

            using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new UnvalidatedApplicantDto
                {
                    Id = reader.GetInt32(0),
                    ExternalId = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Name = reader.GetString(2),
                    Validated = reader.GetBoolean(3),
                });
            }

            return new PagedResponse<UnvalidatedApplicantDto> { Items = items, Total = total };
        }

        public async Task<PagedResponse<IncompleteApplicantDto>> GetIncompleteApplicantsAsync(
            int page, int pageSize, string? search = null)
        {
            int offset = (page - 1) * pageSize;
            string searchFilter = !string.IsNullOrWhiteSpace(search)
                ? "AND (LOWER(a.name) LIKE @SearchPattern OR LOWER(a.externalid) LIKE @SearchPattern)"
                : "";

            string whereClause = $@"WHERE (
                    EXISTS (
                        SELECT 1
                        FROM applicantadmissioncategories aac
                        JOIN admissioncategories ac ON ac.id = aac.admissioncategoryid
                        JOIN evaluationcriteriagroupitems ecgi ON ecgi.groupid = ac.evaluationcriteriagroupid
                        WHERE aac.applicantid = a.id
                        AND ecgi.criteriaid NOT IN (
                            SELECT aev.evaluationcriteriaid FROM applicantevaluationvalues aev WHERE aev.applicantid = a.id
                        )
                    )
                    OR EXISTS (
                        SELECT 1 FROM applicantadmissioncategories sub2
                        WHERE sub2.applicantid = a.id
                        GROUP BY sub2.applicantid
                        HAVING MAX(sub2.selectionpriority) != COUNT(*) OR MIN(sub2.selectionpriority) != 1
                    )
                ) {searchFilter}";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var countSql = $"SELECT COUNT(DISTINCT a.id) FROM applicants a {whereClause}";
            int total;
            using (var countCmd = new NpgsqlCommand(countSql, conn))
            {
                if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("@SearchPattern", $"%{search.ToLower()}%");
                total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            var selectSql = $@"
                SELECT DISTINCT a.id, a.externalid, a.name,
                    (SELECT COUNT(*) > 0
                     FROM applicantadmissioncategories sub
                     WHERE sub.applicantid = a.id
                     GROUP BY sub.applicantid
                     HAVING COUNT(*) > 0 AND (MAX(sub.selectionpriority) != COUNT(*) OR MIN(sub.selectionpriority) != 1)
                    ) AS has_invalid_priorities
                FROM applicants a
                {whereClause}
                ORDER BY a.name
                LIMIT @PageSize OFFSET @Offset";

            var applicantIds = new List<(int id, string externalId, string name, bool invalidPriorities)>();
            using (var cmd = new NpgsqlCommand(selectSql, conn))
            {
                if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search.ToLower()}%");
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    applicantIds.Add((
                        reader.GetInt32(0),
                        reader.IsDBNull(1) ? "" : reader.GetString(1),
                        reader.GetString(2),
                        !reader.IsDBNull(3) && reader.GetBoolean(3)
                    ));
                }
            }

            var items = new List<IncompleteApplicantDto>();
            foreach (var (id, externalId, name, invalidPriorities) in applicantIds)
            {
                var (_, missingCriteria, _) = await CheckValidationBlockersAsync(id);
                items.Add(new IncompleteApplicantDto
                {
                    Id = id,
                    ExternalId = externalId,
                    Name = name,
                    MissingCriteria = missingCriteria,
                    HasInvalidPriorities = invalidPriorities,
                });
            }

            return new PagedResponse<IncompleteApplicantDto> { Items = items, Total = total };
        }

        public async Task<PagedResponse<InvalidCriteriaGroupDto>> GetInvalidCriteriaGroupsAsync(
            int page, int pageSize, string? search = null)
        {
            int offset = (page - 1) * pageSize;
            string searchFilter = !string.IsNullOrWhiteSpace(search)
                ? "AND LOWER(g.name) LIKE @SearchPattern"
                : "";

            string havingClause = @"HAVING MIN(i.priority) != 1
                    OR MAX(i.priority) != COUNT(*)
                    OR COUNT(DISTINCT i.priority) != COUNT(*)";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var countSql = $@"
                SELECT COUNT(*) FROM (
                    SELECT g.id FROM evaluationcriteriagroups g
                    INNER JOIN evaluationcriteriagroupitems i ON i.groupid = g.id
                    WHERE 1=1 {searchFilter}
                    GROUP BY g.id, g.name
                    {havingClause}
                ) sub";

            int total;
            using (var countCmd = new NpgsqlCommand(countSql, conn))
            {
                if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("@SearchPattern", $"%{search.ToLower()}%");
                total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            var selectSql = $@"
                SELECT g.id, g.name
                FROM evaluationcriteriagroups g
                INNER JOIN evaluationcriteriagroupitems i ON i.groupid = g.id
                WHERE 1=1 {searchFilter}
                GROUP BY g.id, g.name
                {havingClause}
                ORDER BY g.name
                LIMIT @PageSize OFFSET @Offset";

            var items = new List<InvalidCriteriaGroupDto>();
            using (var cmd = new NpgsqlCommand(selectSql, conn))
            {
                if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search.ToLower()}%");
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new InvalidCriteriaGroupDto
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            }

            return new PagedResponse<InvalidCriteriaGroupDto> { Items = items, Total = total };
        }

        public async Task<PagedResponse<InvalidCompetitionListDto>> GetInvalidAdmissionCategoriesAsync(
            int page, int pageSize, string? search = null, string? faculty = null, string? department = null, string? specialty = null)
        {
            int offset = (page - 1) * pageSize;

            var filterParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) filterParts.Add("AND LOWER(cl.name) LIKE @SearchPattern");
            if (!string.IsNullOrWhiteSpace(faculty)) filterParts.Add("AND LOWER(f.name) LIKE @FacultyPattern");
            if (!string.IsNullOrWhiteSpace(department)) filterParts.Add("AND LOWER(d.name) LIKE @DepartmentPattern");
            if (!string.IsNullOrWhiteSpace(specialty)) filterParts.Add("AND LOWER(s.name) LIKE @SpecialtyPattern");
            string whereFilters = string.Join(" ", filterParts);

            string havingClause = @"HAVING MIN(ac.priority) != 1
                    OR MAX(ac.priority) != COUNT(*)
                    OR COUNT(DISTINCT ac.priority) != COUNT(*)";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var countSql = $@"
                SELECT COUNT(*) FROM (
                    SELECT cl.id FROM competitionlists cl
                    INNER JOIN admissioncategories ac ON ac.competitionlistid = cl.id
                    INNER JOIN specialties s ON s.id = cl.specialtyid
                    INNER JOIN departments d ON d.id = s.departmentid
                    INNER JOIN faculties f ON f.id = d.facultyid
                    WHERE 1=1 {whereFilters}
                    GROUP BY cl.id, cl.name, f.name, d.name, s.name
                    {havingClause}
                ) sub";

            int total;
            using (var countCmd = new NpgsqlCommand(countSql, conn))
            {
                AddInvalidAdmissionCategoryParams(countCmd, search, faculty, department, specialty);
                total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            var selectSql = $@"
                SELECT cl.id, cl.name, f.name, d.name, s.name
                FROM competitionlists cl
                INNER JOIN admissioncategories ac ON ac.competitionlistid = cl.id
                INNER JOIN specialties s ON s.id = cl.specialtyid
                INNER JOIN departments d ON d.id = s.departmentid
                INNER JOIN faculties f ON f.id = d.facultyid
                WHERE 1=1 {whereFilters}
                GROUP BY cl.id, cl.name, f.name, d.name, s.name
                {havingClause}
                ORDER BY f.name, d.name, s.name, cl.name
                LIMIT @PageSize OFFSET @Offset";

            var items = new List<InvalidCompetitionListDto>();
            using (var cmd = new NpgsqlCommand(selectSql, conn))
            {
                AddInvalidAdmissionCategoryParams(cmd, search, faculty, department, specialty);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new InvalidCompetitionListDto
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        FacultyName = reader.GetString(2),
                        DepartmentName = reader.GetString(3),
                        SpecialtyName = reader.GetString(4),
                    });
                }
            }

            return new PagedResponse<InvalidCompetitionListDto> { Items = items, Total = total };
        }

        private static void AddInvalidAdmissionCategoryParams(NpgsqlCommand cmd, string? search, string? faculty, string? department, string? specialty)
        {
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@SearchPattern", $"%{search.ToLower()}%");
            if (!string.IsNullOrWhiteSpace(faculty)) cmd.Parameters.AddWithValue("@FacultyPattern", $"%{faculty.ToLower()}%");
            if (!string.IsNullOrWhiteSpace(department)) cmd.Parameters.AddWithValue("@DepartmentPattern", $"%{department.ToLower()}%");
            if (!string.IsNullOrWhiteSpace(specialty)) cmd.Parameters.AddWithValue("@SpecialtyPattern", $"%{specialty.ToLower()}%");
        }

        private static void AddFilterParams(NpgsqlCommand cmd, string? username, string? entityType, string? action, string? entityId, DateTime? from, DateTime? to)
        {
            if (!string.IsNullOrEmpty(username)) cmd.Parameters.AddWithValue("@UsernamePattern", $"%{username.ToLower()}%");
            if (!string.IsNullOrEmpty(entityType)) cmd.Parameters.AddWithValue("@EntityType", entityType);
            if (!string.IsNullOrEmpty(action)) cmd.Parameters.AddWithValue("@Action", action);
            if (!string.IsNullOrEmpty(entityId)) cmd.Parameters.AddWithValue("@EntityId", entityId);
            if (from.HasValue) cmd.Parameters.AddWithValue("@From", from.Value);
            if (to.HasValue) cmd.Parameters.AddWithValue("@To", to.Value);
        }

        private static AuditLogEntry MapAuditEntry(NpgsqlDataReader reader) => new()
        {
            Id = reader.GetInt64(0),
            CreatedAt = reader.GetDateTime(1),
            UserId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
            Username = reader.IsDBNull(3) ? null : reader.GetString(3),
            Action = reader.GetString(4),
            EntityType = reader.GetString(5),
            EntityId = reader.GetString(6),
            Changes = reader.IsDBNull(7) ? null : reader.GetString(7),
        };
    }
}
