using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Application.Responses;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Domain.FieldValues;
using AcmeSurveys.Domain.Surveys;
using AcmeSurveys.UnitTests.TestDoubles;
using Xunit;

namespace AcmeSurveys.UnitTests.Application
{
    public class PublicSurveyServiceTests
    {
        private static readonly DateTime Now = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Guid PublicToken = Guid.NewGuid();

        private readonly InMemorySurveyRepository _surveys = new InMemorySurveyRepository();
        private readonly InMemoryResponseRepository _responses = new InMemoryResponseRepository();
        private readonly PublicSurveyService _service;

        public PublicSurveyServiceTests()
        {
            _surveys.Surveys.Add(Survey.Rehydrate(10, Guid.NewGuid(), PublicToken, 1, "Encuesta", null, new[]
            {
                SurveyField.Rehydrate(100, "edad", "Edad", FieldType.Number, true, 0),
                SurveyField.Rehydrate(101, "ingreso", "Fecha de ingreso", FieldType.Date, false, 1)
            }, Now, Now, new byte[] { 1 }));

            _service = new PublicSurveyService(_surveys, _responses, new PrefixFieldValueProtector(),
                FieldValueHandlerRegistry.CreateDefault(), new FakeClock(Now));
        }

        [Fact]
        public async Task GetFormAsync_KnownToken_ReturnsFieldsInOrder()
        {
            var result = await _service.GetFormAsync(PublicToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { "edad", "ingreso" }, result.Value.Fields.Select(f => f.Name));
        }

        [Fact]
        public async Task GetFormAsync_UnknownToken_ReturnsNotFound()
        {
            var result = await _service.GetFormAsync(Guid.NewGuid());

            Assert.True(result.HasError(ErrorCodes.NotFound));
        }

        [Fact]
        public async Task SubmitAsync_ValidValues_SavesCanonicalValuesProtectedPerField()
        {
            var values = new Dictionary<string, string> { { "edad", "34.50" }, { "ingreso", "2024-03-01" } };

            var result = await _service.SubmitAsync(PublicToken, values);

            Assert.True(result.IsSuccess);
            var saved = Assert.Single(_responses.Saved);
            Assert.Equal(new[] { "100:34.5", "101:2024-03-01" }, saved.Select(a => Encoding.UTF8.GetString(a.ValueCipher)));
        }

        [Fact]
        public async Task SubmitAsync_InvalidValues_ReturnsErrorPerFieldAndSavesNothing()
        {
            var values = new Dictionary<string, string> { { "ingreso", "2024-13-01" } };

            var result = await _service.SubmitAsync(PublicToken, values);

            Assert.Contains(result.Errors, e => e.Target == "edad");
            Assert.Contains(result.Errors, e => e.Target == "ingreso");
            Assert.Empty(_responses.Saved);
        }

        [Fact]
        public async Task SubmitAsync_UnknownToken_ReturnsNotFound()
        {
            var result = await _service.SubmitAsync(Guid.NewGuid(), new Dictionary<string, string> { { "edad", "1" } });

            Assert.True(result.HasError(ErrorCodes.NotFound));
            Assert.Empty(_responses.Saved);
        }

        [Fact]
        public async Task SubmitAsync_StructureChangedWhileFilling_ReturnsConflict()
        {
            _responses.NextOutcome = SaveOutcome.SurveyStructureChanged;

            var result = await _service.SubmitAsync(PublicToken, new Dictionary<string, string> { { "edad", "1" } });

            Assert.True(result.HasError(ErrorCodes.Conflict));
        }
    }
}
