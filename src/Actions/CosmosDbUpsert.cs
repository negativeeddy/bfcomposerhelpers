using AdaptiveExpressions.Properties;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NegativeEddy.Bots.Composer.Actions
{
    public class CosmosDbUpsert : Dialog
    {
        private readonly JsonSerializer _serializer = new JsonSerializer();

        [JsonConstructor]
        public CosmosDbUpsert([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
          : base()
        {
            // enable instances of this command as debug break point
            RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = nameof(CosmosDbUpsert);

        [JsonProperty("Collection")]
        public StringExpression Collection { get; set; }

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
            string containerName = Collection.GetValue(dc.State);
            string partitionKey = PartitionKey.GetValue(dc.State);
            object document = Document.GetValue(dc.State);

            var results = await CosmosUpsert(connectionString, databaseName, containerName, document, partitionKey);

            if (ResultProperty != null)
            {
                dc.State.SetValue(ResultProperty.GetValue(dc.State), results);
            }

            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }

        private async Task<object> CosmosUpsert(string connectionString, string databaseName, string containerName, object document, string partitionKey)
        {
            CosmosClient client = new CosmosClient(connectionString);
            Database database = client.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);

            using Stream stream = ToStream(document);
            using ResponseMessage responseMessage = await container.UpsertItemStreamAsync(
                partitionKey: new PartitionKey(partitionKey),
                streamPayload: stream);

            // Item stream operations do not throw exceptions for better performance
            if (responseMessage.IsSuccessStatusCode)
            {
                object streamResponse = FromStream(responseMessage.Content);
                return new { Document = streamResponse };
            }
            else
            {
                return new
                {
                    Error = new
                    {
                        responseMessage.StatusCode,
                        Message = responseMessage.ErrorMessage
                    }
                };
            }
        }

        private object FromStream(Stream stream)
        {
            using (stream)
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
                    {
                        return _serializer.Deserialize(jsonTextReader);
                    }
                }
            }
        }

        private Stream ToStream(object input)
        {
            MemoryStream streamPayload = new MemoryStream();
            using (StreamWriter streamWriter = new StreamWriter(streamPayload, encoding: Encoding.Default, bufferSize: 1024, leaveOpen: true))
            {
                using (JsonWriter writer = new JsonTextWriter(streamWriter))
                {
                    writer.Formatting = Formatting.None;
                    _serializer.Serialize(writer, input);
                    writer.Flush();
                    streamWriter.Flush();
                }
            }

            streamPayload.Position = 0;
            return streamPayload;
        }
    }
}
