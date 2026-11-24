using System;
using System.Collections.Generic;
using System.Linq;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Domain.FieldValues
{
    public sealed class FieldValueHandlerRegistry
    {
        private readonly IReadOnlyDictionary<FieldType, IFieldValueHandler> _handlers;

        public FieldValueHandlerRegistry(IEnumerable<IFieldValueHandler> handlers)
        {
            if (handlers == null) throw new ArgumentNullException(nameof(handlers));

            var map = new Dictionary<FieldType, IFieldValueHandler>();
            foreach (var handler in handlers)
            {
                if (map.ContainsKey(handler.Type))
                    throw new InvalidOperationException("Duplicate handler for field type " + handler.Type + ".");
                map.Add(handler.Type, handler);
            }

            var missing = Enum.GetValues(typeof(FieldType)).Cast<FieldType>().Where(t => !map.ContainsKey(t)).ToList();
            if (missing.Count > 0)
                // Fail-fast: un FieldType sin handler se detecta al arrancar, no al llenar una encuesta.
                throw new InvalidOperationException("Missing field handlers for: " + string.Join(", ", missing));

            _handlers = map;
        }

        public static FieldValueHandlerRegistry CreateDefault() =>
            new FieldValueHandlerRegistry(new IFieldValueHandler[]
            {
                new TextFieldHandler(),
                new NumberFieldHandler(),
                new DateFieldHandler()
            });

        public IFieldValueHandler Get(FieldType type)
        {
            IFieldValueHandler handler;
            if (!_handlers.TryGetValue(type, out handler))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported field type.");
            return handler;
        }
    }
}
