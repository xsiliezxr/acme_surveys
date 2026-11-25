namespace AcmeSurveys.Domain.Surveys
{
    public sealed class FieldDefinition
    {
        public FieldDefinition(int? id, string name, string title, FieldType type, bool isRequired)
        {
            Id = id;
            Name = name;
            Title = title;
            Type = type;
            IsRequired = isRequired;
        }

        public int? Id { get; }
        public string Name { get; }
        public string Title { get; }
        public FieldType Type { get; }
        public bool IsRequired { get; }
    }
}
