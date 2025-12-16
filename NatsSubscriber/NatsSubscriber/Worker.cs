using NATS.Client;
using NATS.Client.Core;

namespace NatsSubscriber
{
    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        private readonly ILogger<Worker> _logger;


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var natsUrl = Environment.GetEnvironmentVariable("NATS_URL")
                          ?? "nats://172.22.4.106:4222";

            _logger.LogInformation("Conectando a NATS en {Url}", natsUrl);

            await using var nc = new NatsConnection(new NatsOpts
            {
                Url = natsUrl
            });

            _logger.LogInformation("Suscrito a test.saludo");

            await foreach (var msg in nc.SubscribeAsync<string>(
                               subject: "test.saludo",
                               cancellationToken: stoppingToken))
            {
                _logger.LogInformation("Recibido: {Msg}", msg.Data);
            }
        }
    }
}
