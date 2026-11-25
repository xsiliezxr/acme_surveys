using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace AcmeSurveys.Infrastructure.Security
{
    // Rfc2898DeriveBytes en .NET Framework es gestionado (~10x más lento); CNG produce el mismo resultado.
    internal static class NativePbkdf2
    {
        private const int HmacFlag = 0x00000008;
        private static readonly IntPtr Sha256Hmac = OpenAlgorithm();

        public static byte[] DeriveSha256(string password, byte[] salt, int iterations, int length)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (salt == null) throw new ArgumentNullException(nameof(salt));

            var passwordBytes = new UTF8Encoding(false).GetBytes(password);
            var output = new byte[length];
            try
            {
                var status = BCryptDeriveKeyPBKDF2(Sha256Hmac, passwordBytes, passwordBytes.Length,
                    salt, salt.Length, (ulong)iterations, output, output.Length, 0);
                if (status != 0)
                    throw new CryptographicException("BCryptDeriveKeyPBKDF2 failed with status 0x" + status.ToString("X8") + ".");
                return output;
            }
            finally
            {
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }

        private static IntPtr OpenAlgorithm()
        {
            IntPtr handle;
            var status = BCryptOpenAlgorithmProvider(out handle, "SHA256", null, HmacFlag);
            if (status != 0)
                throw new CryptographicException("BCryptOpenAlgorithmProvider failed with status 0x" + status.ToString("X8") + ".");
            return handle;
        }

        [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int BCryptOpenAlgorithmProvider(out IntPtr algorithm, string algorithmId, string implementation, int flags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptDeriveKeyPBKDF2(IntPtr algorithm, byte[] password, int passwordLength,
            byte[] salt, int saltLength, ulong iterations, byte[] derivedKey, int derivedKeyLength, int flags);
    }
}
