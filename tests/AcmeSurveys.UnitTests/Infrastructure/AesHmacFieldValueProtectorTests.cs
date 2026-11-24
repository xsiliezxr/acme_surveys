using System.Linq;
using System.Security.Cryptography;
using AcmeSurveys.Infrastructure.Security;
using Xunit;

namespace AcmeSurveys.UnitTests.Infrastructure
{
    public class AesHmacFieldValueProtectorTests
    {
        private const int FieldId = 1;
        private readonly AesHmacFieldValueProtector _protector = new AesHmacFieldValueProtector(KeyRing(1, 1));

        private static DataProtectionKeyRing KeyRing(byte activeKeyId, params byte[] keyIds) =>
            new DataProtectionKeyRing(activeKeyId, keyIds.ToDictionary(id => id, id => Enumerable.Repeat(id, 32).ToArray()));

        [Fact]
        public void Unprotect_ProtectedUnicodeValue_ReturnsOriginal()
        {
            const string value = "Opinión: ¡excelente! 👍";
            var cipher = _protector.Protect(value, FieldId);

            var plaintext = _protector.Unprotect(cipher, FieldId);

            Assert.Equal(value, plaintext);
        }

        [Fact]
        public void Protect_SamePlaintextTwice_ProducesDifferentCiphertexts()
        {
            var first = _protector.Protect("igual", FieldId);
            var second = _protector.Protect("igual", FieldId);

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void Unprotect_TamperedCiphertext_ThrowsCryptographicException()
        {
            var cipher = _protector.Protect("dato", FieldId);

            cipher[20] ^= 0x01;

            Assert.Throws<CryptographicException>(() => _protector.Unprotect(cipher, FieldId));
        }

        [Fact]
        public void Unprotect_CiphertextFromAnotherField_ThrowsCryptographicException()
        {
            var cipher = _protector.Protect("dato", FieldId);

            Assert.Throws<CryptographicException>(() => _protector.Unprotect(cipher, FieldId + 1));
        }

        [Fact]
        public void Unprotect_ValueFromPreviousKeyAfterRotation_ReturnsOriginal()
        {
            var oldCipher = new AesHmacFieldValueProtector(KeyRing(1, 1)).Protect("viejo", FieldId);
            var rotated = new AesHmacFieldValueProtector(KeyRing(2, 1, 2));

            var plaintext = rotated.Unprotect(oldCipher, FieldId);

            Assert.Equal("viejo", plaintext);
        }

        [Fact]
        public void Protect_AfterRotation_UsesActiveKeyId()
        {
            var rotated = new AesHmacFieldValueProtector(KeyRing(2, 1, 2));

            var cipher = rotated.Protect("nuevo", FieldId);

            Assert.Equal(2, cipher[1]);
        }
    }
}
