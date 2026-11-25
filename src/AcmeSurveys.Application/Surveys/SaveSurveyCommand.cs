using System.Collections.Generic;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Application.Surveys
{
    public sealed class SaveSurveyCommand
    {
        public SaveSurveyCommand(string name, string description, IReadOnlyList<FieldDefinition> fields, byte[] rowVersion = null)
        {
            Name = name;
            Description = description;
            Fields = fields ?? new FieldDefinition[0];
            RowVersion = rowVersion;
        }

        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<FieldDefinition> Fields { get; }

        public byte[] RowVersion { get; }
    }
}
