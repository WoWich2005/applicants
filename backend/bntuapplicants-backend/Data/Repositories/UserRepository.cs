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
                "SELECT id, username, password_hash, role, faculty_id, is_active, must_change_password, created_at FROM users WHERE id = @Id",
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
                "SELECT id, username, password_hash, role, faculty_id, is_active, must_change_password, created_at FROM users WHERE username = @Username",
                connection);
            cmd.Parameters.AddWithValue("@Username", username);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapUser(reader);
            return null;
        }

        public async Task<(List<UserResponseDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, string? role = null, bool? isActive = null, string? idSearch = null)
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
                SELECT u.id, u.username, u.role, u.faculty_id, f.name as faculty_name,
                       u.is_active, u.created_at
                FROM users u
                LEFT JOIN faculties f ON u.faculty_id = f.id
                {whereClause}
                ORDER BY u.username ASC
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
                        FacultyId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        FacultyName = reader.IsDBNull(4) ? null : reader.GetString(4),
                        IsActive = reader.GetBoolean(5),
                        CreatedAt = reader.GetDateTime(6)
                    });
                }
            }

            foreach (var item in items)
            {
                item.SpecialtyIds = await GetSpecialtyIdsAsync(item.Id, connection);
                item.FacultyAccessIds = await GetFacultyAccessIdsAsync(item.Id, connection);
            }

            return (items, total);
        }

        public async Task<UserResponseDto?> GetDetailedByIdAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT u.id, u.username, u.role, u.faculty_id, f.name as faculty_name,
                       u.is_active, u.created_at
                FROM users u
                LEFT JOIN faculties f ON u.faculty_id = f.id
                WHERE u.id = @Id";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var dto = new UserResponseDto
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Role = reader.GetString(2),
                FacultyId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                FacultyName = reader.IsDBNull(4) ? null : reader.GetString(4),
                IsActive = reader.GetBoolean(5),
                CreatedAt = reader.GetDateTime(6)
            };
            reader.Close();

            dto.SpecialtyIds = await GetSpecialtyIdsAsync(id, connection);
            dto.FacultyAccessIds = await GetFacultyAccessIdsAsync(id, connection);

            return dto;
        }

        public async Task<User> CreateAsync(User user, List<int> specialtyIds, List<int> facultyAccessIds)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            int newId;
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO users (username, password_hash, role, faculty_id, is_active, must_change_password)
                VALUES (@Username, @PasswordHash, @Role, @FacultyId, @IsActive, @MustChangePassword)
                RETURNING id", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                cmd.Parameters.AddWithValue("@Role", user.Role);
                cmd.Parameters.AddWithValue("@FacultyId", user.FacultyId.HasValue ? user.FacultyId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
                cmd.Parameters.AddWithValue("@MustChangePassword", user.MustChangePassword);
                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            await SetSpecialtyAccessAsync(newId, specialtyIds, connection, transaction);
            await SetFacultyAccessAsync(newId, facultyAccessIds, connection, transaction);

            user.Id = newId;

            var created = new
            {
                user.Id,
                user.Username,
                user.Role,
                user.FacultyId,
                user.IsActive,
                user.MustChangePassword,
                SpecialtyIds = specialtyIds,
                FacultyAccessIds = facultyAccessIds
            };
            await _auditLogger.LogCreateAsync("user", newId.ToString(), created, tx: transaction);

            await transaction.CommitAsync();
            return user;
        }

        public async Task<bool> UpdateAsync(User user, List<int> specialtyIds, List<int> facultyAccessIds)
        {
            var before = await GetByIdAsync(user.Id);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            int rows;
            using (var cmd = new NpgsqlCommand(@"
                UPDATE users SET username = @Username, role = @Role, faculty_id = @FacultyId
                WHERE id = @Id", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@Role", user.Role);
                cmd.Parameters.AddWithValue("@FacultyId", user.FacultyId.HasValue ? user.FacultyId.Value : DBNull.Value);
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

            await SetSpecialtyAccessAsync(user.Id, specialtyIds, connection, transaction);
            await SetFacultyAccessAsync(user.Id, facultyAccessIds, connection, transaction);

            if (rows > 0)
            {
                var afterSnapshot = new
                {
                    user.Id,
                    user.Username,
                    user.Role,
                    user.FacultyId,
                    SpecialtyIds = specialtyIds,
                    FacultyAccessIds = facultyAccessIds
                };
                var beforeSnapshot = before == null ? (object)new { } : new
                {
                    before.Id,
                    before.Username,
                    before.Role,
                    before.FacultyId,
                    SpecialtyIds = new List<int>(),
                    FacultyAccessIds = new List<int>()
                };
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

        public async Task<List<int>> GetSpecialtyIdsAsync(int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return await GetSpecialtyIdsAsync(userId, connection);
        }

        public async Task<List<int>> GetFacultyAccessIdsAsync(int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return await GetFacultyAccessIdsAsync(userId, connection);
        }

        private static async Task<List<int>> GetSpecialtyIdsAsync(int userId, NpgsqlConnection connection)
        {
            var ids = new List<int>();
            using var cmd = new NpgsqlCommand("SELECT specialty_id FROM user_specialty_access WHERE user_id = @UserId", connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                ids.Add(reader.GetInt32(0));
            return ids;
        }

        private static async Task<List<int>> GetFacultyAccessIdsAsync(int userId, NpgsqlConnection connection)
        {
            var ids = new List<int>();
            using var cmd = new NpgsqlCommand("SELECT faculty_id FROM user_faculty_access WHERE user_id = @UserId", connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                ids.Add(reader.GetInt32(0));
            return ids;
        }

        private static async Task SetSpecialtyAccessAsync(int userId, List<int> specialtyIds, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            using (var cmd = new NpgsqlCommand("DELETE FROM user_specialty_access WHERE user_id = @UserId", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                await cmd.ExecuteNonQueryAsync();
            }
            foreach (var sid in specialtyIds)
            {
                using var cmd = new NpgsqlCommand("INSERT INTO user_specialty_access (user_id, specialty_id) VALUES (@UserId, @SpecialtyId)", connection, transaction);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@SpecialtyId", sid);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task SetFacultyAccessAsync(int userId, List<int> facultyIds, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            using (var cmd = new NpgsqlCommand("DELETE FROM user_faculty_access WHERE user_id = @UserId", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                await cmd.ExecuteNonQueryAsync();
            }
            foreach (var fid in facultyIds)
            {
                using var cmd = new NpgsqlCommand("INSERT INTO user_faculty_access (user_id, faculty_id) VALUES (@UserId, @FacultyId)", connection, transaction);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@FacultyId", fid);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static User MapUser(NpgsqlDataReader reader) => new()
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            Role = reader.GetString(3),
            FacultyId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
            IsActive = reader.GetBoolean(5),
            MustChangePassword = reader.GetBoolean(6),
            CreatedAt = reader.GetDateTime(7)
        };
    }
}
