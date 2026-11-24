using System;
using System.Collections.Generic;
using AcmeSurveys.Domain.FieldValues;
using AcmeSurveys.Domain.Responses;
using AcmeSurveys.Domain.Surveys;
using Xunit;

namespace AcmeSurveys.UnitTests.Domain
{
    public class SurveyResponseTests
    {
        private static readonly DateTime Now = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
        private readonly FieldValueHandlerRegistry _handlers = FieldValueHandlerRegistry.CreateDefault();

        private static Survey PersistedSurvey() =>
            Survey.Rehydrate(10, Guid.NewGuid(), Guid.NewGuid(), 1, "Encuesta", null, new[]
            {
                SurveyField.Rehydrate(100, "edad", "Edad", FieldType.Number, true, 0),
                SurveyField.Rehydrate(101, "comentario", "Comentario", FieldType.Text, false, 1)
            }, Now, Now, new byte[] { 1 });

        [Fact]
        public void Create_ValidSubmission_KeepsOnlyAnsweredFields()
        {
            var values = new Dictionary<string, string> { { "edad", "30" } };

            var result = SurveyResponse.Create(PersistedSurvey(), values, _handlers, Now);

            Assert.True(result.IsSuccess);
            var answer = Assert.Single(result.Value.Answers);
            Assert.Equal(100, answer.FieldId);
            Assert.Equal("30", answer.CanonicalValue);
        }

        [Fact]
        public void Create_MissingRequiredField_ReportsErrorOnField()
        {
            var values = new Dictionary<string, string>();

            var result = SurveyResponse.Create(PersistedSurvey(), values, _handlers, Now);

            Assert.Contains(result.Errors, e => e.Target == "edad");
        }

        [Fact]
        public void Create_ValueWithWrongType_ReportsErrorOnField()
        {
            var values = new Dictionary<string, string> { { "edad", "treinta" } };

            var result = SurveyResponse.Create(PersistedSurvey(), values, _handlers, Now);

            Assert.Contains(result.Errors, e => e.Target == "edad");
        }

        [Fact]
        public void Create_UnknownKeysFromClient_AreIgnored()
        {
            var values = new Dictionary<string, string>
            {
                { "edad", "30" },
                { "SurveyFieldId", "999" },
                { "OwnerUserId", "1" }
            };

            var result = SurveyResponse.Create(PersistedSurvey(), values, _handlers, Now);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Answers);
        }
    }
}
