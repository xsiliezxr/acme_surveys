using System;

namespace AcmeSurveys.Domain.Users
{
    public sealed class PasswordHash
    {
        public PasswordHash(byte[] hash, byte[] salt, int iterations)
        {
            if (hash == null || hash.Length == 0) throw new ArgumentException("Hash is required.", nameof(hash));
            if (salt == null || salt.Length == 0) throw new ArgumentException("Salt is required.", nameof(salt));
            if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations));

            Hash = hash;
            Salt = salt;
            Iterations = iterations;
        }

        public byte[] Hash { get; }
        public byte[] Salt { get; }
        public int Iterations { get; }
    }
}
