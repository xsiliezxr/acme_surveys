using System;
using System.Collections.Generic;
using System.Linq;
using AcmeSurveys.Domain.Common;

namespace AcmeSurveys.Domain.Surveys
{
    public sealed class Survey
    {
        private readonly List<SurveyField> _fields;

        private Survey(
            int id, Guid externalId, Guid publicToken, int ownerUserId,
            string name, string description, IEnumerable<SurveyField> fields,
            DateTime createdAtUtc, DateTime updatedAtUtc, byte[] rowVersion)
        {
            Id = id;
            ExternalId = externalId;
            PublicToken = publicToken;
            OwnerUserId = ownerUserId;
            Name = name;
            Description = description;
            _fields = fields.OrderBy(f => f.SortOrder).ToList();
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
            RowVersion = rowVersion;
        }

        public int Id { get; }

        public Guid ExternalId { get; }

        public Guid PublicToken { get; private set; }

        public int OwnerUserId { get; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public DateTime CreatedAtUtc { get; }

        public DateTime UpdatedAtUtc { get; private set; }

        public byte[] RowVersion { get; }

        public IReadOnlyList<SurveyField> Fields => _fields.AsReadOnly();

        public bool IsOwnedBy(int userId) => OwnerUserId == userId;

        public static Result<Survey> Create(
            int ownerUserId, Guid externalId, Guid publicToken,
            string name, string description, IReadOnlyList<FieldDefinition> fields, DateTime nowUtc)
        {
            if (ownerUserId <= 0) throw new ArgumentOutOfRangeException(nameof(ownerUserId));
            if (externalId == Guid.Empty) throw new ArgumentException("ExternalId is required.", nameof(externalId));
            if (publicToken == Guid.Empty) throw new ArgumentException("PublicToken is required.", nameof(publicToken));

            var errors = new List<Error>();
            var normalizedName = ValidateName(name, errors);
            var normalizedDescription = ValidateDescription(description, errors);
            var newFields = BuildFields(fields, existing: new SurveyField[0], hasResponses: false, errors: errors);

            if (errors.Count > 0) return Result.Failure<Survey>(errors);

            return Result.Success(new Survey(
                0, externalId, publicToken, ownerUserId,
                normalizedName, normalizedDescription, newFields,
                nowUtc, nowUtc, rowVersion: null));
        }

        public static Survey Rehydrate(
            int id, Guid externalId, Guid publicToken, int ownerUserId,
            string name, string description, IEnumerable<SurveyField> fields,
            DateTime createdAtUtc, DateTime updatedAtUtc, byte[] rowVersion)
        {
            return new Survey(id, externalId, publicToken, ownerUserId, name, description,
                fields ?? Enumerable.Empty<SurveyField>(), createdAtUtc, updatedAtUtc, rowVersion);
        }

        public Result Update(string name, string description, IReadOnlyList<FieldDefinition> fields, bool hasResponses, DateTime nowUtc)
        {
            var errors = new List<Error>();
            var normalizedName = ValidateName(name, errors);
            var normalizedDescription = ValidateDescription(description, errors);
            var newFields = BuildFields(fields, _fields, hasResponses, errors);

            if (errors.Count > 0) return Result.Failure(errors);

            Name = normalizedName;
            Description = normalizedDescription;
            _fields.Clear();
            _fields.AddRange(newFields);
            UpdatedAtUtc = nowUtc;
            return Result.Success();
        }

        public void RotatePublicToken(Guid newToken, DateTime nowUtc)
        {
            if (newToken == Guid.Empty || newToken == PublicToken)
                throw new ArgumentException("A new, different token is required.", nameof(newToken));
            PublicToken = newToken;
            UpdatedAtUtc = nowUtc;
        }

        private static string ValidateName(string name, ICollection<Error> errors)
        {
            var value = (name ?? string.Empty).Trim();
            if (value.Length < SurveyRules.NameMinLength || value.Length > SurveyRules.NameMaxLength)
            {
                errors.Add(Error.Validation("Name",
                    $"El nombre debe tener entre {SurveyRules.NameMinLength} y {SurveyRules.NameMaxLength} caracteres."));
            }
            return value;
        }

        private static string ValidateDescription(string description, ICollection<Error> errors)
        {
            var value = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            if (value != null && value.Length > SurveyRules.DescriptionMaxLength)
            {
                errors.Add(Error.Validation("Description",
                    $"La descripción no puede superar {SurveyRules.DescriptionMaxLength} caracteres."));
            }
            return value;
        }

        private static List<SurveyField> BuildFields(
            IReadOnlyList<FieldDefinition> definitions, IReadOnlyCollection<SurveyField> existing,
            bool hasResponses, ICollection<Error> errors)
        {
            var result = new List<SurveyField>();
            if (definitions == null || definitions.Count < SurveyRules.MinFields)
            {
                errors.Add(Error.Validation("Fields", "La encuesta debe tener al menos un campo."));
                return result;
            }
            if (definitions.Count > SurveyRules.MaxFields)
            {
                errors.Add(Error.Validation("Fields", $"La encuesta no puede tener más de {SurveyRules.MaxFields} campos."));
                return result;
            }

            var existingById = existing.ToDictionary(f => f.Id);
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var keptIds = new HashSet<int>();

            for (var i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                var prefix = "Fields[" + i + "].";
                var errorCountBefore = errors.Count;

                var fieldName = (def?.Name ?? string.Empty).Trim();
                var title = (def?.Title ?? string.Empty).Trim();

                if (def == null)
                {
                    errors.Add(Error.Validation("Fields[" + i + "]", "Campo inválido."));
                    continue;
                }

                if (!SurveyRules.FieldNameRegex.IsMatch(fieldName))
                {
                    errors.Add(Error.Validation(prefix + "Name",
                        "Use solo letras, números y guion bajo, empezando con letra (máx. 50)."));
                }
                else if (!seenNames.Add(fieldName))
                {
                    errors.Add(Error.Validation(prefix + "Name", "El nombre del campo está repetido."));
                }

                if (title.Length == 0 || title.Length > SurveyRules.FieldTitleMaxLength)
                {
                    errors.Add(Error.Validation(prefix + "Title",
                        $"El título es obligatorio (máx. {SurveyRules.FieldTitleMaxLength} caracteres)."));
                }

                if (!Enum.IsDefined(typeof(FieldType), def.Type))
                {
                    errors.Add(Error.Validation(prefix + "Type", "Tipo de campo no soportado."));
                }

                var id = 0;
                if (def.Id.HasValue)
                {
                    SurveyField current;

                    if (!existingById.TryGetValue(def.Id.Value, out current) || !keptIds.Add(def.Id.Value))
                    {
                        errors.Add(Error.Validation(prefix + "Name", "El campo no pertenece a esta encuesta."));
                    }
                    else
                    {
                        id = current.Id;
                        if (hasResponses && current.Type != def.Type)
                        {
                            errors.Add(Error.Validation(prefix + "Type",
                                "No se puede cambiar el tipo de un campo cuando la encuesta ya tiene respuestas."));
                        }
                    }
                }

                if (errors.Count == errorCountBefore)
                {
                    result.Add(new SurveyField(id, fieldName, title, def.Type, def.IsRequired, (short)i));
                }
            }

            if (hasResponses && existing.Any(f => !keptIds.Contains(f.Id)))
            {
                errors.Add(Error.Validation("Fields",
                    "No se pueden eliminar campos de una encuesta que ya tiene respuestas."));
            }

            return result;
        }
    }
}
