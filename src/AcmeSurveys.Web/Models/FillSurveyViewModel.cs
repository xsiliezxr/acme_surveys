using System;
using System.Collections.Generic;
using AcmeSurveys.Application.Responses;

namespace AcmeSurveys.Web.Models
{
    public sealed class FillSurveyViewModel
    {
        public const string InputPrefix = "f.";

        public FillSurveyViewModel(PublicSurveyFormDto survey, IReadOnlyDictionary<string, string> values = null)
        {
            Survey = survey;
            Values = values ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public PublicSurveyFormDto Survey { get; }
        public IReadOnlyDictionary<string, string> Values { get; }

        public string ValueFor(string fieldName)
        {
            string value;
            return Values.TryGetValue(fieldName, out value) ? value : string.Empty;
        }
    }
}
