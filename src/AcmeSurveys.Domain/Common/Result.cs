using System;
using System.Collections.Generic;
using System.Linq;

namespace AcmeSurveys.Domain.Common
{
    public class Result
    {
        private static readonly IReadOnlyList<Error> NoErrors = new Error[0];
        private static readonly Result SuccessInstance = new Result(NoErrors);

        protected Result(IReadOnlyList<Error> errors)
        {
            Errors = errors ?? NoErrors;
        }

        public IReadOnlyList<Error> Errors { get; }

        public bool IsSuccess => Errors.Count == 0;

        public bool IsFailure => !IsSuccess;

        public bool HasError(string code) => Errors.Any(e => e.Code == code);

        public static Result Success() => SuccessInstance;

        public static Result Failure(params Error[] errors) => Failure((IEnumerable<Error>)errors);

        public static Result Failure(IEnumerable<Error> errors)
        {
            var list = errors?.ToList() ?? new List<Error>();
            if (list.Count == 0) throw new ArgumentException("A failure requires at least one error.", nameof(errors));
            return new Result(list);
        }

        public static Result<T> Success<T>(T value) => new Result<T>(value, NoErrors);

        public static Result<T> Failure<T>(params Error[] errors) => Failure<T>((IEnumerable<Error>)errors);

        public static Result<T> Failure<T>(IEnumerable<Error> errors)
        {
            var list = errors?.ToList() ?? new List<Error>();
            if (list.Count == 0) throw new ArgumentException("A failure requires at least one error.", nameof(errors));
            return new Result<T>(default(T), list);
        }

        public static Result NotFound(string message = "El recurso no existe.") =>
            Failure(new Error(ErrorCodes.NotFound, message));

        public static Result<T> NotFound<T>(string message = "El recurso no existe.") =>
            Failure<T>(new Error(ErrorCodes.NotFound, message));
    }
}
