using System;
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
        private readonly PasswordHash _dummyHash;

        public Pbkdf2PasswordHasher(int iterations = DefaultIterations)
        {
            if (iterations < MinimumIterations)
                throw new ArgumentOutOfRangeException(nameof(iterations), "PBKDF2 iterations below the secure minimum.");

            _iterations = iterations;
            _dummyHash = Hash(Guid.NewGuid().ToString("N"));
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
            Derive(password ?? string.Empty, _dummyHash.Salt, _dummyHash.Iterations);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations) =>
            NativePbkdf2.DeriveSha256(password, salt, iterations, HashSize);
    }
}
