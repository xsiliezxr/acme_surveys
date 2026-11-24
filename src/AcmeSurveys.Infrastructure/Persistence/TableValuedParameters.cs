using System.Collections.Generic;
using System.Data;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Domain.Surveys;
using Dapper;

namespace AcmeSurveys.Infrastructure.Persistence
{
    internal static class TableValuedParameters
    {
        public static SqlMapper.ICustomQueryParameter SurveyFields(IEnumerable<SurveyField> fields)
        {
            var table = new DataTable();
            table.Columns.Add("SurveyFieldId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Title", typeof(string));
            table.Columns.Add("FieldTypeId", typeof(byte));
            table.Columns.Add("IsRequired", typeof(bool));
            table.Columns.Add("SortOrder", typeof(short));

            foreach (var field in fields)
            {
                table.Rows.Add(field.IsNew ? (object)System.DBNull.Value : field.Id,
                    field.Name, field.Title, (byte)field.Type, field.IsRequired, field.SortOrder);
            }

            return table.AsTableValuedParameter("dbo.SurveyFieldTableType");
        }

        public static SqlMapper.ICustomQueryParameter ResponseValues(IEnumerable<EncryptedAnswer> answers)
        {
            var table = new DataTable();
            table.Columns.Add("SurveyFieldId", typeof(int));
            table.Columns.Add("ValueCipher", typeof(byte[]));

            foreach (var answer in answers)
            {
                table.Rows.Add(answer.FieldId, answer.ValueCipher);
            }

            return table.AsTableValuedParameter("dbo.ResponseValueTableType");
        }
    }
}
