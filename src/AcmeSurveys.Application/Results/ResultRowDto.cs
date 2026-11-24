using System;

namespace AcmeSurveys.Application.Results
{
    public sealed class ResultRowDto
    {
        public DateTime SubmittedAtUtc { get; set; }

        public object[] Values { get; set; }

        public bool HasIntegrityError { get; set; }
    }
}
