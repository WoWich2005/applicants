using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Npgsql;

namespace bntuapplicants_backend.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;
        private readonly IAuditLogger _auditLogger;

        public UserRepository(IConfiguration configuration, IAuditLogger auditLogger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _auditLogger = auditLogger;
        }

        public async Task<bool> AnyExistsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users", connection);
            return Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, username, password_hash, role, is_active, must_change_password, created_at FROM users WHERE id = @Id",
                connection);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapUser(reader);
            return null;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, username, password_hash, role, is_active, must_change_password, created_at FROM users WHERE username = @Username",
                connection);
            cmd.Parameters.AddWithValue("@Username", username);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapUser(reader);
            return null;
        }

        public async Task<(List<UserResponseDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, string? role = null, bool? isActive = null, string? idSearch = null, string? sortField = null, string? sortOrder = null)
        {
            var items = new List<UserResponseDto>();
            int total = 0;
            int offset = (page - 1) * pageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasIdSearch = !string.IsNullOrWhiteSpace(idSearch) && int.TryParse(idSearch.Trim(), out _);

            var conditions = new List<string>();
            if (hasSearch) conditions.Add("LOWER(u.username) LIKE @Search");
            if (role != null) conditions.Add("u.role = @Role");
            if (isActive.HasValue) conditions.Add("u.is_active = @IsActive");
            if (hasIdSearch) conditions.Add("CAST(u.id AS TEXT) LIKE @IdPattern");
            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM users u {whereClause}", connection))
            {
                if (hasSearch) cmd.Parameters.AddWithValue("@Search", $"%{search!.ToLower()}%");
                if (role != null) cmd.Parameters.AddWithValue("@Role", role);
                if (isActive.HasValue) cmd.Parameters.AddWithValue("@IsActive", isActive.Value);
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            string query = $@"
                SELECT u.id, u.username, u.role, u.is_active, u.created_at
                FROM users u
                {whereClause}
                ORDER BY {(sortField == "id" ? $"u.id {(sortOrder == "descend" ? "DESC" : "ASC")}" : "u.username ASC")}
                LIMIT @PageSize OFFSET @Offset";

            using (var cmd = new NpgsqlCommand(query, connection))
            {
                if (hasSearch) cmd.Parameters.AddWithValue("@Search", $"%{search!.ToLower()}%");
                if (role != null) cmd.Parameters.AddWithValue("@Role", role);
                if (isActive.HasValue) cmd.Parameters.AddWithValue("@IsActive", isActive.Value);
                if (hasIdSearch) cmd.Parameters.AddWithValue("@IdPattern", $"%{idSearch!.Trim()}%");
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new UserResponseDto
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Role = reader.GetString(2),
                        IsActive = reader.GetBoolean(3),
                        CreatedAt = reader.GetDateTime(4)
                    });
                }
            }

            return (items, total);
        }

        public async Task<UserResponseDto?> GetDetailedByIdAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT id, username, role, is_active, created_at FROM users WHERE id = @Id";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new UserResponseDto
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Role = reader.GetString(2),
                IsActive = reader.GetBoolean(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }

        public async Task<User> CreateAsync(User user)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            int newId;
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO users (username, password_hash, role, is_active, must_change_password)
                VALUES (@Username, @PasswordHash, @Role, @IsActive, @MustChangePassword)
                RETURNING id", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                cmd.Parameters.AddWithValue("@Role", user.Role);
                cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
                cmd.Parameters.AddWithValue("@MustChangePassword", user.MustChangePassword);
                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            user.Id = newId;

            var created = new
            {
                user.Id,
                user.Username,
                user.Role,
                user.IsActive,
                user.MustChangePassword
            };
            await _auditLogger.LogCreateAsync("user", newId.ToString(), created, tx: transaction);

            await transaction.CommitAsync();
            return user;
        }

        public async Task<bool> UpdateAsync(User user)
        {
            var before = await GetByIdAsync(user.Id);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            int rows;
            using (var cmd = new NpgsqlCommand(@"
                UPDATE users SET username = @Username, role = @Role
                WHERE id = @Id", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@Role", user.Role);
                cmd.Parameters.AddWithValue("@Id", user.Id);
                rows = await cmd.ExecuteNonQueryAsync();
            }

            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                using var cmd = new NpgsqlCommand("UPDATE users SET password_hash = @Hash WHERE id = @Id", connection, transaction);
                cmd.Parameters.AddWithValue("@Hash", user.PasswordHash);
                cmd.Parameters.AddWithValue("@Id", user.Id);
                await cmd.ExecuteNonQueryAsync();
            }

            if (rows > 0)
            {
                var afterSnapshot = new { user.Id, user.Username, user.Role };
                var beforeSnapshot = before == null ? (object)new { } : new { before.Id, before.Username, before.Role };
                var diff = JsonDiff.Compute(beforeSnapshot, afterSnapshot);
                await _auditLogger.LogUpdateAsync("user", user.Id.ToString(), diff, tx: transaction);
            }

            await transaction.CommitAsync();
            return rows > 0;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new NpgsqlCommand("UPDATE users SET is_active = NOT is_active WHERE id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var before = await GetByIdAsync(id);
            if (before == null) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            using (var cmd = new NpgsqlCommand("DELETE FROM users WHERE id = @Id", connection, tx))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            await _auditLogger.LogDeleteAsync("user", id.ToString(), before, tx: tx);
            await tx.CommitAsync();
            return true;
        }

        public async Task<int> CountByRoleAsync(string role)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE role = @Role", connection);
            cmd.Parameters.AddWithValue("@Role", role);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<bool> ChangePasswordAsync(int id, string newPasswordHash)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE users SET password_hash = @Hash, must_change_password = false WHERE id = @Id",
                connection);
            cmd.Parameters.AddWithValue("@Hash", newPasswordHash);
            cmd.Parameters.AddWithValue("@Id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private static User MapUser(NpgsqlDataReader reader) => new()
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            Role = reader.GetString(3),
            IsActive = reader.GetBoolean(4),
            MustChangePassword = reader.GetBoolean(5),
            CreatedAt = reader.GetDateTime(6)
        };
    }
}
