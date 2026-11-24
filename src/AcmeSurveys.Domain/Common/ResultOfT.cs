using System;
using System.Collections.Generic;

namespace AcmeSurveys.Domain.Common
{
    public sealed class Result<T> : Result
    {
        private readonly T _value;

        internal Result(T value, IReadOnlyList<Error> errors) : base(errors)
        {
            _value = value;
        }

        public T Value
        {
            get
            {
                if (IsFailure) throw new InvalidOperationException("Cannot read the value of a failed result.");
                return _value;
            }
        }
    }
}
