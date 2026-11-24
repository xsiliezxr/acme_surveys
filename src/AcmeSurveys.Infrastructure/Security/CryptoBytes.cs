using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace AcmeSurveys.Infrastructure.Security
{
    internal static class CryptoBytes
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static byte[] Random(int length)
        {
            var bytes = new byte[length];
            Rng.GetBytes(bytes);
            return bytes;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool FixedTimeEquals(byte[] left, int leftOffset, byte[] right, int rightOffset, int length)
        {
            var diff = 0;
            for (var i = 0; i < length; i++)
            {
                diff |= left[leftOffset + i] ^ right[rightOffset + i];
            }
            return diff == 0;
        }

        public static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            return FixedTimeEquals(left, 0, right, 0, left.Length);
        }
    }
}
