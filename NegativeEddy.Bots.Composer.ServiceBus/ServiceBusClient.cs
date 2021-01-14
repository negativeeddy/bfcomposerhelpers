using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Hosting;
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

        public ServiceBusClientService(ILogger<ServiceBusClientService> logger)
        {
            _logger = logger;
        }

        ServiceBusClient client;
        ServiceBusProcessor processor;

        public string connectionString { get; set; }
        public string topicName { get; set; }
        public string subscriptionName { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            connectionString = "";
            topicName = "bottopic";
            subscriptionName = "S1";

            _logger.LogInformation("Starting");

            client = new ServiceBusClient(connectionString);
            // create a processor that we can use to process the messages
            processor = client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions());

            // add handler to process messages
            processor.ProcessMessageAsync += MessageHandler;

            // add handler to process any errors
            processor.ProcessErrorAsync += ErrorHandler;

            // start processing 
            await processor.StartProcessingAsync();
            _logger.LogInformation("Started");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Stopping the receiver");
                await processor.StopProcessingAsync();
                _logger.LogInformation("Stopped receiving messages");
            }
            finally
            {
                _logger.LogInformation("Stopping the service bus client");
                await client.DisposeAsync();
                _logger.LogInformation("Stopped the service bus client");
            }
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            _logger.LogInformation($"Received: {body} from subscription: {subscriptionName}");

            // complete the message. messages is deleted from the queue. 
            await args.CompleteMessageAsync(args.Message);
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Received an error");
            return Task.CompletedTask;
        }
    }
}
