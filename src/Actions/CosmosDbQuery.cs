using AdaptiveExpressions.Properties;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NegativeEddy.Bots.Composer.Actions
{
    public class CosmosDbQuery : Dialog
    {
        [JsonConstructor]
        public CosmosDbQuery([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
          : base()
        {
            // enable instances of this command as debug break point
            RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = nameof(CosmosDbQuery);

        [JsonProperty("Container")]
        public StringExpression Container { get; set; }

        [JsonProperty("Database")]
        public StringExpression Database { get; set; }

        [JsonProperty("ConnectionString")]
        public StringExpression ConnectionString { get; set; }

        [JsonProperty("Query")]
        public StringExpression Query { get; set; }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var connectionString = ConnectionString.GetValue(dc.State);
            var databaseName = Database.GetValue(dc.State);
            var containerName = Container.GetValue(dc.State);
            var queryText = Query.GetValue(dc.State);

            dynamic[] finalResults = null;

            if (ResultProperty != null)
            {
                var queryResult = await CosmosQuery(connectionString, databaseName, containerName, queryText);
                finalResults = queryResult.ToArray();

                dc.State.SetValue(ResultProperty.GetValue(dc.State), finalResults);
            }

            await dc.Context.TraceActivityAsync(nameof(CosmosDbQuery), label: "Cosmos DB Query result",
                value: new
                {
                    Query = queryText,
                    Container = containerName,
                    Database = databaseName,
                    Results = finalResults
                });

            return await dc.EndDialogAsync(result: finalResults, cancellationToken: cancellationToken);
        }

        private static async Task<IEnumerable<dynamic>> CosmosQuery(string connectionString, string databaseName, string containerName, string queryText)
        {
            CosmosClient client = new CosmosClient(connectionString);
            Database database = client.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);

            List<dynamic> documents = new List<dynamic>();
            using (FeedIterator setIterator = container.GetItemQueryStreamIterator(queryText))
            {
                while (setIterator.HasMoreResults)
                {
                    using (ResponseMessage response = await setIterator.ReadNextAsync())
                    {
                        response.EnsureSuccessStatusCode();
                        using (StreamReader sr = new StreamReader(response.Content))
                        using (JsonTextReader jtr = new JsonTextReader(sr))
                        {
                            JsonSerializer jsonSerializer = new JsonSerializer();
                            dynamic array = jsonSerializer.Deserialize<dynamic>(jtr);
                            documents.AddRange(array.Documents);
                        }
                    }
                }
            }
            return documents;
        }
    }
}
