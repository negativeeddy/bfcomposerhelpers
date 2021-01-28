using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(NegativeEddy.Bots.Composer.ServiceBus.ServiceBusClientStartup))]

namespace NegativeEddy.Bots.Composer.ServiceBus
{
    public class ServiceBusClientStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddHostedService<ServiceBusClientService>();
            });
        }
    }
}
