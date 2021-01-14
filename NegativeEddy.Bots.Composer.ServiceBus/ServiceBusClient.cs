using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

[assembly: HostingStartup(typeof(NegativeEddy.Bots.Composer.ServiceBus.ServiceBusClientStartup))]

namespace NegativeEddy.Bots.Composer.ServiceBus
{
    public class ServiceBusClientService : IHostedService
    {
        private readonly ILogger<ServiceBusClientService> _logger;
        private readonly string _connectionString;
        private readonly string _topicName;
        private readonly string _subscriptionName;

        private ServiceBusClient _client;
        private ServiceBusProcessor _processor;

        public ServiceBusClientService(ILogger<ServiceBusClientService> logger, IConfiguration config)
        {
            _logger = logger;
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
            string body = args.Message.Body.ToString();
            _logger.LogInformation($"Received: {body} from subscription: {_subscriptionName}");

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
