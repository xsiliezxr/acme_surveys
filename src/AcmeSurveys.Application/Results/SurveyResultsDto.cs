using System;
using System.Collections.Generic;

namespace AcmeSurveys.Application.Results
{
    public sealed class SurveyResultsDto
    {
        public Guid ExternalId { get; set; }
        public string Name { get; set; }
        public IReadOnlyList<ResultColumnDto> Columns { get; set; }
        public IReadOnlyList<ResultRowDto> Rows { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)((TotalCount + PageSize - 1) / PageSize);
    }
}
