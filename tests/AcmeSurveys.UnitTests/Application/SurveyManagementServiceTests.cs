using System;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Application.Surveys;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Domain.Surveys;
using AcmeSurveys.UnitTests.TestDoubles;
using Xunit;

namespace AcmeSurveys.UnitTests.Application
{
    public class SurveyManagementServiceTests
    {
        private const int OwnerId = 1;
        private const int OtherUserId = 2;
        private readonly InMemorySurveyRepository _repository = new InMemorySurveyRepository();
        private readonly NullAuditLog _audit = new NullAuditLog();
        private readonly SurveyManagementService _service;

        public SurveyManagementServiceTests()
        {
            _service = new SurveyManagementService(_repository, new FakeIdentifierGenerator(),
                new FakeClock(DateTime.UtcNow), _audit);
        }

        private static SaveSurveyCommand ValidCommand() =>
            new SaveSurveyCommand("Encuesta", null, new[] { new FieldDefinition(null, "edad", "Edad", FieldType.Number, true) });

        [Fact]
        public async Task GetAsync_SurveyOfAnotherUser_ReturnsNotFoundAndAuditsAttempt()
        {
            var created = await _service.CreateAsync(OwnerId, ValidCommand());

            var result = await _service.GetAsync(OtherUserId, created.Value);

            Assert.True(result.HasError(ErrorCodes.NotFound));
            Assert.Contains(SecurityEvent.AccessDenied, _audit.Events);
        }

        [Fact]
        public async Task DeleteAsync_SurveyOfAnotherUser_ReturnsNotFound()
        {
            var created = await _service.CreateAsync(OwnerId, ValidCommand());

            var result = await _service.DeleteAsync(OtherUserId, created.Value);

            Assert.True(result.HasError(ErrorCodes.NotFound));
        }

        [Fact]
        public async Task GetAsync_Owner_ReturnsFieldsAndPublicTokenDistinctFromExternalId()
        {
            var created = await _service.CreateAsync(OwnerId, ValidCommand());

            var result = await _service.GetAsync(OwnerId, created.Value);

            Assert.True(result.IsSuccess);
            Assert.Equal("edad", result.Value.Fields[0].Name);
            Assert.NotEqual(result.Value.ExternalId, result.Value.PublicToken);
        }

        [Fact]
        public async Task UpdateAsync_StaleRowVersion_ReturnsConflict()
        {
            var created = await _service.CreateAsync(OwnerId, ValidCommand());
            var stale = new SaveSurveyCommand("Nuevo", null, ValidCommand().Fields, rowVersion: new byte[] { 9, 9 });

            var result = await _service.UpdateAsync(OwnerId, created.Value, stale);

            Assert.True(result.HasError(ErrorCodes.Conflict));
        }
    }
}
