using System;
using System.Collections.Generic;
using System.Linq;

namespace AcmeSurveys.Infrastructure.Security
{
    public sealed class DataProtectionKeyRing
    {
        public const int MasterKeySize = 32;

        public DataProtectionKeyRing(byte activeKeyId, IDictionary<byte, byte[]> masterKeys)
        {
            if (masterKeys == null || masterKeys.Count == 0)
                throw new ArgumentException("At least one data protection key is required.", nameof(masterKeys));

            foreach (var pair in masterKeys)
            {
                if (pair.Value == null || pair.Value.Length != MasterKeySize)
                    throw new ArgumentException("Data protection key " + pair.Key + " must be exactly 256 bits.", nameof(masterKeys));
            }

            if (!masterKeys.ContainsKey(activeKeyId))
                throw new ArgumentException("The active key id " + activeKeyId + " is not present in the key ring.", nameof(activeKeyId));

            ActiveKeyId = activeKeyId;
            MasterKeys = masterKeys.ToDictionary(p => p.Key, p => (byte[])p.Value.Clone());
        }

        public byte ActiveKeyId { get; }

        public IReadOnlyDictionary<byte, byte[]> MasterKeys { get; }
    }
}
