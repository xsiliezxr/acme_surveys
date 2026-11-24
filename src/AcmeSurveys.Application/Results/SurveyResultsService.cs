using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Domain.FieldValues;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Application.Results
{
    public sealed class SurveyResultsService
    {
        public const int DefaultPageSize = 25;
        public const int MaxPageSize = 100;

        private readonly ISurveyRepository _surveys;
        private readonly IResponseRepository _responses;
        private readonly IFieldValueProtector _protector;
        private readonly FieldValueHandlerRegistry _handlers;
        private readonly ISecurityAuditLog _audit;

        public SurveyResultsService(ISurveyRepository surveys, IResponseRepository responses,
            IFieldValueProtector protector, FieldValueHandlerRegistry handlers, ISecurityAuditLog audit)
        {
            _surveys = surveys;
            _responses = responses;
            _protector = protector;
            _handlers = handlers;
            _audit = audit;
        }

        public async Task<Result<SurveyResultsDto>> GetPageAsync(int ownerUserId, Guid externalId, int page, int pageSize)
        {
            var survey = await _surveys.FindByExternalIdAsync(externalId).ConfigureAwait(false);
            if (survey == null || !survey.IsOwnedBy(ownerUserId))
            {
                if (survey != null)
                    _audit.Write(SecurityEvent.AccessDenied, "user:" + ownerUserId, "results of survey=" + externalId);
                return Result.NotFound<SurveyResultsDto>("La encuesta no existe.");
            }

            pageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
            page = Math.Max(page, 1);

            // Solo se descifra la página visible; el pivote usa diccionarios para recorrer los valores una sola vez.
            var data = await _responses.GetPageAsync(survey.Id, (page - 1) * pageSize, pageSize).ConfigureAwait(false);

            var columns = new List<ResultColumnDto>(survey.Fields.Count);
            var columnIndexByFieldId = new Dictionary<int, int>(survey.Fields.Count);
            var fieldTypeByFieldId = new Dictionary<int, FieldType>(survey.Fields.Count);
            foreach (var field in survey.Fields)
            {
                columnIndexByFieldId[field.Id] = columns.Count;
                fieldTypeByFieldId[field.Id] = field.Type;
                columns.Add(new ResultColumnDto { Name = field.Name, Title = field.Title, Type = field.Type });
            }

            var rows = new List<ResultRowDto>(data.Responses.Count);
            var rowByResponseId = new Dictionary<long, ResultRowDto>(data.Responses.Count);
            foreach (var header in data.Responses)
            {
                var row = new ResultRowDto { SubmittedAtUtc = header.SubmittedAtUtc, Values = new object[columns.Count] };
                rows.Add(row);
                rowByResponseId[header.ResponseId] = row;
            }

            foreach (var value in data.Values)
            {
                ResultRowDto row;
                int column;
                if (!rowByResponseId.TryGetValue(value.ResponseId, out row) ||
                    !columnIndexByFieldId.TryGetValue(value.SurveyFieldId, out column))
                {
                    continue;
                }

                try
                {
                    var canonical = _protector.Unprotect(value.ValueCipher, value.SurveyFieldId);
                    row.Values[column] = _handlers.Get(fieldTypeByFieldId[value.SurveyFieldId]).ToTypedValue(canonical);
                }
                // Fail-closed: un valor alterado en la BD no se muestra; se marca la fila y se audita.
                catch (CryptographicException)
                {
                    row.HasIntegrityError = true;
                    _audit.Write(SecurityEvent.CiphertextIntegrityFailure, "survey=" + externalId,
                        "response=" + value.ResponseId + " field=" + value.SurveyFieldId);
                }
            }

            return Result.Success(new SurveyResultsDto
            {
                ExternalId = survey.ExternalId,
                Name = survey.Name,
                Columns = columns,
                Rows = rows,
                Page = page,
                PageSize = pageSize,
                TotalCount = data.TotalCount
            });
        }
    }
}
