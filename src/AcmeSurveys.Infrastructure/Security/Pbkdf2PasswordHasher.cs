using System;
using System.Security.Cryptography;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Domain.Users;

namespace AcmeSurveys.Infrastructure.Security
{
    public sealed class Pbkdf2PasswordHasher : IPasswordHasher
    {
        public const int SaltSize = 16;
        public const int HashSize = 32;
        public const int DefaultIterations = 600000;
        public const int MinimumIterations = 100000;

        private readonly int _iterations;
        private readonly Lazy<PasswordHash> _dummyHash;

        public Pbkdf2PasswordHasher(int iterations = DefaultIterations)
        {
            if (iterations < MinimumIterations)
                throw new ArgumentOutOfRangeException(nameof(iterations), "PBKDF2 iterations below the secure minimum.");

            _iterations = iterations;
            _dummyHash = new Lazy<PasswordHash>(() => Hash(Guid.NewGuid().ToString("N")));
        }

        public PasswordHash Hash(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            var salt = CryptoBytes.Random(SaltSize);
            return new PasswordHash(Derive(password, salt, _iterations), salt, _iterations);
        }

        public PasswordVerificationResult Verify(string password, PasswordHash stored)
        {
            if (password == null || stored == null || stored.Hash.Length != HashSize)
                return PasswordVerificationResult.Failed;

            var candidate = Derive(password, stored.Salt, stored.Iterations);
            if (!CryptoBytes.FixedTimeEquals(candidate, stored.Hash))
                return PasswordVerificationResult.Failed;

            return stored.Iterations < _iterations
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Success;
        }

        // Mismo costo de CPU cuando el usuario no existe: evita enumerar usuarios por tiempo de respuesta.
        public void SimulateVerification(string password)
        {
            var dummy = _dummyHash.Value;
            Derive(password ?? string.Empty, dummy.Salt, dummy.Iterations);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return kdf.GetBytes(HashSize);
            }
        }
    }
}
