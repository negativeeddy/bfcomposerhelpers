using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace NegativeEddy.Bots.Composer.Actions
{
    public class NegativeEddyActionComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            return new DeclarativeType[] {
                new DeclarativeType<CosmosDbQuery>(CosmosDbQuery.Kind),
                new DeclarativeType<CosmosDbUpsert>(CosmosDbUpsert.Kind),
                new DeclarativeType<CosmosDbDelete>(CosmosDbDelete.Kind),
                new DeclarativeType<HelloWorld>(HelloWorld.Kind),
                new DeclarativeType<PublishEventGridEvent>(PublishEventGridEvent.Kind)
            };
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}