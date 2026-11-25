using System;

namespace AcmeSurveys.Domain.Common
{
    public sealed class Error
    {
        public Error(string code, string message, string target = null)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
            Code = code;
            Message = message ?? string.Empty;
            Target = target;
        }

        public string Code { get; }

        public string Message { get; }

        public string Target { get; }

        public static Error Validation(string target, string message) => new Error(ErrorCodes.Validation, message, target);

        public override string ToString() => Target == null ? Code + ": " + Message : Code + " (" + Target + "): " + Message;
    }
}
