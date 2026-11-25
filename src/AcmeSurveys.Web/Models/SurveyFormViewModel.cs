using System;
using System.Collections.Generic;
using System.Linq;
using AcmeSurveys.Application.Surveys;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Web.Models
{
    public sealed class SurveyFormViewModel
    {
        public SurveyFormViewModel()
        {
            Fields = new List<FieldFormViewModel>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<FieldFormViewModel> Fields { get; set; }

        public string RowVersion { get; set; }

        public bool HasResponses { get; set; }

        public Guid? ExternalId { get; set; }

        public bool IsEdit => ExternalId.HasValue;

        public static SurveyFormViewModel ForCreate() => new SurveyFormViewModel
        {
            Fields = new List<FieldFormViewModel> { new FieldFormViewModel { Type = FieldType.Text } }
        };

        public static SurveyFormViewModel FromDetails(SurveyDetailsDto survey) => new SurveyFormViewModel
        {
            ExternalId = survey.ExternalId,
            Name = survey.Name,
            Description = survey.Description,
            RowVersion = survey.RowVersion == null ? null : Convert.ToBase64String(survey.RowVersion),
            HasResponses = survey.HasResponses,
            Fields = survey.Fields.Select(f => new FieldFormViewModel
            {
                Id = f.Id,
                Name = f.Name,
                Title = f.Title,
                Type = f.Type,
                IsRequired = f.IsRequired
            }).ToList()
        };

        public SaveSurveyCommand ToCommand() => new SaveSurveyCommand(
            Name,
            Description,
            (Fields ?? new List<FieldFormViewModel>())
                .Select(f => new FieldDefinition(f.Id, f.Name, f.Title, f.Type, f.IsRequired))
                .ToList(),
            DecodeRowVersion(RowVersion));

        private static byte[] DecodeRowVersion(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
