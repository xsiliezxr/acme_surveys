using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Domain.Users;

namespace AcmeSurveys.UnitTests.TestDoubles
{
    internal sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _users = new Dictionary<int, User>();

        public void Add(User user) => _users[user.Id] = user;

        public User Get(int id) => _users[id];

        public Task<User> FindByUsernameAsync(string username) =>
            Task.FromResult(_users.Values.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));

        public Task<User> FindByExternalIdAsync(Guid externalId) =>
            Task.FromResult(_users.Values.FirstOrDefault(u => u.ExternalId == externalId));

        public Task<bool> RegisterFailedLoginAsync(int userId, int maxFailedAttempts, DateTime lockoutEndUtc)
        {
            var u = _users[userId];
            var locked = u.FailedLoginCount + 1 >= maxFailedAttempts;
            _users[userId] = new User(u.Id, u.ExternalId, u.Username, u.Password,
                locked ? 0 : u.FailedLoginCount + 1, locked ? lockoutEndUtc : u.LockoutEndUtc);
            return Task.FromResult(locked);
        }

        public Task RegisterSuccessfulLoginAsync(int userId, DateTime nowUtc)
        {
            var u = _users[userId];
            _users[userId] = new User(u.Id, u.ExternalId, u.Username, u.Password, 0, null);
            return Task.CompletedTask;
        }

        public Task UpdatePasswordHashAsync(int userId, PasswordHash passwordHash)
        {
            var u = _users[userId];
            _users[userId] = new User(u.Id, u.ExternalId, u.Username, passwordHash, u.FailedLoginCount, u.LockoutEndUtc);
            return Task.CompletedTask;
        }
    }
}
