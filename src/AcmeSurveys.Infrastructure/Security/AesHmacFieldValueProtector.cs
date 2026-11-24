using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using AcmeSurveys.Application.Abstractions.Security;

namespace AcmeSurveys.Infrastructure.Security
{
    // Encrypt-then-MAC: AES-256-CBC + HMAC-SHA256 (.NET Framework 4.8 no incluye AesGcm).
    // Formato: [versión][keyId][IV 16][cifrado][tag 32]; el tag cubre cabecera, IV, cifrado y fieldId.
    public sealed class AesHmacFieldValueProtector : IFieldValueProtector
    {
        private const byte FormatVersion = 1;
        private const int HeaderSize = 2;
        private const int IvSize = 16;
        private const int TagSize = 32;
        private const int BlockSize = 16;
        private const int MinimumSize = HeaderSize + IvSize + BlockSize + TagSize;

        private static readonly byte[] EncryptionPurpose = Encoding.ASCII.GetBytes("AcmeSurveys.FieldValue.Encryption.v1");
        private static readonly byte[] AuthenticationPurpose = Encoding.ASCII.GetBytes("AcmeSurveys.FieldValue.Authentication.v1");

        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private readonly byte _activeKeyId;
        private readonly IReadOnlyDictionary<byte, DerivedKeys> _keys;

        public AesHmacFieldValueProtector(DataProtectionKeyRing keyRing)
        {
            if (keyRing == null) throw new ArgumentNullException(nameof(keyRing));

            var keys = new Dictionary<byte, DerivedKeys>();
            foreach (var pair in keyRing.MasterKeys)
            {
                keys[pair.Key] = new DerivedKeys(Derive(pair.Value, EncryptionPurpose), Derive(pair.Value, AuthenticationPurpose));
            }

            _keys = keys;
            _activeKeyId = keyRing.ActiveKeyId;
        }

        public byte[] Protect(string plaintext, int fieldId)
        {
            if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));

            var keys = _keys[_activeKeyId];
            var plainBytes = StrictUtf8.GetBytes(plaintext);
            try
            {
                byte[] iv, cipher;
                using (var aes = CreateAes(keys.EncryptionKey))
                {
                    aes.GenerateIV();
                    iv = aes.IV;
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    }
                }

                var output = new byte[HeaderSize + IvSize + cipher.Length + TagSize];
                output[0] = FormatVersion;
                output[1] = _activeKeyId;
                Buffer.BlockCopy(iv, 0, output, HeaderSize, IvSize);
                Buffer.BlockCopy(cipher, 0, output, HeaderSize + IvSize, cipher.Length);

                var tag = ComputeTag(keys.AuthenticationKey, output, output.Length - TagSize, fieldId);
                Buffer.BlockCopy(tag, 0, output, output.Length - TagSize, TagSize);
                return output;
            }
            finally
            {
                Array.Clear(plainBytes, 0, plainBytes.Length);
            }
        }

        public string Unprotect(byte[] protectedValue, int fieldId)
        {
            if (protectedValue == null ||
                protectedValue.Length < MinimumSize ||
                (protectedValue.Length - HeaderSize - IvSize - TagSize) % BlockSize != 0 ||
                protectedValue[0] != FormatVersion)
            {
                throw new CryptographicException("Invalid protected value format.");
            }

            DerivedKeys keys;
            if (!_keys.TryGetValue(protectedValue[1], out keys))
                throw new CryptographicException("Unknown data protection key id.");

            var cipherLength = protectedValue.Length - HeaderSize - IvSize - TagSize;
            var expectedTag = ComputeTag(keys.AuthenticationKey, protectedValue, protectedValue.Length - TagSize, fieldId);

            // El MAC se verifica en tiempo constante antes de descifrar (evita padding oracle).
            if (!CryptoBytes.FixedTimeEquals(expectedTag, 0, protectedValue, protectedValue.Length - TagSize, TagSize))
                throw new CryptographicException("Protected value failed integrity validation.");

            var iv = new byte[IvSize];
            Buffer.BlockCopy(protectedValue, HeaderSize, iv, 0, IvSize);

            using (var aes = CreateAes(keys.EncryptionKey))
            {
                aes.IV = iv;
                using (var decryptor = aes.CreateDecryptor())
                {
                    var plainBytes = decryptor.TransformFinalBlock(protectedValue, HeaderSize + IvSize, cipherLength);
                    try
                    {
                        return StrictUtf8.GetString(plainBytes);
                    }
                    finally
                    {
                        Array.Clear(plainBytes, 0, plainBytes.Length);
                    }
                }
            }
        }

        private static Aes CreateAes(byte[] key)
        {
            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            return aes;
        }

        private static byte[] ComputeTag(byte[] macKey, byte[] data, int length, int fieldId)
        {
            var associatedData = new[]
            {
                (byte)(fieldId >> 24), (byte)(fieldId >> 16), (byte)(fieldId >> 8), (byte)fieldId
            };

            using (var hmac = new HMACSHA256(macKey))
            {
                hmac.TransformBlock(data, 0, length, null, 0);
                hmac.TransformFinalBlock(associatedData, 0, associatedData.Length);
                return hmac.Hash;
            }
        }

        private static byte[] Derive(byte[] masterKey, byte[] purpose)
        {
            using (var hmac = new HMACSHA256(masterKey))
            {
                return hmac.ComputeHash(purpose);
            }
        }

        private sealed class DerivedKeys
        {
            public DerivedKeys(byte[] encryptionKey, byte[] authenticationKey)
            {
                EncryptionKey = encryptionKey;
                AuthenticationKey = authenticationKey;
            }

            public byte[] EncryptionKey { get; }
            public byte[] AuthenticationKey { get; }
        }
    }
}
