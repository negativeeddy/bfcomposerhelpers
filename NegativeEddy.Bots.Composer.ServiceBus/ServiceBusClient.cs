using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

[assembly: HostingStartup(typeof(NegativeEddy.Bots.Composer.ServiceBus.ServiceBusClientStartup))]

namespace NegativeEddy.Bots.Composer.ServiceBus
{
    public class ServiceBusClientService : IHostedService
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;
        private readonly string _appId;

        private readonly ILogger<ServiceBusClientService> _logger;

        private readonly string _connectionString;
        private readonly string _topicName;
        private readonly string _subscriptionName;

        private ServiceBusClient _client;
        private ServiceBusProcessor _processor;

        public ServiceBusClientService(ILogger<ServiceBusClientService> logger, IBotFrameworkHttpAdapter adapter, IConfiguration config, IBot bot)
        {
            _logger = logger;

            _adapter = adapter;
            _bot = bot;
            _appId = config["MicrosoftAppId"];

            _connectionString = config["serviceBus:connectionString"];
            _topicName = config["serviceBus:topicName"];
            _subscriptionName = config["serviceBus:subscriptionName"];
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting");

            _client = new ServiceBusClient(_connectionString);
            // create a processor that we can use to process the messages
            _processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions());

            // add handler to process messages
            _processor.ProcessMessageAsync += MessageHandler;

            // add handler to process any errors
            _processor.ProcessErrorAsync += ErrorHandler;

            // start processing 
            await _processor.StartProcessingAsync();
            _logger.LogInformation("Started");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Stopping the receiver");
                await _processor?.StopProcessingAsync();
                _processor = null;
                _logger.LogInformation("Stopped receiving messages");
            }
            finally
            {
                if (_client != null)
                {
                    _logger.LogInformation("Stopping the service bus client");
                    await _client.DisposeAsync();
                    _client = null;
                    _logger.LogInformation("Stopped the service bus client");
                }
            }
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            dynamic body;

            using (StreamReader sr = new StreamReader(args.Message.Body.ToStream()))
            using (var jtr = new Newtonsoft.Json.JsonTextReader(sr))
            {
                var jsonSerializer = new Newtonsoft.Json.JsonSerializer();
                body = jsonSerializer.Deserialize<dynamic>(jtr);
            }
            _logger.LogInformation($"Received: {body} from subscription: {_subscriptionName}");

            var conversationInfo = body.conversation;
            ChannelAccount user = new ChannelAccount { Id = conversationInfo.userId, Name = "User", Role = "user" };
            ChannelAccount bot = new ChannelAccount { Id = conversationInfo.botId, Name = "Bot", Role = "bot" };
            string conversationId = conversationInfo.conversationId;
            ConversationAccount conversation = new ConversationAccount(id: conversationId);
            string activityId = conversationInfo.activityId;
            string serviceUrl = conversationInfo.serviceUrl;
            string channelId = conversationInfo.channelId;
            var conversationReference = new ConversationReference(activityId, user, bot, conversation, channelId, serviceUrl);

            await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference, async (turnContext, token) =>
            {
                // If you encounter permission-related errors when sending this message, see
                // https://aka.ms/BotTrustServiceUrl
                turnContext.Activity.ChannelData = new { Message = body.message, source = "serviceBus" };
                await _bot.OnTurnAsync(turnContext, token);
            }, 
            default(CancellationToken));

            // complete the message. messages are deleted from the queue. 
            await args.CompleteMessageAsync(args.Message);
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Received an error");
            return Task.CompletedTask;
        }
    }
}
