using AdaptiveExpressions.Properties;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using NegativeEddy.Bots.Composer.Serialization;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NegativeEddy.Bots.Composer.Actions
{
    public class CosmosDbUpsert : Dialog
    {
        [JsonConstructor]
        public CosmosDbUpsert([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
          : base()
        {
            // enable instances of this command as debug break point
            RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = nameof(CosmosDbUpsert);

        [JsonProperty("Container")]
        public StringExpression Container { get; set; }

        [JsonProperty("Database")]
        public StringExpression Database { get; set; }

        [JsonProperty("ConnectionString")]
        public StringExpression ConnectionString { get; set; }

        [JsonProperty("Document")]
        public ValueExpression Document { get; set; }

        [JsonProperty("PartitionKey")]
        public StringExpression PartitionKey { get; set; }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            string connectionString = ConnectionString.GetValue(dc.State);
            string databaseName = Database.GetValue(dc.State);
            string containerName = Container.GetValue(dc.State);
            string partitionKey = PartitionKey.GetValue(dc.State);
            object document = Document.GetValue(dc.State);

            var results = await CosmosUpsert(connectionString, databaseName, containerName, document, partitionKey);

            if (ResultProperty != null)
            {
                dc.State.SetValue(ResultProperty.GetValue(dc.State), results);
            }

            await dc.Context.TraceActivityAsync(nameof(CosmosDbUpsert), label: "Cosmos DB Upsert result",
                value: new
                {
                    Container = containerName,
                    Database = databaseName,
                    PartitionKey = partitionKey,
                    Document = document,
                    Results = results
                });

            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }

        private async Task<object> CosmosUpsert(string connectionString, string databaseName, string containerName, object document, string partitionKey)
        {
            CosmosClient client = new CosmosClient(connectionString);
            Database database = client.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);
            var ser = new ObjectSerializer();

            using Stream stream = ser.ToStream(document);
            using ResponseMessage responseMessage = await container.UpsertItemStreamAsync(
                partitionKey: new PartitionKey(partitionKey),
                streamPayload: stream);

            // Item stream operations do not throw exceptions for better performance
            if (responseMessage.IsSuccessStatusCode)
            {
                object streamResponse = ser.FromStream(responseMessage.Content);
                return new
                {
                    Success = true,
                    Document = streamResponse
                };
            }
            else
            {
                return new
                {
                    Success = false,
                    Error = new
                    {
                        responseMessage.StatusCode,
                        Message = responseMessage.ErrorMessage
                    }
                };
            }
        }

    }
}
