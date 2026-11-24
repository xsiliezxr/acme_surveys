using System;
using System.Linq;
using AcmeSurveys.Domain.Surveys;
using Xunit;

namespace AcmeSurveys.UnitTests.Domain
{
    public class SurveyTests
    {
        private static readonly DateTime Now = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

        private static FieldDefinition Field(string name, FieldType type = FieldType.Text, int? id = null) =>
            new FieldDefinition(id, name, "Título " + name, type, false);

        private static Survey PersistedSurvey() =>
            Survey.Rehydrate(10, Guid.NewGuid(), Guid.NewGuid(), 1, "Encuesta", null, new[]
            {
                SurveyField.Rehydrate(100, "edad", "Edad", FieldType.Number, true, 0),
                SurveyField.Rehydrate(101, "nombre", "Nombre", FieldType.Text, false, 1)
            }, Now, Now, new byte[] { 1 });

        [Fact]
        public void Create_ValidFields_AssignsSortOrderByPosition()
        {
            var fields = new[] { Field("b"), Field("a") };

            var survey = Survey.Create(1, Guid.NewGuid(), Guid.NewGuid(), "Encuesta", null, fields, Now).Value;

            Assert.Equal(new[] { "b", "a" }, survey.Fields.Select(f => f.Name));
            Assert.Equal(new short[] { 0, 1 }, survey.Fields.Select(f => f.SortOrder));
        }

        [Fact]
        public void Create_DuplicateFieldNamesIgnoringCase_Fails()
        {
            var fields = new[] { Field("Edad"), Field("edad") };

            var result = Survey.Create(1, Guid.NewGuid(), Guid.NewGuid(), "Encuesta", null, fields, Now);

            Assert.True(result.IsFailure);
            Assert.Contains(result.Errors, e => e.Target == "Fields[1].Name");
        }

        [Theory]
        [InlineData("1edad")]
        [InlineData("con espacio")]
        [InlineData("<script>")]
        [InlineData("")]
        public void Create_InvalidFieldName_ReportsErrorOnName(string name)
        {
            var fields = new[] { Field(name) };

            var result = Survey.Create(1, Guid.NewGuid(), Guid.NewGuid(), "Encuesta", null, fields, Now);

            Assert.Contains(result.Errors, e => e.Target == "Fields[0].Name");
        }

        [Fact]
        public void Create_BlankNameAndNoFields_ReportsBothErrors()
        {
            var fields = new FieldDefinition[0];

            var result = Survey.Create(1, Guid.NewGuid(), Guid.NewGuid(), "  ", null, fields, Now);

            Assert.Contains(result.Errors, e => e.Target == "Name");
            Assert.Contains(result.Errors, e => e.Target == "Fields");
        }

        [Fact]
        public void Update_FieldIdFromAnotherSurvey_Fails()
        {
            var survey = PersistedSurvey();

            var result = survey.Update("Encuesta", null, new[] { Field("edad", FieldType.Number, id: 999) }, false, Now);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Update_ChangeTypeWhenSurveyHasResponses_ReportsErrorOnType()
        {
            var survey = PersistedSurvey();
            var fields = new[] { Field("edad", FieldType.Text, id: 100), Field("nombre", FieldType.Text, id: 101) };

            var result = survey.Update("Encuesta", null, fields, hasResponses: true, nowUtc: Now);

            Assert.Contains(result.Errors, e => e.Target == "Fields[0].Type");
        }

        [Fact]
        public void Update_RemoveFieldWhenSurveyHasResponses_ReportsErrorOnFields()
        {
            var survey = PersistedSurvey();
            var fields = new[] { Field("edad", FieldType.Number, id: 100) };

            var result = survey.Update("Encuesta", null, fields, hasResponses: true, nowUtc: Now);

            Assert.Contains(result.Errors, e => e.Target == "Fields");
        }

        [Fact]
        public void Update_AddFieldWhenSurveyHasResponses_Succeeds()
        {
            var survey = PersistedSurvey();
            var fields = new[]
            {
                Field("edad", FieldType.Number, id: 100),
                Field("nombre", FieldType.Text, id: 101),
                Field("correo")
            };

            var result = survey.Update("Encuesta", null, fields, hasResponses: true, nowUtc: Now);

            Assert.True(result.IsSuccess);
            Assert.Equal(3, survey.Fields.Count);
            Assert.True(survey.Fields[2].IsNew);
        }

        [Fact]
        public void Update_InvalidData_DoesNotModifyAggregate()
        {
            var survey = PersistedSurvey();

            survey.Update("X", null, new FieldDefinition[0], false, Now);

            Assert.Equal("Encuesta", survey.Name);
            Assert.Equal(2, survey.Fields.Count);
        }

        [Fact]
        public void RotatePublicToken_SameToken_ThrowsArgumentException()
        {
            var survey = PersistedSurvey();

            Assert.Throws<ArgumentException>(() => survey.RotatePublicToken(survey.PublicToken, Now));
        }

        [Fact]
        public void RotatePublicToken_NewToken_ReplacesToken()
        {
            var survey = PersistedSurvey();
            var newToken = Guid.NewGuid();

            survey.RotatePublicToken(newToken, Now);

            Assert.Equal(newToken, survey.PublicToken);
        }
    }
}
