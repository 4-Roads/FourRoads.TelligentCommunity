using System;
using System.Collections.Generic;
using Telligent.Evolution.Components.TokenizedTemplates;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Templating.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.Common.TelligentCommunity.Components.Tokenizers
{
    public class TokenizedTemplateTokenBase : ITokenizedTemplateTokenBase
    {
        private ITranslatablePluginController _translator;
        private string _nameResouce;
        private string _descriptionResource;

        public TokenizedTemplateTokenBase( Guid id , Guid? dataTypeId , string nameResouce , string descriptionResource , ITranslatablePluginController translator)
        {
            Id = id;
            DataTypeId = dataTypeId;
            _translator = translator;
            _nameResouce = nameResouce;
            _descriptionResource = descriptionResource;
        }

        public Guid? DataTypeId
        {
            get;
            private set;
        }

        public string Description
        {
            get
            {
                return _translator.GetLanguageResourceValue(_descriptionResource);
            }
        }

        public Guid Id
        {
            get;
            private set;
        }

        public string Name
        {
            get
            {
                return _translator.GetLanguageResourceValue(_nameResouce);
            }
        }
    }

    public class TokenizedTemplateFunctionBasedToken : TokenizedTemplateTokenBase 
    {
        private Func<TemplateContext, IEnumerable<TemplateContext>> _resolveFunc;

        public TokenizedTemplateFunctionBasedToken(Guid id, Guid? dataTypeId, string name, string description, ITranslatablePluginController translator, Func<TemplateContext, IEnumerable<TemplateContext>> resolveFunc, params Guid[] contextualDataTypeIds) :
            base(id, dataTypeId, name, description, translator)
        {
            _resolveFunc = resolveFunc;
            ContextualDataTypeIds = contextualDataTypeIds;
        }

        public Guid[] ContextualDataTypeIds
        {
            get;
            private set;
        }

        public IEnumerable<TemplateContext> Resolve(TemplateContext context)
        {
            return _resolveFunc(context);
        }
    }

    public class TokenizedTemplateDataTypeContainerToken : TokenizedTemplateTokenBase, ITokenizedTemplateDataTypeContainerToken
    {
        private Action<TemplateContext> _resolveFunc;

        public TokenizedTemplateDataTypeContainerToken(Guid id, Guid? dataTypeId, string name, string description, ITranslatablePluginController translator, Action<TemplateContext> resolveFunc, params Guid[] contextualDataTypeIds) :
            base(id, dataTypeId, name, description, translator)
        {
            _resolveFunc = resolveFunc;
            ContextualDataTypeIds = contextualDataTypeIds;
        }

        public Guid[] ContextualDataTypeIds
        {
            get;
            private set;
        }

        public void Resolve(TemplateContext context)
        {
            _resolveFunc(context);
        }
    }

    public class TokenizedTemplateEnumerableToken : TokenizedTemplateFunctionBasedToken, ITokenizedTemplateEnumerableToken
    {
        public TokenizedTemplateEnumerableToken(Guid id, Guid? dataTypeId, string name, string description, ITranslatablePluginController translator, Func<TemplateContext, IEnumerable<TemplateContext>> resolveFunc, params Guid[] contextualDataTypeIds) :
            base(id, dataTypeId, name, description, translator, resolveFunc, contextualDataTypeIds)
        {
        }
    }

    public class TokenizedTemplateImageBasedToken : TokenizedTemplateTokenBase
    {
        private Func<TemplateContext, string> _resolveFunc;
        private Func<string> _preview;

        public TokenizedTemplateImageBasedToken(Guid id, Guid? dataTypeId, string name, string description, ITranslatablePluginController translator, Func<TemplateContext, string> resolveFunc, Func<string> preview) :
            base(id, dataTypeId, name, description, translator)
        {
            _resolveFunc = resolveFunc;
            _preview = preview;
        }

        public string Resolve(TemplateContext context)
        {
            return _resolveFunc(context);
        }

        public string Preview()
        {
            return _preview();
        }
    }

    public class TokenizedTemplateImageUrlToken : TokenizedTemplateImageBasedToken, ITokenizedTemplateImageUrlToken
    {
        public TokenizedTemplateImageUrlToken(Guid id, Guid? dataTypeId, string name, string description, ITranslatablePluginController translator, Func<TemplateContext, string> resolveFunc, Func<string> preview) :
            base(id, dataTypeId, name, description, translator, resolveFunc, preview)
        {
        }
    }

    public class TokenizedTemplateLinkUrlToken : TokenizedTemplateImageBasedToken, ITokenizedTemplateLinkUrlToken
    {
        public TokenizedTemplateLinkUrlToken(Guid id, Guid? dataTypeId, string name, string description, ITranslatablePluginController translator, Func<TemplateContext, string> resolveFunc, Func<string> preview) :
            base(id, dataTypeId, name, description, translator, resolveFunc, preview)
        {
        }
    }

    public class TokenizedTemplateToken : TokenizedTemplateTokenBase, ITokenizedTemplateToken
    {
        private Func<TemplateContext, object> _resolveFunc;
        private Func<string> _preview;

        public TokenizedTemplateToken(Guid id, Guid? dataTypeId, string name, string description, ITranslatablePluginController translator, Func<TemplateContext, object> resolveFunc, Func<string> preview) :
            base(id, dataTypeId, name, description, translator)
        {
            _resolveFunc = resolveFunc;
            _preview = preview;
        }

        public object Resolve(TemplateContext context)
        {
            return _resolveFunc(context);
        }

        public string Preview()
        {
            return _preview();
        }
    }
}
