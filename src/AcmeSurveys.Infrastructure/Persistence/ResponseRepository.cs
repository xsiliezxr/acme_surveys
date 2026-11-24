using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions.Persistence;
using Dapper;

namespace AcmeSurveys.Infrastructure.Persistence
{
    public sealed class ResponseRepository : IResponseRepository
    {
        private readonly SqlConnectionFactory _connections;

        public ResponseRepository(SqlConnectionFactory connections)
        {
            _connections = connections;
        }

        public async Task<SaveOutcome> AddAsync(int surveyId, DateTime submittedAtUtc, IReadOnlyList<EncryptedAnswer> answers)
        {
            const string sql = @"
SET XACT_ABORT ON;
BEGIN TRANSACTION;

INSERT INTO dbo.Responses (SurveyId, SubmittedAtUtc) VALUES (@SurveyId, @SubmittedAtUtc);

DECLARE @ResponseId BIGINT = SCOPE_IDENTITY();

INSERT INTO dbo.ResponseValues (ResponseId, SurveyFieldId, ValueCipher)
SELECT @ResponseId, SurveyFieldId, ValueCipher
FROM @Values;

COMMIT TRANSACTION;";

            try
            {
                using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
                {
                    await connection.ExecuteAsync(sql, new
                    {
                        SurveyId = surveyId,
                        SubmittedAtUtc = submittedAtUtc,
                        Values = TableValuedParameters.ResponseValues(answers)
                    }).ConfigureAwait(false);
                }
                return SaveOutcome.Saved;
            }
            catch (SqlException ex) when (SqlErrors.Is(ex, SqlErrors.ConstraintViolation))
            {
                return SaveOutcome.SurveyStructureChanged;
            }
        }

        // Un round-trip con 3 result sets: total, cabeceras de la página (OFFSET/FETCH) y sus valores cifrados.
        public async Task<EncryptedResultsPage> GetPageAsync(int surveyId, int skip, int take)
        {
            const string sql = @"
DECLARE @Page TABLE (ResponseId BIGINT NOT NULL PRIMARY KEY, SubmittedAtUtc DATETIME2(0) NOT NULL);

INSERT INTO @Page (ResponseId, SubmittedAtUtc)
SELECT r.ResponseId, r.SubmittedAtUtc
FROM dbo.Responses r
WHERE r.SurveyId = @SurveyId
ORDER BY r.ResponseId DESC
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;

SELECT COUNT_BIG(*) FROM dbo.Responses WHERE SurveyId = @SurveyId;

SELECT ResponseId, SubmittedAtUtc FROM @Page ORDER BY ResponseId DESC;

SELECT v.ResponseId, v.SurveyFieldId, v.ValueCipher
FROM dbo.ResponseValues v
INNER JOIN @Page p ON p.ResponseId = v.ResponseId;";

            using (var connection = await _connections.OpenAsync().ConfigureAwait(false))
            using (var results = await connection.QueryMultipleAsync(sql, new { SurveyId = surveyId, Skip = skip, Take = take }).ConfigureAwait(false))
            {
                var total = await results.ReadSingleAsync<long>().ConfigureAwait(false);
                var headers = (await results.ReadAsync<ResponseHeader>().ConfigureAwait(false)).AsList();
                var values = (await results.ReadAsync<EncryptedValueRow>().ConfigureAwait(false)).AsList();
                return new EncryptedResultsPage(total, headers, values);
            }
        }
    }
}
