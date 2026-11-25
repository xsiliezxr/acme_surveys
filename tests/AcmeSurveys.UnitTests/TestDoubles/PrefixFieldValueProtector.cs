using System.Text;
using AcmeSurveys.Application.Abstractions.Security;

namespace AcmeSurveys.UnitTests.TestDoubles
{
    /// <summary>"Cifra" anteponiendo el id del campo, para verificar qué se protegió sin depender de AES.</summary>
    internal sealed class PrefixFieldValueProtector : IFieldValueProtector
    {
        public byte[] Protect(string plaintext, int fieldId) => Encoding.UTF8.GetBytes(fieldId + ":" + plaintext);

        public string Unprotect(byte[] protectedValue, int fieldId) =>
            Encoding.UTF8.GetString(protectedValue).Substring((fieldId + ":").Length);
    }
}
