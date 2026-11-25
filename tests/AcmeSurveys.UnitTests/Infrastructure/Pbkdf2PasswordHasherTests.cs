using System;
using System.Linq;
using System.Security.Cryptography;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Domain.Users;
using AcmeSurveys.Infrastructure.Security;
using Xunit;

namespace AcmeSurveys.UnitTests.Infrastructure
{
    public class Pbkdf2PasswordHasherTests
    {
        private const string Password = "Secreta#123";
        private readonly Pbkdf2PasswordHasher _hasher = new Pbkdf2PasswordHasher(Pbkdf2PasswordHasher.MinimumIterations);

        private static byte[] Hex(string hex) =>
            Enumerable.Range(0, hex.Length / 2).Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();

        [Fact]
        public void Hash_AnyPassword_ProducesHashOfExpectedSize()
        {
            var hash = _hasher.Hash(Password);

            Assert.Equal(Pbkdf2PasswordHasher.HashSize, hash.Hash.Length);
            Assert.Equal(Pbkdf2PasswordHasher.SaltSize, hash.Salt.Length);
        }

        [Fact]
        public void Verify_CorrectPassword_ReturnsSuccess()
        {
            var hash = _hasher.Hash(Password);

            var result = _hasher.Verify(Password, hash);

            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void Verify_PasswordWithDifferentCase_ReturnsFailed()
        {
            var hash = _hasher.Hash(Password);

            var result = _hasher.Verify(Password.ToLowerInvariant(), hash);

            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void Verify_HashWithFewerIterationsThanConfigured_ReturnsSuccessRehashNeeded()
        {
            var hash = _hasher.Hash(Password);
            var stronger = new Pbkdf2PasswordHasher(Pbkdf2PasswordHasher.MinimumIterations * 2);

            var result = stronger.Verify(Password, hash);

            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void DeriveSha256_NativeImplementation_MatchesRfc2898DeriveBytes()
        {
            var salt = Enumerable.Range(1, Pbkdf2PasswordHasher.SaltSize).Select(i => (byte)i).ToArray();
            byte[] expected;
            using (var managed = new Rfc2898DeriveBytes(Password, salt, Pbkdf2PasswordHasher.MinimumIterations, HashAlgorithmName.SHA256))
            {
                expected = managed.GetBytes(Pbkdf2PasswordHasher.HashSize);
            }

            var actual = NativePbkdf2.DeriveSha256(Password, salt, Pbkdf2PasswordHasher.MinimumIterations, Pbkdf2PasswordHasher.HashSize);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Verify_DemoAdminFromDatabaseSeed_ReturnsSuccess()
        {
            var seed = new PasswordHash(
                Hex("CC9572694F6D1E317C573B1D4F42841098FC9CAC9399CBDBAFC1332CF84FA5A2"),
                Hex("170376C05F923D36DCB4C71E59FBFC80"),
                600000);

            var result = new Pbkdf2PasswordHasher().Verify("Acme#Admin2026!", seed);

            Assert.Equal(PasswordVerificationResult.Success, result);
        }
    }
}
