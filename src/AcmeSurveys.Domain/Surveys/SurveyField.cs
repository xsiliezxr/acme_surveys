namespace AcmeSurveys.Domain.Surveys
{
    public sealed class SurveyField
    {
        internal SurveyField(int id, string name, string title, FieldType type, bool isRequired, short sortOrder)
        {
            Id = id;
            Name = name;
            Title = title;
            Type = type;
            IsRequired = isRequired;
            SortOrder = sortOrder;
        }

        public int Id { get; }

        public string Name { get; }

        public string Title { get; }

        public FieldType Type { get; }

        public bool IsRequired { get; }

        public short SortOrder { get; }

        public bool IsNew => Id == 0;

        public static SurveyField Rehydrate(int id, string name, string title, FieldType type, bool isRequired, short sortOrder) =>
            new SurveyField(id, name, title, type, isRequired, sortOrder);
    }
}
