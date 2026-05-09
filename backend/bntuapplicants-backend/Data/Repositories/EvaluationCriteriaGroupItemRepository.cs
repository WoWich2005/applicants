using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class EvaluationCriteriaGroupItemRepository : IEvaluationCriteriaGroupItemRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public EvaluationCriteriaGroupItemRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<EvaluationCriteriaGroupItem?> CreateAsync(EvaluationCriteriaGroupItem item)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO evaluationcriteriagroupitems (groupid, criteriaid, priority)
                VALUES (@GroupId, @CriteriaId, @Priority)
                RETURNING *
            ";

            EvaluationCriteriaGroupItem? created = null;
            using var tx = await connection.BeginTransactionAsync();

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@GroupId", item.GroupId);
                command.Parameters.AddWithValue("@CriteriaId", item.CriteriaId);
                command.Parameters.AddWithValue("@Priority", item.Priority);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    created = new EvaluationCriteriaGroupItem
                    {
                        Id = reader.GetInt32(0),
                        GroupId = reader.GetInt32(1),
                        CriteriaId = reader.GetInt32(2),
                        Priority = reader.GetInt32(3)
                    };
                }
            }

            if (created != null)
            {
                var entityId = created.Id.ToString();
                await _auditLogger.LogCreateAsync("evaluation_criteria_group_item", entityId, created, tx: (NpgsqlTransaction)tx);
                await tx.CommitAsync();
                return created;
            }

            await tx.RollbackAsync();
            return null;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var before = await GetByIdAsync(id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = "DELETE FROM evaluationcriteriagroupitems WHERE id = @Id";

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

            var entityId = before.Id.ToString();
            await _auditLogger.LogDeleteAsync("evaluation_criteria_group_item", entityId, before, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }

        public async Task<PagedResponse<EvaluationCriteriaGroupItemDto>> GetAllByGroupPagedAsync(
            int groupId, int page, int pageSize,
            string? sortField = null, string? sortOrder = null,
            string? id = null, string? priority = null, string? criteria = null)
        {
            int offset = (page - 1) * pageSize;

            int idVal = 0, priorityVal = 0;
            bool hasId = !string.IsNullOrWhiteSpace(id) && int.TryParse(id, out idVal);
            bool hasPriority = !string.IsNullOrWhiteSpace(priority) && int.TryParse(priority, out priorityVal);
            bool hasCriteria = !string.IsNullOrWhiteSpace(criteria);

            var filterParts = new List<string>();
            if (hasId) filterParts.Add("AND ecgi.id = @IdValue");
            if (hasPriority) filterParts.Add("AND ecgi.priority = @PriorityValue");
            if (hasCriteria) filterParts.Add("AND LOWER(ec.name) LIKE @CriteriaPattern");
            string whereFilters = string.Join(" ", filterParts);

            string orderByColumn = sortField switch
            {
                "id" => "ecgi.id",
                "priority" => "ecgi.priority",
                "criteriaName" => "ec.name",
                _ => "ecgi.priority"
            };
            string direction = sortOrder == "descend" ? "DESC" : "ASC";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            void AddParams(NpgsqlCommand cmd)
            {
                cmd.Parameters.AddWithValue("@GroupId", groupId);
                if (hasId) cmd.Parameters.AddWithValue("@IdValue", idVal);
                if (hasPriority) cmd.Parameters.AddWithValue("@PriorityValue", priorityVal);
                if (hasCriteria) cmd.Parameters.AddWithValue("@CriteriaPattern", $"%{criteria!.ToLower()}%");
            }

            int total;
            using (var countCmd = new NpgsqlCommand($@"
                SELECT COUNT(*) FROM evaluationcriteriagroupitems ecgi
                INNER JOIN evaluationcriteria ec ON ec.id = ecgi.criteriaid
                WHERE ecgi.groupid = @GroupId {whereFilters}", conn))
            {
                AddParams(countCmd);
                total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            var items = new List<EvaluationCriteriaGroupItemDto>();
            using (var cmd = new NpgsqlCommand($@"
                SELECT ecgi.id, ecgi.groupid, ecgi.criteriaid, ecgi.priority, ec.name
                FROM evaluationcriteriagroupitems ecgi
                INNER JOIN evaluationcriteria ec ON ec.id = ecgi.criteriaid
                WHERE ecgi.groupid = @GroupId {whereFilters}
                ORDER BY {orderByColumn} {direction}
                LIMIT @PageSize OFFSET @Offset", conn))
            {
                AddParams(cmd);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new EvaluationCriteriaGroupItemDto
                    {
                        Id = reader.GetInt32(0),
                        GroupId = reader.GetInt32(1),
                        CriteriaId = reader.GetInt32(2),
                        Priority = reader.GetInt32(3),
                        CriteriaName = reader.GetString(4),
                    });
                }
            }

            return new PagedResponse<EvaluationCriteriaGroupItemDto> { Items = items, Total = total };
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

        public async Task<bool> ExistsInGroupAsync(int groupId, int criteriaId, int? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = excludeId.HasValue
                ? "SELECT 1 FROM evaluationcriteriagroupitems WHERE groupid = @GroupId AND criteriaid = @CriteriaId AND id <> @ExcludeId LIMIT 1"
                : "SELECT 1 FROM evaluationcriteriagroupitems WHERE groupid = @GroupId AND criteriaid = @CriteriaId LIMIT 1";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@GroupId", groupId);
            command.Parameters.AddWithValue("@CriteriaId", criteriaId);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync();
        }

        public async Task<bool> UpdateAsync(EvaluationCriteriaGroupItem item)
        {
            var before = await GetByIdAsync(item.Id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            string query = @"
                UPDATE evaluationcriteriagroupitems
                SET groupid = @GroupId, criteriaid = @CriteriaId, priority = @Priority
                WHERE id = @Id
            ";

            using (var command = new NpgsqlCommand(query, connection, (NpgsqlTransaction)tx))
            {
                command.Parameters.AddWithValue("@GroupId", item.GroupId);
                command.Parameters.AddWithValue("@CriteriaId", item.CriteriaId);
                command.Parameters.AddWithValue("@Priority", item.Priority);
                command.Parameters.AddWithValue("@Id", item.Id);

                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            var entityId = item.Id.ToString();
            var diff = JsonDiff.Compute(before, new { item.Id, item.GroupId, item.CriteriaId, item.Priority });
            await _auditLogger.LogUpdateAsync("evaluation_criteria_group_item", entityId, diff, tx: (NpgsqlTransaction)tx);
            await tx.CommitAsync();
            return true;
        }
    }
}
