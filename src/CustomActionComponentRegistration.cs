using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Negativeeddy.Bots.Composer.Actions
{
    public class CustomActionComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            return new DeclarativeType[] {
                new DeclarativeType<CosmosDbQuery>(CosmosDbQuery.Kind),
                new DeclarativeType<CosmosDbUpsert>(CosmosDbUpsert.Kind),
                new DeclarativeType<PublishEventGridEvent>(PublishEventGridEvent.Kind)
            };
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}