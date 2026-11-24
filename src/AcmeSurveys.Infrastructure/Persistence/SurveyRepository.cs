using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Domain.Surveys;
using Dapper;

namespace AcmeSurveys.Infrastructure.Persistence
{
    public sealed class SurveyRepository : ISurveyRepository
    {
        private const string SelectAggregate = @"
SELECT s.SurveyId, s.ExternalId, s.PublicToken, s.OwnerUserId, s.Name, s.Description,
       s.CreatedAtUtc, s.UpdatedAtUtc, s.RowVersion
FROM dbo.Surveys s
WHERE {0} AND s.DeletedAtUtc IS NULL;

SELECT f.SurveyFieldId, f.Name, f.Title, f.FieldTypeId, f.IsRequired, f.SortOrder
FROM dbo.SurveyFields f
INNER JOIN dbo.Surveys s ON s.SurveyId = f.SurveyId
WHERE {0} AND s.DeletedAtUtc IS NULL
ORDER BY f.SortOrder;";

        private static readonly string SelectByExternalId = string.Format(SelectAggregate, "s.ExternalId = @Key");
        private static readonly string SelectByPublicToken = string.Format(SelectAggregate, "s.PublicToken = @Key");

        private readonly SqlConnectionFactory _connections;

        public SurveyRepository(SqlConnectionFactory connections)
        {
            _connections = connections;
        }

        public async Task<IReadOnlyList<SurveySummary>> ListByOwnerAsync(int ownerUserId)
        {
            const string sql = @"
SELECT s.ExternalId, s.PublicToken, s.Name, s.Description, s.CreatedAtUtc, s.UpdatedAtUtc,
       (SELECT COUNT(*)     FROM dbo.SurveyFields f WHERE f.SurveyId = s.SurveyId) AS FieldCount,
       (SELECT COUNT_BIG(*) FROM dbo.Responses r    WHERE r.SurveyId = s.SurveyId) AS ResponseCount
FROM dbo.Surveys s
WHERE s.OwnerUserId = @OwnerUserId AND s.DeletedAtUtc IS NULL
ORDER BY s.CreatedAtUtc DESC;";

            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            {
                var rows = await connection.QueryAsync<SurveySummary>(sql, new { OwnerUserId = ownerUserId }).ConfigureAwait(false);
                return rows.AsList();
            }
        }

        public Task<Survey> FindByExternalIdAsync(Guid externalId) => FindAsync(SelectByExternalId, externalId);

        public Task<Survey> FindByPublicTokenAsync(Guid publicToken) => FindAsync(SelectByPublicToken, publicToken);

        public async Task<bool> HasResponsesAsync(int surveyId)
        {
            const string sql = "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Responses WHERE SurveyId = @SurveyId) THEN 1 ELSE 0 END AS BIT);";
            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            {
                return await connection.ExecuteScalarAsync<bool>(sql, new { SurveyId = surveyId }).ConfigureAwait(false);
            }
        }

        public async Task AddAsync(Survey survey)
        {
            const string sql = @"
SET XACT_ABORT ON;
BEGIN TRANSACTION;

INSERT INTO dbo.Surveys (ExternalId, PublicToken, OwnerUserId, Name, Description, CreatedAtUtc, UpdatedAtUtc)
VALUES (@ExternalId, @PublicToken, @OwnerUserId, @Name, @Description, @CreatedAtUtc, @UpdatedAtUtc);

DECLARE @SurveyId INT = SCOPE_IDENTITY();

INSERT INTO dbo.SurveyFields (SurveyId, Name, Title, FieldTypeId, IsRequired, SortOrder)
SELECT @SurveyId, Name, Title, FieldTypeId, IsRequired, SortOrder
FROM @Fields;

COMMIT TRANSACTION;";

            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            {
                await connection.ExecuteAsync(sql, new
                {
                    survey.ExternalId,
                    survey.PublicToken,
                    survey.OwnerUserId,
                    survey.Name,
                    survey.Description,
                    survey.CreatedAtUtc,
                    survey.UpdatedAtUtc,
                    Fields = TableValuedParameters.SurveyFields(survey.Fields)
                }).ConfigureAwait(false);
            }
        }

