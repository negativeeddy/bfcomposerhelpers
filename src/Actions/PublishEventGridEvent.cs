using AdaptiveExpressions.Properties;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NegativeEddy.Bots.Composer.Actions
{
    public class PublishEventGridEvent : Dialog
    {
        [JsonConstructor]
        public PublishEventGridEvent([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
          : base()
        {
            // enable instances of this command as debug break point
            RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = nameof(PublishEventGridEvent);

        [JsonProperty("TopicEndpoint")]
        public StringExpression TopicEndpoint { get; set; }

        [JsonProperty("TopicKey")]
        public StringExpression TopicKey { get; set; }

        [JsonProperty("EventType")]
        public StringExpression EventType { get; set; }

        [JsonProperty("EventData")]
        public ValueExpression EventData { get; set; }

        [JsonProperty("Subject")]
        public StringExpression Subject { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            string topicKey = TopicKey.GetValue(dc.State);
            string endpoint = TopicEndpoint.GetValue(dc.State);
            string topicHostname = new Uri(endpoint).DnsSafeHost;
            string eventType = EventType.GetValue(dc.State);
            object eventData = EventData.GetValue(dc.State);
            string eventSubject = Subject.GetValue(dc.State);

            TopicCredentials topicCredentials = new TopicCredentials(topicKey);
            EventGridClient client = new EventGridClient(topicCredentials);

            var events = new List<EventGridEvent>()
            {
                new EventGridEvent()
                {
                    Id = Guid.NewGuid().ToString(),
                    EventType = eventType,
                    Data = eventData,
                    EventTime = DateTime.Now,
                    Subject = eventSubject,
                    DataVersion = "2.0"
                }
            };

            await client.PublishEventsAsync(topicHostname, events, cancellationToken);

            await dc.Context.TraceActivityAsync(nameof(PublishEventGridEvent), label: "Event Grid events published",
                value: new
                {
                    TopicHostname = topicHostname,
                    Endpoint = endpoint,
                    Events = events,
                });

            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
