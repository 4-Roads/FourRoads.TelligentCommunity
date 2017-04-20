using System;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Templating.Version1;

namespace FourRoads.TelligentCommunity.Rules.Tokens
{
    public class CustomTriggerParametersToken : ITokenizedTemplatePrimitiveToken
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public Guid Id { get; private set; }
        public Guid? DataTypeId { get; private set; }
        public PrimitiveType ValueType { get; private set; }
        private readonly Func<TemplateContext, object> _resolve;

        public object Resolve(TemplateContext context)
        {
            return _resolve(context);
        }

        private readonly string _preview;

        public string Preview()
        {
            return _preview;
        }

        public CustomTriggerParametersToken(string name, string description,
            Guid id, Guid? dataTypeId, PrimitiveType valueType,
            Func<TemplateContext, object> resolveFunction, string preview = null)
        {
            Name = name;
            Description = description;
            Id = id;
            DataTypeId = dataTypeId;
            ValueType = valueType;
            _resolve = resolveFunction;

            _preview = preview ?? Name;
        }
    }
}