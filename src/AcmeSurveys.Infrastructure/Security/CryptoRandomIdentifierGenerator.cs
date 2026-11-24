using System;
using AcmeSurveys.Application.Abstractions;

namespace AcmeSurveys.Infrastructure.Security
{
    public sealed class CryptoRandomIdentifierGenerator : IIdentifierGenerator
    {
        public Guid NewRandomUuid()
        {
            var bytes = CryptoBytes.Random(16);

            // System.Guid guarda los primeros grupos en little-endian: la versión (4) va en bytes[7] y la variante en bytes[8].
            bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40);

            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

            return new Guid(bytes);
        }
    }
}
