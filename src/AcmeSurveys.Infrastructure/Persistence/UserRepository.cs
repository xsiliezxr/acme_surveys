using System;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Domain.Users;
using Dapper;

namespace AcmeSurveys.Infrastructure.Persistence
{
    public sealed class UserRepository : IUserRepository
    {
        private const string SelectColumns = @"
SELECT UserId, ExternalId, Username, PasswordHash, PasswordSalt, PasswordIterations, FailedLoginCount, LockoutEndUtc
FROM dbo.Users";

        private readonly SqlConnectionFactory _connections;

        public UserRepository(SqlConnectionFactory connections)
        {
            _connections = connections;
        }

        public Task<User> FindByUsernameAsync(string username) =>
            FindAsync(SelectColumns + " WHERE Username = @Username;", new { Username = username });

        public Task<User> FindByExternalIdAsync(Guid externalId) =>
            FindAsync(SelectColumns + " WHERE ExternalId = @ExternalId;", new { ExternalId = externalId });

        public async Task<bool> RegisterFailedLoginAsync(int userId, int maxFailedAttempts, DateTime lockoutEndUtc)
        {
            const string sql = @"
UPDATE dbo.Users
SET FailedLoginCount = CASE WHEN FailedLoginCount + 1 >= @MaxFailedAttempts THEN 0 ELSE FailedLoginCount + 1 END,
    LockoutEndUtc    = CASE WHEN FailedLoginCount + 1 >= @MaxFailedAttempts THEN @LockoutEndUtc ELSE LockoutEndUtc END
OUTPUT CAST(CASE WHEN deleted.FailedLoginCount + 1 >= @MaxFailedAttempts THEN 1 ELSE 0 END AS BIT)
WHERE UserId = @UserId;";

            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            {
                return await connection.ExecuteScalarAsync<bool>(sql,
                    new { UserId = userId, MaxFailedAttempts = maxFailedAttempts, LockoutEndUtc = lockoutEndUtc }).ConfigureAwait(false);
            }
        }

        public async Task RegisterSuccessfulLoginAsync(int userId, DateTime nowUtc)
        {
            const string sql = @"
UPDATE dbo.Users
SET FailedLoginCount = 0, LockoutEndUtc = NULL, LastLoginAtUtc = @NowUtc
WHERE UserId = @UserId;";

            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            {
                await connection.ExecuteAsync(sql, new { UserId = userId, NowUtc = nowUtc }).ConfigureAwait(false);
            }
        }

        public async Task UpdatePasswordHashAsync(int userId, PasswordHash passwordHash)
        {
            const string sql = @"
UPDATE dbo.Users
SET PasswordHash = @Hash, PasswordSalt = @Salt, PasswordIterations = @Iterations
WHERE UserId = @UserId;";

            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            {
                await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    passwordHash.Hash,
                    passwordHash.Salt,
                    passwordHash.Iterations
                }).ConfigureAwait(false);
            }
        }

        private async Task<User> FindAsync(string sql, object parameters)
        {
            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            {
                var row = await connection.QuerySingleOrDefaultAsync<UserRow>(sql, parameters).ConfigureAwait(false);
                return row == null
                    ? null
                    : new User(row.UserId, row.ExternalId, row.Username,
                        new PasswordHash(row.PasswordHash, row.PasswordSalt, row.PasswordIterations),
                        row.FailedLoginCount, row.LockoutEndUtc);
            }
        }

        private sealed class UserRow
        {
            public int UserId { get; set; }
            public Guid ExternalId { get; set; }
            public string Username { get; set; }
            public byte[] PasswordHash { get; set; }
            public byte[] PasswordSalt { get; set; }
            public int PasswordIterations { get; set; }
            public byte FailedLoginCount { get; set; }
            public DateTime? LockoutEndUtc { get; set; }
        }
    }
}
