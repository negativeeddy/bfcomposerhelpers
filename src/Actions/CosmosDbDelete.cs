using AdaptiveExpressions.Properties;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder.Dialogs;
using NegativeEddy.Bots.Composer.Serialization;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
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

        [JsonProperty("Collection")]
        public StringExpression Collection { get; set; }

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
            string containerName = Collection.GetValue(dc.State);
            string partitionKey = PartitionKey.GetValue(dc.State);
            string document = DocumentId.GetValue(dc.State);

            var results = await CosmosDelete(connectionString, databaseName, containerName, document, partitionKey, cancellationToken);

            if (ResultProperty != null)
            {
                dc.State.SetValue(ResultProperty.GetValue(dc.State), results);
            }

            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }

        private async Task<object> CosmosDelete(string connectionString, string databaseName, string containerName, string documentId, string partitionKey, CancellationToken cancellationToken)
        {
            CosmosClient client = new CosmosClient(connectionString);
            Database database = client.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);

            var responseMessage = await container.DeleteItemStreamAsync(documentId, new PartitionKey(partitionKey), cancellationToken: cancellationToken);


            // Item stream operations do not throw exceptions for better performance
            if (responseMessage.IsSuccessStatusCode)
            {
                var ser = new ObjectSerializer();
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
