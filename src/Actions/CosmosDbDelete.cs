using AdaptiveExpressions.Properties;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NegativeEddy.Bots.Composer.Actions
{
    public class CosmosDbDelete : Dialog
    {
        private readonly JsonSerializer _serializer = new JsonSerializer();

        [JsonConstructor]
        public CosmosDbDelete([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
          : base()
        {
            // enable instances of this command as debug break point
            RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = nameof(CosmosDbDelete);

        [JsonProperty("Container")]
        public StringExpression Container { get; set; }

        [JsonProperty("Database")]
        public StringExpression Database { get; set; }

        [JsonProperty("ConnectionString")]
        public StringExpression ConnectionString { get; set; }

        [JsonProperty("DocumentId")]
        public StringExpression DocumentId { get; set; }

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
            string document = DocumentId.GetValue(dc.State);

            var results = await CosmosDelete(connectionString, databaseName, containerName, document, partitionKey, cancellationToken);

            if (ResultProperty != null)
            {
                dc.State.SetValue(ResultProperty.GetValue(dc.State), results);
            }

            await dc.Context.TraceActivityAsync(nameof(CosmosDbDelete), label: "Cosmos DB Delete result",
                value: new
                {
                    Container = containerName,
                    Database = databaseName,
                    PartitionKey = partitionKey,
                    DocumentId = document,
                    Results = results
                });

            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }

        private static async Task<dynamic> CosmosDelete(string connectionString, string databaseName, string containerName, string documentId, string partitionKey, CancellationToken cancellationToken)
        {
            CosmosClient client = new CosmosClient(connectionString);
            Database database = client.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);

            var responseMessage = await container.DeleteItemStreamAsync(documentId, new PartitionKey(partitionKey), cancellationToken: cancellationToken);


            // Item stream operations do not throw exceptions for better performance
            if (responseMessage.IsSuccessStatusCode)
            {
                return new
                {
                    Success = true,
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