        // Concurrencia optimista con ROWVERSION. UPDLOCK+HOLDLOCK impide que entre una respuesta mientras
        // se valida que no se cambien tipos ni se borren campos de una encuesta con respuestas.
        public async Task<SaveOutcome> UpdateAsync(Survey survey)
        {
            const string sql = @"
SET XACT_ABORT ON;
BEGIN TRANSACTION;

UPDATE dbo.Surveys
SET Name = @Name, Description = @Description, UpdatedAtUtc = @UpdatedAtUtc
WHERE SurveyId = @SurveyId AND RowVersion = @RowVersion AND DeletedAtUtc IS NULL;

IF @@ROWCOUNT = 0
    THROW 50002, N'Concurrency conflict.', 1;

IF EXISTS (SELECT 1 FROM dbo.Responses WITH (UPDLOCK, HOLDLOCK) WHERE SurveyId = @SurveyId)
   AND (
        EXISTS (SELECT 1 FROM dbo.SurveyFields f
                INNER JOIN @Fields t ON t.SurveyFieldId = f.SurveyFieldId
                WHERE f.SurveyId = @SurveyId AND f.FieldTypeId <> t.FieldTypeId)
     OR EXISTS (SELECT 1 FROM dbo.SurveyFields f
                WHERE f.SurveyId = @SurveyId
                  AND NOT EXISTS (SELECT 1 FROM @Fields t WHERE t.SurveyFieldId = f.SurveyFieldId))
   )
    THROW 50001, N'Survey structure is locked by existing responses.', 1;

DELETE f
FROM dbo.SurveyFields f
WHERE f.SurveyId = @SurveyId
  AND NOT EXISTS (SELECT 1 FROM @Fields t WHERE t.SurveyFieldId = f.SurveyFieldId);

UPDATE f
SET f.Name = t.Name, f.Title = t.Title, f.FieldTypeId = t.FieldTypeId,
    f.IsRequired = t.IsRequired, f.SortOrder = t.SortOrder
FROM dbo.SurveyFields f
INNER JOIN @Fields t ON t.SurveyFieldId = f.SurveyFieldId
WHERE f.SurveyId = @SurveyId;

INSERT INTO dbo.SurveyFields (SurveyId, Name, Title, FieldTypeId, IsRequired, SortOrder)
SELECT @SurveyId, Name, Title, FieldTypeId, IsRequired, SortOrder
FROM @Fields
WHERE SurveyFieldId IS NULL;

COMMIT TRANSACTION;";

            try
            {
                using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
                {
                    await connection.ExecuteAsync(sql, new
                    {
                        SurveyId = survey.Id,
                        survey.Name,
                        survey.Description,
                        survey.UpdatedAtUtc,
                        survey.RowVersion,
                        Fields = TableValuedParameters.SurveyFields(survey.Fields)
                    }).ConfigureAwait(false);
                }
                return SaveOutcome.Saved;
            }
            catch (SqlException ex) when (SqlErrors.Is(ex, SqlErrors.ConcurrencyConflict))
            {
                return SaveOutcome.ConcurrencyConflict;
            }
            catch (SqlException ex) when (SqlErrors.Is(ex, SqlErrors.SurveyStructureLocked) ||
                                          SqlErrors.Is(ex, SqlErrors.ConstraintViolation))
            {
                return SaveOutcome.StructureLockedByResponses;
            }
        }

        public async Task<bool> SoftDeleteAsync(int surveyId, DateTime nowUtc)
        {
            const string sql = @"
UPDATE dbo.Surveys
SET DeletedAtUtc = @NowUtc, UpdatedAtUtc = @NowUtc
WHERE SurveyId = @SurveyId AND DeletedAtUtc IS NULL;";

            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            {
                return await connection.ExecuteAsync(sql, new { SurveyId = surveyId, NowUtc = nowUtc }).ConfigureAwait(false) == 1;
            }
        }

        public async Task<SaveOutcome> UpdatePublicTokenAsync(Survey survey)
        {
            const string sql = @"
UPDATE dbo.Surveys
SET PublicToken = @PublicToken, UpdatedAtUtc = @UpdatedAtUtc
WHERE SurveyId = @SurveyId AND RowVersion = @RowVersion AND DeletedAtUtc IS NULL;";

            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            {
                var affected = await connection.ExecuteAsync(sql, new
                {
                    SurveyId = survey.Id,
                    survey.PublicToken,
                    survey.UpdatedAtUtc,
                    survey.RowVersion
                }).ConfigureAwait(false);

                return affected == 1 ? SaveOutcome.Saved : SaveOutcome.ConcurrencyConflict;
            }
        }

        private async Task<Survey> FindAsync(string sql, Guid key)
        {
            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            using (var results = await connection.QueryMultipleAsync(sql, new { Key = key }).ConfigureAwait(false))
            {
                var row = await results.ReadSingleOrDefaultAsync<SurveyRow>().ConfigureAwait(false);
                if (row == null) return null;

                var fields = (await results.ReadAsync<FieldRow>().ConfigureAwait(false))
                    .Select(f => SurveyField.Rehydrate(f.SurveyFieldId, f.Name, f.Title, (FieldType)f.FieldTypeId, f.IsRequired, f.SortOrder));

                return Survey.Rehydrate(row.SurveyId, row.ExternalId, row.PublicToken, row.OwnerUserId,
                    row.Name, row.Description, fields, row.CreatedAtUtc, row.UpdatedAtUtc, row.RowVersion);
            }
        }

        private sealed class SurveyRow
        {
            public int SurveyId { get; set; }
            public Guid ExternalId { get; set; }
            public Guid PublicToken { get; set; }
            public int OwnerUserId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public DateTime UpdatedAtUtc { get; set; }
            public byte[] RowVersion { get; set; }
        }

        private sealed class FieldRow
        {
            public int SurveyFieldId { get; set; }
            public string Name { get; set; }
            public string Title { get; set; }
            public byte FieldTypeId { get; set; }
            public bool IsRequired { get; set; }
            public short SortOrder { get; set; }
        }
    }
}
